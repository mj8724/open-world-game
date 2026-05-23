import { MOCK_FULL_STATE } from './mock-data.js';

const AI_ATTACK_INTERVAL_TICKS = 12;
const AI_MIN_GARRISON_TO_KEEP = 2;
const AI_MIN_GARRISON_TO_ATTACK = 4;

const TRANSPORTS = {
  PORTER: { capacity: 20, speed: 1, buildTicks: 3, costFood: 5, costIron: 0, maintenanceFood: 1, maintenanceIron: 0 },
  CARRIAGE: { capacity: 60, speed: 2, buildTicks: 8, costFood: 10, costIron: 15, maintenanceFood: 1, maintenanceIron: 1 },
  TRAIN: { capacity: 200, speed: 4, buildTicks: 15, costFood: 20, costIron: 50, maintenanceFood: 2, maintenanceIron: 2 },
};

export function createMockRuntime() {
  return new MockRuntime(MOCK_FULL_STATE);
}

class MockRuntime {
  constructor(initialState) {
    this.state = structuredClone(initialState);
    this.nextEntityId = 1;
  }

  getFullState() {
    return structuredClone(this.state);
  }

  tick() {
    this.state.tick++;
    const delta = this.createDelta();
    this.processProduction(delta);
    this.processMaintenance(delta);
    this.planLogistics(delta);
    this.processLogistics(delta);
    this.processArmies(delta);
    this.planAIAttacks(delta);
    return delta;
  }

  executeCommand(action, payload = {}) {
    const delta = this.createDelta();
    switch (action) {
      case 'CREATE_ROUTE':
        this.createRoute(payload, delta);
        break;
      case 'CANCEL_ROUTE':
        this.cancelRoute(payload.routeId ?? payload.troopCount, delta);
        break;
      case 'UPDATE_ROUTE':
        this.updateRoute(payload.routeId ?? payload.troopCount, payload, delta);
        break;
      case 'SET_RALLY_POINT':
        this.setRallyPoint(payload.nodeId, payload.policies, delta);
        this.planLogistics(delta);
        break;
      case 'CLEAR_RALLY_POINT':
        this.clearRallyPoint(payload.nodeId, delta);
        break;
      case 'PRODUCE_TRANSPORT':
        this.produceTransport(payload, delta);
        break;
      case 'ATTACK':
        this.createAttack(payload, delta);
        break;
      case 'RETREAT':
        this.retreatArmy(payload.armyId ?? payload.troopCount, delta);
        break;
    }
    return delta;
  }

  createDelta() {
    return {
      tick: this.state.tick,
      nodes: {},
      logisticsEntities: {},
      armies: {},
      rallyPoints: {},
      transportStocks: {},
      factions: {},
      removedEntityIds: [],
      removedRallyPointIds: [],
      transportProductionQueue: this.state.transportProductionQueue,
      events: [],
    };
  }

  createRoute(payload, delta) {
    const from = this.state.nodes[payload.fromNodeId];
    const to = this.state.nodes[payload.targetNodeId];
    if (!from || !to || from.factionId !== 'PLAYER' || to.factionId !== 'PLAYER') return;

    const transportType = payload.transportType || 'PORTER';
    const transport = TRANSPORTS[transportType];
    const count = Math.max(1, Number(payload.transportCount || 1));
    const stock = this.ensureStock(from.id);
    const entry = this.ensureStockEntry(stock, transportType);
    if (!transport || entry.idle < count) return;

    const path = this.findPath(from.id, to.id, transportType);
    if (path.edgeIds.length === 0) return;

    entry.idle -= count;
    entry.assigned += count;
    delta.transportStocks[from.id] = structuredClone(stock);

    const id = this.nextEntityId++;
    const route = {
      entityId: id,
      factionId: 'PLAYER',
      mode: payload.routeMode || 'MANUAL',
      enabled: true,
      transportType,
      assignedTransportCount: count,
      cargoType: payload.cargoType || 'FOOD',
      cargoAmount: 0,
      fromNodeId: from.id,
      toNodeId: to.id,
      currentEdgeId: path.edgeIds[0],
      edgeProgress: 0,
      returning: false,
      pathNodeIds: path.nodeIds,
      pathEdgeIds: path.edgeIds,
      priority: Math.max(0, Math.min(100, Number(payload.priority || 50))),
      desiredTargetQuantity: payload.targetQuantity,
      unlimitedTarget: !!payload.unlimited,
      retireWhenIdle: false,
      trips: Array.from({ length: count }, (_, i) => ({ tripId: i + 1, returning: false, cargoAmount: 0, currentPathIndex: 0, edgeProgress: 0 })),
      estimatedRoundTripTicks: this.estimateRoundTrip(path.edgeIds, transportType),
      estimatedThroughputPerTick: 0,
      estimatedRequiredTransportCount: count,
      deliveredLastTick: 0,
      deliveredTotal: 0,
    };
    route.estimatedThroughputPerTick = route.estimatedRoundTripTicks > 0
      ? (count * transport.capacity) / route.estimatedRoundTripTicks
      : 0;

    this.state.logisticsEntities[id] = route;
    delta.logisticsEntities[id] = structuredClone(route);
  }

  cancelRoute(routeId, delta) {
    const route = this.state.logisticsEntities[routeId];
    if (!route) return;
    const cargo = route.trips.reduce((sum, trip) => sum + trip.cargoAmount, 0);
    if (cargo > 0) {
      const from = this.state.nodes[route.fromNodeId];
      this.addCargo(from, route.cargoType, cargo);
      delta.nodes[from.id] = structuredClone(from);
    }
    const stock = this.state.transportStocks[route.fromNodeId];
    const entry = stock?.stock?.[route.transportType];
    if (entry) {
      entry.assigned = Math.max(0, entry.assigned - route.assignedTransportCount);
      entry.idle += route.assignedTransportCount;
      delta.transportStocks[route.fromNodeId] = structuredClone(stock);
    }
    delete this.state.logisticsEntities[routeId];
    delta.removedEntityIds.push(Number(routeId));
  }

  updateRoute(routeId, payload, delta) {
    const route = this.state.logisticsEntities[routeId];
    if (!route) return;
    route.mode = 'MANUAL';
    route.priority = Math.max(0, Math.min(100, Number(payload.priority ?? route.priority)));
    if (payload.enabled !== undefined) route.enabled = !!payload.enabled;
    delta.logisticsEntities[routeId] = structuredClone(route);
  }

  setRallyPoint(nodeId, policies, delta) {
    const node = this.state.nodes[nodeId];
    if (!node || node.factionId !== 'PLAYER') return;
    const rally = { nodeId, factionId: 'PLAYER', enabled: true, cargoPolicies: policies || {} };
    this.state.rallyPoints[nodeId] = rally;
    delta.rallyPoints[nodeId] = structuredClone(rally);
  }

  clearRallyPoint(nodeId, delta) {
    if (!this.state.rallyPoints[nodeId]) return;
    delete this.state.rallyPoints[nodeId];
    delta.removedRallyPointIds.push(nodeId);
  }

  produceTransport(payload, delta) {
    const node = this.state.nodes[payload.nodeId];
    const def = TRANSPORTS[payload.transportType];
    if (!node || node.factionId !== 'PLAYER' || !def) return;
    const quantity = Math.max(1, Number(payload.quantity || 1));
    const foodCost = def.costFood * quantity;
    const ironCost = def.costIron * quantity;
    if (node.invFood < foodCost || node.invIron < ironCost) return;
    node.invFood -= foodCost;
    node.invIron -= ironCost;
    delta.nodes[node.id] = structuredClone(node);
    this.state.transportProductionQueue.push({
      nodeId: node.id,
      transportType: payload.transportType,
      quantity,
      remainingTicks: def.buildTicks * quantity,
      totalTicks: def.buildTicks * quantity,
      factionId: 'PLAYER',
    });
    delta.transportProductionQueue = structuredClone(this.state.transportProductionQueue);
  }


  createAttack(payload, delta) {
    const from = this.state.nodes[payload.fromNodeId];
    const target = this.state.nodes[payload.targetNodeId];
    const troopCount = Math.max(0, Number(payload.troopCount || 0));
    if (!from || !target || from.factionId !== 'PLAYER' || target.factionId === 'PLAYER') return;
    if (troopCount <= 0 || (from.garrisonCount || 0) < troopCount) return;
    this.launchAttack(from, target, troopCount, delta);
  }

  retreatArmy(armyId, delta) {
    const army = this.state.armies?.[armyId];
    if (!army || army.factionId !== 'PLAYER' || army.state !== 'MOVING') return;
    const homeNode = this.state.nodes[army.currentNodeId];
    if (!homeNode || homeNode.factionId !== 'PLAYER') return;

    homeNode.garrisonCount = (homeNode.garrisonCount || 0) + (army.troopCount || 0);
    delta.nodes[homeNode.id] = structuredClone(homeNode);
    this.removeArmy(army, delta);
    delta.events.push({
      type: 'COMBAT_RETREAT',
      textKey: 'event.combat_retreat',
      params: { node: homeNode.name, troops: army.troopCount || 0 },
    });
  }

  launchAttack(from, target, troopCount, delta) {
    const edge = this.findAdjacentEdge(from.id, target.id);
    if (!edge) return false;

    this.state.armies ||= {};
    from.garrisonCount -= troopCount;
    delta.nodes[from.id] = structuredClone(from);
    const id = this.nextEntityId++;
    const army = {
      entityId: id,
      factionId: from.factionId,
      troopCount,
      meleeTroops: troopCount,
      rangedTroops: 0,
      morale: 1,
      carryFood: 0,
      carryAmmo: 0,
      currentNodeId: from.id,
      currentEdgeId: edge.id,
      edgeProgress: 0,
      targetNodeId: target.id,
      state: 'MOVING',
    };
    this.state.armies[id] = army;
    delta.armies[id] = structuredClone(army);
    delta.events.push({
      type: 'COMBAT_ATTACK',
      textKey: 'event.combat_attack',
      params: { from: from.name, to: target.name, troops: troopCount },
    });
    return true;
  }

  planAIAttacks(delta) {
    if (this.state.tick % AI_ATTACK_INTERVAL_TICKS !== 0) return;
    const source = Object.values(this.state.nodes)
      .filter(node => node.factionId === 'AI' && (node.garrisonCount || 0) >= AI_MIN_GARRISON_TO_ATTACK)
      .sort((a, b) => (b.garrisonCount || 0) - (a.garrisonCount || 0) || a.id.localeCompare(b.id))
      .find(node => this.findAdjacentTargets(node).length > 0);
    if (!source) return;

    const target = this.findAdjacentTargets(source)
      .sort((a, b) => Number(b.factionId === 'NEUTRAL') - Number(a.factionId === 'NEUTRAL') || this.estimateDefenderPower(a) - this.estimateDefenderPower(b) || a.id.localeCompare(b.id))[0];
    if (!target) return;

    const troopCount = Math.min((source.garrisonCount || 0) - AI_MIN_GARRISON_TO_KEEP, 3);
    if (troopCount <= 0) return;
    this.launchAttack(source, target, troopCount, delta);
  }

  findAdjacentTargets(source) {
    return Object.values(this.state.edges)
      .map(edge => edge.sourceNodeId === source.id ? edge.targetNodeId : edge.targetNodeId === source.id ? edge.sourceNodeId : null)
      .filter(Boolean)
      .map(id => this.state.nodes[id])
      .filter(node => node && node.factionId !== source.factionId);
  }

  estimateDefenderPower(node) {
    return (node.garrisonCount || 0) * 2 + (node.wallHpCurrent || 0) + (node.wallLevel || 0) * 25;
  }
  processArmies(delta) {
    for (const army of Object.values({ ...(this.state.armies || {}) })) {
      if (army.state !== 'MOVING' || !army.currentEdgeId || !army.targetNodeId) continue;
      const edge = this.state.edges[army.currentEdgeId];
      const target = this.state.nodes[army.targetNodeId];
      if (!edge || !target) {
        this.removeArmy(army, delta);
        continue;
      }
      army.edgeProgress += 10 / Math.max(1, edge.length || 1);
      if (army.edgeProgress < 1) {
        delta.armies[army.entityId] = structuredClone(army);
        continue;
      }
      army.edgeProgress = 1;
      army.currentNodeId = target.id;
      army.currentEdgeId = null;
      army.state = 'FIGHTING';
      this.resolveCombat(army, target, delta);
    }
  }

  resolveCombat(army, target, delta) {
    const oldFactionId = target.factionId;
    const attackerPower = army.troopCount * 3 * Math.max(0.1, army.morale || 1);
    const defenderPower = (target.garrisonCount || 0) * 2 + (target.wallHpCurrent || 0) + (target.wallLevel || 0) * 25;
    if (attackerPower > defenderPower) {
      const survivors = Math.max(1, Math.min(army.troopCount, Math.ceil((attackerPower - defenderPower) / 3)));
      target.factionId = army.factionId;
      target.garrisonCount = survivors;
      target.loyalty = 0.6;
      target.wallHpCurrent = Math.max(0, (target.wallHpCurrent || 0) - army.troopCount * 5);
      this.updateFactionOwnership(oldFactionId, army.factionId, target.id, delta);
      this.ensureCapturedStock(target.id, army.factionId, delta);
      delta.nodes[target.id] = structuredClone(target);
      this.removeArmy(army, delta);
      delta.events.push({
        type: 'COMBAT_CAPTURE',
        textKey: 'event.combat_capture',
        params: { node: target.name, troops: survivors },
      });
      return;
    }

    let damage = Math.floor(attackerPower / 2);
    const wallDamage = Math.min(target.wallHpCurrent || 0, damage);
    target.wallHpCurrent = (target.wallHpCurrent || 0) - wallDamage;
    damage -= wallDamage;
    target.garrisonCount = Math.max(0, (target.garrisonCount || 0) - damage);
    delta.nodes[target.id] = structuredClone(target);
    this.removeArmy(army, delta);
    delta.events.push({
      type: 'COMBAT_DEFEAT',
      textKey: 'event.combat_defeat',
      params: { node: target.name, losses: army.troopCount },
    });
  }

  ensureCapturedStock(nodeId, factionId, delta) {
    if (this.state.transportStocks[nodeId]) return;
    this.state.transportStocks[nodeId] = { nodeId, factionId, stock: {} };
    delta.transportStocks[nodeId] = structuredClone(this.state.transportStocks[nodeId]);
  }

  updateFactionOwnership(oldFactionId, newFactionId, nodeId, delta) {
    const oldFaction = this.state.factions?.[oldFactionId];
    if (oldFaction) {
      oldFaction.ownedNodeIds = (oldFaction.ownedNodeIds || []).filter(id => id !== nodeId);
      delta.factions[oldFactionId] = structuredClone(oldFaction);
    }
    const newFaction = this.state.factions?.[newFactionId];
    if (newFaction && !(newFaction.ownedNodeIds || []).includes(nodeId)) {
      newFaction.ownedNodeIds.push(nodeId);
      delta.factions[newFactionId] = structuredClone(newFaction);
    }
  }

  removeArmy(army, delta) {
    delete this.state.armies?.[army.entityId];
    delta.removedEntityIds.push(Number(army.entityId));
  }

  findAdjacentEdge(from, to) {
    return Object.values(this.state.edges).find(edge =>
      (edge.sourceNodeId === from && edge.targetNodeId === to) ||
      (edge.sourceNodeId === to && edge.targetNodeId === from));
  }

  processProduction(delta) {
    for (let i = this.state.transportProductionQueue.length - 1; i >= 0; i--) {
      const item = this.state.transportProductionQueue[i];
      item.remainingTicks--;
      if (item.remainingTicks > 0) continue;
      const stock = this.ensureStock(item.nodeId);
      const entry = this.ensureStockEntry(stock, item.transportType);
      entry.total += item.quantity;
      entry.idle += item.quantity;
      delta.transportStocks[item.nodeId] = structuredClone(stock);
      this.state.transportProductionQueue.splice(i, 1);
    }
    delta.transportProductionQueue = structuredClone(this.state.transportProductionQueue);
  }

  processMaintenance(delta) {
    for (const stock of Object.values(this.state.transportStocks)) {
      const node = this.state.nodes[stock.nodeId];
      if (!node) continue;
      let changed = false;
      for (const entry of Object.values(stock.stock || {})) {
        const def = TRANSPORTS[entry.transportType];
        if (!def) continue;
        const active = Math.max(0, (entry.total || 0) - (entry.maintenanceBlocked || 0));
        const foodCost = active * def.maintenanceFood;
        const ironCost = active * def.maintenanceIron;
        if (foodCost === 0 && ironCost === 0) continue;
        if (node.invFood >= foodCost && node.invIron >= ironCost) {
          node.invFood -= foodCost;
          node.invIron -= ironCost;
          entry.maintenanceBlocked = 0;
          delta.nodes[node.id] = structuredClone(node);
        } else {
          entry.maintenanceBlocked = entry.total || 0;
        }
        changed = true;
      }
      if (changed) delta.transportStocks[stock.nodeId] = structuredClone(stock);
    }
  }

  planLogistics(delta) {
    this.processAutoRouteLifecycle(delta);
    for (const rally of Object.values(this.state.rallyPoints)) {
      if (!rally.enabled || rally.factionId !== 'PLAYER') continue;
      for (const [cargoType, policy] of Object.entries(rally.cargoPolicies || {})) {
        if (!policy.enabled) continue;
        const destination = this.state.nodes[rally.nodeId];
        if (!destination) continue;
        const cargo = policy.cargoType || cargoType;
        const current = this.getCargo(destination, cargo);
        if (!policy.unlimited && current >= (policy.targetQuantity || 0)) continue;
        const existing = Object.values(this.state.logisticsEntities).find(route =>
          route.mode === 'AUTO' && route.enabled && !route.retireWhenIdle && route.toNodeId === rally.nodeId && route.cargoType === cargo);
        if (existing) continue;

        const source = Object.values(this.state.nodes)
          .filter(node => node.factionId === 'PLAYER' && node.id !== rally.nodeId && this.isProductionSource(node, cargo))
          .filter(node => this.getCargo(node, cargo) > 120)
          .sort((a, b) => this.getCargo(b, cargo) - this.getCargo(a, cargo))[0];
        if (!source) continue;
        const transportType = this.chooseAvailableTransport(source.id);
        if (!transportType) continue;
        this.createRoute({
          fromNodeId: source.id,
          targetNodeId: rally.nodeId,
          cargoType: cargo,
          transportType,
          transportCount: 1,
          priority: policy.priority || 50,
          routeMode: 'AUTO',
          targetQuantity: policy.targetQuantity,
          unlimited: policy.unlimited,
        }, delta);
      }
    }
  }

  processLogistics(delta) {
    const budgets = this.buildRouteBudgets();
    for (const route of Object.values(this.state.logisticsEntities)) {
      if (!route.enabled || !route.pathEdgeIds?.length) continue;
      const def = TRANSPORTS[route.transportType];
      let delivered = 0;
      const budgetKey = `${route.fromNodeId}:${route.transportType}`;
      let remainingBudget = budgets.get(budgetKey) || 0;
      for (const trip of route.trips) {
        if (remainingBudget <= 0) continue;
        remainingBudget--;
        if (!trip.returning && trip.cargoAmount === 0 && !route.retireWhenIdle) {
          const from = this.state.nodes[route.fromNodeId];
          const load = Math.min(def.capacity, Math.max(0, this.getCargo(from, route.cargoType) - 50));
          if (load <= 0) continue;
          this.addCargo(from, route.cargoType, -load);
          trip.cargoAmount = load;
          delta.nodes[from.id] = structuredClone(from);
        }

        const edgeId = route.pathEdgeIds[trip.currentPathIndex];
        const edge = this.state.edges[edgeId];
        trip.edgeProgress += def.speed / Math.max(1, edge.length);
        route.currentEdgeId = edgeId;
        route.edgeProgress = trip.edgeProgress;
        route.returning = trip.returning;
        if (trip.edgeProgress < 1) continue;
        trip.edgeProgress = 0;

        if (!trip.returning && trip.currentPathIndex < route.pathEdgeIds.length - 1) {
          trip.currentPathIndex++;
        } else if (!trip.returning) {
          const to = this.state.nodes[route.toNodeId];
          this.addCargo(to, route.cargoType, trip.cargoAmount);
          delivered += trip.cargoAmount;
          trip.cargoAmount = 0;
          trip.returning = true;
          delta.nodes[to.id] = structuredClone(to);
        } else if (trip.currentPathIndex > 0) {
          trip.currentPathIndex--;
        } else {
          trip.returning = false;
          if (route.retireWhenIdle && this.isRouteIdleAtSource(route)) {
            this.releaseRouteTransport(route, delta);
            delete this.state.logisticsEntities[route.entityId];
            delta.removedEntityIds.push(Number(route.entityId));
            break;
          }
        }
      }
      budgets.set(budgetKey, remainingBudget);
      route.cargoAmount = route.trips.reduce((sum, trip) => sum + trip.cargoAmount, 0);
      route.deliveredLastTick = delivered;
      route.deliveredTotal += delivered;
      delta.logisticsEntities[route.entityId] = structuredClone(route);
    }
  }

  processAutoRouteLifecycle(delta) {
    for (const route of Object.values(this.state.logisticsEntities)) {
      if (route.mode !== 'AUTO' || route.unlimitedTarget) continue;
      const target = route.desiredTargetQuantity;
      if (target == null) continue;
      const destination = this.state.nodes[route.toNodeId];
      if (!destination) continue;
      if (this.getCargo(destination, route.cargoType) >= target) {
        route.retireWhenIdle = true;
        delta.logisticsEntities[route.entityId] = structuredClone(route);
      }
      if (route.retireWhenIdle && this.isRouteIdleAtSource(route)) {
        this.releaseRouteTransport(route, delta);
        delete this.state.logisticsEntities[route.entityId];
        delta.removedEntityIds.push(Number(route.entityId));
      }
    }
  }

  buildRouteBudgets() {
    const budgets = new Map();
    for (const stock of Object.values(this.state.transportStocks)) {
      for (const entry of Object.values(stock.stock || {})) {
        const blockedAssigned = Math.max(0, (entry.maintenanceBlocked || 0) - (entry.idle || 0));
        budgets.set(`${stock.nodeId}:${entry.transportType}`, Math.max(0, (entry.assigned || 0) - blockedAssigned));
      }
    }
    return budgets;
  }

  isRouteIdleAtSource(route) {
    return route.trips.every(trip => !trip.returning && trip.cargoAmount === 0 && trip.currentPathIndex === 0 && trip.edgeProgress === 0);
  }

  releaseRouteTransport(route, delta) {
    const stock = this.state.transportStocks[route.fromNodeId];
    const entry = stock?.stock?.[route.transportType];
    if (!entry) return;
    entry.assigned = Math.max(0, entry.assigned - route.assignedTransportCount);
    entry.idle += route.assignedTransportCount;
    delta.transportStocks[route.fromNodeId] = structuredClone(stock);
  }

  findPath(from, to, transportType = 'PORTER') {
    const queue = [{ id: from, nodeIds: [from], edgeIds: [] }];
    const seen = new Set([from]);
    while (queue.length) {
      const current = queue.shift();
      if (current.id === to) return current;
      for (const edge of Object.values(this.state.edges)) {
        if (!this.isEdgeCompatible(transportType, edge.edgeType)) continue;
        const next = edge.sourceNodeId === current.id ? edge.targetNodeId : edge.targetNodeId === current.id ? edge.sourceNodeId : null;
        if (!next || seen.has(next)) continue;
        seen.add(next);
        queue.push({ id: next, nodeIds: [...current.nodeIds, next], edgeIds: [...current.edgeIds, edge.id] });
      }
    }
    return { nodeIds: [], edgeIds: [] };
  }

  estimateRoundTrip(edgeIds, transportType) {
    const def = TRANSPORTS[transportType];
    const oneWay = edgeIds.reduce((sum, id) => sum + this.state.edges[id].length / Math.max(1, def.speed), 0);
    return oneWay * 2;
  }

  ensureStock(nodeId) {
    if (!this.state.transportStocks[nodeId]) {
      this.state.transportStocks[nodeId] = { nodeId, factionId: 'PLAYER', stock: {} };
    }
    return this.state.transportStocks[nodeId];
  }

  ensureStockEntry(stock, transportType) {
    if (!stock.stock[transportType]) {
      stock.stock[transportType] = { transportType, total: 0, idle: 0, assigned: 0, maintenanceBlocked: 0 };
    }
    return stock.stock[transportType];
  }

  isProductionSource(node, cargoType) {
    if (cargoType === 'FOOD') return (node.farmLevel || 0) > 0;
    if (cargoType === 'IRON') return (node.mineLevel || 0) > 0;
    if (cargoType === 'AMMO') return (node.arsenalLevel || 0) > 0;
    return false;
  }

  chooseAvailableTransport(nodeId) {
    const stock = this.ensureStock(nodeId);
    return ['TRAIN', 'CARRIAGE', 'PORTER'].find(type => (stock.stock[type]?.idle || 0) > 0) || '';
  }

  isEdgeCompatible(transportType, edgeType) {
    if (transportType === 'TRAIN') return edgeType === 'RAILWAY';
    if (transportType === 'CARRIAGE') return edgeType === 'ROAD' || edgeType === 'RAILWAY';
    return edgeType === 'TRAIL' || edgeType === 'ROAD' || edgeType === 'RAILWAY';
  }

  getCargo(node, cargoType) {
    if (cargoType === 'IRON') return node.invIron;
    if (cargoType === 'AMMO') return node.invAmmo;
    return node.invFood;
  }

  addCargo(node, cargoType, amount) {
    if (cargoType === 'IRON') node.invIron += amount;
    else if (cargoType === 'AMMO') node.invAmmo += amount;
    else node.invFood += amount;
  }
}
