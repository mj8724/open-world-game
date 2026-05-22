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
export function sendCreateRoute(fromNodeId, targetNodeId, cargoType = 'FOOD') {
  gameBridge.sendCommand('CREATE_ROUTE', { fromNodeId, targetNodeId, buildingType: cargoType });
}

/**
 * 取消运输路线
 * @param {number} entityId
 */
export function sendCancelRoute(entityId) {
  gameBridge.sendCommand('CANCEL_ROUTE', { troopCount: entityId });
}
