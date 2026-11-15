import { Vector3 } from './math.js';

let recastPromise;
async function recast() {
  recastPromise ??= import('recast-navigation').then(async module => {
    await module.init();
    return module;
  });
  return recastPromise;
}

/** Bake once during asset preprocessing; store the returned bytes with the scene. */
export async function bakeNavMesh(meshes, config = {}) {
  const [navigation, three] = await Promise.all([recast(), import('@recast-navigation/three')]);
  const { tiled = false, ...settings } = config;
  const result = tiled ? three.threeToTiledNavMesh(meshes, { tileSize: 64, ...settings })
    : three.threeToSoloNavMesh(meshes, settings);
  if (!result.success) throw new Error('Recast could not bake the navigation mesh');
  try { return navigation.exportNavMesh(result.navMesh); }
  finally { result.navMesh.destroy(); }
}

/** A runtime system: one navigation mesh per UnityRuntime. */
export class NavigationSystem {
  constructor(navMesh, query, boundaryEdges = [], crowd = null) {
    this.navMesh = navMesh;
    this.query = query;
    this.boundaryEdges = boundaryEdges;
    this.crowd = crowd;
    this.crowdAgents = new Map();
  }
  static async fromBytes(bytes) {
    const navigation = await recast();
    const { navMesh } = navigation.importNavMesh(bytes);
    const [positions, indices] = navigation.getNavMeshPositionsAndIndices(navMesh);
    return new NavigationSystem(navMesh, new navigation.NavMeshQuery(navMesh),
      boundaryEdges(positions, indices), new navigation.Crowd(navMesh,
        { maxAgents: 128, maxAgentRadius: 2 }));
  }
  static async fromMeshes(meshes, config) {
    return this.fromBytes(await bakeNavMesh(meshes, config));
  }
  attach(runtime) {
    if (runtime.navigation && runtime.navigation !== this) throw new Error('Runtime already has a navigation system');
    runtime.navigation = this;
    this.runtime = runtime;
  }
  detach(runtime) {
    if (runtime.navigation === this) runtime.navigation = null;
    this.runtime = null;
  }
  dispose() {
    if (this.runtime) this.runtime.removeSystem(this);
    this.crowd.destroy();
    this.query.destroy();
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
  afterFixedUpdate(deltaTime) {
    const activeCrowdAgents = new Set();
    for (const object of this.runtime?._objects ?? []) {
      if (!object.activeInHierarchy) continue;
      for (const component of object._components) {
        if (component.useCrowd && component._advanceCrowd) {
          activeCrowdAgents.add(component);
          let record = this.crowdAgents.get(component);
          if (!record) {
            const agent = this.crowd.addAgent(component.transform.position,
              { radius: component.radius, height: component.height,
                maxAcceleration: component.acceleration, maxSpeed: component.speed });
            record = { agent, targetVersion: -1, stopped: null };
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
