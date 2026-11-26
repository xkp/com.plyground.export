import { AnimationCurve, Animator, Camera, Input, MonoBehaviour, Object,
  Quaternion, THREE, Time, Vector3, reportUnsupportedUnityAPI } from './index.js';

// Browser path for the SplineArchitect traffic example. Mesh deformation,
// spline editing, and its Unity.Jobs workers remain native-only features.
const point = value => value instanceof Vector3 ? value.clone()
  : new Vector3(...(value?.$vector3 ?? [value?.x ?? 0, value?.y ?? 0, value?.z ?? 0]));
const bezier = (a, b, c, d, t) => {
  const u = 1 - t;
  return a.clone().multiplyScalar(u * u * u)
    .addScaledVector(b, 3 * u * u * t)
    .addScaledVector(c, 3 * u * t * t)
    .addScaledVector(d, t * t * t);
};

export class PreviewArchitectSpline extends MonoBehaviour {
  static __unityFullName = 'SplineArchitect.Spline';
  length = 0;
  segments = [];
  get Length() { return Number(this.length) || 0; }
  _sample(distance) {
    const segments = this.segments ?? [];
    if (segments.length < 2) return null;
    const z = Math.max(0, Math.min(this.Length, Number(distance) || 0));
    let index = segments.length - 2;
    for (let i = 0; i < segments.length - 1; i++) {
      if (z < Number(segments[i + 1].zPosition) || i === segments.length - 2) {
        index = i; break;
      }
    }
    const first = segments[index], second = segments[index + 1];
    const span = Math.max(0.001, Number(first.length)
      || Number(second.zPosition) - Number(first.zPosition));
    const t = Math.max(0, Math.min(1, (z - Number(first.zPosition || 0)) / span));
    const a = point(first.anchor), b = point(first.tangentA);
    const c = point(second.tangentB), d = point(second.anchor);
    const local = bezier(a, b, c, d, t);
    const ahead = bezier(a, b, c, d, Math.min(1, t + 0.002));
    const behind = bezier(a, b, c, d, Math.max(0, t - 0.002));
    const world = this.transform.TransformPoint(local);
    const tangent = this.transform.TransformDirection(ahead.sub(behind)).normalize();
    return { position: world, tangent };
  }
  EvaluateTrafficPosition(position) {
    const sample = this._sample(position?.z ?? 0);
    if (!sample) return null;
    const right = new Vector3(sample.tangent.z, 0, -sample.tangent.x).normalize();
    sample.position.addScaledVector(right, position?.x ?? 0);
    sample.position.y += position?.y ?? 0;
    return sample;
  }
}

export class PreviewArchitectSplineObject extends MonoBehaviour {
  static __unityFullName = 'SplineArchitect.SplineObject';
  localSplinePosition = new Vector3();
  splineParent = null;
  get SplineParent() { return this.splineParent; }
  get LockPosition() { return this.lockPosition ?? 0; }
  set LockPosition(value) { this.lockPosition = value; }
  Start() {
    if (!this.gameObject._previewSplineOriginMesh || !this.splineParent) return;
    const scale = this.transform.localScale;
    const base = new Vector3();
    let parent = this;
    for (let depth = 0; parent && depth < 25; depth++) {
      base.add(parent.localSplinePosition ?? Vector3.zero);
      parent = parent.soParent;
    }
    this.gameObject.object3D.traverse(visual => {
      if (!visual.isMesh || !visual.geometry?.getAttribute('position')) return;
      visual.geometry = visual.geometry.clone();
      const geometry = visual.geometry;
      const vertices = geometry.getAttribute('position');
      for (let index = 0; index < vertices.count; index++) {
        const pathPosition = new Vector3(
          base.x + vertices.getX(index) * scale.x,
          base.y + vertices.getY(index) * scale.y,
          base.z + vertices.getZ(index) * scale.z);
        const sample = this.splineParent.EvaluateTrafficPosition(pathPosition);
        if (!sample) continue;
        const local = this.transform.InverseTransformPoint(sample.position);
        vertices.setXYZ(index, local.x, local.y, local.z);
      }
      vertices.needsUpdate = true;
      geometry.computeVertexNormals();
      geometry.computeBoundingBox();
      geometry.computeBoundingSphere();
    });
  }
  LateUpdate() {
    const animator = this.gameObject.GetComponent(Animator);
    const values = new Map();
    for (const layer of animator?._graphOutputs ?? []) {
      for (const entry of layer.clips ?? []) {
        const clip = animator.clips.get(entry.name);
        if (!clip?.componentCurves?.length) continue;
        const duration = clip.duration || 1;
        const time = ((entry.time % duration) + duration) % duration;
        for (const record of clip.componentCurves) {
          if (record.componentType !== 'SplineArchitect.SplineObject'
            || !/^localSplinePosition\.[xyz]$/.test(record.property)) continue;
          if (!record._previewCurve) {
            record._previewCurve = new AnimationCurve(record.keys);
            record._previewCurve.preWrapMode = record.preWrapMode;
            record._previewCurve.postWrapMode = record.postWrapMode;
          }
          const axis = record.property.at(-1);
          const previous = values.get(axis) ?? { sum: 0, weight: 0 };
          previous.sum += record._previewCurve.Evaluate(time) * entry.weight;
          previous.weight += entry.weight;
          values.set(axis, previous);
        }
      }
    }
    for (const [axis, value] of values) {
      this.localSplinePosition[axis] = value.sum / value.weight;
      this._previewTrafficFollower = true;
    }
    if (!this._previewTrafficFollower || !this.splineParent) return;
    const sample = this.splineParent.EvaluateTrafficPosition(this.localSplinePosition);
    if (!sample) return;
    this.transform.position = sample.position;
    this.transform.rotation = new Quaternion()
      .setFromUnitVectors(Vector3.forward, sample.tangent);
  }
}

export class PreviewArchitectTrafficLight extends MonoBehaviour {
  static __unityFullName = 'SplineArchitect.Examples.TrafficLight';
  state = 0;
  greenLightTime = 5;
  redLightTime = 5;
  orangeLightTime = 2;
  _timer = 0;
  Start() {
    this._lane = this.gameObject.GetComponent(PreviewArchitectSplineObject);
    if (this._lane && this.state === 0) this._lane.localSplinePosition.x = 0.1;
  }
  Update() {
    this._timer += Time.deltaTime;
    const durations = [this.greenLightTime, this.redLightTime, this.orangeLightTime];
    if (this._timer < durations[this.state] || !this._lane) return;
    this._timer = 0;
    this.state = this.state === 0 ? 2 : this.state === 2 ? 1 : 0;
    this._lane.localSplinePosition.x = this.state === 0 ? 0.1 : 0;
  }
}

export class PreviewArchitectVehicle extends MonoBehaviour {
  static __unityFullName = 'SplineArchitect.Examples.Vehicle';
  wheelRadius = 0.5;
  wheels = [];
  Start() { this._follower = this.gameObject.GetComponent(PreviewArchitectSplineObject); }
  Update() {
    const z = this._follower?.localSplinePosition?.z;
    if (z == null) return;
    if (this._lastZ != null && z >= this._lastZ && this.wheelRadius > 0)
      for (const wheel of this.wheels ?? []) wheel?.Rotate?.(Vector3.right,
        (z - this._lastZ) / (2 * Math.PI * this.wheelRadius) * 360, 1);
    this._lastZ = z;
  }
}

export class PreviewArchitectLookAt extends MonoBehaviour {
  static __unityFullName = 'SplineArchitect.Examples.LookAt';
  lookAtPoint = null;
  LateUpdate() {
    if (this.lookAtPoint) this.transform.LookAt(this.lookAtPoint);
  }
}

export class PreviewArchitectTrafficHandler extends MonoBehaviour {
  static __unityFullName = 'SplineArchitect.Examples.TrafficHandler';
  totalVechicles = 0;
  splines = [];
  prefabs = [];
  Awake() {
    this._traffic = [];
    this._lights = Object.FindObjectsByType(PreviewArchitectTrafficLight, false, 0);
    const splines = (this.splines ?? []).filter(item => item instanceof PreviewArchitectSpline);
    const prefabs = (this.prefabs ?? []).filter(item => item?.transform);
    if (!splines.length || !prefabs.length) return;
    for (let index = 0; index < Math.min(200, this.totalVechicles); index++) {
      const prefab = prefabs[Math.floor(Math.random() * prefabs.length)];
      const spline = splines[Math.floor(Math.random() * splines.length)];
      const car = Object.Instantiate(prefab);
      const follower = car.GetComponent(PreviewArchitectSplineObject)
        ?? car.AddComponent(PreviewArchitectSplineObject);
      follower.splineParent = spline;
      follower.localSplinePosition = new Vector3(0, this.heightOffset ?? 0,
        Math.random() * spline.Length);
      follower._previewTrafficFollower = true;
      const speed = (this.minSpeed ?? 0)
        + Math.random() * ((this.maxSpeed ?? 0) - (this.minSpeed ?? 0));
      this._traffic.push({ follower, speed,
        spacing: (this.minDistanceBetweenVechicles ?? 0)
          + Math.random() * ((this.maxDistanceBetweenVechicles ?? 0)
            - (this.minDistanceBetweenVechicles ?? 0)) });
    }
  }
  Update() {
    for (const car of this._traffic ?? []) {
      const follower = car.follower;
      const spline = follower.splineParent;
      if (!spline?.Length) continue;
      const z = follower.localSplinePosition.z;
      let gap = Infinity;
      for (const other of this._traffic) {
        if (other === car || other.follower.splineParent !== spline) continue;
        const distance = (other.follower.localSplinePosition.z - z + spline.Length)
          % spline.Length;
        if (distance > 0.001) gap = Math.min(gap, distance);
      }
      for (const light of this._lights ?? []) {
        const lane = light._lane;
        if (light.state === 0 || lane?.splineParent !== spline) continue;
        const distance = (lane.localSplinePosition.z - z + spline.Length) % spline.Length;
        if (distance > 0.001) gap = Math.min(gap, distance);
      }
      const speedFactor = Math.max(0, Math.min(1,
        (gap - car.spacing) / Math.max(0.1, car.spacing)));
      follower.localSplinePosition.z = (z + car.speed * Time.deltaTime * speedFactor)
        % spline.Length;
    }
  }
}

// The PipesSystem example builds splines through Unity Jobs and mesh deformation.
// This keeps its grid placement and segment workflow usable in the browser while
// representing the result with Three.js tubes. Junction deformation stays native.
export class PreviewArchitectPipelineHandler extends MonoBehaviour {
  static __unityFullName = 'SplineArchitect.Examples.PipelineHandler';
  gridSize = 2;
  distanceThreshold = 4;
  Start() {
    this._pipelines = [];
    this._currentPipe = null;
    this._pipeMaterial = new THREE.MeshStandardMaterial({ color: 0x59646c,
      metalness: 0.5, roughness: 0.42 });
    this._indicator = new THREE.Mesh(new THREE.SphereGeometry(0.75, 12, 8),
      new THREE.MeshBasicMaterial({ color: 0x46c978, transparent: true,
        opacity: 0.75, depthWrite: false }));
    this.gameObject.object3D.add(this._indicator);
    reportUnsupportedUnityAPI('SplineArchitect.PipeSystem', {
      runtime: this.gameObject._runtime, system: 'standalone-component',
      gameObject: this.gameObject.name,
      detail: 'Pipe placement uses a Three.js tube approximation; native spline deformation and junction meshes are unavailable' });
  }
  _mousePoint() {
    const camera = Camera.main;
    if (!camera) return null;
    const ray = camera.ScreenPointToRay(Input.mousePosition);
    if (!ray?.direction || Math.abs(ray.direction.y) < 1e-6) return null;
    const distance = -ray.origin.y / ray.direction.y;
    if (distance < 0) return null;
    const point = ray.origin.clone().addScaledVector(ray.direction, distance);
    const step = Math.max(0.01, Number(this.gridSize) || 2);
    return new Vector3(Math.round(point.x / step) * step, 0,
      Math.round(point.z / step) * step);
  }
  _nearestEnd(point) {
    let nearest = null;
    for (const pipe of this._pipelines) {
      if (pipe === this._currentPipe || pipe.points.length < 2) continue;
      for (const index of [0, pipe.points.length - 1]) {
        const end = pipe.points[index];
        const distance = end.distanceTo(point);
        if (distance <= 1.5 && (!nearest || distance < nearest.distance))
          nearest = { pipe, index, point: end, distance };
      }
    }
    return nearest;
  }
  _draw(pipe, cursor = null) {
    if (pipe.mesh) {
      this.gameObject.object3D.remove(pipe.mesh);
      pipe.mesh.geometry.dispose();
      pipe.mesh = null;
    }
    const points = [...pipe.points, ...(cursor ? [cursor] : [])];
    const shape = points.filter((value, index) =>
      index === 0 || value.distanceTo(points[index - 1]) > 0.01);
    if (shape.length < 2) return;
    const offset = this.pipeObjects?.[0]?.offset?.y ?? 1.2;
    const curve = new THREE.CatmullRomCurve3(shape.map(value =>
      new THREE.Vector3(value.x, offset, value.z)), false, 'centripetal');
    pipe.mesh = new THREE.Mesh(new THREE.TubeGeometry(curve,
      Math.max(8, shape.length * 12), 0.8, 8, false), this._pipeMaterial);
    pipe.mesh.renderOrder = 10;
    this.gameObject.object3D.add(pipe.mesh);
  }
  Update() {
    const point = this._mousePoint();
    if (!point) return;
    this._indicator.position.set(point.x, (this.indicatorOffset?.y ?? 2), point.z);
    const nearby = this._nearestEnd(point);
    this._indicator.material.color.setHex(nearby ? 0xe7c545 : 0x46c978);
    const cursor = nearby?.point ?? point;
    if (this._currentPipe && (!this._lastCursor || !this._lastCursor.equals(cursor))) {
      this._draw(this._currentPipe, cursor);
      this._lastCursor = cursor.clone();
    }
    if (Input.GetKeyDown('Escape') && this._currentPipe) {
      this._draw(this._currentPipe);
      this._currentPipe = null;
      this._lastCursor = null;
      return;
    }
    if (!Input.GetMouseButtonDown(0)) return;
    if (!this._currentPipe) {
      this._currentPipe = { points: [cursor.clone()], mesh: null };
      this._pipelines.push(this._currentPipe);
      this._lastCursor = null;
      return;
    }
    const last = this._currentPipe.points.at(-1);
    if (last.distanceTo(cursor) < Math.max(0.1, this.distanceThreshold || 4)) return;
    this._currentPipe.points.push(cursor.clone());
    this._draw(this._currentPipe);
    this._lastCursor = null;
    if (nearby) this._currentPipe = null;
  }
}

export const PreviewSplineArchitectComponents = {
  'SplineArchitect.Spline': PreviewArchitectSpline,
  'SplineArchitect.SplineObject': PreviewArchitectSplineObject,
  'SplineArchitect.Examples.TrafficHandler': PreviewArchitectTrafficHandler,
  'SplineArchitect.Examples.TrafficLight': PreviewArchitectTrafficLight,
  'SplineArchitect.Examples.Vehicle': PreviewArchitectVehicle,
  'SplineArchitect.Examples.LookAt': PreviewArchitectLookAt,
  'SplineArchitect.Examples.PipelineHandler': PreviewArchitectPipelineHandler,
};
