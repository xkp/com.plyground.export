import { spawn } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdir, readFile, stat } from 'node:fs/promises';
import { dirname, isAbsolute, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const transpilerProject = resolve(dirname(fileURLToPath(import.meta.url)),
  '../tools/UnityTranspiler/UnityTranspiler.csproj');

function fieldValue(field, bundle) {
  switch (field.kind) {
    case 'Boolean': return field.value === 'true';
    case 'Integer': case 'Float': case 'Enum': case 'LayerMask': return Number(field.value);
    case 'String': return field.value;
    case 'Vector2': case 'Vector3': case 'Quaternion': case 'Color': {
      const parsed = JSON.parse(field.value);
      const keys = field.kind === 'Color' ? ['r', 'g', 'b', 'a']
        : field.kind === 'Quaternion' ? ['x', 'y', 'z', 'w']
          : field.kind === 'Vector3' ? ['x', 'y', 'z'] : ['x', 'y'];
      const values = keys.map(key => parsed[key] ?? (key === 'w' || key === 'a' ? 1 : 0));
      return { [`$${field.kind.toLowerCase()}`]: values };
    }
    case 'ObjectReference': {
      if (field.value === 'null') return null;
      const target = bundle.scene.roots.find(root => root.id === `item:${field.targetItemId}`);
      if (!target) throw new Error(`Preview binding target item is missing: ${field.targetItemId}`);
      if (field.value === 'item') return { $ref: target.id };
      if (field.value.startsWith('component:')) {
        const type = field.value.slice('component:'.length);
        const component = target.components.find(value => value.type === type);
        if (!component) throw new Error(`Preview binding target component is missing: ${type} on ${field.targetItemId}`);
        return { $ref: component.id };
      }
      throw new Error(`Preview asset reference needs an exported asset mapping: ${field.value}`);
    }
    default: throw new Error(`Preview does not support binding field kind ${field.kind} (${field.path})`);
  }
}

function setField(properties, name, value) {
  const parts = name.split('.');
  let target = properties;
  for (const part of parts.slice(0, -1)) {
    if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(part))
      throw new Error(`Preview does not support indexed binding field ${name}`);
    target[part] ??= {};
    target = target[part];
  }
  const last = parts.at(-1);
  if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(last))
    throw new Error(`Preview does not support indexed binding field ${name}`);
  target[last] = value;
}

/** Attach canonical item-root C# bindings to the materialized scene. */
export async function applyVariationBindings(projectRoot, project, bundle) {
  const path = project.inputs?.code?.bundle;
  if (!path) return { sources: [], bindings: [] };
  const file = resolve(projectRoot, path);
  const inside = relative(projectRoot, file);
  if (isAbsolute(path) || inside === '..' || inside.startsWith(`..${sep}`) || isAbsolute(inside))
    throw new Error(`Preview code bundle path escapes project: ${path}`);
  const record = JSON.parse((await readFile(file, 'utf8')).replace(/^\uFEFF/, ''));
  if (record.bundle?.schema !== 'plyground.coding.bundle/v1' || !Array.isArray(record.bundle.sources)
    || !Array.isArray(record.bundle.bindings)) throw new Error('Invalid Preview canonical code bundle');
  const revision = createHash('sha256').update(JSON.stringify(record.bundle)).digest('hex');
  if (record.revision !== revision) throw new Error('Preview canonical code bundle revision mismatch');
  const sources = record.bundle.sources;
  for (const source of sources) {
    const name = source.path;
    if (typeof name !== 'string' || !/^Assets\/plyground\/Features\/[A-Za-z0-9_./-]+\.cs$/.test(name)
      || name.split('/').includes('..')) throw new Error(`Invalid Preview C# path: ${name}`);
    const sourceFile = resolve(projectRoot, 'code', name);
    if (!(await stat(sourceFile).catch(() => null))?.isFile())
      throw new Error(`Preview C# source is missing: ${name}`);
    if ((await readFile(sourceFile)).toString('base64') !== source.contentBase64
      || (await readFile(`${sourceFile}.meta`).catch(() => null))?.toString('base64') !== source.metaBase64)
      throw new Error(`Preview C# source differs from the canonical bundle: ${name}`);
  }
  for (const binding of record.bundle.bindings) {
    if (!sources.some(source => source.path === binding.scriptPath))
      throw new Error(`Preview binding source is missing: ${binding.scriptPath}`);
    const root = bundle.scene.roots.find(value => value.id === `item:${binding.itemId}`);
    if (!root) throw new Error(`Preview binding item is missing: ${binding.itemId}`);
    const id = `variation:${binding.itemId}:${binding.typeName}`;
    if (root.components.some(value => value.id === id || value.type === binding.typeName))
      throw new Error(`Duplicate Preview component binding: ${binding.typeName} on ${binding.itemId}`);
    root.components.push({ id, type: binding.typeName, enabled: binding.enabled, properties: {} });
  }
  for (const binding of record.bundle.bindings) {
    const root = bundle.scene.roots.find(value => value.id === `item:${binding.itemId}`);
    const component = root.components.find(value => value.id === `variation:${binding.itemId}:${binding.typeName}`);
    for (const field of binding.fields)
      setField(component.properties, field.path, fieldValue(field, bundle));
  }
  return { sources, bindings: record.bundle.bindings };
}

async function run(command, args) {
  await new Promise((done, fail) => {
    const child = spawn(command, args, { windowsHide: true, stdio: ['ignore', 'ignore', 'pipe'] });
    let stderr = '';
    child.stderr.setEncoding('utf8').on('data', chunk => { stderr += chunk; });
    child.on('error', fail);
    child.on('close', code => code === 0 ? done() : fail(new Error(`Preview C# transpilation failed: ${stderr.trim()}`)));
  });
}

/** Transpile copied C# sources using the local Unity API stubs, without opening Unity. */
export async function transpileVariationSources(projectRoot, outputRoot, sources, moduleRecords = []) {
  if (!sources.length) return null;
  const codeEntry = 'variation/code/module.mjs';
  const output = resolve(outputRoot, codeEntry);
  await mkdir(dirname(output), { recursive: true });
  const args = process.env.PREVIEW_TRANSPILER_DLL
    ? [process.env.PREVIEW_TRANSPILER_DLL, '--preview-sources', resolve(projectRoot, 'code/Assets'), output, '@plythreejs/runtime']
    : ['run', '--project', transpilerProject, '--', '--preview-sources', resolve(projectRoot, 'code/Assets'), output, '@plythreejs/runtime'];
  for (const record of moduleRecords) {
    const manifestPath = resolve(projectRoot, record.manifest);
    const manifestRelative = relative(projectRoot, manifestPath);
    if (isAbsolute(record.manifest) || manifestRelative === '..' || manifestRelative.startsWith(`..${sep}`)
      || isAbsolute(manifestRelative)) throw new Error('Preview module manifest escapes project');
    const manifest = JSON.parse((await readFile(manifestPath, 'utf8')).replace(/^\uFEFF/, ''));
    if (!manifest.solution?.manifest) continue;
    const moduleRoot = resolve(dirname(manifestPath), '..');
    const solutionPath = resolve(moduleRoot, manifest.solution.manifest);
    const solutionRelative = relative(moduleRoot, solutionPath);
    if (isAbsolute(manifest.solution.manifest) || solutionRelative === '..'
      || solutionRelative.startsWith(`..${sep}`) || isAbsolute(solutionRelative))
      throw new Error('Preview module solution escapes module');
    args.push('--module-solution', solutionPath);
  }
  await run('dotnet', args);
  return { moduleId: 'variation', revision: `sha256:${createHash('sha256').update(await readFile(output)).digest('hex')}`,
    codeEntry };
}
