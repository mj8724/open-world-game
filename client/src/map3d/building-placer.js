/**
 * 建筑自由放置交互 — 拖拽 + 碰撞检测 + 预览
 */
import * as THREE from 'three';
import { BUILDING_MODELS, CITY_CONFIG } from './constants.js';
import { getHeightAt } from './terrain-generator.js';
import { stateStore } from '../bridge/state-store.js';
import { sendPlaceBuilding } from '../bridge/command-sender.js';
import { toBackendXZ, toWorldXZ } from './world-space.js';
import { getScene, getCamera } from './scene-manager.js';

let isPlacing = false;
let currentBuildingType = null;
let currentCityNodeId = null;
let previewMesh = null;
let scene = null;

/**
 * 开始建筑放置模式
 * @param {string} buildingType - FARM | MINE | ARSENAL | ORACLE_BEACON
 * @param {string} cityNodeId - 所属城市节点 ID
 */
export function startPlacement(buildingType, cityNodeId) {
  if (isPlacing) cancelPlacement();

  isPlacing = true;
  currentBuildingType = buildingType;
  currentCityNodeId = cityNodeId;
  scene = getScene();

  // 创建预览网格
  const model = BUILDING_MODELS[buildingType] || BUILDING_MODELS.FARM;
  const geo = new THREE.BoxGeometry(model.w, model.hBase, model.d);
  const mat = new THREE.MeshStandardMaterial({
    color: model.color, transparent: true, opacity: 0.5,
  });
  previewMesh = new THREE.Mesh(geo, mat);
  previewMesh.position.y = model.hBase / 2;
  previewMesh.name = 'placementPreview';
  scene.add(previewMesh);

  // 添加屋顶预览
  if (model.roofColor) {
    const rGeo = new THREE.ConeGeometry(Math.max(model.w, model.d) * 0.7, model.hBase * 0.4, 4);
    const rMat = new THREE.MeshStandardMaterial({ color: model.roofColor, transparent: true, opacity: 0.5 });
    const rMesh = new THREE.Mesh(rGeo, rMat);
    rMesh.position.y = model.hBase + model.hBase * 0.2;
    rMesh.rotation.y = Math.PI / 4;
    previewMesh.add(rMesh);
  }

  document.body.style.cursor = 'crosshair';
}

/**
 * 更新预览位置（跟随鼠标 raycast 到地面）
 * @param {THREE.Vector3} worldPos - 鼠标射线与地面的交点
 */
export function updatePreviewPosition(worldPos) {
  if (!isPlacing || !previewMesh) return;

  const node = stateStore.nodes[currentCityNodeId];
  if (!node) return;

  const { x: cityWX, z: cityWZ } = toWorldXZ(node.x, node.y);
  const localX = worldPos.x - cityWX;
  const localZ = worldPos.z - cityWZ;

  // 检查是否在城市范围内
  const dist = Math.sqrt(localX * localX + localZ * localZ);
  const inRange = dist < CITY_CONFIG.influenceRadius;

  // 碰撞检测（简化：检查与已有建筑的距离）
  const canPlace = inRange && !checkCollision(localX, localZ);

  previewMesh.position.set(worldPos.x, getHeightAt(worldPos.x, worldPos.z) + previewMesh.geometry.parameters.height / 2, worldPos.z);
  previewMesh.material.color.set(canPlace ? 0x00ff00 : 0xff0000);
  previewMesh.userData = { localX, localZ, canPlace };
}

/**
 * 碰撞检测（简化版：检查与已有建筑的最小距离）
 */
function checkCollision(localX, localZ) {
  const node = stateStore.nodes[currentCityNodeId];
  if (!node || !node.placedBuildings) return false;

  const model = BUILDING_MODELS[currentBuildingType] || BUILDING_MODELS.FARM;
  const minDist = Math.max(model.w, model.d) * 0.6;

  for (const bld of node.placedBuildings) {
    const dx = localX - bld.localX;
    const dz = localZ - bld.localZ;
    if (Math.sqrt(dx * dx + dz * dz) < minDist) return true;
  }
  return false;
}

/**
 * 确认放置
 */
export function confirmPlacement() {
  if (!isPlacing || !previewMesh) return;

  const { localX, localZ, canPlace } = previewMesh.userData;
  if (!canPlace) return;

  // 发送命令到后端
  const node = stateStore.nodes[currentCityNodeId];
  if (!node) return;

  // 将局部坐标转回后端坐标偏移
  sendPlaceBuilding(
    currentCityNodeId,
    currentBuildingType,
    localX,
    localZ,
    0 // rotation
  );

  cancelPlacement();
}

/**
 * 取消放置模式
 */
export function cancelPlacement() {
  if (previewMesh && scene) {
    scene.remove(previewMesh);
    previewMesh.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) obj.material.dispose();
    });
    previewMesh = null;
  }
  isPlacing = false;
  currentBuildingType = null;
  currentCityNodeId = null;
  document.body.style.cursor = 'default';
}

/** 是否正在放置 */
export function isPlacingMode() { return isPlacing; }

/** 获取当前放置类型 */
export function getCurrentBuildingType() { return currentBuildingType; }
