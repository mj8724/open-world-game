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

const TECHS = {
  SCAVENGING:     { id:'SCAVENGING',     category:'BUILD',   prereq:[], default:true },
  BLAST_SMELTING: { id:'BLAST_SMELTING', category:'BUILD',   prereq:['SCAVENGING'] },
  STANDARDIZATION:{ id:'STANDARDIZATION',category:'BUILD',   prereq:['BLAST_SMELTING'] },
  PORTERS:        { id:'PORTERS',        category:'TRANSIT', prereq:[], default:true },
  CARRIAGES:      { id:'CARRIAGES',      category:'TRANSIT', prereq:['PORTERS'] },
  STEAM_RAILWAY:  { id:'STEAM_RAILWAY',  category:'TRANSIT', prereq:['CARRIAGES','STANDARDIZATION'] },
  SHANTY_TOWN:    { id:'SHANTY_TOWN',    category:'FOOD',    prereq:[] },
  AGRI_NETWORK:   { id:'AGRI_NETWORK',   category:'FOOD',    prereq:['SHANTY_TOWN'] },
  CANNING:        { id:'CANNING',        category:'FOOD',    prereq:['AGRI_NETWORK','BLAST_SMELTING'] },
  BLACKSMITHING:  { id:'BLACKSMITHING',  category:'WEAPON',  prereq:[] },
  GUNPOWDER:      { id:'GUNPOWDER',      category:'WEAPON',  prereq:['BLACKSMITHING','BLAST_SMELTING'] },
  AUTO_FIREARMS:  { id:'AUTO_FIREARMS',  category:'WEAPON',  prereq:['GUNPOWDER','STANDARDIZATION'] },
};

export function getBuilding(id) { return BUILDINGS[id]; }
export function getTech(id) { return TECHS[id]; }
export function getResource(id) { return RESOURCES[id]; }
export function getAllBuildings() { return BUILDINGS; }
export function getAllTechs() { return TECHS; }
export function getAllResources() { return RESOURCES; }
