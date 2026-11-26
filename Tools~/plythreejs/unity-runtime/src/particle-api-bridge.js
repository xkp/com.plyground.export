// Browser-backed subset of Unity's ParticleSystem module structs.
export function installParticleBrowserModules(ParticleSystem, types, enums, report) {
  const module = (system, name, Type) =>
    (system._particleModules ??= {})[name] ??= new Type(system);
  const curve = value => typeof value === 'number' ? value : Number(value?.constant ?? 0);
  const enumName = (values, value, fallback) => typeof value === 'string' ? value
    : Object.keys(values).find(name => values[name] === value) ?? fallback;
  const access = (target, name, get, set) => Object.defineProperty(target, name, {
    get, set: set ?? (() => {}), configurable: true,
  });
  for (const [name, Type] of [['main', types.MainModule],
    ['emission', types.EmissionModule], ['shape', types.ShapeModule]])
    access(ParticleSystem.prototype, name, function () { return module(this, name, Type); });

  for (const name of ['startLifetime', 'startSpeed', 'startSize', 'startDelay',
    'gravityModifier'])
    access(types.MainModule.prototype, name,
      function () {
        const range = this._system[`${name}Range`];
        return range ? new types.MinMaxCurve(range[0], range[1])
          : new types.MinMaxCurve(this._system[name]);
      },
      function (value) {
        const range = value?.mode === enums.ParticleSystemCurveMode.TwoConstants
          ? [Number(value.constantMin), Number(value.constantMax)] : null;
        this._system[name] = range ? range[1] : curve(value);
        if (name === 'startLifetime' || name === 'startSpeed' || name === 'startSize')
          this._system[`${name}Range`] = range;
        else if (range) report(`ParticleSystem.main.${name}.TwoConstants`);
      });
  access(types.MainModule.prototype, 'startColor',
    function () { return new types.MinMaxGradient(this._system.startColor); },
    function (value) { this._system.startColor = value?.color ?? value; });
  for (const name of ['duration', 'loop', 'playOnAwake', 'maxParticles'])
    access(types.MainModule.prototype, name,
      function () { return this._system[name]; },
      function (value) { this._system[name] = value; });
  access(types.MainModule.prototype, 'simulationSpace',
    function () { return enums.ParticleSystemSimulationSpace[this._system.simulationSpace]; },
    function (value) { this._system.simulationSpace = enumName(
      enums.ParticleSystemSimulationSpace, value, 'Local'); });

  access(types.EmissionModule.prototype, 'enabled',
    function () { return this._system.emissionEnabled; },
    function (value) { this._system.emissionEnabled = Boolean(value); });
  access(types.EmissionModule.prototype, 'rateOverTime',
    function () { return new types.MinMaxCurve(this._system.rateOverTime); },
    function (value) { this._system.rateOverTime = curve(value); });

  access(types.ShapeModule.prototype, 'enabled',
    function () { return this._system.shapeType !== 'None'; },
    function (value) { this._system.shapeType = value
      ? this._system.shapeType === 'None' ? 'Cone' : this._system.shapeType : 'None'; });
  access(types.ShapeModule.prototype, 'shapeType',
    function () { return enums.ParticleSystemShapeType[this._system.shapeType] ?? 0; },
    function (value) {
      const name = enumName(enums.ParticleSystemShapeType, value, 'Cone');
      this._system.shapeType = name;
      if (!['Cone', 'ConeVolume', 'Sphere', 'Box'].includes(name))
        report(`ParticleSystem.shape.${name}`);
    });
  for (const [moduleName, systemName] of [['radius', 'shapeRadius'],
    ['radiusThickness', 'shapeRadiusThickness'], ['angle', 'shapeAngle'],
    ['length', 'shapeLength'], ['scale', 'shapeScale'],
    ['position', 'shapePosition'], ['rotation', 'shapeRotation']])
    access(types.ShapeModule.prototype, moduleName,
      function () { return this._system[systemName]; },
      function (value) { this._system[systemName] = value; });

  access(ParticleSystem.prototype, 'particleCount', function () { return this._particles.length; });
  access(ParticleSystem.prototype, 'isPaused', function () { return this._paused === true; });
  access(ParticleSystem.prototype, 'isStopped', function () {
    return !this.isPlaying && !this._paused;
  });
  access(ParticleSystem.prototype, 'isEmitting', function () {
    return this.isPlaying && this.emissionEnabled
      && (this.duration == null || this.loop
        || this._playElapsed < this.startDelay + this.duration);
  });
  access(ParticleSystem.prototype, 'enableEmission',
    function () { return this.emissionEnabled; },
    function (value) { this.emissionEnabled = Boolean(value); });
  access(ParticleSystem.prototype, 'emissionRate',
    function () { return this.rateOverTime; },
    function (value) { this.rateOverTime = Number(value) || 0; });
  access(ParticleSystem.prototype, 'time', function () { return this._playElapsed; },
    function (value) { this._playElapsed = Math.max(0, Number(value) || 0); });

  access(types.ParticleSystemRenderer.prototype, 'renderMode',
    function () { return this._particleRenderMode ?? enums.ParticleSystemRenderMode.Billboard; },
    function (value) {
      this._particleRenderMode = value;
      if (value !== enums.ParticleSystemRenderMode.Billboard)
        report(`ParticleSystemRenderer.renderMode.${value}`);
    });
  const sharedMaterial = Object.getOwnPropertyDescriptor(
    Object.getPrototypeOf(types.ParticleSystemRenderer.prototype), 'sharedMaterial');
  access(types.ParticleSystemRenderer.prototype, 'sharedMaterial',
    function () { return sharedMaterial.get.call(this); },
    function (value) {
      sharedMaterial.set.call(this, value);
      const effect = this.gameObject?.GetComponent(ParticleSystem);
      if (effect) { effect.particleMaterial = value; effect._syncMaterialTexture(); }
    });
}
