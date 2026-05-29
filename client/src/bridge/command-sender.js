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
