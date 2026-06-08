/**
 * StateStore — 客户端状态缓存
 * 存储从服务器接收的游戏状态，应用增量更新，通知 UI
 */
import { eventBus } from '../ui/event-bus.js';

class StateStore {
  constructor() {
    this.nodes = {};
    this.edges = {};
    this.factions = {};
    this.armies = {};
    this.formations = {};
    this.logistics = {};
    this.rallyPoints = {};
    this.transportStocks = {};
    this.transportProductionQueue = [];
    this.buildQueue = [];
    this.wildResources = {};
    this.neutralStructures = {};
    this.currentTick = 0;
    this._initialized = false;
  }

  /** @returns {boolean} */
  get initialized() { return this._initialized; }

  /**
   * 初始化完整状态
   * @param {object} fullState - 服务器推送的 FULL_STATE
   */
  applyFullState(fullState) {
    this.currentTick = fullState.tick || 0;
    this.nodes = fullState.nodes || {};
    this.edges = fullState.edges || {};
    this.factions = fullState.factions || {};
    this.armies = fullState.armies || {};
    this.formations = fullState.formations || {};
    this.logistics = fullState.logisticsEntities || {};
    this.rallyPoints = fullState.rallyPoints || {};
    this.transportStocks = fullState.transportStocks || {};
    this.transportProductionQueue = fullState.transportProductionQueue || [];
    this.buildQueue = fullState.buildQueue || [];
    this.wildResources = fullState.wildResources || {};
    this.neutralStructures = fullState.neutralStructures || {};
    this._initialized = true;
    eventBus.emit('state-full-update', this);
  }

  /**
   * 应用增量更新
   * @param {object} delta - TICK_UPDATE 的 data
   */
  applyDelta(delta) {
    this.currentTick = delta.tick || this.currentTick;

    // 合并节点
    if (delta.nodes) {
      for (const [id, node] of Object.entries(delta.nodes)) {
        this.nodes[id] = { ...this.nodes[id], ...node };
      }
    }
    // 合并边
    if (delta.edges) {
      for (const [id, edge] of Object.entries(delta.edges)) {
        this.edges[id] = { ...this.edges[id], ...edge };
      }
    }
    // 合并军队
    if (delta.armies) {
      for (const [id, army] of Object.entries(delta.armies)) {
        this.armies[id] = { ...this.armies[id], ...army };
      }
    }
    // 合并编组
    if (delta.formations) {
      for (const [id, formation] of Object.entries(delta.formations)) {
        this.formations[id] = { ...this.formations[id], ...formation };
      }
    }
    // 合并物流
    if (delta.logisticsEntities) {
      for (const [id, logi] of Object.entries(delta.logisticsEntities)) {
        this.logistics[id] = { ...this.logistics[id], ...logi };
      }
    }
    // 合并集结点
    if (delta.rallyPoints) {
      for (const [id, rally] of Object.entries(delta.rallyPoints)) {
        this.rallyPoints[id] = { ...this.rallyPoints[id], ...rally };
      }
    }
    // 合并运输工具库存
    if (delta.transportStocks) {
      for (const [id, stock] of Object.entries(delta.transportStocks)) {
        this.transportStocks[id] = { ...this.transportStocks[id], ...stock };
      }
    }
    // 合并势力（研发进度等）
    if (delta.factions) {
      for (const [id, faction] of Object.entries(delta.factions)) {
        this.factions[id] = { ...this.factions[id], ...faction };
      }
    }
    // 合并野外资源点
    if (delta.wildResources) {
      for (const [id, wr] of Object.entries(delta.wildResources)) {
        this.wildResources[id] = { ...this.wildResources[id], ...wr };
      }
    }
    // 合并中立建筑
    if (delta.neutralStructures) {
      for (const [id, ns] of Object.entries(delta.neutralStructures)) {
        this.neutralStructures[id] = { ...this.neutralStructures[id], ...ns };
      }
    }
    // 移除实体
    if (delta.removedEntityIds) {
      for (const id of delta.removedEntityIds) {
        delete this.armies[id];
        delete this.formations[id];
        delete this.logistics[id];
      }
    }
    // 移除集结点
    if (delta.removedRallyPointIds) {
      for (const id of delta.removedRallyPointIds) {
        delete this.rallyPoints[id];
      }
    }
    // 移除野外资源点
    if (delta.removedWildResourceIds) {
      for (const id of delta.removedWildResourceIds) {
        delete this.wildResources[id];
      }
    }
    // 移除中立建筑
    if (delta.removedNeutralStructureIds) {
      for (const id of delta.removedNeutralStructureIds) {
        delete this.neutralStructures[id];
      }
    }
    // 更新建造队列
    if (delta.buildQueue) {
      this.buildQueue = delta.buildQueue;
    }
    if (delta.transportProductionQueue) {
      this.transportProductionQueue = delta.transportProductionQueue;
    }

    eventBus.emit('state-tick-update', { tick: this.currentTick, delta });

    // 广播事件日志
    if (delta.events) {
      for (const evt of delta.events) {
        eventBus.emit('game-event', evt);
      }
    }
  }

  /** 获取节点 */
  getNode(id) { return this.nodes[id]; }
  /** 获取边 */
  getEdge(id) { return this.edges[id]; }
  /** 获取势力 */
  getFaction(id) { return this.factions[id]; }
  /** 获取玩家势力 */
  getPlayerFaction() { return this.factions['PLAYER']; }
  /** 获取地形类型，默认 PLAINS */
  getTerrain(nodeId) { return this.nodes[nodeId]?.terrain || 'PLAINS'; }

  /** 计算玩家全局资源总量 */
  getPlayerTotals() {
    const player = this.getPlayerFaction();
    if (!player) return { food: 0, iron: 0, ammo: 0, pop: 0 };
    let food = 0, iron = 0, ammo = 0, pop = 0;
    for (const nodeId of player.ownedNodeIds || []) {
      const n = this.nodes[nodeId];
      if (n) {
        food += n.invFood || 0;
        iron += n.invIron || 0;
        ammo += n.invAmmo || 0;
        pop  += n.popCount || 0;
      }
    }
    return { food, iron, ammo, pop };
  }
}

export const stateStore = new StateStore();
export default stateStore;
