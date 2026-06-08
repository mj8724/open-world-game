/**
 * 3D 地图常量配置 — 所有魔法数字集中于此，便于迁移 Unity3D
 */

// ─── 坐标缩放 ───
/** 后端坐标 → 3D 世界坐标的缩放因子 */
export const WORLD_SCALE = 0.05;
/** 高度缩放因子 */
export const ELEVATION_SCALE = 1.0;

// ─── 地形配置 ───
export const TERRAIN_CONFIG = {
  PLAINS:   { elevation: 0,    color: 0x4a7c3f, cssColor: '#4a7c3f', roughness: 0.8  },
  MOUNTAIN: { elevation: 8,    color: 0x8b8b8b, cssColor: '#8b8b8b', roughness: 1.0  },
  HILLS:    { elevation: 4,    color: 0x6b8e4e, cssColor: '#6b8e4e', roughness: 0.9  },
  COASTAL:  { elevation: -0.5, color: 0xc2b280, cssColor: '#c2b280', roughness: 0.6  },
  FOREST:   { elevation: 1,    color: 0x2d5a1e, cssColor: '#2d5a1e', roughness: 1.0  },
  DESERT:   { elevation: 0.5,  color: 0xd4a855, cssColor: '#d4a855', roughness: 0.7  },
  RUINS:    { elevation: 1.5,  color: 0x6b5b4f, cssColor: '#6b5b4f', roughness: 0.85 },
  SWAMP:    { elevation: -0.3, color: 0x3a5c3a, cssColor: '#3a5c3a', roughness: 0.95 },
};

// ─── 势力颜色（16 进制整数，Three.js 用） ───
export const FACTION_COLORS_3D = {
  PLAYER:  0x2563EB,
  AI:      0xDC2626,
  NEUTRAL: 0x9CA3AF,
};

/** 势力 CSS 颜色 */
export const FACTION_CSS_COLORS = {
  PLAYER:  '#2563EB',
  AI:      '#DC2626',
  NEUTRAL: '#9CA3AF',
};

// ─── 建筑模型尺寸 ───
export const BUILDING_MODELS = {
  FARM:          { w: 1.2, d: 1.0, hBase: 0.4,  hPerLevel: 0.1,  color: 0xd4a017, roofColor: 0x8B4513 },
  MINE:          { w: 1.0, d: 1.0, hBase: 0.6,  hPerLevel: 0.15, color: 0x696969, roofColor: 0x4a4a4a },
  ARSENAL:       { w: 1.4, d: 1.0, hBase: 0.5,  hPerLevel: 0.2,  color: 0x8B0000, roofColor: 0x5c0000 },
  ORACLE_BEACON: { w: 0.6, d: 0.6, hBase: 1.2,  hPerLevel: 0.3,  color: 0xDAA520, roofColor: 0xFFD700 },
  HALL:          { w: 1.6, d: 1.4, hBase: 0.8,  hPerLevel: 0,    color: 0xF5DEB3, roofColor: 0x8B4513 },
};

// ─── 城墙参数 ───
export const WALL_CONFIG = {
  thickness: 0.15,
  heightBase: 0.8,
  heightPerLevel: 0.4,
  battlementWidth: 0.1,
  battlementHeight: 0.15,
  battlementSpacing: 0.3,
  cornerTowerSize: 0.4,
  cornerTowerHeight: 1.2,
};

// ─── 城市参数 ───
export const CITY_CONFIG = {
  influenceRadius: 5.0,   // 城市影响半径（3D 世界单位）
  platformHeight: 0.1,
  flagPoleHeight: 1.5,
  flagSize: 0.4,
  selectionRingRadius: 3.0,
  selectionRingHeight: 0.05,
};

// ─── 道路参数 ───
export const ROAD_CONFIG = {
  ROAD:    { width: 0.3, color: 0x8B7355, segments: 20 },
  TRAIL:   { width: 0.15, color: 0xA0937D, segments: 20 },
  RAILWAY: { width: 0.25, color: 0x4a4a4a, segments: 20, railWidth: 0.03, tieSpacing: 0.3 },
};

// ─── 军队参数 ───
export const ARMY_CONFIG = {
  soldierHeight: 0.25,
  soldierRadius: 0.08,
  formationSpacing: 0.2,
  maxVisibleSoldiers: 12,
  healthBarWidth: 0.5,
  healthBarHeight: 0.05,
};

// ─── 相机默认值 ───
export const CAMERA_DEFAULTS = {
  fov: 60,
  near: 0.1,
  far: 500,
  polarAngle: Math.PI / 4,       // 45° 俯视
  azimuthalAngle: -Math.PI / 4,
  distance: 30,
  minDistance: 5,
  maxDistance: 80,
  minPolarAngle: 0.15,           // 接近正上方
  maxPolarAngle: Math.PI / 2.2,  // 不太水平
  dampingFactor: 0.1,
  focusLerpSpeed: 0.05,
};

// ─── 小地图 ───
export const MINIMAP_CONFIG = {
  width: 200,
  height: 150,
  borderWidth: 2,
  borderColor: '#4a5568',
  backgroundColor: 0x1a1a2e,
  viewportColor: 0xffffff,
  viewportOpacity: 0.3,
};

// ─── 天空/环境 ───
export const ENV_CONFIG = {
  skyColor: 0x87CEEB,
  groundColor: 0x4a7c3f,
  fogNear: 40,
  fogFar: 80,
  ambientLightIntensity: 0.5,
  directionalLightIntensity: 0.8,
  hemisphereSkyColor: 0x87CEEB,
  hemisphereGroundColor: 0x4a7c3f,
  hemisphereIntensity: 0.3,
  shadowMapSize: 2048,
  shadowCameraSize: 50,
};

// ─── 地形生成 ───
export const TERRAIN_GEN = {
  gridSize: 200,          // 地形网格细分数
  worldSize: 60,          // 地形世界空间大小
  heightBlendPower: 2,    // IDW 插值幂次
  maxInfluenceDistance: 30, // 节点影响最大距离
  waterLevel: -0.2,       // 水面高度
  waterColor: 0x1e90ff,
  waterOpacity: 0.6,
  decorationDensity: {
    MOUNTAIN: 8,  // 每节点岩石数
    FOREST: 15,   // 每节点树木数
    DESERT: 5,    // 每节点装饰数
    SWAMP: 6,     // 每节点水洼数
    RUINS: 4,     // 每节点碎石数
  },
};

// ─── 野外实体 ───
export const WILD_ENTITY_CONFIG = {
  RESOURCE: {
    IRON: { color: 0x808080, size: 0.4, sparkleColor: 0xC0C0C0 },
    FOOD: { color: 0x90EE90, size: 0.3, rows: 3, cols: 3 },
    AMMO: { color: 0x8B4513, size: 0.25 },
  },
  STRUCTURE: {
    RUINS:   { color: 0x6b5b4f, height: 1.5, pillarCount: 4 },
    OUTPOST: { color: 0x8B7355, height: 2.0 },
    SHRINE:  { color: 0xDAA520, height: 1.0, beamColor: 0xFFD700 },
  },
  flagPoleHeight: 1.0,
  captureRadius: 2.0,
};
