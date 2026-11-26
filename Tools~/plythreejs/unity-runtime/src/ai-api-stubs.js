// Generated from audits/unity-api-inventory.csv.gz by generate-ai-stubs.mjs.
// These stubs keep unsupported AIModule references importable and report use.
let reportUse = (api, fallback = null) => { console.warn(`${api} is unsupported in Preview`); return fallback; };
export class OnNavMeshPreUpdate {
  constructor() { reportUse('UnityEngine.AI.OnNavMeshPreUpdate'); }
}
export class NavMeshBuildDebugFlags {
  constructor() { reportUse('UnityEngine.AI.NavMeshBuildDebugFlags'); }
}
export class NavMeshBuildDebugSettings {
  constructor() { reportUse('UnityEngine.AI.NavMeshBuildDebugSettings'); }
}
export class NavMeshBuildMarkup {
  constructor() { reportUse('UnityEngine.AI.NavMeshBuildMarkup'); }
}
export class NavMeshBuildSettings {
  constructor() { reportUse('UnityEngine.AI.NavMeshBuildSettings'); }
}
export class NavMeshBuildSourceShape {
  constructor() { reportUse('UnityEngine.AI.NavMeshBuildSourceShape'); }
}
export class NavMeshBuildSource {
  constructor() { reportUse('UnityEngine.AI.NavMeshBuildSource'); }
}
export class NavMeshBuilder {
  constructor() { reportUse('UnityEngine.AI.NavMeshBuilder'); }
}
export class NavMeshCollectGeometry {
  constructor() { reportUse('UnityEngine.AI.NavMeshCollectGeometry'); }
}
export class NavMeshDataInstance {
  constructor() { reportUse('UnityEngine.AI.NavMeshDataInstance'); }
}
export class NavMeshData {
  constructor() { reportUse('UnityEngine.AI.NavMeshData'); }
}
export class NavMeshLinkData {
  constructor() { reportUse('UnityEngine.AI.NavMeshLinkData'); }
}
export class NavMeshLinkInstance {
  constructor() { reportUse('UnityEngine.AI.NavMeshLinkInstance'); }
}
export class NavMeshTriangulation {
  constructor() { reportUse('UnityEngine.AI.NavMeshTriangulation'); }
}
export class ObstacleAvoidanceType {
  constructor() { reportUse('UnityEngine.AI.ObstacleAvoidanceType'); }
}
export const AIUnsupportedTypes = Object.freeze({ OnNavMeshPreUpdate, NavMeshBuildDebugFlags, NavMeshBuildDebugSettings, NavMeshBuildMarkup, NavMeshBuildSettings, NavMeshBuildSourceShape, NavMeshBuildSource, NavMeshBuilder, NavMeshCollectGeometry, NavMeshDataInstance, NavMeshData, NavMeshLinkData, NavMeshLinkInstance, NavMeshTriangulation, ObstacleAvoidanceType });
const members = [
  ["OnNavMeshPreUpdate","method","BeginInvoke",false],
  ["OnNavMeshPreUpdate","method","EndInvoke",false],
  ["NavMeshAgent","method","ActivateCurrentOffMeshLink",false],
  ["NavMeshAgent","method","CalculatePath",false],
  ["NavMeshAgent","method","CompleteOffMeshLink",false],
  ["NavMeshAgent","method","FindClosestEdge",false],
  ["NavMeshAgent","method","GetAreaCost",false],
  ["NavMeshAgent","method","GetLayerCost",false],
  ["NavMeshAgent","method","Move",false],
  ["NavMeshAgent","method","Raycast",false],
  ["NavMeshAgent","method","ResetPath",false],
  ["NavMeshAgent","method","Resume",false],
  ["NavMeshAgent","method","SamplePathPosition",false],
  ["NavMeshAgent","method","SetAreaCost",false],
  ["NavMeshAgent","method","SetDestination",false],
  ["NavMeshAgent","method","SetLayerCost",false],
  ["NavMeshAgent","method","SetPath",false],
  ["NavMeshAgent","method","Stop",false],
  ["NavMeshAgent","method","Warp",false],
  ["NavMeshAgent","property","acceleration",false],
  ["NavMeshAgent","property","agentTypeID",false],
  ["NavMeshAgent","property","angularSpeed",false],
  ["NavMeshAgent","property","areaMask",false],
  ["NavMeshAgent","property","autoBraking",false],
  ["NavMeshAgent","property","autoRepath",false],
  ["NavMeshAgent","property","autoTraverseOffMeshLink",false],
  ["NavMeshAgent","property","avoidancePriority",false],
  ["NavMeshAgent","property","baseOffset",false],
  ["NavMeshAgent","property","currentOffMeshLinkData",false],
  ["NavMeshAgent","property","desiredVelocity",false],
  ["NavMeshAgent","property","destination",false],
  ["NavMeshAgent","property","hasPath",false],
  ["NavMeshAgent","property","height",false],
  ["NavMeshAgent","property","isOnNavMesh",false],
  ["NavMeshAgent","property","isOnOffMeshLink",false],
  ["NavMeshAgent","property","isPathStale",false],
  ["NavMeshAgent","property","isStopped",false],
  ["NavMeshAgent","property","navMeshOwner",false],
  ["NavMeshAgent","property","nextOffMeshLinkData",false],
  ["NavMeshAgent","property","nextPosition",false],
  ["NavMeshAgent","property","obstacleAvoidanceType",false],
  ["NavMeshAgent","property","path",false],
  ["NavMeshAgent","property","pathEndPosition",false],
  ["NavMeshAgent","property","pathPending",false],
  ["NavMeshAgent","property","pathStatus",false],
  ["NavMeshAgent","property","radius",false],
  ["NavMeshAgent","property","remainingDistance",false],
  ["NavMeshAgent","property","speed",false],
  ["NavMeshAgent","property","steeringTarget",false],
  ["NavMeshAgent","property","stoppingDistance",false],
  ["NavMeshAgent","property","updatePosition",false],
  ["NavMeshAgent","property","updateRotation",false],
  ["NavMeshAgent","property","updateUpAxis",false],
  ["NavMeshAgent","property","velocity",false],
  ["NavMeshAgent","property","walkableMask",false],
  ["NavMeshBuildDebugFlags","field","All",true],
  ["NavMeshBuildDebugFlags","field","InputGeometry",true],
  ["NavMeshBuildDebugFlags","field","None",true],
  ["NavMeshBuildDebugFlags","field","PolygonMeshes",true],
  ["NavMeshBuildDebugFlags","field","PolygonMeshesDetail",true],
  ["NavMeshBuildDebugFlags","field","RawContours",true],
  ["NavMeshBuildDebugFlags","field","Regions",true],
  ["NavMeshBuildDebugFlags","field","SimplifiedContours",true],
  ["NavMeshBuildDebugFlags","field","Voxels",true],
  ["NavMeshBuildDebugSettings","property","flags",false],
  ["NavMeshBuildMarkup","property","applyToChildren",false],
  ["NavMeshBuildMarkup","property","area",false],
  ["NavMeshBuildMarkup","property","generateLinks",false],
  ["NavMeshBuildMarkup","property","ignoreFromBuild",false],
  ["NavMeshBuildMarkup","property","overrideArea",false],
  ["NavMeshBuildMarkup","property","overrideGenerateLinks",false],
  ["NavMeshBuildMarkup","property","overrideIgnore",false],
  ["NavMeshBuildMarkup","property","root",false],
  ["NavMeshBuildSettings","method","ValidationReport",false],
  ["NavMeshBuildSettings","property","agentClimb",false],
  ["NavMeshBuildSettings","property","agentHeight",false],
  ["NavMeshBuildSettings","property","agentRadius",false],
  ["NavMeshBuildSettings","property","agentSlope",false],
  ["NavMeshBuildSettings","property","agentTypeID",false],
  ["NavMeshBuildSettings","property","buildHeightMesh",false],
  ["NavMeshBuildSettings","property","debug",false],
  ["NavMeshBuildSettings","property","ledgeDropHeight",false],
  ["NavMeshBuildSettings","property","maxJobWorkers",false],
  ["NavMeshBuildSettings","property","maxJumpAcrossDistance",false],
  ["NavMeshBuildSettings","property","minRegionArea",false],
  ["NavMeshBuildSettings","property","overrideTileSize",false],
  ["NavMeshBuildSettings","property","overrideVoxelSize",false],
  ["NavMeshBuildSettings","property","preserveTilesOutsideBounds",false],
  ["NavMeshBuildSettings","property","tileSize",false],
  ["NavMeshBuildSettings","property","voxelSize",false],
  ["NavMeshBuildSourceShape","field","Box",true],
  ["NavMeshBuildSourceShape","field","Capsule",true],
  ["NavMeshBuildSourceShape","field","Mesh",true],
  ["NavMeshBuildSourceShape","field","ModifierBox",true],
  ["NavMeshBuildSourceShape","field","Sphere",true],
  ["NavMeshBuildSourceShape","field","Terrain",true],
  ["NavMeshBuildSource","property","area",false],
  ["NavMeshBuildSource","property","component",false],
  ["NavMeshBuildSource","property","generateLinks",false],
  ["NavMeshBuildSource","property","shape",false],
  ["NavMeshBuildSource","property","size",false],
  ["NavMeshBuildSource","property","sourceObject",false],
  ["NavMeshBuildSource","property","transform",false],
  ["NavMeshBuilder","method","BuildNavMeshData",true],
  ["NavMeshBuilder","method","Cancel",true],
  ["NavMeshBuilder","method","CollectSources",true],
  ["NavMeshBuilder","method","UpdateNavMeshData",true],
  ["NavMeshBuilder","method","UpdateNavMeshDataAsync",true],
  ["NavMeshCollectGeometry","field","PhysicsColliders",true],
  ["NavMeshCollectGeometry","field","RenderMeshes",true],
  ["NavMeshDataInstance","method","Remove",false],
  ["NavMeshDataInstance","property","owner",false],
  ["NavMeshDataInstance","property","valid",false],
  ["NavMeshData","property","position",false],
  ["NavMeshData","property","rotation",false],
  ["NavMeshData","property","sourceBounds",false],
  ["NavMeshHit","property","distance",false],
  ["NavMeshHit","property","hit",false],
  ["NavMeshHit","property","mask",false],
  ["NavMeshHit","property","normal",false],
  ["NavMeshHit","property","position",false],
  ["NavMeshLinkData","property","agentTypeID",false],
  ["NavMeshLinkData","property","area",false],
  ["NavMeshLinkData","property","bidirectional",false],
  ["NavMeshLinkData","property","costModifier",false],
  ["NavMeshLinkData","property","endPosition",false],
  ["NavMeshLinkData","property","startPosition",false],
  ["NavMeshLinkData","property","width",false],
  ["NavMeshLinkInstance","method","Remove",false],
  ["NavMeshLinkInstance","property","owner",false],
  ["NavMeshLinkInstance","property","valid",false],
  ["NavMeshObstacleShape","field","Box",true],
  ["NavMeshObstacleShape","field","Capsule",true],
  ["NavMeshObstacle","property","carveOnlyStationary",false],
  ["NavMeshObstacle","property","carving",false],
  ["NavMeshObstacle","property","carvingMoveThreshold",false],
  ["NavMeshObstacle","property","carvingTimeToStationary",false],
  ["NavMeshObstacle","property","center",false],
  ["NavMeshObstacle","property","height",false],
  ["NavMeshObstacle","property","radius",false],
  ["NavMeshObstacle","property","shape",false],
  ["NavMeshObstacle","property","size",false],
  ["NavMeshObstacle","property","velocity",false],
  ["NavMeshPathStatus","field","PathComplete",true],
  ["NavMeshPathStatus","field","PathInvalid",true],
  ["NavMeshPathStatus","field","PathPartial",true],
  ["NavMeshPath","method","ClearCorners",false],
  ["NavMeshPath","method","GetCornersNonAlloc",false],
  ["NavMeshPath","property","corners",false],
  ["NavMeshPath","property","status",false],
  ["NavMeshQueryFilter","method","GetAreaCost",false],
  ["NavMeshQueryFilter","method","SetAreaCost",false],
  ["NavMeshQueryFilter","property","agentTypeID",false],
  ["NavMeshQueryFilter","property","areaMask",false],
  ["NavMeshTriangulation","field","areas",false],
  ["NavMeshTriangulation","field","indices",false],
  ["NavMeshTriangulation","field","vertices",false],
  ["NavMeshTriangulation","property","layers",false],
  ["NavMesh","field","AllAreas",true],
  ["NavMesh","field","onPreUpdate",true],
  ["NavMesh","method","AddLink",true],
  ["NavMesh","method","AddNavMeshData",true],
  ["NavMesh","method","AddOffMeshLinks",true],
  ["NavMesh","method","CalculatePath",true],
  ["NavMesh","method","CalculateTriangulation",true],
  ["NavMesh","method","CreateSettings",true],
  ["NavMesh","method","FindClosestEdge",true],
  ["NavMesh","method","GetAreaCost",true],
  ["NavMesh","method","GetAreaFromName",true],
  ["NavMesh","method","GetAreaNames",true],
  ["NavMesh","method","GetLayerCost",true],
  ["NavMesh","method","GetLinkOwner",true],
  ["NavMesh","method","GetNavMeshLayerFromName",true],
  ["NavMesh","method","GetSettingsByID",true],
  ["NavMesh","method","GetSettingsByIndex",true],
  ["NavMesh","method","GetSettingsCount",true],
  ["NavMesh","method","GetSettingsNameFromID",true],
  ["NavMesh","method","IsLinkActive",true],
  ["NavMesh","method","IsLinkOccupied",true],
  ["NavMesh","method","IsLinkValid",true],
  ["NavMesh","method","Raycast",true],
  ["NavMesh","method","RemoveAllNavMeshData",true],
  ["NavMesh","method","RemoveLink",true],
  ["NavMesh","method","RemoveNavMeshData",true],
  ["NavMesh","method","RemoveSettings",true],
  ["NavMesh","method","RestoreNavMesh",true],
  ["NavMesh","method","SamplePosition",true],
  ["NavMesh","method","SetAreaCost",true],
  ["NavMesh","method","SetLayerCost",true],
  ["NavMesh","method","SetLinkActive",true],
  ["NavMesh","method","SetLinkOwner",true],
  ["NavMesh","method","Triangulate",true],
  ["NavMesh","property","avoidancePredictionTime",true],
  ["NavMesh","property","pathfindingIterationsPerFrame",true],
  ["ObstacleAvoidanceType","field","GoodQualityObstacleAvoidance",true],
  ["ObstacleAvoidanceType","field","HighQualityObstacleAvoidance",true],
  ["ObstacleAvoidanceType","field","LowQualityObstacleAvoidance",true],
  ["ObstacleAvoidanceType","field","MedQualityObstacleAvoidance",true],
  ["ObstacleAvoidanceType","field","NoObstacleAvoidance",true],
  ["OffMeshLinkData","property","activated",false],
  ["OffMeshLinkData","property","endPos",false],
  ["OffMeshLinkData","property","linkType",false],
  ["OffMeshLinkData","property","offMeshLink",false],
  ["OffMeshLinkData","property","owner",false],
  ["OffMeshLinkData","property","startPos",false],
  ["OffMeshLinkData","property","valid",false],
  ["OffMeshLinkType","field","LinkTypeDropDown",true],
  ["OffMeshLinkType","field","LinkTypeJumpAcross",true],
  ["OffMeshLinkType","field","LinkTypeManual",true],
  ["OffMeshLink","method","UpdatePositions",false],
  ["OffMeshLink","property","activated",false],
  ["OffMeshLink","property","area",false],
  ["OffMeshLink","property","autoUpdatePositions",false],
  ["OffMeshLink","property","biDirectional",false],
  ["OffMeshLink","property","costOverride",false],
  ["OffMeshLink","property","endTransform",false],
  ["OffMeshLink","property","navMeshLayer",false],
  ["OffMeshLink","property","occupied",false],
  ["OffMeshLink","property","startTransform",false],
];
export function installAIModuleStubs(implementedTypes, report) {
  reportUse = report;
  const types = { ...AIUnsupportedTypes, ...implementedTypes };
  const ownFields = new Map();
  for (const [name, Type] of Object.entries(implementedTypes))
    if (typeof Type === 'function') ownFields.set(name, new Set(Object.keys(new Type())));
  for (const [typeName, kind, name, isStatic] of members) {
    const target = isStatic ? types[typeName] : types[typeName]?.prototype;
    if (!target || name in target || (!isStatic && ownFields.get(typeName)?.has(name))) continue;
    const api = `UnityEngine.AI.${typeName}.${name}`;
    const fallback = /^(GetAll|GetAreas)/.test(name) ? []
      : /^(Is|Has|Calculate|Sample|Raycast|Find|Set|Warp|Reset)/.test(name) ? false : null;
    if (kind === 'method') Object.defineProperty(target, name, {
      value() { return reportUse(api, fallback); }, configurable: true });
    else Object.defineProperty(target, name, {
      get() { return reportUse(api, fallback); },
      set() { reportUse(api); }, configurable: true });
  }
}
