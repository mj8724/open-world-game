/**
 * 选中处理 — Raycaster 点击/悬停 + tooltip
 */
import * as THREE from 'three';
import { eventBus } from '../ui/event-bus.js';
import { getScene, getCamera, getRenderer } from './scene-manager.js';
import { isPlacingMode, updatePreviewPosition, confirmPlacement, cancelPlacement } from './building-placer.js';
import { isWallBuildMode, addWallPoint, updateWallPreview, cancelWallBuild } from './wall-builder.js';

const raycaster = new THREE.Raycaster();
const mouse = new THREE.Vector2();
let tooltip = null;
let hoveredObject = null;

/**
 * 初始化选中处理器
 */
export function initSelectionHandler() {
  const canvas = getRenderer()?.domElement;
  if (!canvas) return;

  // 创建 tooltip
  tooltip = document.createElement('div');
  tooltip.className = 'map3d-tooltip';
  tooltip.style.display = 'none';
  document.body.appendChild(tooltip);

  canvas.addEventListener('pointerdown', onPointerDown);
  canvas.addEventListener('pointermove', onPointerMove);
  canvas.addEventListener('contextmenu', onRightClick);
}

/**
 * 获取射线与场景中可点击对象的交点
 */
function raycast(event) {
  const rect = getRenderer().domElement.getBoundingClientRect();
  mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
  mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

  raycaster.setFromCamera(mouse, getCamera());

  const scene = getScene();
  const interactables = [];
  scene.traverse((obj) => {
    if (obj.userData?.type === 'node' || obj.userData?.type === 'army' ||
        obj.userData?.type === 'wildResource' || obj.userData?.type === 'neutralStructure') {
      interactables.push(obj);
    }
  });

  return raycaster.intersectObjects(interactables, true);
}

function onPointerDown(event) {
  if (event.button !== 0) return; // 只处理左键

  // 建筑放置模式
  if (isPlacingMode()) {
    const intersects = raycast(event);
    if (intersects.length > 0) {
      const point = intersects[0].point;
      updatePreviewPosition(point);
      confirmPlacement();
    }
    return;
  }

  // 城墙绘制模式
  if (isWallBuildMode()) {
    const intersects = raycast(event);
    if (intersects.length > 0) {
      const point = intersects[0].point;
      const isDouble = event.detail > 1; // 简化：不支持真正的双击检测
      addWallPoint(point.x, point.z, isDouble);
    }
    return;
  }

  // 正常选中
  const intersects = raycast(event);
  if (intersects.length > 0) {
    const obj = findRootInteractable(intersects[0].object);
    if (obj) {
      handleSelection(obj);
      return;
    }
  }

  // 点击空白处取消选中
  eventBus.emit('node-deselected');
}

function onPointerMove(event) {
  // 建筑预览
  if (isPlacingMode()) {
    const intersects = raycast(event);
    if (intersects.length > 0) {
      updatePreviewPosition(intersects[0].point);
    }
    return;
  }

  // 城墙预览
  if (isWallBuildMode()) {
    const intersects = raycast(event);
    if (intersects.length > 0) {
      updateWallPreview(intersects[0].point.x, intersects[0].point.z);
    }
    return;
  }

  // 悬停 tooltip
  const intersects = raycast(event);
  if (intersects.length > 0) {
    const obj = findRootInteractable(intersects[0].object);
    if (obj && obj !== hoveredObject) {
      hoveredObject = obj;
      showTooltip(event, obj);
    } else if (obj) {
      moveTooltip(event);
    }
  } else {
    hideTooltip();
    hoveredObject = null;
  }
}

function onRightClick(event) {
  event.preventDefault();
  if (isPlacingMode()) cancelPlacement();
  if (isWallBuildMode()) cancelWallBuild();
}

/**
 * 查找最近的交互根节点
 */
function findRootInteractable(obj) {
  let current = obj;
  while (current) {
    if (current.userData?.type === 'node' ||
        current.userData?.type === 'army' ||
        current.userData?.type === 'wildResource' ||
        current.userData?.type === 'neutralStructure') {
      return current;
    }
    current = current.parent;
  }
  return null;
}

/**
 * 处理选中逻辑
 */
function handleSelection(obj) {
  const { type, nodeId, entityId, id } = obj.userData;

  switch (type) {
    case 'node':
      eventBus.emit('node-selected', nodeId);
      break;
    case 'army':
      eventBus.emit('army-selected', entityId);
      break;
    case 'wildResource':
      eventBus.emit('wild-resource-selected', id);
      break;
    case 'neutralStructure':
      eventBus.emit('neutral-structure-selected', id);
      break;
  }
}

/**
 * Tooltip 显示/隐藏
 */
function showTooltip(event, obj) {
  if (!tooltip) return;

  const { type } = obj.userData;
  let text = '';

  switch (type) {
    case 'node':
      text = obj.userData.nodeId || '城市';
      break;
    case 'army':
      text = `军队 #${obj.userData.entityId}`;
      break;
    case 'wildResource':
      text = `资源点 ${obj.userData.id}`;
      break;
    case 'neutralStructure':
      text = `中立建筑 ${obj.userData.id}`;
      break;
  }

  tooltip.textContent = text;
  tooltip.style.display = 'block';
  moveTooltip(event);
}

function moveTooltip(event) {
  if (!tooltip) return;
  tooltip.style.left = `${event.clientX + 12}px`;
  tooltip.style.top = `${event.clientY + 12}px`;
}

function hideTooltip() {
  if (tooltip) tooltip.style.display = 'none';
}
