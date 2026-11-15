import { Animator, AssetBundleLoader, Camera, Component, Object as UnityObject,
  Physics, PhysicsSystem, ScriptableObject,
  PlygroundItem, THREE, UnityRuntime } from './index.js';
import { createPreviewBrowserInput, withPreviewInterpolatedPose } from './preview-browser.js';
import { bindPreviewClothingToBody, installPreviewHumanoidAnimation } from './preview-humanoid-animation.js';
import { PreviewControllerAnimation, installPreviewControllerAnimation } from './preview-controller-animation.js';
import { registerPreviewGameplayMessages } from './preview-gameplay-messages.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';

const status = document.querySelector('#status');
const canvas = document.querySelector('#scene');
const reticleStyle = document.createElement('style');
reticleStyle.textContent = `#aim-reticle{position:fixed;left:50%;top:50%;width:30px;height:30px;
  transform:translate(-50%,-50%);pointer-events:none;display:none;color:white;
  filter:drop-shadow(0 0 1px #000)}
  #aim-reticle i{position:absolute;display:block;background:currentColor}
  #aim-reticle .top,#aim-reticle .bottom{width:2px;height:7px;left:14px}
  #aim-reticle .top{top:1px}#aim-reticle .bottom{bottom:1px}
  #aim-reticle .left,#aim-reticle .right{width:7px;height:2px;top:14px}
  #aim-reticle .left{left:1px}#aim-reticle .right{right:1px}
  #aim-reticle .center{width:2px;height:2px;left:14px;top:14px}`;
document.head.append(reticleStyle);
const reticle = document.createElement('div');
reticle.id = 'aim-reticle';
reticle.setAttribute('aria-hidden', 'true');
reticle.innerHTML = '<i class="top"></i><i class="right"></i><i class="bottom"></i>'
  + '<i class="left"></i><i class="center"></i>';
document.body.append(reticle);
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.outputColorSpace = THREE.SRGBColorSpace;
const browserInput = createPreviewBrowserInput(canvas);

try {
  const [bundle, moduleRecords] = await Promise.all([
    fetch('./bundle.json').then(response => { if (!response.ok) throw new Error('Missing scene bundle'); return response.json(); }),
    fetch('./modules.json').then(response => { if (!response.ok) throw new Error('Missing module registry'); return response.json(); })
  ]);
  const types = { 'Plyground.Pipeline.Runtime.PlygroundItem': PlygroundItem };
  for (const module of moduleRecords) {
    const source = await import(new URL(module.codeEntry, new URL('../../', import.meta.url)));
    if (!source.unityTypes || typeof source.unityTypes !== 'object')
      throw new Error(`Module ${module.moduleId} does not export unityTypes`);
    for (const [name, constructor] of Object.entries(source.unityTypes)) {
      if (types[name]) throw new Error(`Duplicate transpiled type: ${name}`);
      types[name] = constructor;
    }
  }
  Object.assign(types, registerPreviewGameplayMessages());
  const components = Object.fromEntries(Object.entries(types)
    .filter(([, type]) => typeof type === 'function' && type.prototype instanceof Component));
  const scriptableObjects = Object.fromEntries(Object.entries(types)
    .filter(([, type]) => typeof type === 'function' && type.prototype instanceof ScriptableObject));
  const runtime = new UnityRuntime();
  runtime.scene.background = new THREE.Color(0x8bb9d4);
  const environmentGenerator = new THREE.PMREMGenerator(renderer);
  const roomEnvironment = new RoomEnvironment();
  runtime.scene.environment = environmentGenerator.fromScene(roomEnvironment).texture;
  roomEnvironment.dispose();
  environmentGenerator.dispose();
  runtime.scene.add(new THREE.HemisphereLight(0xffffff, 0x8995a6, 2));
  runtime.scene.add(new THREE.AmbientLight(0xffffff, 0.5));
  const sunlight = new THREE.DirectionalLight(0xffffff, 2);
  sunlight.position.set(4, 10, 5);
  runtime.scene.add(sunlight);
  const fillLight = new THREE.DirectionalLight(0xdce8ff, 1.25);
  fillLight.position.set(-5, 5, -6);
  runtime.scene.add(fillLight);
  const physics = await PhysicsSystem.create(runtime);
  const loaded = await new AssetBundleLoader(runtime, { components, scriptableObjects, types,
    assetConcurrency: 6 })
    .load(bundle, { baseUrl: new URL('./', location.href).href });
  const juType = types['JUTPS.JUCharacterController'];
  const groundMask = ['Default', 'Terrain', 'Walls'].reduce((mask, name) =>
    mask | (Number.isInteger(bundle.layers?.[name]) ? 1 << bundle.layers[name] : 0), 0);
  for (const root of loaded.roots) {
    if (!root.assetId?.startsWith('item:')) continue;
    bindPreviewClothingToBody(root);
    if (!juType) continue;
    const controller = root.GetComponent(juType);
    if (!controller) continue;
    for (const component of root._components) {
      const fieldOfView = component.FieldOfView;
      if (!fieldOfView) continue;
      if (!fieldOfView.TargetsLayer?.value && Number.isInteger(bundle.layers?.Character))
        fieldOfView.TargetsLayer.value = 1 << bundle.layers.Character;
      fieldOfView.TargetTags ??= [];
      if (component.constructor.name === 'JU_AI_Zombie' && fieldOfView.TargetTags.length === 0)
        fieldOfView.TargetTags = ['Player'];
    }
    if (!controller.WhatIsGround?.value) controller.WhatIsGround.value = groundMask;
  }
  for (const module of moduleRecords) {
    if (!module.animationEntry) continue;
    const animationUrl = new URL(module.animationEntry, new URL('../../', import.meta.url));
    const response = await fetch(animationUrl);
    if (!response.ok) throw new Error(`Missing Preview animation data: ${module.animationEntry}`);
    const animation = await response.json();
    if (animation.format === 'plyground.preview.humanoid-controllers/v1')
      installPreviewControllerAnimation(loaded.roots, animation);
    else await installPreviewHumanoidAnimation(loaded.roots, animation, types,
      { baseUrl: animationUrl });
  }
  for (const root of loaded.roots) {
    const controller = juType && root.GetComponent(juType);
    const animator = root.GetComponent(Animator);
    if (!controller || !animator || animator.clips.size
      || root.GetComponent(PreviewControllerAnimation)) continue;
    controller.RootMotion = false;
    // A controller without playable clips cannot supply its base pose for IK.
    const lean = root._components.find(component => component.constructor.name === 'BodyLeanInert');
    if (lean) lean.enabled = false;
  }
  const fallbackCamera = new THREE.PerspectiveCamera(60, 1, 0.1, 2000);
  const actors = bundle.scene.roots.filter(root => root.id.startsWith('item:'));
  const player = actors.map(root => loaded.objects.get(root.id))
    .find(object => juType && object?.GetComponent(juType) && !object._components.some(component =>
      /JU_AI_(?:Zombie|PatrolCharacter)/.test(component.constructor.name))) ?? null;
  if (player && juType) player.tag = 'Player';
  const cameraType = types['JUTPS.CameraSystems.TPSCameraController'];
  const cameraAsset = cameraType && player && !Camera.main && bundle.assets.find(asset =>
    asset.kind === 'prefab' && asset.root?.components?.some(component =>
      component.type === 'JUTPS.CameraSystems.TPSCameraController'));
  if (cameraAsset) {
    const cameraRoot = UnityObject.Instantiate(loaded.assets.get(cameraAsset.id));
    const controller = cameraRoot.GetComponent(cameraType);
    controller.TargetToFollow = player.GetComponent(juType)?.HumanoidSpine ?? player.transform;
    player.GetComponent(juType).MyPivotCamera = controller;
  }
  const healthType = types['JU.JUHealth'];
  const playerController = player && juType && player.GetComponent(juType);
  const playerHealth = player && healthType && player.GetComponent(healthType);
  const zombieType = types['JU.CharacterSystem.AI.JU_AI_Zombie'];
  const aimHit = { value: null };
  const updateAimReticle = camera => {
    const weapon = playerController?.RightHandWeapon;
    const visible = camera && weapon?.Shoot_Position && weapon.gameObject.activeInHierarchy
      && !playerController.IsDead && !playerHealth?.IsDead;
    reticle.style.display = visible ? 'block' : 'none';
    if (!visible) return;
    const ray = camera.ScreenPointToRay(new THREE.Vector3(innerWidth / 2, innerHeight / 2, 0));
    Physics.Raycast(ray, aimHit, 500, weapon.RaycastingLayers);
    const target = aimHit.value.collider?.gameObject;
    const zombie = zombieType && target?.GetComponentInParent(zombieType);
    const shotDirection = aimHit.value.point.clone().sub(weapon.Shoot_Position.position).normalize();
    const canAimAtTarget = shotDirection.dot(weapon.Shoot_Position.forward) >= 0.3;
    const onZombie = Boolean(zombie && playerController.IsAiming && canAimAtTarget);
    reticle.style.color = onZombie ? '#ff5252' : 'white';
    reticle.dataset.target = onZombie ? 'zombie' : '';
  };
  const followOffset = new THREE.Vector3(0, 3, -6);
  const followLook = new THREE.Vector3(0, 1.5, 0);
  const playerPosition = new THREE.Vector3();
  const desiredPosition = new THREE.Vector3();
  const desiredLook = new THREE.Vector3();
  const currentLook = new THREE.Vector3();
  let lastRender = performance.now();
  const followPlayer = () => {
    if (!player) return;
    player.object3D.updateWorldMatrix(true, false);
    const position = player.object3D.getWorldPosition(playerPosition);
    const rotation = player.object3D.getWorldQuaternion(new THREE.Quaternion());
    desiredPosition.copy(followOffset).applyQuaternion(rotation).add(position);
    desiredLook.copy(followLook).add(position);
    const now = performance.now();
    const blend = 1 - Math.exp(-8 * Math.min((now - lastRender) / 1000, 0.1));
    lastRender = now;
    fallbackCamera.position.lerp(desiredPosition, blend);
    currentLook.lerp(desiredLook, blend);
    fallbackCamera.lookAt(currentLook);
  };
  if (player) {
    player.object3D.updateWorldMatrix(true, false);
    const position = player.object3D.getWorldPosition(new THREE.Vector3());
    const rotation = player.object3D.getWorldQuaternion(new THREE.Quaternion());
    fallbackCamera.position.copy(followOffset).applyQuaternion(rotation).add(position);
    currentLook.copy(followLook).add(position);
    fallbackCamera.lookAt(currentLook);
  }
  const resize = () => {
    renderer.setSize(innerWidth, innerHeight);
    fallbackCamera.aspect = innerWidth / innerHeight;
    fallbackCamera.updateProjectionMatrix();
    Camera.main?.SetViewport(innerWidth, innerHeight);
  };
  addEventListener('resize', resize);
  resize();
  runtime.addSystem(browserInput);
  // Local validation can inspect the materialized Unity objects without
  // reaching into the renderer or changing transpiled gameplay code.
  window.__plygroundPreview = { runtime, loaded, player, types, moduleRecords };
  status.hidden = true;
  runtime.start({ render: scene => {
    const activeCamera = Camera.main;
    activeCamera?.SetViewport(innerWidth, innerHeight);
    if (activeCamera) activeCamera.useUnityRenderSpace = true;
    updateAimReticle(activeCamera);
    withPreviewInterpolatedPose(physics, player, () => {
      if (activeCamera) {
        activeCamera.Render(renderer, scene);
      } else {
        followPlayer();
        renderer.render(scene, fallbackCamera);
      }
    });
  } });
} catch (error) {
  status.hidden = false;
  status.textContent = `Preview failed: ${error.stack ?? error.message}`;
  throw error;
}
