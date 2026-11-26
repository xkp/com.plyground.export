import { Application, AssetBundleLoader, BrowserCanvasUI, BrowserToolkitUI,
  Camera, Component, Input, PhysicsSystem, QualitySettings, Screen, Terrain,
  ScriptableObject, StateMachineBehaviour, THREE, UnityRuntime, UnityType,
  PreviewCinemachineComponents, PreviewSplineComponents,
  reportUnsupportedUnityAPI } from './index.js';
import { createPreviewBrowserInput } from './preview-browser.js';
import { installPreviewControllerAnimation } from './preview-controller-animation.js';
import { correctMalbersTerrainLanding,
  stabilizeMalbersGravityTime } from './preview-malbers-gravity.js';
import { fetchHumanoidGzipJson } from './humanoid-raw-curve-clip.js';
import { PreviewSplineArchitectComponents } from './preview-spline-architect.js';

const status = document.querySelector('#status');
const canvas = document.querySelector('#scene');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.outputColorSpace = THREE.SRGBColorSpace;

try {
  const [bundle, scripts] = await Promise.all([
    fetch('./bundle.json').then(response => {
      if (!response.ok) throw new Error(`Scene bundle: HTTP ${response.status}`);
      return response.json();
    }),
    fetch('./scripts.json').then(response => response.ok ? response.json() : []),
  ]);
  if (bundle.qualitySettings) QualitySettings.Configure(bundle.qualitySettings);
  if (bundle.inputBindings) Input.ConfigureBindings({ ...bundle.inputBindings, replace: true });
  Application.ConfigureIdentity(bundle.applicationIdentity
    ?? { unityVersion: bundle.sourceUnityVersion ?? '' });
  UnityType.Clear();
  const types = {};
  const animationPackages = [];
  for (const script of scripts) {
    const module = await import(new URL(script.codeEntry, location.href));
    for (const [name, type] of Object.entries(module.unityTypes ?? {})) {
      if (types[name]) throw new Error(`Duplicate exported C# type: ${name}`);
      types[name] = type;
    }
  }
  const components = { ...PreviewCinemachineComponents, ...PreviewSplineComponents,
    ...PreviewSplineArchitectComponents,
    ...Object.fromEntries(Object.entries(types)
    .filter(([, type]) => typeof type === 'function' && type.prototype instanceof Component)) };
  const scriptableObjects = Object.fromEntries(Object.entries(types)
    .filter(([, type]) => typeof type === 'function' && type.prototype instanceof ScriptableObject));
  const runtime = new UnityRuntime();
  runtime._renderer = renderer;
  let scriptlessJU = false;
  runtime._onCompatibilityFinding = finding => {
    status.hidden = false;
    status.textContent = `${scriptlessJU
      ? 'Visual-only preview: JU gameplay scripts are missing. ' : ''}Preview compatibility: ${finding.api}`
      + `${finding.gameObject ? ` on ${finding.gameObject}` : ''}: ${finding.detail}`;
  };
  if (bundle.scene.roots.some(root => JSON.stringify(root).includes('Unity.Cinemachine.')))
    reportUnsupportedUnityAPI('Unity.Cinemachine', { runtime, system: 'standalone-camera',
      detail: 'Cinemachine blending, noise, impulses, and Deoccluder collision are not yet simulated' });
  for (const asset of bundle.assets) {
    if (asset.kind !== 'scriptableObject' || scriptableObjects[asset.type]) continue;
    scriptableObjects[asset.type] = class UnsupportedSceneScriptableObject extends ScriptableObject {};
    reportUnsupportedUnityAPI(asset.type, { runtime, system: 'standalone-scriptable-object',
      detail: 'ScriptableObject behavior is unavailable in this browser export; serialized data is retained' });
  }
  const serializedTypes = new Set();
  const findSerializedTypes = value => {
    if (!value || typeof value !== 'object') return;
    if (typeof value.$serializedType === 'string') serializedTypes.add(value.$serializedType);
    for (const child of Object.values(value)) findSerializedTypes(child);
  };
  findSerializedTypes(bundle.assets);
  findSerializedTypes(bundle.scene);
  for (const name of serializedTypes) {
    if (types[name]) continue;
    const generic = /^(.+)`(\d+)\[\[/.exec(name);
    if (generic) {
      const definition = Object.entries(types).find(([key]) =>
        key.startsWith(`${generic[1]}<`) && key.endsWith('>')
        && key.slice(generic[1].length + 1, -1).split(',').length === Number(generic[2]));
      if (definition) { types[name] = definition[1]; continue; }
    }
    types[name] = class UnsupportedSerializedType {};
    reportUnsupportedUnityAPI(name, { runtime, system: 'standalone-serialized-type',
      detail: 'Serialized type behavior is unavailable in this browser export; field data is retained' });
  }
  const findAnimatorBehaviours = value => {
    if (!value || typeof value !== 'object') return;
    for (const behaviour of Array.isArray(value.behaviours) ? value.behaviours : []) {
      if (typeof behaviour.type !== 'string' || types[behaviour.type]) continue;
      types[behaviour.type] = class UnsupportedStateMachineBehaviour extends StateMachineBehaviour {};
      reportUnsupportedUnityAPI(behaviour.type, { runtime, system: 'standalone-state-machine-behaviour',
        detail: 'Animator state behavior is unavailable in this browser export' });
    }
    for (const child of Object.values(value)) findAnimatorBehaviours(child);
  };
  findAnimatorBehaviours(bundle.assets);
  findAnimatorBehaviours(bundle.scene);
  UnityType.Register(Object.fromEntries(Object.entries(types)
    .filter(([name]) => !UnityType.GetType(name))));
  for (const phase of [4, 2, 3, 1]) runtime.runRuntimeInitializers(types, phase);
  runtime.scene.background = new THREE.Color(0x25303b);
  await PhysicsSystem.create(runtime);
  const stubbedComponents = new Set();
  const loaded = await new AssetBundleLoader(runtime, { components, scriptableObjects,
    types, assetConcurrency: 6, unknownComponentFactory: name => {
      if (!stubbedComponents.has(name)) {
        stubbedComponents.add(name);
        reportUnsupportedUnityAPI(name, { runtime, system: 'standalone-component',
          detail: 'Component behavior is unavailable in this browser export; serialized object is retained' });
      }
      return class UnsupportedSceneComponent extends Component {};
    } }).load(bundle, {
    baseUrl: new URL('./', location.href).href,
    loadingPolicy: bundle.resources?.References ? 'scene-and-resources' : 'scene',
  });
  const prepareBow = component => {
    if (component?.constructor?.__unityFullName !== 'MalbersAnimations.Weapons.MBow'
      || !component.BowIsSet || typeof component.SerializeBow !== 'function') return;
    if ((component.UpperBnInitRot?.length ?? 0) < (component.UpperBn?.length ?? 0)
      || (component.LowerBnInitRot?.length ?? 0) < (component.LowerBn?.length ?? 0))
      component.SerializeBow();
  };
  const animalType = types['MalbersAnimations.Controller.MAnimal'];
  const preparedAnimals = new Set();
  const terrains = runtime._components().filter(component => component instanceof Terrain);
  const prepareAnimal = component => {
    if (animalType && component instanceof animalType) {
      stabilizeMalbersGravityTime(component);
      preparedAnimals.add(component);
    }
  };
  for (const component of runtime._components()) {
    prepareBow(component);
    prepareAnimal(component);
  }
  runtime.addSystem({ onGameObjectAdded(root) {
    const bowType = types['MalbersAnimations.Weapons.MBow'];
    if (bowType) for (const bow of root.GetComponentsInChildren(bowType, true)) prepareBow(bow);
    if (animalType) for (const animal of root.GetComponentsInChildren(animalType, true))
      prepareAnimal(animal);
  }, afterLateUpdate() {
    for (const animal of preparedAnimals) {
      if (animal._destroyed || animal.gameObject?._destroyed) {
        preparedAnimals.delete(animal);
        continue;
      }
      // Malbers creates a Default StanceID even for actors with no stance entries.
      // Its Start reset then leaves that ID null when Stance_Get cannot find an entry.
      if ((animal.Stances?.Count ?? animal.Stances?.length) === 0
        && animal.Stance == null && animal.DefaultStanceID)
        animal.currentStance = animal.DefaultStanceID;
      correctMalbersTerrainLanding(animal, terrains);
    }
  } });
  for (const script of scripts) {
    if (!script.animationEntry) continue;
    const animationUrl = new URL(script.animationEntry, location.href);
    const response = await fetch(animationUrl);
    if (!response.ok) throw new Error(`Humanoid animation: HTTP ${response.status}`);
    const animation = await response.json();
    const rawData = animation.rawMotionEntry && animation.calibrationEntry
      && animation.controllers?.some(controller => controller.rawReadyClips?.length)
      ? { rawMotion: await fetchHumanoidGzipJson(new URL(animation.rawMotionEntry, animationUrl)),
        calibrations: await fetchHumanoidGzipJson(new URL(animation.calibrationEntry, animationUrl)) }
      : null;
    const count = installPreviewControllerAnimation(loaded.roots, animation, rawData);
    if (!count) throw new Error('Humanoid animation did not bind to any scene actor');
    animationPackages.push({ animation, rawData });
  }
  if (animationPackages.length) runtime.addSystem({
    onGameObjectAdded(root) {
      for (const { animation, rawData } of animationPackages)
        installPreviewControllerAnimation([root], animation, rawData, true);
    }
  });
  runtime.runRuntimeInitializers(types, 0);
  if (!runtime.scene.children.some(object => object.isLight)) {
    runtime.scene.add(new THREE.HemisphereLight(0xffffff, 0x667788, 2));
    runtime.scene.add(new THREE.AmbientLight(0xffffff, 0.35));
  }
  const fallbackCamera = new THREE.PerspectiveCamera(60, 1, 0.1, 2000);
  fallbackCamera.position.set(0, 3, 6);
  fallbackCamera.lookAt(0, 0, 0);
  const sceneCamera = [...loaded.components.values()].find(component =>
    component instanceof Camera && component.isActiveAndEnabled) ?? null;
  const resize = () => {
    const width = Screen._requestedResolution?.width ?? innerWidth;
    const height = Screen._requestedResolution?.height ?? innerHeight;
    renderer.setSize(width, height, false);
    Screen._ConfigureViewport(width, height);
    fallbackCamera.aspect = width / height;
    fallbackCamera.updateProjectionMatrix();
    (Camera.main ?? sceneCamera)?.SetViewport(width, height);
  };
  Screen._BindRenderSurface(resize);
  addEventListener('resize', resize);
  resize();
  const browserInput = createPreviewBrowserInput(canvas);
  runtime.addSystem(browserInput);
  const canvasUI = runtime.addSystem(new BrowserCanvasUI(document.body,
    { fitReferenceResolution: true }));
  const toolkitUI = runtime.addSystem(new BrowserToolkitUI(document.body));
  browserInput.addUIRoot(canvasUI.root);
  browserInput.addUIRoot(toolkitUI.root);
  const hasJUController = node => (node.components ?? [])
    .some(component => component.type === 'JUTPS.JUCharacterController')
    || (node.children ?? []).some(hasJUController);
  scriptlessJU = scripts.length === 0 && bundle.scene.roots.some(hasJUController);
  const previewCharacter = scriptlessJU
    ? [...loaded.objects.values()].find(object => object.name === 'TPS Character') : null;
  if (scriptlessJU) {
    canvasUI.root.style.display = 'none';
    toolkitUI.root.style.display = 'none';
  }
  window.__standalonePreview = { runtime, loaded, types, bundle, stubbedComponents,
    uiDiagnostics: canvasUI.diagnostics, toolkitDiagnostics: toolkitUI.diagnostics };
  const diagnostics = bundle.diagnostics ?? [];
  status.hidden = !scriptlessJU && diagnostics.length === 0
    && runtime.compatibilityFindings.length === 0;
  if (!status.hidden) status.textContent = `${scriptlessJU
    ? 'Visual-only preview: JU gameplay scripts are missing; camera, controls, and UI are inactive. '
    : ''}${diagnostics.length} export finding(s), `
    + `${stubbedComponents.size} component type(s) stubbed. `
    + diagnostics.slice(0, 3).map(item => typeof item === 'string' ? item : item.message).join('; ');
  runtime.start({ render: scene => {
    if (previewCharacter) {
      const target = previewCharacter.transform.position;
      fallbackCamera.position.set(target.x, target.y + 2.3, target.z + 5);
      fallbackCamera.lookAt(target.x, target.y + 1.25, target.z);
      fallbackCamera.updateProjectionMatrix();
      renderer.render(scene, fallbackCamera);
      return;
    }
    const activeCamera = Camera.main ?? sceneCamera;
    if (activeCamera?.isActiveAndEnabled) {
      activeCamera.GetComponent(PreviewCinemachineComponents[
        'Unity.Cinemachine.CinemachineBrain'])?.ApplyToCamera();
      activeCamera.SetViewport(Screen.width, Screen.height);
      activeCamera.useUnityRenderSpace = true;
      activeCamera.Render(renderer, scene);
    } else renderer.render(scene, fallbackCamera);
  } });
} catch (error) {
  status.hidden = false;
  status.textContent = `Scene preview failed: ${error.stack ?? error.message}`;
  throw error;
}
