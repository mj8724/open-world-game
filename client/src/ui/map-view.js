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
          'width': 36,
          'height': 36,
          'background-color': '#FFFFFF',
          'border-width': 3,
          'border-color': '#9CA3AF',
          'label': 'data(label)',
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

export function getSelectedNodeId() { return selectedNodeId; }
export function getCy() { return cy; }
