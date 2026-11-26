/** Adapt exported Unity tangent-space maps to the Preview mesh UV convention. */
export function orientUnityNormalMaps(scene) {
  scene.traverse(object => {
    if (!object.isMesh) return;
    for (const material of Array.isArray(object.material) ? object.material : [object.material]) {
      if (!material?.normalMap || !material.userData?.unityNormalFlipY) continue;
      material.normalScale.y *= -1;
      material.needsUpdate = true;
    }
  });
}
