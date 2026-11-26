/** Replace immutable module output paths with their published Preview URLs. */
export function linkModuleResources(bundle, modules) {
  const linkedPrefixes = new Set();
  for (const module of modules) {
    if (!module.staticAssetBaseUrl) continue;
    const base = new URL(module.staticAssetBaseUrl);
    if (base.protocol !== 'https:' || base.username || base.password || base.search || base.hash
      || !base.pathname.endsWith('/Preview/')) {
      throw new Error(`Invalid static Preview asset URL for ${module.moduleId}`);
    }
    const match = /^(modules\/[^/]+\/Preview\/)(.+)$/.exec(module.codeEntry);
    if (!match) throw new Error(`Invalid Preview module code path for ${module.moduleId}`);
    const [prefix] = match.slice(1);
    linkedPrefixes.add(prefix);
    const link = path => path?.startsWith(prefix)
      ? new URL(path.slice(prefix.length), base).href : path;
    module.codeEntry = link(module.codeEntry);
    if (module.animationEntry) module.animationEntry = link(module.animationEntry);
    for (const asset of bundle.assets ?? []) if (asset.url) asset.url = link(asset.url);
    for (const scene of bundle.scenes ?? []) scene.recordUrl = link(scene.recordUrl);
    delete module.staticAssetBaseUrl;
  }
  return linkedPrefixes;
}
