/**
 * CommandSender — 封装发送指令的便捷 API
 */
import { gameBridge } from './game-bridge.js';

/**
 * 发送建造/升级指令
 * @param {string} nodeId
 * @param {string} buildingType - FARM|MINE|ARSENAL|WALL|ORACLE_BEACON
 */
export function sendBuild(nodeId, buildingType) {
  gameBridge.sendCommand('BUILD', { nodeId, buildingType });
}

/**
 * 发送科技研发指令
 * @param {string} techId
 */
export function sendResearch(techId) {
  gameBridge.sendCommand('RESEARCH', { techId });
}

/**
 * 发送进攻指令
 * @param {string} fromNodeId
 * @param {string} targetNodeId
 * @param {number} troopCount
 */
export function sendAttack(fromNodeId, targetNodeId, troopCount) {
  gameBridge.sendCommand('ATTACK', { fromNodeId, targetNodeId, troopCount });
}

export function sendRetreat(armyId) {
  gameBridge.sendCommand('RETREAT', { armyId, troopCount: armyId });
}

export function sendCreateCompany(nodeId, unitDefId = 'MILITIA') {
  gameBridge.sendCommand('CREATE_COMPANY', { nodeId, unitDefId });
}

export function sendMoveUnit(entityId, targetNodeId) {
  gameBridge.sendCommand('MOVE_UNIT', { entityId, targetNodeId });
}

export function sendAttackNode(entityId, targetNodeId) {
  gameBridge.sendCommand('ATTACK_NODE', { entityId, targetNodeId });
}

export function sendRetreatUnit(entityId) {
  gameBridge.sendCommand('RETREAT_UNIT', { entityId, armyId: entityId, troopCount: entityId });
}

export function sendCreateFormation(name, formationType, entityIds) {
  gameBridge.sendCommand('CREATE_FORMATION', { name, formationType, entityIds });
}

/**
 * 设置游戏速度
 * @param {number} speed - 0=暂停, 1=正常, 2=二倍速, 5=五倍速
 */
export function sendSetSpeed(speed) {
  gameBridge.sendCommand('SET_SPEED', { speed });
}

/**
 * 升级道路为铁轨
 * @param {string} edgeId
 */
export function sendUpgradeEdge(edgeId) {
  gameBridge.sendCommand('UPGRADE_EDGE', { edgeId });
}

/**
 * 创建运输路线
 * @param {string} fromNodeId
 * @param {string} targetNodeId
 * @param {string} cargoType - FOOD|IRON|AMMO
 */
export function sendCreateRoute(routeOrFromNodeId, targetNodeId, cargoType = 'FOOD') {
  if (typeof routeOrFromNodeId === 'object') {
    const {
      fromNodeId,
      targetNodeId: target,
      cargoType: cargo = 'FOOD',
      transportType = 'PORTER',
      transportCount = 1,
      priority = 50,
      routeMode = 'MANUAL',
    } = routeOrFromNodeId;
    gameBridge.sendCommand('CREATE_ROUTE', {
      fromNodeId,
      targetNodeId: target,
      cargoType: cargo,
      transportType,
      transportCount,
      priority,
      routeMode,
    });
    return;
  }
  gameBridge.sendCommand('CREATE_ROUTE', {
    fromNodeId: routeOrFromNodeId,
    targetNodeId,
    cargoType,
    transportType: 'PORTER',
    transportCount: 1,
    priority: 50,
    routeMode: 'MANUAL',
  });
}

/**
 * 取消运输路线
 * @param {number} entityId
 */
export function sendCancelRoute(entityId) {
  gameBridge.sendCommand('CANCEL_ROUTE', { routeId: entityId, troopCount: entityId });
}

export function sendUpdateRoute(routeId, patch = {}) {
  gameBridge.sendCommand('UPDATE_ROUTE', { routeId, troopCount: routeId, ...patch });
}

export function sendSetRallyPoint(nodeId, policies) {
  gameBridge.sendCommand('SET_RALLY_POINT', { nodeId, policies });
}

export function sendClearRallyPoint(nodeId) {
  gameBridge.sendCommand('CLEAR_RALLY_POINT', { nodeId });
}

export function sendProduceTransport(nodeId, transportType, quantity = 1) {
  gameBridge.sendCommand('PRODUCE_TRANSPORT', { nodeId, transportType, quantity });
}

// ─── 新增：3D 建筑放置 & 城墙绘制命令 ───

/**
 * 自由放置建筑
 * @param {string} nodeId
 * @param {string} buildingType - FARM|MINE|ARSENAL|ORACLE_BEACON
 * @param {number} localX - 相对城市的局部 X
 * @param {number} localZ - 相对城市的局部 Z
 * @param {number} rotation - Y轴旋转角度（度）
 */
export function sendPlaceBuilding(nodeId, buildingType, localX, localZ, rotation = 0) {
  gameBridge.sendCommand('PLACE_BUILDING', { nodeId, buildingType, localX, localZ, rotation });
}

/**
 * 建造城墙段
 * @param {string} nodeId
 * @param {number} fromX - 起点 X（局部坐标）
 * @param {number} fromZ - 起点 Z
 * @param {number} toX - 终点 X
 * @param {number} toZ - 终点 Z
 */
export function sendBuildWall(nodeId, fromX, fromZ, toX, toZ) {
  gameBridge.sendCommand('BUILD_WALL', { nodeId, fromX, fromZ, toX, toZ });
}

/**
 * 拆除建筑
 * @param {string} nodeId
 * @param {number} buildingIndex
 */
export function sendDemolishBuilding(nodeId, buildingIndex) {
  gameBridge.sendCommand('DEMOLISH_BUILDING', { nodeId, buildingIndex });
}

/**
 * 拆除城墙段
 * @param {string} nodeId
 * @param {number} wallIndex
 */
export function sendDemolishWall(nodeId, wallIndex) {
  gameBridge.sendCommand('DEMOLISH_WALL', { nodeId, wallIndex });
}
