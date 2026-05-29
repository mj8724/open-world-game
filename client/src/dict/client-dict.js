/**
 * ClientDict — 客户端数据字典缓存
 * 提供建筑/科技/资源的元数据查询
 */

const BUILDINGS = {
  FARM:          { id:'FARM',          icon:'🌱', maxLevel:5 },
  MINE:          { id:'MINE',          icon:'⛰️', maxLevel:5 },
  ARSENAL:       { id:'ARSENAL',       icon:'🏭', maxLevel:3 },
  WALL:          { id:'WALL',          icon:'🏰', maxLevel:3 },
  ORACLE_BEACON: { id:'ORACLE_BEACON', icon:'📡', maxLevel:5 },
};

const RESOURCES = {
  FOOD: { id:'FOOD', icon:'🌾', color:'#16A34A' },
  IRON: { id:'IRON', icon:'⛏️', color:'#64748B' },
  AMMO: { id:'AMMO', icon:'💥', color:'#D97706' },
};

const TRANSPORTS = {
  PORTER: { id:'PORTER', icon:'🧺', capacity:20, speed:1, costFood:5, costIron:0, buildTicks:3, maintenanceFood:1, maintenanceIron:0, requiredTech:'TRANSIT_ROADS' },
  CARRIAGE: { id:'CARRIAGE', icon:'🐎', capacity:60, speed:2, costFood:10, costIron:15, buildTicks:8, maintenanceFood:1, maintenanceIron:1, requiredTech:'TRANSIT_WAGONS' },
  TRAIN: { id:'TRAIN', icon:'🚂', capacity:200, speed:4, costFood:20, costIron:50, buildTicks:15, maintenanceFood:2, maintenanceIron:2, requiredTech:'TRANSIT_RAILWAY' },
};

const UNITS = {
  MILITIA: { id:'MILITIA', icon:'🛡️', attack:3, defense:2, recruitCostFood:10, recruitCostIron:5, upkeepFood:1, requiredTech:null },
  SWORDSMAN: { id:'SWORDSMAN', icon:'⚔️', attack:8, defense:5, recruitCostFood:20, recruitCostIron:15, upkeepFood:2, requiredTech:'WEAPON_BLADES' },
  MUSKETEER: { id:'MUSKETEER', icon:'🔫', attack:12, defense:3, recruitCostFood:25, recruitCostIron:25, upkeepFood:2, requiredTech:'WEAPON_GUNPOWDER' },
  MAXIM_GUN: { id:'MAXIM_GUN', icon:'💥', attack:30, defense:2, recruitCostFood:40, recruitCostIron:60, upkeepFood:3, requiredTech:'WEAPON_MACHINEGUN' },
};

const TECHS = {
  SCAVENGING:     { id:'SCAVENGING',     category:'BUILD',   prereq:[], default:true },
  BUILD_MASONRY:  { id:'BUILD_MASONRY',  category:'BUILD',   prereq:[] },
  BUILD_ARCHITECTURE:{ id:'BUILD_ARCHITECTURE', category:'BUILD', prereq:['BUILD_MASONRY'] },
  BUILD_ENGINEERING: { id:'BUILD_ENGINEERING', category:'BUILD', prereq:['BUILD_ARCHITECTURE'] },
  TRANSIT_ROADS:  { id:'TRANSIT_ROADS',  category:'TRANSIT', prereq:[], default:true },
  TRANSIT_WAGONS: { id:'TRANSIT_WAGONS', category:'TRANSIT', prereq:['TRANSIT_ROADS'] },
  TRANSIT_RAILWAY:{ id:'TRANSIT_RAILWAY',category:'TRANSIT', prereq:['TRANSIT_WAGONS'] },
  FOOD_IRRIGATION:{ id:'FOOD_IRRIGATION',category:'FOOD',    prereq:[] },
  FOOD_FERTILIZER:{ id:'FOOD_FERTILIZER',category:'FOOD',    prereq:['FOOD_IRRIGATION'] },
  FOOD_MECHANIZATION:{ id:'FOOD_MECHANIZATION', category:'FOOD', prereq:['FOOD_FERTILIZER'] },
  WEAPON_BLADES:  { id:'WEAPON_BLADES',  category:'WEAPON',  prereq:[] },
  WEAPON_GUNPOWDER:{ id:'WEAPON_GUNPOWDER', category:'WEAPON', prereq:['WEAPON_BLADES'] },
  WEAPON_MACHINEGUN:{ id:'WEAPON_MACHINEGUN', category:'WEAPON', prereq:['WEAPON_GUNPOWDER'] },
};

export function getBuilding(id) { return BUILDINGS[id]; }
export function getTech(id) { return TECHS[id]; }
export function getResource(id) { return RESOURCES[id]; }
export function getTransport(id) { return TRANSPORTS[id]; }
export function getUnit(id) { return UNITS[id]; }
export function getAllBuildings() { return BUILDINGS; }
export function getAllTechs() { return TECHS; }
export function getAllResources() { return RESOURCES; }
export function getAllTransports() { return TRANSPORTS; }
export function getAllUnits() { return UNITS; }
