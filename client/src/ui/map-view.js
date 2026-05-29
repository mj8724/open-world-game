/**
 * MapView — Cytoscape.js 拓扑地图渲染
 */
import cytoscape from 'cytoscape';
import i18n from '../i18n/i18n.js';
import { stateStore } from '../bridge/state-store.js';
import { eventBus } from './event-bus.js';

/** @type {cytoscape.Core|null} */
let cy = null;
let selectedNodeId = null;
let entityLayer = null;
let mapTooltip = null;
let entityRefreshFrame = null;

const FACTION_COLORS = {
  PLAYER:  '#2563EB',
  AI:      '#DC2626',
  NEUTRAL: '#9CA3AF',
};

/**
 * 初始化 Cytoscape 地图
 * @param {string} containerId - DOM 容器 ID
 */
export function initMap(containerId) {
  const container = document.getElementById(containerId);
  if (!container) { console.error('[MapView] 容器不存在:', containerId); return; }

  cy = cytoscape({
    container,
    style: [
      {
        selector: 'node',
        style: {
          'width': 12,
          'height': 12,
          'background-color': '#FFFFFF',
          'border-width': 1,
          'border-color': '#9CA3AF',
          'opacity': 0.12,
          'label': '',
          'font-size': '10px',
          'font-family': 'Inter, Noto Sans SC, sans-serif',
          'text-valign': 'bottom',
          'text-margin-y': 6,
          'color': '#6B7280',
          'text-outline-color': '#FFFFFF',
          'text-outline-width': 2,
          'overlay-opacity': 0,
          'transition-property': 'border-color, border-width, background-color',
          'transition-duration': '0.2s',
        }
      },
      {
        selector: 'node[factionId="PLAYER"]',
        style: {
          'border-color': '#2563EB',
          'color': '#1e40af',
        }
      },
      {
        selector: 'node[factionId="AI"]',
        style: {
          'border-color': '#DC2626',
          'color': '#991b1b',
        }
      },
      {
        selector: 'node[isCapital]',
        style: {
          'width': 44,
          'height': 44,
          'border-width': 4,
          'font-weight': 600,
          'font-size': '11px',
        }
      },
      {
        selector: 'node[rallyEnabled]',
        style: {
          'border-width': 5,
          'border-style': 'double',
          'background-color': '#fef3c7',
        }
      },
      {
        selector: 'edge[routeCount > 0]',
        style: {
          'width': 4,
          'line-color': '#16A34A',
          'line-style': 'solid',
          'opacity': 1,
        }
      },
      {
        selector: 'edge[manualRouteCount > 0]',
        style: {
          'line-color': '#2563EB',
        }
      },
      {
        selector: 'node[logisticsMarker]',
        style: {
          'width': 'data(markerSize)',
          'height': 'data(markerSize)',
          'background-color': 'data(markerColor)',
          'border-width': 2,
          'border-color': '#FFFFFF',
          'label': 'data(label)',
          'font-size': '8px',
          'text-valign': 'center',
          'text-halign': 'center',
          'color': '#FFFFFF',
          'text-outline-width': 0,
          'events': 'no',
          'opacity': 'data(markerOpacity)',
          'z-index': 20,
        }
      },
      {
        selector: 'node[armyMarker]',
        style: {
          'width': 'data(markerSize)',
          'height': 'data(markerSize)',
          'background-color': 'data(markerColor)',
          'border-width': 2,
          'border-color': '#FFFFFF',
          'label': 'data(label)',
          'font-size': '8px',
          'text-valign': 'center',
          'text-halign': 'center',
          'color': '#FFFFFF',
          'text-outline-width': 0,
          'events': 'no',
          'opacity': 0.95,
          'z-index': 30,
        }
      },
      {
        selector: 'node:selected',
        style: {
          'border-width': 4,
          'border-color': '#1d4ed8',
          'background-color': '#eff6ff',
        }
      },
      {
        selector: 'node:active',
        style: { 'overlay-opacity': 0 }
      },
      {
        selector: 'edge',
        style: {
          'width': 1.5,
          'line-color': '#D1D5DB',
          'line-style': 'dashed',
          'line-dash-pattern': [6, 4],
          'curve-style': 'bezier',
          'opacity': 0.7,
        }
      },
      {
        selector: 'edge[edgeType="RAILWAY"]',
        style: {
          'width': 2.5,
          'line-color': '#D97706',
          'line-style': 'solid',
          'opacity': 1,
        }
      },
    ],
    layout: { name: 'preset' },
    autoungrabify: true,
    zoomingEnabled: true,
    userZoomingEnabled: true,
    panningEnabled: true,
    boxSelectionEnabled: false,
    minZoom: 0.4,
    maxZoom: 3,
  });

  // 事件绑定
  cy.on('tap', 'node', (e) => {
    const nodeId = e.target.id();
    selectNode(nodeId);
  });

  cy.on('tap', (e) => {
    if (e.target === cy) {
      deselectAll();
    }
  });

  container.addEventListener('mouseenter', () => {
    container.style.cursor = 'pointer';
  });
  container.addEventListener('mouseleave', () => {
    container.style.cursor = 'default';
  });

  cy.on('pan zoom resize', refreshMapEntities);

  ensureEntityLayer(container.parentElement || container);

  return cy;
}

/**
 * 从 stateStore 加载地图数据
 */
export function loadMapData() {
  if (!cy) return;

  const elements = [];

  // 添加节点
  for (const [id, node] of Object.entries(stateStore.nodes)) {
    elements.push({
      group: 'nodes',
      data: {
        id: node.id,
        label: i18n.t(`map.node.${node.id}`, {}) !== `map.node.${node.id}`
          ? i18n.t(`map.node.${node.id}`)
          : node.name,
        factionId: node.factionId,
        isCapital: node.isCapital || false,
        popCount: node.popCount,
        rallyEnabled: !!stateStore.rallyPoints[id]?.enabled,
      },
      position: { x: node.x, y: node.y },
    });
  }

  // 添加边
  for (const [id, edge] of Object.entries(stateStore.edges)) {
    elements.push({
      group: 'edges',
      data: {
        id: edge.id,
        source: edge.sourceNodeId,
        target: edge.targetNodeId,
        edgeType: edge.edgeType,
        routeCount: 0,
        manualRouteCount: 0,
        autoRouteCount: 0,
      },
    });
  }

  cy.elements().remove();
  cy.add(elements);
  refreshLogisticsVisuals();
  cy.fit(undefined, 40);
  refreshMapEntities();
}

/**
 * 选中节点
 * @param {string} nodeId
 */
export function selectNode(nodeId) {
  if (!cy) return;
  selectedNodeId = nodeId;
  cy.nodes().unselect();
  const node = cy.getElementById(nodeId);
  if (node.length) {
    node.select();
  }
  eventBus.emit('node-selected', nodeId);
}

/** 取消选择 */
export function deselectAll() {
  if (!cy) return;
  selectedNodeId = null;
  cy.nodes().unselect();
  eventBus.emit('node-deselected');
}

/**
 * 刷新节点的外观数据（势力变更等）
 * @param {string} nodeId
 */
export function updateNodeVisual(nodeId) {
  if (!cy) return;
  const node = stateStore.getNode(nodeId);
  if (!node) return;
  const cyNode = cy.getElementById(nodeId);
  if (cyNode.length) {
    cyNode.data('factionId', node.factionId);
    cyNode.data('popCount', node.popCount);
  }
}

/** 刷新所有节点标签（语言切换后） */
export function refreshLabels() {
  if (!cy) return;
  cy.nodes().forEach(n => {
    const id = n.id();
    const node = stateStore.getNode(id);
    const label = i18n.t(`map.node.${id}`, {}) !== `map.node.${id}`
      ? i18n.t(`map.node.${id}`)
      : (node?.name || id);
    n.data('label', label);
  });
  refreshMapEntities();
}

export function refreshLogisticsVisuals() {
  if (!cy) return;
  cy.nodes().forEach(node => {
    node.data('rallyEnabled', !!stateStore.rallyPoints[node.id()]?.enabled);
  });
  cy.edges().forEach(edge => {
    edge.data('routeCount', 0);
    edge.data('manualRouteCount', 0);
    edge.data('autoRouteCount', 0);
  });
  const activeMarkerIds = new Set();
  for (const route of Object.values(stateStore.logistics || {})) {
    for (const edgeId of route.pathEdgeIds || (route.currentEdgeId ? [route.currentEdgeId] : [])) {
      const edge = cy.getElementById(edgeId);
      if (!edge.length) continue;
      edge.data('routeCount', (edge.data('routeCount') || 0) + 1);
      const key = route.mode === 'AUTO' ? 'autoRouteCount' : 'manualRouteCount';
      edge.data(key, (edge.data(key) || 0) + 1);
    }
    updateLogisticsMarkers(route, activeMarkerIds);
  }
  cy.nodes('[logisticsMarker]').forEach(marker => {
    if (!activeMarkerIds.has(marker.id())) marker.remove();
  });
  refreshMapEntities();
}

function updateLogisticsMarkers(route, activeMarkerIds) {
  for (const trip of route.trips || []) {
    const edgeId = route.pathEdgeIds?.[trip.currentPathIndex] || route.currentEdgeId;
    if (!edgeId) continue;
    const edge = stateStore.edges[edgeId];
    const sourceNode = edge && stateStore.nodes[edge.sourceNodeId];
    const targetNode = edge && stateStore.nodes[edge.targetNodeId];
    if (!sourceNode || !targetNode) continue;

    const markerId = `logi-${route.entityId}-${trip.tripId}`;
    activeMarkerIds.add(markerId);
    const progress = Math.max(0, Math.min(1, trip.edgeProgress || 0));
    const forward = !trip.returning;
    const start = forward ? sourceNode : targetNode;
    const end = forward ? targetNode : sourceNode;
    const position = {
      x: start.x + (end.x - start.x) * progress,
      y: start.y + (end.y - start.y) * progress,
    };
    const color = route.mode === 'AUTO' ? '#16A34A' : '#2563EB';
    const markerData = {
      id: markerId,
      label: trip.cargoAmount > 0 ? String(trip.cargoAmount) : '',
      logisticsMarker: true,
      markerColor: color,
      markerOpacity: trip.returning ? 0.55 : 0.9,
      markerSize: route.enabled ? 14 : 10,
    };
    const existing = cy.getElementById(markerId);
    if (existing.length) {
      existing.data(markerData);
      existing.position(position);
    } else {
      cy.add({ group: 'nodes', data: markerData, position });
    }
  }
}


export function refreshArmyVisuals() {
  if (!cy) return;
  const activeMarkerIds = new Set();
  for (const army of Object.values(stateStore.armies || {})) {
    const markerId = `army-${army.entityId}`;
    const position = armyPosition(army);
    if (!position) continue;
    activeMarkerIds.add(markerId);
    const markerData = {
      id: markerId,
      label: companyLabel(army),
      armyMarker: true,
      markerColor: army.factionId === 'PLAYER' ? '#2563EB' : army.factionId === 'AI' ? '#DC2626' : '#6B7280',
      markerSize: army.currentEdgeId ? 18 : 16 + Math.min(10, Math.max(0, Number(army.strength ?? army.troopCount ?? 0))),
    };
    const existing = cy.getElementById(markerId);
    if (existing.length) {
      existing.data(markerData);
      existing.position(position);
    } else {
      cy.add({ group: 'nodes', data: markerData, position });
    }
  }
  cy.nodes('[armyMarker]').forEach(marker => {
    if (!activeMarkerIds.has(marker.id())) marker.remove();
  });
  refreshMapEntities();
}

function companyLabel(army) {
  const strength = army.strength ?? army.troopCount ?? '';
  return army.currentEdgeId ? String(strength) : `${strength}`;
}

function armyPosition(army) {
  if (army.currentEdgeId) {
    const edge = stateStore.edges[army.currentEdgeId];
    const sourceNode = edge && stateStore.nodes[edge.sourceNodeId];
    const targetNode = edge && stateStore.nodes[edge.targetNodeId];
    if (!sourceNode || !targetNode) return null;
    const targetId = army.targetNodeId;
    const start = edge.targetNodeId === targetId ? sourceNode : targetNode;
    const end = edge.targetNodeId === targetId ? targetNode : sourceNode;
    const progress = Math.max(0, Math.min(1, army.edgeProgress || 0));
    return {
      x: start.x + (end.x - start.x) * progress,
      y: start.y + (end.y - start.y) * progress,
    };
  }
  const node = army.currentNodeId && stateStore.nodes[army.currentNodeId];
  if (!node) return null;
  const siblings = Object.values(stateStore.armies || {}).filter(unit => unit.currentNodeId === army.currentNodeId && !unit.currentEdgeId);
  const index = Math.max(0, siblings.findIndex(unit => unit.entityId === army.entityId));
  const angle = (index / Math.max(1, siblings.length)) * Math.PI * 2;
  const radius = 16;
  return { x: node.x + Math.cos(angle) * radius, y: node.y + Math.sin(angle) * radius };
}

function ensureEntityLayer(container) {
  if (!container) return;
  entityLayer = document.getElementById('map-entity-layer');
  if (!entityLayer) {
    entityLayer = document.createElement('div');
    entityLayer.id = 'map-entity-layer';
    entityLayer.className = 'map-entity-layer';
    container.appendChild(entityLayer);
  }
  mapTooltip = document.getElementById('map-tooltip');
  if (!mapTooltip) {
    mapTooltip = document.createElement('div');
    mapTooltip.id = 'map-tooltip';
    mapTooltip.className = 'map-tooltip';
    container.appendChild(mapTooltip);
  }
}

export function refreshMapEntities() {
  if (entityRefreshFrame) return;
  entityRefreshFrame = requestAnimationFrame(() => {
    entityRefreshFrame = null;
    renderMapEntities();
  });
}

function renderMapEntities() {
  if (!cy || !entityLayer) return;
  const cityHtml = Object.values(stateStore.nodes || {}).map(node => cityEntityHtml(node)).join('');
  const armyHtml = Object.values(stateStore.armies || {})
    .filter(army => army.currentEdgeId)
    .map(army => armySwarmHtml(army))
    .join('');
  entityLayer.innerHTML = cityHtml + armyHtml;
  bindEntityInteractions();
}

function nodeName(id) {
  return i18n.t(`map.node.${id}`, {}) !== `map.node.${id}`
    ? i18n.t(`map.node.${id}`)
    : (stateStore.nodes[id]?.name || id);
}

function cityEntityHtml(node) {
  const cyNode = cy.getElementById(node.id);
  if (!cyNode.length) return '';
  const pos = cyNode.renderedPosition();
  const size = node.isCapital ? 112 : 96;
  const faction = (node.factionId || 'NEUTRAL').toLowerCase();
  const wallLevel = Math.max(0, Math.min(5, node.wallLevel || 0));
  const troops = stationedTroops(node.id);
  return `
    <div class="city-entity faction-${faction} ${selectedNodeId === node.id ? 'selected' : ''}" data-node-id="${node.id}" style="left:${pos.x}px;top:${pos.y}px;width:${size}px;height:${size}px">
      <svg class="city-entity-svg" viewBox="0 0 100 100" aria-label="${nodeName(node.id)}">
        <polygon class="city-wall city-wall-level-${wallLevel}" points="50,7 78,18 93,45 84,78 51,94 18,82 7,51 18,20" />
        <rect class="city-building city-hall" x="42" y="40" width="16" height="18" rx="2" />
        <path class="city-building city-hall-roof" d="M39 40 L50 28 L61 40 Z" />
        ${buildingShape('farm', node.farmLevel, 'M13 70 h23 v13 h-23 z M17 73 v8 M24 73 v8 M31 73 v8')}
        ${buildingShape('mine', node.mineLevel, 'M13 25 l10 -14 l10 18 h-20 z M20 29 q6 -9 13 0')}
        ${buildingShape('arsenal', node.arsenalLevel, 'M64 64 h20 v17 h-20 z M68 64 v-9 h6 v9 M78 58 q8 -7 13 0')}
        ${buildingShape('beacon', node.beaconLevel, 'M71 37 v-24 M64 37 h14 M67 20 h8 l-4 -9 z M58 15 q13 -9 26 0')}
        ${resourceDots(node)}
        ${troopDots(troops.total || node.garrisonCount || 0, 50, wallLevel > 0 ? 63 : 58, faction)}
        <text class="city-map-label" x="50" y="99" text-anchor="middle">${nodeName(node.id)}</text>
      </svg>
    </div>
  `;
}

function buildingShape(kind, level, path) {
  const active = (level || 0) > 0 ? 'active' : 'inactive';
  return `<path class="city-building city-${kind} ${active} level-${Math.min(5, level || 0)}" d="${path}" />`;
}

function resourceDots(node) {
  const rocks = node.mineLevel > 0 ? '<circle class="city-resource mine" cx="8" cy="32" r="3" /><circle class="city-resource mine" cx="9" cy="39" r="2" />' : '';
  const trees = node.farmLevel > 0 ? '<circle class="city-resource farm" cx="91" cy="72" r="3" /><circle class="city-resource farm" cx="87" cy="80" r="2.5" />' : '';
  return rocks + trees;
}

function troopDots(total, centerX, centerY, faction) {
  if (total <= 0) return '';
  const count = Math.min(22, Math.max(3, Math.ceil(total / 5)));
  return Array.from({ length: count }, (_, index) => {
    const cols = Math.ceil(Math.sqrt(count));
    const x = centerX + (index % cols - (cols - 1) / 2) * 4;
    const y = centerY + (Math.floor(index / cols) - 1) * 4;
    return `<circle class="army-dot faction-${faction}" cx="${x}" cy="${y}" r="1.5" />`;
  }).join('');
}

function armySwarmHtml(army) {
  const pos = armyPosition(army);
  if (!pos) return '';
  const rendered = renderedPosition(pos);
  const faction = (army.factionId || 'NEUTRAL').toLowerCase();
  const strength = Number(army.strength ?? army.troopCount ?? 0);
  const dots = troopDots(strength, 24, 24, faction);
  return `
    <div class="army-swarm faction-${faction}" data-army-id="${army.entityId}" style="left:${rendered.x}px;top:${rendered.y}px">
      <svg viewBox="0 0 48 48" class="army-swarm-svg">${dots}<path class="army-direction" d="M24 6 l6 8 h-4 v9 h-4 v-9 h-4 z" /></svg>
      <span>${strength}</span>
    </div>
  `;
}

function renderedPosition(position) {
  const pan = cy.pan();
  const zoom = cy.zoom();
  return { x: position.x * zoom + pan.x, y: position.y * zoom + pan.y };
}

function bindEntityInteractions() {
  entityLayer.querySelectorAll('.city-entity').forEach(el => {
    el.addEventListener('click', () => selectNode(el.dataset.nodeId));
    el.addEventListener('mouseenter', (event) => showCityTooltip(el.dataset.nodeId, event));
    el.addEventListener('mousemove', moveTooltip);
    el.addEventListener('mouseleave', hideTooltip);
  });
  entityLayer.querySelectorAll('.army-swarm').forEach(el => {
    el.addEventListener('mouseenter', (event) => showArmyTooltip(Number(el.dataset.armyId), event));
    el.addEventListener('mousemove', moveTooltip);
    el.addEventListener('mouseleave', hideTooltip);
  });
}

function showCityTooltip(nodeId, event) {
  const node = stateStore.nodes[nodeId];
  if (!node || !mapTooltip) return;
  const troops = stationedTroops(node.id);
  mapTooltip.innerHTML = `
    <strong>${nodeName(node.id)}</strong>
    <span>${i18n.t('faction.' + (node.factionId === 'PLAYER' ? 'player' : node.factionId === 'AI' ? 'ai' : 'neutral'))}</span>
    <span>${i18n.t('panel.population')}: ${node.popCount || 0}</span>
    <span>城墙: Lv.${node.wallLevel || 0} · HP ${node.wallHpCurrent || 0}</span>
    <span>🌾 ${node.invFood || 0} · ⛏️ ${node.invIron || 0} · 💥 ${node.invAmmo || 0}</span>
    <span>${i18n.t('panel.companies')}: ${troops.companies || 0} · ${troops.total || node.garrisonCount || 0}</span>
  `;
  moveTooltip(event);
  mapTooltip.classList.add('visible');
}

function showArmyTooltip(entityId, event) {
  const army = stateStore.armies?.[entityId];
  if (!army || !mapTooltip) return;
  mapTooltip.innerHTML = `
    <strong>${army.name || `${army.unitDefId || 'UNIT'} #${army.entityId}`}</strong>
    <span>${i18n.t('panel.strength')}: ${army.strength ?? army.troopCount ?? 0}</span>
    <span>${army.state || 'MOVING'} → ${nodeName(army.targetNodeId)}</span>
    <span>${Math.round((army.edgeProgress || 0) * 100)}%</span>
  `;
  moveTooltip(event);
  mapTooltip.classList.add('visible');
}

function moveTooltip(event) {
  if (!mapTooltip) return;
  mapTooltip.style.left = `${event.clientX + 12}px`;
  mapTooltip.style.top = `${event.clientY + 12}px`;
}

function hideTooltip() {
  mapTooltip?.classList.remove('visible');
}

function stationedTroops(nodeId) {
  const companies = Object.values(stateStore.armies || {})
    .filter(army => army.currentNodeId === nodeId && !army.currentEdgeId);
  return {
    companies: companies.length,
    total: companies.reduce((sum, army) => sum + Number(army.strength ?? army.troopCount ?? 0), 0),
  };
}

export function getCy() { return cy; }
