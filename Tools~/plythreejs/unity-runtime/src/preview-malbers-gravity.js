// Malbers stores gravity age as an update count, then multiplies it by the
// current frame's DeltaTime. Browser frames can vary substantially in a heavy
// preview, making that product jump backwards and forwards while airborne.
// Keep the script's public counter contract but accumulate its elapsed time.
const installed = new WeakSet();
const lastPosition = new WeakMap();

export function stabilizeMalbersGravityTime(animal) {
  if (!animal || installed.has(animal)
    || typeof animal.StoredGravityVelocity !== 'function') return false;
  installed.add(animal);
  const delta = () => Number(animal.DeltaTime) > 0 ? Number(animal.DeltaTime) : 1 / 60;
  let elapsed = (Number(animal.GravityTime) || 0) * delta();
  Object.defineProperty(animal, 'GravityTime', {
    configurable: true,
    get() { return elapsed / delta(); },
    set(value) {
      const next = Number(value) || 0;
      const current = elapsed / delta();
      if (Math.abs(next - (current + 1)) < 1e-4) elapsed += delta();
      else if (Math.abs(next - (current - 1)) < 1e-4) elapsed -= delta();
      else elapsed = next * delta();
    },
  });
  return true;
}

export function correctMalbersTerrainLanding(animal, terrains) {
  if (!animal?.isActiveAndEnabled || !Array.isArray(terrains)) return false;
  const position = animal.transform.position;
  const previous = lastPosition.get(animal);
  lastPosition.set(animal, position.clone());
  if (!previous || animal.Grounded) return false;
  for (const terrain of terrains) {
    if (!terrain.isActiveAndEnabled || !terrain.terrainData) continue;
    const local = terrain.gameObject.object3D.worldToLocal(position.clone());
    const size = terrain.terrainData.size;
    if (local.x < 0 || local.z < 0 || local.x > size.x || local.z > size.z) continue;
    const floor = terrain.SampleHeight(position);
    const depth = floor - position.y;
    if (depth < 0.015 || depth > 0.35 || previous.y < floor - 0.015) continue;
    const corrected = position.clone();
    corrected.y = floor;
    animal.transform.position = corrected;
    animal.RB.position = corrected;
    animal.LastPosition = corrected.clone();
    lastPosition.set(animal, corrected);
    return true;
  }
  return false;
}
