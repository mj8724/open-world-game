/**
 * 3D 地图标签 — CSS2DRenderer 节点名称标签
 */
import * as THREE from 'three';
import { CSS2DRenderer, CSS2DObject } from 'three/addons/renderers/CSS2DRenderer.js';
import { toWorldXZ } from './world-space.js';
import { getHeightAt } from './terrain-generator.js';
import { FACTION_CSS_COLORS } from './constants.js';
import { getScene, getCamera } from './scene-manager.js';
import i18n from '../i18n/i18n.js';

let labelRenderer = null;
const labels = new Map(); // nodeId → CSS2DObject

/**
 * 初始化标签渲染器
 * @param {HTMLElement} container
 */
export function initMapLabels(container) {
  labelRenderer = new CSS2DRenderer();
  labelRenderer.setSize(container.clientWidth, container.clientHeight);
  labelRenderer.domElement.style.position = 'absolute';
  labelRenderer.domElement.style.top = '0';
  labelRenderer.domElement.style.left = '0';
  labelRenderer.domElement.style.pointerEvents = 'none';
  container.appendChild(labelRenderer.domElement);
}

/**
 * 为所有节点创建标签
 * @param {object} nodes - stateStore.nodes
 */
export function createAllLabels(nodes) {
  for (const node of Object.values(nodes)) {
    const label = createNodeLabel(node);
    if (label) labels.set(node.id, label);
  }
}

/**
 * 创建节点标签
 */
function createNodeLabel(node) {
  const div = document.createElement('div');
  div.className = 'map3d-label';

  const faction = node.factionId || 'NEUTRAL';
  const color = FACTION_CSS_COLORS[faction] || FACTION_CSS_COLORS.NEUTRAL;

  div.style.cssText = `
    color: white;
    font-size: 11px;
    font-family: 'Inter', sans-serif;
    font-weight: 600;
    text-shadow: 0 1px 3px rgba(0,0,0,0.8), 0 0 1px rgba(0,0,0,1);
    padding: 1px 4px;
    border-bottom: 2px solid ${color};
    white-space: nowrap;
    pointer-events: none;
    user-select: none;
  `;

  // i18n 名称
  const name = i18n.t(`map.node.${node.id}`, node.name || node.id);
  div.textContent = name;

  const label = new CSS2DObject(div);
  const { x, z } = toWorldXZ(node.x, node.y);
  const y = getHeightAt(x, z) + 2.5; // 高于建筑
  label.position.set(x, y, z);

  return label;
}

/**
 * 刷新标签文本（i18n 切换后调用）
 */
export function refreshLabels(nodes) {
  for (const node of Object.values(nodes || {})) {
    const label = labels.get(node.id);
    if (label) {
      const name = i18n.t(`map.node.${node.id}`, node.name || node.id);
      label.element.textContent = name;

      // 更新势力色
      const faction = node.factionId || 'NEUTRAL';
      const color = FACTION_CSS_COLORS[faction] || FACTION_CSS_COLORS.NEUTRAL;
      label.element.style.borderBottom = `2px solid ${color}`;
    }
  }
}

/**
 * 渲染标签（在每帧调用）
 */
export function renderLabels() {
  if (labelRenderer) {
    labelRenderer.render(getScene(), getCamera());
  }
}

/**
 * 更新标签渲染器尺寸
 */
export function resizeLabels(width, height) {
  if (labelRenderer) labelRenderer.setSize(width, height);
}

/**
 * 根据缩放级别控制标签可见性
 * @param {number} zoomLevel - 相机缩放级别
 */
export function setLabelsVisible(zoomLevel) {
  for (const [id, label] of labels) {
    label.element.style.display = zoomLevel > 0.5 ? '' : 'none';
  }
}

/** 获取所有标签 */
export function getAllLabels() { return labels; }

/** 清理 */
export function clearAllLabels() {
  for (const [id, label] of labels) {
    if (label.parent) label.parent.remove(label);
    label.element?.remove();
  }
  labels.clear();
}
