// Small executable subset of Unity AnimatorController data. The graph and
// clip poses are exported by Unity; rendering remains the caller's choice.
const number = value => Number(value) || 0;
const clamp01 = value => Math.max(0, Math.min(1, value));

export function animatorConditionMatches(condition, parameters) {
  const value = parameters.get(condition.parameter);
  const threshold = number(condition.threshold);
  switch (condition.mode) {
    case 'If': return Boolean(value);
    case 'IfNot': return !value;
    case 'Greater': return number(value) > threshold;
    case 'Less': return number(value) < threshold;
    case 'Equals': return number(value) === threshold;
    case 'NotEqual': return number(value) !== threshold;
    default: throw new Error(`Unsupported Animator condition ${condition.mode}`);
  }
}

function between(value, children, coordinate) {
  const sorted = [...children].sort((a, b) => coordinate(a) - coordinate(b));
  if (sorted.length === 1) return [{ child: sorted[0], weight: 1 }];
  if (value <= coordinate(sorted[0])) return [{ child: sorted[0], weight: 1 }];
  if (value >= coordinate(sorted.at(-1))) return [{ child: sorted.at(-1), weight: 1 }];
  for (let index = 0; index < sorted.length - 1; index++) {
    const first = sorted[index];
    const second = sorted[index + 1];
    const low = coordinate(first);
    const high = coordinate(second);
    if (value > high) continue;
    if (high === low) return [{ child: second, weight: 1 }];
    const fraction = clamp01((value - low) / (high - low));
    return [{ child: first, weight: 1 - fraction }, { child: second, weight: fraction }];
  }
  return [];
}

function cartesianWeights(children, x, y) {
  const points = children.map(child => ({ child,
    x: number(child.position?.x), y: number(child.position?.y) }));
  for (const point of points)
    if (Math.hypot(x - point.x, y - point.y) < 1e-6)
      return [{ child: point.child, weight: 1 }];
  let best = null;
  for (let i = 0; i < points.length; i++)
    for (let j = i + 1; j < points.length; j++)
      for (let k = j + 1; k < points.length; k++) {
        const a = points[i], b = points[j], c = points[k];
        const det = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (Math.abs(det) < 1e-8) continue;
        const wa = ((b.y - c.y) * (x - c.x) + (c.x - b.x) * (y - c.y)) / det;
        const wb = ((c.y - a.y) * (x - c.x) + (a.x - c.x) * (y - c.y)) / det;
        const wc = 1 - wa - wb;
        if (Math.min(wa, wb, wc) < -1e-6) continue;
        const length = (p, q) => Math.hypot(p.x - q.x, p.y - q.y);
        const score = Math.max(length(a, b), length(b, c), length(c, a));
        if (!best || score < best.score) best = { score,
          weights: [{ child: a.child, weight: Math.max(0, wa) },
            { child: b.child, weight: Math.max(0, wb) },
            { child: c.child, weight: Math.max(0, wc) }] };
      }
  if (best) {
    const total = best.weights.reduce((sum, item) => sum + item.weight, 0);
    return best.weights.filter(item => item.weight > 1e-8)
      .map(item => ({ ...item, weight: item.weight / total }));
  }
  // Beyond the convex hull, continue along the closest sampled edge.
  for (let i = 0; i < points.length; i++) for (let j = i + 1; j < points.length; j++) {
    const a = points[i], b = points[j];
    const dx = b.x - a.x, dy = b.y - a.y;
    const size = dx * dx + dy * dy;
    if (size < 1e-12) continue;
    const t = clamp01(((x - a.x) * dx + (y - a.y) * dy) / size);
    const distance = Math.hypot(x - a.x - t * dx, y - a.y - t * dy);
    if (!best || distance < best.score) best = { score: distance,
      weights: [{ child: a.child, weight: 1 - t }, { child: b.child, weight: t }] };
  }
  return best?.weights.filter(item => item.weight > 1e-8)
    ?? [{ child: points[0].child, weight: 1 }];
}

function weightedChildren(motion, parameters, available) {
  const children = (motion.children ?? []).filter(child => child.motion && available(child.motion));
  if (!children.length) return [];
  if (motion.blendType === 'Simple1D')
    return between(number(parameters.get(motion.parameter)), children, child => number(child.threshold));
  if (motion.blendType === 'FreeformCartesian2D' || motion.blendType === 'SimpleDirectional2D'
    || motion.blendType === 'FreeformDirectional2D') {
    const xs = children.map(child => number(child.position?.x));
    const ys = children.map(child => number(child.position?.y));
    // Values outside the sampled rectangle remain on its edge. In particular,
    // JU's Speed may exceed its 2D tree's highest row while the character
    // accelerates; inverse-distance extrapolation would add turn clips there.
    const cartesian = motion.blendType === 'FreeformCartesian2D';
    const inputX = number(parameters.get(motion.parameter));
    const inputY = number(parameters.get(motion.parameterY));
    const x = cartesian ? Math.max(Math.min(...xs), Math.min(Math.max(...xs), inputX)) : inputX;
    const y = cartesian ? Math.max(Math.min(...ys), Math.min(Math.max(...ys), inputY)) : inputY;
    // Collinear samples are an exact one-dimensional slice of a 2D tree.
    if (children.every(child => Math.abs(number(child.position?.x) - number(children[0].position?.x)) < 1e-6))
      return between(y, children, child => number(child.position?.y));
    if (cartesian) return cartesianWeights(children, x, y);
    if (motion.blendType === 'SimpleDirectional2D' && Math.hypot(x, y) > 1e-6) {
      // Simple directional trees interpolate between neighboring rays. A
      // distance-based blend gives diagonal clips large weights even when the
      // input is almost exactly forward.
      const direction = Math.atan2(y, x);
      const rays = children.filter(child => Math.hypot(
        number(child.position?.x), number(child.position?.y)) > 1e-6)
        .map(child => ({ child, angle: Math.atan2(
          number(child.position.y), number(child.position.x)) }))
        .sort((a, b) => a.angle - b.angle);
      const aligned = rays.filter(ray => Math.abs(Math.atan2(
        Math.sin(direction - ray.angle), Math.cos(direction - ray.angle))) < 1e-6);
      if (aligned.length === 1) return [{ child: aligned[0].child, weight: 1 }];
      if (aligned.length > 1)
        return between(Math.hypot(x, y), aligned.map(ray => ray.child),
          child => Math.hypot(number(child.position?.x), number(child.position?.y)));
      if (rays.length > 1) {
        const next = rays.findIndex(ray => ray.angle > direction);
        const right = next < 0 ? 0 : next;
        const left = (right - 1 + rays.length) % rays.length;
        const start = rays[left].angle;
        const end = rays[right].angle + (right === 0 ? 2 * Math.PI : 0);
        const input = direction + (right === 0 && direction < start ? 2 * Math.PI : 0);
        const weight = clamp01((input - start) / (end - start));
        return [{ child: rays[left].child, weight: 1 - weight },
          { child: rays[right].child, weight }].filter(item => item.weight > 1e-8);
      }
    }
    // For non-collinear samples, use the nearest three positions. Unity's
    // full freeform weighting is still a separate compatibility task.
    const nearest = children.map(child => ({ child,
      distance: Math.hypot(x - number(child.position?.x), y - number(child.position?.y)) }))
      .sort((a, b) => a.distance - b.distance).slice(0, 3);
    if (nearest[0].distance < 1e-6) return [{ child: nearest[0].child, weight: 1 }];
    const total = nearest.reduce((sum, item) => sum + 1 / (item.distance * item.distance), 0);
    return nearest.map(item => ({ child: item.child,
      weight: 1 / (item.distance * item.distance) / total }));
  }
  throw new Error(`Unsupported Animator blend type ${motion.blendType}`);
}

export function evaluateAnimatorMotion(motion, parameters, availableClip = () => true) {
  const available = node => node?.kind === 'clip'
    ? availableClip(node) : Boolean(node?.children?.some(child => available(child.motion)));
  const result = [];
  const visit = (node, weight, timeScale, cycleOffset) => {
    if (node.kind === 'clip') {
      if (availableClip(node) && weight > 0)
        result.push({ name: node.name, assetPath: node.assetPath,
          duration: node.duration, weight, timeScale, cycleOffset });
      return;
    }
    for (const item of weightedChildren(node, parameters, available))
      visit(item.child.motion, weight * item.weight,
        timeScale * number(item.child.timeScale ?? 1), cycleOffset + number(item.child.cycleOffset));
  };
  if (motion) visit(motion, 1, 1, 0);
  return result;
}

function combine(entries) {
  const clips = new Map();
  for (const entry of entries) {
    const key = entry.assetPath || entry.name;
    const previous = clips.get(key) ?? { ...entry, weight: 0, timeScale: 0 };
    previous.weight += entry.weight;
    previous.timeScale += entry.timeScale * entry.weight;
    clips.set(key, previous);
  }
  for (const entry of clips.values()) entry.timeScale /= entry.weight;
  return clips;
}

export class AnimatorGraphPlayer {
  constructor(graph, { layer = 0, availableClip = () => true,
    clipDuration = entry => entry.duration || 1,
    allowMissingClips = false } = {}) {
    this.layer = graph.layers[layer];
    if (!this.layer) throw new Error(`Animator layer ${layer} is missing`);
    this.states = new Map(this.layer.states.map(state => [state.name, state]));
    this._triggerTypes = new Map((graph.parameters ?? []).map(parameter => [parameter.name, parameter.type]));
    this.availableClip = availableClip;
    this.clipDuration = clipDuration;
    this.allowMissingClips = allowMissingClips;
    this.state = this.layer.defaultState;
    this.elapsed = 0;
    this.transition = null;
    this._entered = false;
    this.previousParameters = null;
  }
  play(name, normalizedTime = 0) {
    const state = this.states.get(name);
    if (!state) return false;
    const motion = evaluateAnimatorMotion(state.motion, new Map(), this.availableClip)[0];
    const duration = motion ? this.clipDuration(motion) : 1;
    this.state = name;
    this.elapsed = Math.max(0, normalizedTime) * duration;
    this.transition = null;
    this._entered = false;
    this.previousParameters = null;
    return true;
  }
  update(deltaTime, parameters) {
    const events = [];
    // The JU Unity trace records a parameter crossing its threshold one
    // rendered frame before Mecanim begins the corresponding transition.
    // Blend tree values and one-shot triggers still use the current parameters.
    const transitionParameters = this.previousParameters ?? parameters;
    this.previousParameters = new Map(parameters);
    const current = this.states.get(this.state);
    if (!current) throw new Error(`Animator state ${this.state} is missing`);
    const beforeEntries = evaluateAnimatorMotion(current.motion, parameters, this.availableClip);
    const before = combine(beforeEntries.map(entry => ({ ...entry,
      timeScale: entry.timeScale * number(current.speed ?? 1) })));
    const duration = beforeEntries.reduce((sum, entry) => sum + entry.weight
      * this.clipDuration(entry) / Math.max(0.001, entry.timeScale * number(current.speed ?? 1)), 0) || 1;
    const candidates = [
      ...(this.layer.anyStateTransitions ?? []).map(edge => ({ edge, anyState: true })),
      ...(!this.transition ? (current.transitions ?? []).map(edge => ({ edge, anyState: false })) : []),
    ];
    if (candidates.length) {
      for (const { edge, anyState } of candidates) {
        const destination = this.states.get(edge.destination);
        if (anyState && destination?.name === this.state && edge.canTransitionToSelf !== true)
          continue;
        if (!destination || (!this.allowMissingClips
          && !evaluateAnimatorMotion(destination.motion, parameters, this.availableClip).length))
          continue;
        if (edge.hasExitTime && this.elapsed + deltaTime < number(edge.exitTime) * duration) continue;
        if (!(edge.conditions ?? []).every(condition => animatorConditionMatches(condition,
          this._triggerTypes.get(condition.parameter) === 'Trigger'
            ? parameters : transitionParameters)))
          continue;
        const sourceElapsed = this.elapsed;
        this.state = destination.name;
        this.elapsed = 0;
        this.transition = { from: before, fromState: current.name, fromElapsed: sourceElapsed,
          fromDuration: duration,
          duration: number(edge.duration) * (edge.fixedDuration === false ? duration : 1), elapsed: 0 };
        if (!this._entered) {
          events.push({ method: 'OnStateEnter', state: current.name, normalizedTime: 0, length: duration });
          this._entered = true;
        }
        const destinationMotion = evaluateAnimatorMotion(destination.motion, parameters, this.availableClip)[0];
        events.push({ method: 'OnStateEnter', state: destination.name, normalizedTime: 0,
          length: destinationMotion ? this.clipDuration(destinationMotion) : 1 });
        for (const condition of edge.conditions ?? [])
          if (this._triggerTypes.get(condition.parameter) === 'Trigger') {
            parameters.set(condition.parameter, false);
            this.previousParameters.set(condition.parameter, false);
          }
        break;
      }
    }
    this.elapsed += deltaTime;
    if (!this._entered) {
      events.push({ method: 'OnStateEnter', state: this.state, normalizedTime: 0, length: duration });
      this._entered = true;
    } else if (!events.some(event => event.method === 'OnStateEnter' && event.state === this.state))
      events.push({ method: 'OnStateUpdate', state: this.state,
        normalizedTime: this.elapsed / duration, length: duration });
    const state = this.states.get(this.state);
    const target = combine(evaluateAnimatorMotion(state.motion, parameters, this.availableClip)
      .map(entry => ({ ...entry, timeScale: entry.timeScale * number(state.speed ?? 1) })));
    const timed = (entry, elapsed) => ({ ...entry,
      time: elapsed * entry.timeScale + (entry.cycleOffset ?? 0) * this.clipDuration(entry) });
    if (!this.transition) return { state: this.state,
      clips: [...target.values()].map(entry => timed(entry, this.elapsed)), events };
    // Unity enters a transition before its first blend step. On that frame
    // GetCurrentAnimatorClipInfo still reports the full source clip weight.
    const alpha = this.transition.duration <= 0 ? 1
      : clamp01(this.transition.elapsed / this.transition.duration);
    this.transition.elapsed += deltaTime;
    const output = new Map();
    for (const [key, entry] of this.transition.from)
      output.set(key, { ...timed(entry, this.transition.fromElapsed + this.transition.elapsed),
        weight: entry.weight * (1 - alpha) });
    for (const [key, entry] of target) {
      const previous = output.get(key);
      if (previous) {
        previous.weight += entry.weight * alpha;
        previous.timeScale = entry.timeScale;
        previous.time = timed(entry, this.elapsed).time;
      } else output.set(key, { ...timed(entry, this.elapsed), weight: entry.weight * alpha });
    }
    if (alpha >= 1) {
      events.push({ method: 'OnStateExit', state: this.transition.fromState,
        normalizedTime: (this.transition.fromElapsed + this.transition.elapsed) / this.transition.fromDuration,
        length: this.transition.fromDuration });
      this.transition = null;
    }
    return { state: this.state, clips: [...output.values()].filter(entry => entry.weight > 0), events };
  }
}

/** Drives exported Animator layers and invokes transpiled StateMachineBehaviour instances. */
export class AnimatorControllerRuntime {
  constructor(graph, animator, behaviourTypes = {}, options = {}) {
    this.animator = animator;
    this.players = graph.layers.map((record, layer) => record.defaultState
      ? new AnimatorGraphPlayer(graph, {
        ...options, layer, allowMissingClips: layer !== 0 || options.allowMissingClips === true,
      }) : null);
    this.behaviours = graph.layers.map(layer => new Map(layer.states.map(state => [state.name,
      (state.behaviours ?? []).filter(record => !options.skipBehaviour?.(record, state, layer))
        .map(record => {
        const Type = behaviourTypes[record.type];
        if (typeof Type !== 'function') throw new Error(`Unregistered StateMachineBehaviour: ${record.type}`);
        return globalThis.Object.assign(new Type(), record.properties ?? {});
      }),
    ])));
  }
  play(name, layer = 0, normalizedTime = 0) {
    return this.players[layer]?.play(name, normalizedTime) ?? false;
  }
  update(deltaTime, parameters = this.animator.parameters) {
    return this.players.map((player, layer) => {
      if (!player) return { state: null, clips: [], events: [] };
      const output = player.update(deltaTime, parameters);
      this.animator._stateByLayer.set(layer, output.state);
      for (const event of output.events) {
        const info = { IsName: name => name === event.state,
          normalizedTime: event.normalizedTime, length: event.length };
        for (const behaviour of this.behaviours[layer].get(event.state) ?? [])
          behaviour[event.method]?.(this.animator, info, layer);
      }
      return output;
    });
  }
}
