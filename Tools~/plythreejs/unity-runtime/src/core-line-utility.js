function squaredDistanceToSegment(point, start, end) {
  let span = 0, dot = 0;
  for (const axis of ['x', 'y', 'z']) {
    const delta = (end[axis] ?? 0) - (start[axis] ?? 0);
    span += delta * delta;
    dot += ((point[axis] ?? 0) - (start[axis] ?? 0)) * delta;
  }
  const t = span === 0 ? 0 : Math.max(0, Math.min(1, dot / span));
  let distance = 0;
  for (const axis of ['x', 'y', 'z']) {
    const offset = (point[axis] ?? 0) - (start[axis] ?? 0)
      - t * ((end[axis] ?? 0) - (start[axis] ?? 0));
    distance += offset * offset;
  }
  return distance;
}

function keptIndices(points, tolerance) {
  if (!Number.isFinite(tolerance) || tolerance < 0)
    throw new RangeError('LineUtility.Simplify requires a nonnegative finite tolerance');
  if (points.length === 0) return [];
  if (points.length === 1) return [0];
  const keep = new Set([0, points.length - 1]);
  const segments = [[0, points.length - 1]];
  const threshold = tolerance * tolerance;
  while (segments.length) {
    const [start, end] = segments.pop();
    let farthest = -1, distance = threshold;
    for (let index = start + 1; index < end; index++) {
      const current = squaredDistanceToSegment(points[index], points[start], points[end]);
      if (current > distance) { distance = current; farthest = index; }
    }
    if (farthest >= 0) {
      keep.add(farthest);
      segments.push([start, farthest], [farthest, end]);
    }
  }
  return [...keep].sort((a, b) => a - b);
}

export class LineUtility {
  static SimplifyIndices(points, tolerance, output) {
    const selected = keptIndices(points, tolerance);
    output.length = 0;
    output.push(...selected);
  }
  static SimplifyPoints(points, tolerance, output) {
    const selected = keptIndices(points, tolerance);
    output.length = 0;
    for (const index of selected) output.push(points[index].clone());
  }
}
