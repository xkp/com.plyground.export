import * as THREE from 'three';
import { PropertyName } from './core-property-name.js';

export class ExposedReference {
  constructor() {
    this.exposedName = new PropertyName();
    this.defaultValue = null;
  }
  Resolve(resolver) {
    if (!resolver) return this.defaultValue;
    const valid = { value: false };
    const value = resolver.GetReferenceValue(this.exposedName, valid);
    return valid.value ? value : this.defaultValue;
  }
  clone() {
    const copy = new ExposedReference();
    copy.exposedName = new PropertyName(this.exposedName);
    copy.defaultValue = this.defaultValue;
    return copy;
  }
}

export class Notification {
  constructor(name) { this.id = new PropertyName(name); }
}

export class PlayableBinding {
  constructor(streamName = '', sourceObject = null, outputTargetType = null) {
    this.streamName = streamName;
    this.sourceObject = sourceObject;
    this.outputTargetType = outputTargetType == null ? null : {
      FullName: outputTargetType.unityTypeName ?? outputTargetType.__unityFullName
        ?? `UnityEngine.${outputTargetType.name}`,
      runtimeType: outputTargetType.runtimeType ?? outputTargetType,
    };
  }
  static get DefaultDuration() { return Infinity; }
  static get None() { return []; }
}

export class ScriptPlayableBinding {
  static Create(name, source, type) { return new PlayableBinding(name, source, type); }
}

const handleIds = new WeakMap();
let nextHandleId = 1;
const handleHash = value => {
  if (!value) return 0;
  if (!handleIds.has(value)) handleIds.set(value, nextHandleId++);
  return handleIds.get(value);
};
const playableType = value => ({ FullName: value.constructor === AnimationClipPlayable
  ? 'UnityEngine.Animations.AnimationClipPlayable' : 'UnityEngine.Playables.Playable',
  runtimeType: value.constructor });
const outputType = value => ({ FullName: value instanceof ScriptPlayableOutput
  ? 'UnityEngine.Playables.ScriptPlayableOutput' : 'UnityEngine.Animations.AnimationPlayableOutput',
  runtimeType: value.constructor });

export class PlayableHandle {
  constructor(value = null) { this._value = value; }
  static get Null() { return new PlayableHandle(); }
  static op_Equality(a, b) { return a?._value === b?._value; }
  static op_Inequality(a, b) { return !PlayableHandle.op_Equality(a, b); }
  Equals(other) { return other instanceof PlayableHandle && PlayableHandle.op_Equality(this, other); }
  GetHashCode() { return handleHash(this._value); }
}

export class PlayableOutputHandle {
  constructor(value = null) { this._value = value; }
  static get Null() { return new PlayableOutputHandle(); }
  static op_Equality(a, b) { return a?._value === b?._value; }
  static op_Inequality(a, b) { return !PlayableOutputHandle.op_Equality(a, b); }
  Equals(other) { return other instanceof PlayableOutputHandle
    && PlayableOutputHandle.op_Equality(this, other); }
  GetHashCode() { return handleHash(this._value); }
}

export class PlayableOutput {
  constructor(value = null) { this._value = value; }
  static get Null() { return new PlayableOutput(); }
  IsOutputValid() { return this._value?.IsOutputValid() ?? false; }
  GetHandle() { return new PlayableOutputHandle(this._value); }
  GetPlayableOutputType() { return this._value ? outputType(this._value) : null; }
  IsPlayableOutputOfType(type) {
    return this._value instanceof (type?.runtimeType ?? type);
  }
  Equals(other) { return this._value === (other instanceof PlayableOutput ? other._value : other); }
}

/** Unity graph handles and the selected clip-to-Animator output path. */
export class PlayableGraph {
  constructor(state = null) { this._state = state; }

  static Create(name = null) {
    return new PlayableGraph({ valid: true, playing: false, done: false,
      name: name ?? '', timeMode: 0, resolver: null, playables: [], outputs: [],
      firstAutomaticFrame: false, lastDspTime: null, frameId: 0 });
  }

  IsValid() { return this._state?.valid === true; }
  IsPlaying() { return this.IsValid() && this._state.playing; }
  IsDone() {
    return this.IsValid() && (this._state.done
      || this._state.outputs.length > 0
        && this._state.outputs.every(output => output.source?.IsDone() === true));
  }
  GetResolver() { return this._state?.resolver ?? null; }
  SetResolver(resolver) {
    if (!this.IsValid()) throw new Error('PlayableGraph is invalid');
    this._state.resolver = resolver;
  }
  GetEditorName() { return this._state?.name ?? ''; }
  GetTimeUpdateMode() { return this._state?.timeMode ?? 0; }
  SetTimeUpdateMode(value) {
    if (!this.IsValid()) throw new Error('PlayableGraph is invalid');
    this._state.timeMode = value;
    this._state.lastDspTime = value === 0 ? this._dspNow() : null;
  }
  GetPlayableCount() { return this._state?.playables.length ?? 0; }
  GetRootPlayableCount() { return this._state?.playables.filter(node => !node._outputNode).length ?? 0; }
  GetOutputCount() { return this._state?.outputs.length ?? 0; }
  GetRootPlayable(index) {
    return this._state?.playables.filter(node => !node._outputNode)[index] ?? null;
  }
  GetOutput(index) { return this._state?.outputs[index] ?? null; }
  GetOutputByType(type, index) {
    const runtimeClass = type?.runtimeType ?? type;
    return this._state?.outputs.filter(output => output instanceof runtimeClass)[index] ?? null;
  }
  GetOutputCountByType(type) {
    const runtimeClass = type?.runtimeType ?? type;
    return this._state?.outputs.filter(output => output instanceof runtimeClass).length ?? 0;
  }
  Play() {
    if (!this.IsValid()) throw new Error('PlayableGraph is invalid');
    this._state.playing = true;
    this._state.firstAutomaticFrame = true;
    this._state.lastDspTime = this._dspNow();
    for (const output of this._state.outputs) output._start();
  }
  Stop() {
    if (!this.IsValid()) throw new Error('PlayableGraph is invalid');
    this._state.playing = false;
    for (const output of this._state.outputs) output._stop();
  }
  Evaluate(deltaTime = 0) {
    if (!this.IsValid()) throw new Error('PlayableGraph is invalid');
    this._state.frameId++;
    for (const output of this._state.outputs) output._evaluate(deltaTime);
    for (const output of this._state.outputs) output._dispatchNotifications();
  }
  Connect(source, sourcePort, target, targetPort) {
    if (!this.IsValid() || !source?.IsValid() || !target?.IsValid()
      || source.graph !== this || target.graph !== this
      || !Number.isInteger(sourcePort) || !Number.isInteger(targetPort)
      || sourcePort < 0 || sourcePort >= source._outputs.length
      || targetPort < 0 || targetPort >= target._inputs.length
      || source._outputs[sourcePort] || target._inputs[targetPort].source) return false;
    source._outputs[sourcePort] = { target, targetPort };
    target._inputs[targetPort].source = source;
    target._inputs[targetPort].sourcePort = sourcePort;
    target._inputs[targetPort].weight = 0;
    return true;
  }
  Disconnect(target, targetPort) {
    const slot = target?._inputs?.[targetPort];
    if (!slot) return;
    if (slot.source) slot.source._outputs[slot.sourcePort] = null;
    slot.source = null;
    slot.sourcePort = -1;
    slot.weight = 1;
  }
  DestroyPlayable(playable) {
    if (!this.IsValid() || playable?.graph !== this || !playable.IsValid()) return;
    if (playable instanceof ScriptPlayable) {
      if (playable._graphStarted) playable.behaviour.OnGraphStop(playable);
      playable.behaviour.OnPlayableDestroy(playable);
    }
    for (let port = 0; port < playable._inputs.length; port++) this.Disconnect(playable, port);
    for (const link of [...playable._outputs])
      if (link) this.Disconnect(link.target, link.targetPort);
    this._state.playables.splice(this._state.playables.indexOf(playable), 1);
    playable._valid = false;
  }
  DestroySubgraph(playable) {
    if (!this.IsValid() || playable?.graph !== this) return;
    const upstream = new Set();
    const visit = node => {
      if (!node?.IsValid() || upstream.has(node)) return;
      upstream.add(node);
      for (const slot of node._inputs) visit(slot.source);
    };
    visit(playable);
    for (const node of upstream) this.DestroyPlayable(node);
  }
  DestroyOutput(output) {
    if (!this.IsValid() || output?.graph !== this || !output.IsOutputValid()) return;
    const index = this._state.outputs.indexOf(output);
    if (index < 0) return;
    this._state.outputs.splice(index, 1);
    output._stop();
    output.animator?._playableOutputs?.delete(output);
    output._valid = false;
  }
  _dspNow() { return this._state?.dspNow?.() ?? performance.now() / 1000; }
  _advanceAutomatic(scaledDeltaTime, unscaledDeltaTime) {
    if (!this.IsPlaying() || this._state.timeMode === 3) return;
    const now = this._state.timeMode === 0 ? this._dspNow() : null;
    if (this._state.firstAutomaticFrame) {
      this._state.firstAutomaticFrame = false;
      return;
    }
    const elapsed = this._state.timeMode === 0
      ? Math.max(0, now - this._state.lastDspTime)
      : this._state.timeMode === 2 ? unscaledDeltaTime : scaledDeltaTime;
    this._state.lastDspTime = now;
    this.Evaluate(elapsed);
  }
  Destroy() {
    if (!this.IsValid()) return;
    this._state.playing = false;
    this._state.valid = false;
    for (const output of this._state.outputs) {
      output._stop();
      output.animator?._playableOutputs?.delete(output);
    }
  }
}

const emptyPlayableInput = () => ({ source: null, sourcePort: -1, weight: 0 });

export class Playable {
  constructor(graph = null, inputCount = 0) {
    this.graph = graph;
    this._inputs = Array.from({ length: inputCount }, emptyPlayableInput);
    this._outputs = [null];
    this.time = 0;
    this.previousTime = 0;
    this.speed = 1;
    this.duration = Number.MAX_VALUE;
    this.delay = 0;
    this.leadTime = 0;
    this.playState = 1;
    this.traversalMode = 0;
    this.propagateSetTime = false;
    this.done = false;
    this._valid = graph?.IsValid() === true;
  }
  static Create(graph, inputCount = 0) {
    if (!graph?.IsValid()) throw new Error('Playable needs a valid graph');
    const playable = new Playable(graph, inputCount);
    graph._state.playables.push(playable);
    return playable;
  }
  static get Null() { return new Playable(); }
  GetHandle() { return new PlayableHandle(this.IsNull() ? null : this); }
  GetPlayableType() { return this.IsNull() ? null : playableType(this); }
  IsPlayableOfType(type) { return !this.IsNull()
    && this.constructor === (type?.runtimeType ?? type); }
  IsValid() { return this._valid; }
  IsNull() { return this.graph == null; }
  Equals(other) { return this === other || this.IsNull() && other?.IsNull?.(); }
  CanChangeInputs() { return true; }
  CanDestroy() { return this.IsValid(); }
  CanSetWeights() { return true; }
  GetGraph() { return this.graph; }
  GetInputCount() { return this._inputs.length; }
  SetInputCount(value) {
    if (!this.CanChangeInputs()) throw new Error('Playable inputs are fixed');
    const count = Number(value);
    if (!Number.isInteger(count) || count < 0) throw new RangeError('Invalid input count');
    while (this._inputs.length > count) this.graph.Disconnect(this, this._inputs.length - 1), this._inputs.pop();
    while (this._inputs.length < count) this._inputs.push(emptyPlayableInput());
  }
  GetInput(index) { return this._inputs[index]?.source ?? Playable.Null; }
  GetInputWeight(index) { return this._inputs[index]?.weight ?? 0; }
  SetInputWeight(inputOrIndex, weight) {
    const index = typeof inputOrIndex === 'number' ? inputOrIndex
      : this._inputs.findIndex(slot => slot.source === inputOrIndex);
    if (index < 0 || index >= this._inputs.length) throw new RangeError('Invalid input');
    this._inputs[index].weight = Number(weight);
  }
  ConnectInput(index, source, sourcePort, weight = 0) {
    if (!this.graph.Connect(source, sourcePort, this, index)) throw new Error('Cannot connect input');
    this.SetInputWeight(index, weight);
  }
  AddInput(source, sourcePort, weight) {
    const index = this.GetInputCount();
    this.SetInputCount(index + 1);
    this.ConnectInput(index, source, sourcePort, weight);
    return index;
  }
  DisconnectInput(index) { this.graph.Disconnect(this, index); }
  GetOutputCount() { return this._outputs.length; }
  SetOutputCount(value) {
    const count = Number(value);
    if (!Number.isInteger(count) || count < 0) throw new RangeError('Invalid output count');
    while (this._outputs.length > count) {
      const link = this._outputs.at(-1);
      if (link) this.graph.Disconnect(link.target, link.targetPort);
      this._outputs.pop();
    }
    while (this._outputs.length < count) this._outputs.push(null);
  }
  GetOutput(index) { return this._outputs[index]?.target ?? Playable.Null; }
  Destroy() { this.graph?.DestroyPlayable(this); }
  GetTime() { return this.time; }
  SetTime(value) { this.time = Number(value); }
  GetPreviousTime() { return this.previousTime; }
  SetSpeed(value) { this.speed = Number(value); }
  GetSpeed() { return this.speed; }
  GetDuration() { return this.duration; }
  SetDuration(value) { this.duration = Number(value); }
  GetDelay() { return this.delay; }
  SetDelay(value) { this.delay = Number(value); }
  GetLeadTime() { return this.leadTime; }
  SetLeadTime(value) { this.leadTime = Number(value); }
  GetPlayState() { return this.playState; }
  SetPlayState(value) { this.playState = Number(value); }
  Play() { this.playState = 1; }
  Pause() { this.playState = 0; }
  GetTraversalMode() { return this.traversalMode; }
  SetTraversalMode(value) { this.traversalMode = Number(value); }
  GetPropagateSetTime() { return this.propagateSetTime; }
  SetPropagateSetTime(value) { this.propagateSetTime = Boolean(value); }
  IsDelayed() { return this.delay > 0; }
  IsDone() { return this.done; }
  SetDone(value) { this.done = Boolean(value); }
}

export class AnimationClipPlayable extends Playable {
  constructor(graph = null, clip = null) {
    super(graph, 0);
    this.clip = clip;
    this.applyFootIK = false;
    this._valid = this._valid && clip != null;
  }
  static Create(graph, clip) {
    if (!graph?.IsValid()) throw new Error('AnimationClipPlayable needs a valid graph');
    if (!(clip instanceof THREE.AnimationClip)) throw new TypeError('Expected an AnimationClip');
    const playable = new AnimationClipPlayable(graph, clip);
    graph._state.playables.push(playable);
    return playable;
  }
  CanChangeInputs() { return false; }
  CanSetWeights() { return false; }
  SetApplyFootIK(value) { this.applyFootIK = Boolean(value); }
}

export class PlayableBehaviour {
  Clone() { return globalThis.Object.assign(globalThis.Object.create(globalThis.Object.getPrototypeOf(this)), this); }
  OnBehaviourDelay(_playable, _info) { }
  OnBehaviourPause(_playable, _info) { }
  OnBehaviourPlay(_playable, _info) { }
  OnGraphStart(_playable) { }
  OnGraphStop(_playable) { }
  OnPlayableCreate(_playable) { }
  OnPlayableDestroy(_playable) { }
  PrepareData(_playable, _info) { }
  PrepareFrame(_playable, _info) { }
  ProcessFrame(_playable, _info, _playerData) { }
}

export class ScriptPlayable extends Playable {
  constructor(graph = null, behaviour = null, inputCount = 0) {
    super(graph, inputCount);
    this.behaviour = behaviour;
    this._graphStarted = false;
    this._behaviourPlaying = false;
    this._behaviourDelayed = false;
  }
  static Create(graph, behaviourType, templateOrInputCount = 0, inputCount = 0) {
    if (!graph?.IsValid()) throw new Error('ScriptPlayable needs a valid graph');
    const template = typeof templateOrInputCount === 'number' ? null : templateOrInputCount;
    const count = template ? inputCount : templateOrInputCount;
    const behaviour = template ? template.Clone() : new (behaviourType?.runtimeType ?? behaviourType)();
    const playable = new ScriptPlayable(graph, behaviour, count);
    graph._state.playables.push(playable);
    behaviour.OnPlayableCreate(playable);
    return playable;
  }
  static get Null() { return new ScriptPlayable(); }
  static FromPlayable(playable, behaviourType) {
    const expected = behaviourType?.runtimeType ?? behaviourType;
    if (playable instanceof ScriptPlayable && playable.behaviour instanceof expected)
      return playable;
    throw new TypeError('Playable has a different script behaviour type');
  }
  GetBehaviour() { return this.behaviour; }
  Equals(other) { return super.Equals(other); }
  _evaluate(info, playerData) {
    if (!this.IsValid()) return;
    if (!this._graphStarted) {
      this._graphStarted = true;
      this.behaviour.OnGraphStart(this);
    }
    if (this.playState === 0) {
      if (this._behaviourPlaying) {
        this._behaviourPlaying = false;
        this.behaviour.OnBehaviourPause(this, info);
      }
      return;
    }
    if (this.delay > 0) {
      this.delay = Math.max(0, this.delay - info.deltaTime);
      if (this.delay > 0) {
        if (!this._behaviourDelayed) this.behaviour.OnBehaviourDelay(this, info);
        this._behaviourDelayed = true;
        this._behaviourPlaying = false;
        return;
      }
    }
    this._behaviourDelayed = false;
    if (!this._behaviourPlaying) {
      this._behaviourPlaying = true;
      this.behaviour.OnBehaviourPlay(this, info);
    }
    this.behaviour.PrepareFrame(this, info);
    this.behaviour.ProcessFrame(this, info, playerData);
  }
}

export class FrameData {
  static EvaluationType = globalThis.Object.freeze({ Evaluate: 0, Playback: 1 });
  constructor(output, deltaTime, frameId) {
    this.deltaTime = deltaTime;
    this.effectiveParentDelay = 0;
    this.effectiveParentSpeed = 1;
    this.effectivePlayState = 1;
    this.effectiveSpeed = 1;
    this.effectiveWeight = 1;
    this.evaluationType = 1;
    this.frameId = frameId;
    this.output = output;
    this.seekOccurred = false;
    this.timeHeld = false;
    this.timeLooped = false;
    this.weight = 1;
  }
}

export class AnimationPlayableOutput {
  constructor(graph, name, animator) {
    this.graph = graph;
    this.name = name;
    this.animator = animator;
    this.source = null;
    this._valid = graph?.IsValid() === true && animator != null;
    this._weight = 1;
    this._referenceObject = null;
    this._userData = null;
    this._mixer = null;
    this._action = null;
    this._notificationReceivers = [];
    this._pendingNotifications = [];
  }
  static Create(graph, name, animator) {
    if (!graph?.IsValid()) throw new Error('AnimationPlayableOutput needs a valid graph');
    if (!animator?.gameObject?.object3D) throw new TypeError('Expected an Animator with a GameObject');
    const output = new AnimationPlayableOutput(graph, name, animator);
    graph._state.outputs.push(output);
    animator._playableOutputs ??= new Set();
    animator._playableOutputs.add(output);
    return output;
  }
  IsOutputValid() { return this._valid; }
  GetHandle() { return new PlayableOutputHandle(this.IsOutputNull() ? null : this); }
  GetPlayableOutputType() { return this.IsOutputNull() ? null : outputType(this); }
  IsPlayableOutputOfType(type) { return this instanceof (type?.runtimeType ?? type); }
  Equals(other) { return this === (other instanceof PlayableOutput ? other._value : other); }
  IsOutputNull() { return this.graph == null; }
  GetWeight() { return this._weight; }
  SetWeight(value) {
    this._weight = Number(value);
    this._action?.setEffectiveWeight(this._weight);
  }
  GetReferenceObject() { return this._referenceObject; }
  SetReferenceObject(value) { this._referenceObject = value; }
  GetUserData() { return this._userData; }
  SetUserData(value) { this._userData = value; }
  AddNotificationReceiver(receiver) {
    if (!this._notificationReceivers.includes(receiver))
      this._notificationReceivers.push(receiver);
  }
  GetNotificationReceivers() { return [...this._notificationReceivers]; }
  RemoveNotificationReceiver(receiver) {
    const index = this._notificationReceivers.indexOf(receiver);
    if (index >= 0) this._notificationReceivers.splice(index, 1);
  }
  PushNotification(origin, notification, context) {
    this._pendingNotifications.push({ origin, notification, context });
  }
  _dispatchNotifications() {
    const pending = this._pendingNotifications.splice(0);
    for (const { origin, notification, context } of pending)
      for (const receiver of [...this._notificationReceivers])
        receiver.OnNotify(origin, notification, context);
  }
  GetSourceInputPort() { return 0; }
  SetSourceInputPort(_port) { }
  GetSourceOutputPort() { return 0; }
  SetSourceOutputPort(_port) { }
  SetSourcePlayable(playable, _sourceOutputPort = 0) {
    if (!(playable instanceof AnimationClipPlayable) || playable.graph !== this.graph)
      throw new TypeError('Output source must be a clip playable in the same graph');
    this._action?.stop();
    this.source = playable;
    this._mixer = new THREE.AnimationMixer(this.animator.gameObject.object3D);
    this._action = this._mixer.clipAction(playable.clip);
    this._action.setEffectiveWeight(this._weight);
    if (this.graph.IsPlaying()) this._start();
  }
  GetSourcePlayable() { return this.source; }
  _start() { if (this._action) this._action.paused = false; this._action?.play(); }
  _stop() { if (this._action) this._action.paused = true; }
  _evaluate(deltaTime) {
    if (!this.source || !this.graph.IsPlaying() || this.source.playState !== 1 || this.source.done) return;
    const elapsed = deltaTime * this.source.speed;
    this.source.previousTime = this.source.time;
    this.source.time += elapsed;
    this._action.time = this.source.previousTime;
    this._mixer.update(elapsed);
  }
}

export class ScriptPlayableOutput extends AnimationPlayableOutput {
  constructor(graph = null, name = '') {
    super(graph, name, null);
    this._valid = graph?.IsValid() === true;
  }
  static Create(graph, name) {
    if (!graph?.IsValid()) throw new Error('ScriptPlayableOutput needs a valid graph');
    const output = new ScriptPlayableOutput(graph, name);
    graph._state.outputs.push(output);
    return output;
  }
  static get Null() { return new ScriptPlayableOutput(); }
  static FromPlayableOutput(output) {
    const value = output instanceof PlayableOutput ? output._value : output;
    if (value instanceof ScriptPlayableOutput) return value;
    throw new TypeError('PlayableOutput is not a ScriptPlayableOutput');
  }
  SetSourcePlayable(playable, _sourceOutputPort = 0) {
    if (!(playable instanceof ScriptPlayable) || playable.graph !== this.graph)
      throw new TypeError('Script output source must be a ScriptPlayable in the same graph');
    this.source = playable;
  }
  _start() { }
  _stop() { }
  _evaluate(deltaTime) {
    if (!this.source) return;
    const info = new FrameData(this, deltaTime, this.graph._state.frameId);
    this.source._evaluate(info, this._userData);
  }
}
