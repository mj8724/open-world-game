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
        selector: 'node:selected',
        style: {
          'border-width': 4,
          'border-color': '#1d4ed8',
          'background-color': '#eff6ff',
          'shadow-blur': 12,
          'shadow-color': 'rgba(37, 99, 235, 0.3)',
          'shadow-offset-x': 0,
          'shadow-offset-y': 0,
          'shadow-opacity': 1,
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

  cy.on('mouseover', 'node', (e) => {
    e.target.style('cursor', 'pointer');
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
      },
    });
  }

  cy.elements().remove();
  cy.add(elements);
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

export function getSelectedNodeId() { return selectedNodeId; }
export function getCy() { return cy; }
