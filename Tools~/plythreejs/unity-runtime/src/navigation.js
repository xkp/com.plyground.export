import { Vector3 } from './math.js';
import * as THREE from 'three';

let recastPromise;
async function recast() {
  recastPromise ??= import('recast-navigation').then(async module => {
    await module.init();
    return module;
  });
  return recastPromise;
}

function tileCacheMeshProcess(navigation, links) {
  return new navigation.TileCacheMeshProcess((params, areas, flags) => {
    for (let i = 0; i < params.polyCount(); i++) {
      areas.set(i, 0);
      flags.set(i, 1);
    }
    const min = params.boundsMin(), max = params.boundsMax();
    params.setOffMeshConnections(links.filter(link => {
      const point = link.startPosition;
      return point.x >= min[0] && point.x <= max[0]
        && point.z >= min[2] && point.z <= max[2];
    }));
  });
}

/** Bake once during asset preprocessing; store the returned bytes with the scene. */
export async function bakeNavMesh(meshes, config = {}) {
  const [navigation, three] = await Promise.all([recast(), import('@recast-navigation/three')]);
  const { tiled = false, tileCache = false, offMeshConnections = [], ...settings } = config;
  const result = tileCache ? three.threeToTileCache(meshes,
    offMeshConnections.length ? { ...settings,
      tileCacheMeshProcess: tileCacheMeshProcess(navigation, offMeshConnections) } : settings)
    : tiled ? three.threeToTiledNavMesh(meshes, { tileSize: 64, offMeshConnections, ...settings })
    : three.threeToSoloNavMesh(meshes, { offMeshConnections, ...settings });
  if (!result.success) throw new Error('Recast could not bake the navigation mesh');
  if (tileCache && offMeshConnections.length) {
    let count = 0;
    for (let i = 0; i < result.navMesh.getMaxTiles(); i++)
      count += result.navMesh.getTile(i).header()?.offMeshConCount() ?? 0;
    if (count !== offMeshConnections.length) {
      result.tileCache.destroy(); result.navMesh.destroy();
      throw new Error(`Tile-cache NavMesh baked ${count} of ${offMeshConnections.length} off-mesh links`);
    }
  }
  try { return tileCache ? navigation.exportTileCache(result.navMesh, result.tileCache)
    : navigation.exportNavMesh(result.navMesh); }
  finally { result.tileCache?.destroy(); result.navMesh.destroy(); }
}

/** Convert Unity's exported walkable triangulation to the existing Recast asset. */
export async function bakeUnityTriangulation(record,
  { tileCache = false, tiled = false, cellSize, offMeshConnections = [] } = {}) {
  const vertices = record?.vertices;
  const indices = record?.indices;
  const areas = record?.areas;
  if (record?.format !== 'unity-navmesh-triangulation/v1'
    || !Array.isArray(vertices) || vertices.length < 9 || vertices.length % 3
    || !vertices.every(Number.isFinite)
    || !Array.isArray(indices) || indices.length < 3 || indices.length % 3
    || !indices.every(index => Number.isInteger(index)
      && index >= 0 && index < vertices.length / 3)
    || !Array.isArray(areas) || areas.length !== indices.length / 3
    || !areas.every(area => Number.isInteger(area) && area >= 0 && area < 32))
    throw new Error('Invalid Unity NavMesh triangulation');
  if (areas.some(area => area !== 0))
    throw new Error('Unity NavMesh areas beyond Default need a browser area mapping');
  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
  geometry.setIndex(indices);
  const surface = new THREE.Mesh(geometry);
  surface.updateMatrixWorld(true);
  try { return await bakeNavMesh([surface], { walkableRadius: 0, tileCache, tiled,
    ...(cellSize == null ? {} : { cellSize }), offMeshConnections }); }
  finally { geometry.dispose(); }
}

/** A runtime system: one navigation mesh per UnityRuntime. */
export class NavigationSystem {
  constructor(navMesh, query, boundaryEdges = [], crowd = null, tileCache = null, navigationModule = null) {
    this.navMesh = navMesh;
    this.query = query;
    this.boundaryEdges = boundaryEdges;
    this.crowd = crowd;
    this.tileCache = tileCache;
    this.navigationModule = navigationModule;
    this.crowdAgents = new Map();
    this.obstacles = new Map();
  }
  static async fromBytes(bytes, { tileCache = false, offMeshConnections = [] } = {}) {
    const navigation = await recast();
    const imported = tileCache
      ? navigation.importTileCache(bytes, offMeshConnections.length
        ? tileCacheMeshProcess(navigation, offMeshConnections)
        : (await import('@recast-navigation/generators'))
          .createDefaultTileCacheMeshProcess()) : navigation.importNavMesh(bytes);
    const { navMesh } = imported;
    const [positions, indices] = navigation.getNavMeshPositionsAndIndices(navMesh);
    return new NavigationSystem(navMesh, new navigation.NavMeshQuery(navMesh),
      boundaryEdges(positions, indices), new navigation.Crowd(navMesh,
        { maxAgents: 128, maxAgentRadius: 2 }), imported.tileCache ?? null, navigation);
  }
  static async fromMeshes(meshes, config) {
    return this.fromBytes(await bakeNavMesh(meshes, config));
  }
  attach(runtime) {
    if (runtime.navigation && runtime.navigation !== this) throw new Error('Runtime already has a navigation system');
    runtime.navigation = this;
    this.runtime = runtime;
    this._syncObstacles(0);
  }
  detach(runtime) {
    if (runtime.navigation === this) runtime.navigation = null;
    this.runtime = null;
  }
  dispose() {
    if (this.runtime) this.runtime.removeSystem(this);
    this.crowd.destroy();
    this.query.destroy();
    this.tileCache?.destroy();
    this.navMesh.destroy();
  }
  samplePosition(position, maxDistance) {
    if (maxDistance < 0) return null;
    const result = this.query.findClosestPoint(position, {
      halfExtents: { x: maxDistance, y: maxDistance, z: maxDistance },
    });
    if (!result.success || new Vector3(result.point.x, result.point.y, result.point.z).distanceTo(position) > maxDistance)
      return null;
    return { position: new Vector3(result.point.x, result.point.y, result.point.z), polyRef: result.polyRef };
  }
  calculatePath(start, end) {
    this._links();
    const result = this.query.computePath(start, end);
    return result.success ? result.path.map(point => new Vector3(point.x, point.y, point.z)) : null;
  }
  raycast(start, end) {
    const origin = this.samplePosition(start, 1);
    if (!origin) return null;
    const result = this.query.raycast(origin.polyRef, origin.position, end);
    if (!result.success || result.t >= 1) return null;
    const distance = new Vector3().subVectors(end, origin.position).length() * result.t;
    return {
      position: origin.position.clone().lerp(end, result.t),
      normal: new Vector3(result.hitNormal?.x ?? 0, result.hitNormal?.y ?? 0, result.hitNormal?.z ?? 0),
      distance,
    };
  }
  findClosestEdge(position) {
    const source = this.samplePosition(position, 2);
    if (!source) return null;
    let nearest = null;
    for (const edge of this.boundaryEdges) {
      const direction = edge.end.clone().sub(edge.start);
      const t = Math.max(0, Math.min(1, source.position.clone().sub(edge.start).dot(direction) / direction.lengthSq()));
      const point = edge.start.clone().addScaledVector(direction, t);
      const distance = point.distanceTo(source.position);
      if (!nearest || distance < nearest.distance) nearest = {
        position: point,
        normal: edge.normal,
        distance,
      };
    }
    return nearest;
  }
  _links() {
    const links = [];
    for (const object of this.runtime?._objects ?? []) {
      for (const component of object._components) {
        if (!component._isOffMeshLink) continue;
        if (component.activated !== component.bakedActivated
          || component.enabled !== component.bakedEnabled
          || object.activeInHierarchy !== component.bakedActive)
          throw new Error(`Changing OffMeshLink activation needs a NavMesh rebake: ${object.name}`);
        if (component.startTransform?.position.distanceTo(component.startPosition) > 0.1
          || component.endTransform?.position.distanceTo(component.endPosition) > 0.1)
          throw new Error(`Moving OffMeshLink endpoints needs a NavMesh rebake: ${object.name}`);
        if (!component.enabled || !object.activeInHierarchy || !component.activated) continue;
        links.push({ link: component,
          start: component.startTransform?.position ?? component.startPosition,
          end: component.endTransform?.position ?? component.endPosition });
      }
    }
    return links;
  }
  linkForSegment(start, end) {
    for (const link of this._links()) {
      const tolerance = Math.max(link.link.radius, 0.5) * 2;
      if (start.distanceTo(link.start) <= tolerance && end.distanceTo(link.end) <= tolerance)
        return link;
      if (link.link.biDirectional && start.distanceTo(link.end) <= tolerance
        && end.distanceTo(link.start) <= tolerance)
        return { link: link.link, start: link.end, end: link.start };
    }
    return null;
  }
  _syncObstacles(deltaTime) {
    const active = new Set();
    let changed = false;
    for (const object of this.runtime?._objects ?? []) {
      if (!object.activeInHierarchy) continue;
      for (const component of object._components) {
        if (!component._isNavMeshObstacle || !component.enabled) continue;
        if (!component.carving)
          throw new Error(`NavMeshObstacle without carving needs crowd avoidance: ${object.name}`);
        if (!this.tileCache)
          throw new Error(`NavMeshObstacle needs a tile-cache NavMesh: ${object.name}`);
        if (component.shape !== 1)
          throw new Error(`NavMeshObstacle shape beyond Box is unsupported: ${object.name}`);
        active.add(component);
        const position = component.transform.TransformPoint(component.center);
        const scale = component.transform.lossyScale;
        const halfExtents = new Vector3(Math.abs(component.size.x * scale.x) / 2,
          Math.abs(component.size.y * scale.y) / 2,
          Math.abs(component.size.z * scale.z) / 2);
        const rotation = component.transform.rotation;
        const angle = Math.atan2(2 * (rotation.w * rotation.y + rotation.x * rotation.z),
          1 - 2 * (rotation.y ** 2 + rotation.z ** 2));
        const key = [...position, ...halfExtents, angle].map(value => value.toFixed(3)).join(',');
        let record = this.obstacles.get(component);
        if (!record) record = { position, key: null, obstacle: null, stationaryFor: 0 };
        const moved = position.distanceTo(record.position);
        component.velocity = deltaTime > 0
          ? position.clone().sub(record.position).multiplyScalar(1 / deltaTime) : new Vector3();
        record.stationaryFor = moved > component.carvingMoveThreshold
          ? 0 : record.stationaryFor + deltaTime;
        record.position = position;
        const shouldCarve = !component.carveOnlyStationary
          || record.stationaryFor >= component.carvingTimeToStationary;
        const wanted = shouldCarve ? key : null;
        if (record.key !== wanted) {
          if (record.obstacle) {
            this.tileCache.removeObstacle(record.obstacle);
            record.obstacle = null;
          }
          if (wanted) {
            const added = this.tileCache.addBoxObstacle(position, halfExtents, angle);
            if (!added.success) throw new Error(`Could not carve NavMeshObstacle: ${object.name}`);
            record.obstacle = added.obstacle;
          }
          record.key = wanted;
          changed = true;
        }
        this.obstacles.set(component, record);
      }
    }
    for (const [component, record] of this.obstacles) if (!active.has(component)) {
      if (record.obstacle) { this.tileCache.removeObstacle(record.obstacle); changed = true; }
      this.obstacles.delete(component);
    }
    if (!changed) return;
    for (let i = 0; i < 8; i++) {
      const update = this.tileCache.update(this.navMesh);
      if (!update.success) throw new Error('Could not update obstacle NavMesh tiles');
      if (update.upToDate) break;
      if (i === 7) throw new Error('NavMeshObstacle tile updates did not finish');
    }
    const [positions, indices] = this.navigationModule.getNavMeshPositionsAndIndices(this.navMesh);
    this.boundaryEdges = boundaryEdges(positions, indices);
    for (const object of this.runtime?._objects ?? [])
      for (const component of object._components)
        if (component.autoRepath && component.hasPath)
          component.SetDestination(component.destination);
  }
  afterFixedUpdate(deltaTime) {
    this._syncObstacles(deltaTime);
    const hasLinks = this._links().length > 0;
    const activeCrowdAgents = new Set();
    for (const object of this.runtime?._objects ?? []) {
      if (!object.activeInHierarchy) continue;
      for (const component of object._components) {
        if (component.useCrowd && component._advanceCrowd
          && !(hasLinks && component.autoTraverseOffMeshLink === false)) {
          activeCrowdAgents.add(component);
          let record = this.crowdAgents.get(component);
          if (!record) {
            const agent = this.crowd.addAgent(component.nextPosition,
              { radius: component.radius, height: component.height,
                maxAcceleration: component.acceleration, maxSpeed: component.speed });
            record = { agent, targetVersion: -1, stopped: null,
              stalledTime: 0, recoveryTime: 0 };
            this.crowdAgents.set(component, record);
          }
          record.agent.updateParameters({ radius: component.radius, height: component.height,
            maxAcceleration: component.acceleration, maxSpeed: component.speed });
          if (record.targetVersion !== component._targetVersion || record.stopped !== component.isStopped) {
            if (component.hasPath && !component.isStopped) record.agent.requestMoveTarget(component.destination);
            else record.agent.resetMoveTarget();
            record.targetVersion = component._targetVersion;
            record.stopped = component.isStopped;
          }
          // Recast avoidance can hold an agent against a NavMesh boundary even
          // while its corridor and desired velocity remain valid. Let a stalled
          // agent follow that corridor briefly, then restore normal avoidance.
          const shouldMove = component.enabled && !component.isStopped && component.hasPath
            && component.remainingDistance > Math.max(1, component.stoppingDistance)
            && component.desiredVelocity.length() > 0.5;
          if (!shouldMove) {
            record.stalledTime = 0;
            record.recoveryTime = 0;
          } else if (record.recoveryTime > 0) {
            record.recoveryTime = Math.max(0, record.recoveryTime - deltaTime);
            if (component.velocity.length() > 0.75) record.stalledTime = 0;
          } else {
            record.stalledTime = component.velocity.length() < 0.2
              ? record.stalledTime + deltaTime : 0;
            if (record.stalledTime >= 1) {
              record.recoveryTime = 0.75;
              record.stalledTime = 0;
            }
          }
          const updateFlags = record.recoveryTime > 0 ? 1 : 7;
          if (record.updateFlags !== updateFlags) {
            record.agent.updateParameters({ updateFlags });
            record.updateFlags = updateFlags;
          }
        } else component._advanceNavigation?.(this, deltaTime);
      }
    }
    for (const [component, record] of this.crowdAgents) if (!activeCrowdAgents.has(component)
      || component._destroyed || !component.enabled) {
      this.crowd.removeAgent(record.agent);
      this.crowdAgents.delete(component);
    }
    if (this.crowdAgents.size) {
      this.crowd.update(deltaTime);
      for (const [component, record] of this.crowdAgents)
        component._advanceCrowd(record.agent, deltaTime);
    }
  }
}

function boundaryEdges(positions, indices) {
  const vertex = index => new Vector3(...positions.slice(index * 3, index * 3 + 3));
  const key = point => [point.x, point.y, point.z].map(value => Math.round(value * 10000)).join(',');
  const edges = new Map();
  for (let i = 0; i < indices.length; i += 3) {
    const triangle = [vertex(indices[i]), vertex(indices[i + 1]), vertex(indices[i + 2])];
    for (let j = 0; j < 3; j++) {
      const start = triangle[j];
      const end = triangle[(j + 1) % 3];
      const opposite = triangle[(j + 2) % 3];
      const id = [key(start), key(end)].sort().join('|');
      const existing = edges.get(id);
      if (existing) { existing.count++; continue; }
      const tangent = end.clone().sub(start);
      const inward = opposite.clone().sub(start);
      const normal = new Vector3(-tangent.z, 0, tangent.x).normalize();
      if (normal.dot(inward) > 0) normal.negate();
      edges.set(id, { start, end, normal, count: 1 });
    }
  }
  return [...edges.values()].filter(edge => edge.count === 1);
}
