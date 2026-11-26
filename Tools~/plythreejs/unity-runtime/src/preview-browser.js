import { BrowserInput, Cursor, Screen } from './index.js';

/** Browser controls used by every generated Preview, including JU scenes. */
export function createPreviewBrowserInput(canvas, target = globalThis.window) {
  Cursor.ConfigureBrowser({ pointerLock: true, hide: true, pointerTarget: canvas });
  return new BrowserInput(target, { pointerTarget: canvas,
    coordinateSize: () => ({ width: Screen.width, height: Screen.height }) });
}

/** Render a dynamic actor at its interpolated physics pose without changing simulation state. */
export function withPreviewInterpolatedPose(physics, actor, render) {
  const pose = actor && physics.getInterpolatedPose(actor);
  if (!pose) return render();
  const position = actor.transform.position.clone();
  const rotation = actor.transform.rotation.clone();
  actor.transform.position = pose.position;
  actor.transform.rotation = pose.rotation;
  try { return render(); }
  finally {
    actor.transform.position = position;
    actor.transform.rotation = rotation;
  }
}
