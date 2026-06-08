/**
 * 城墙点击连线交互 — A→B 连线 + 封闭环检测
 */
import * as THREE from 'three';
import { WALL_CONFIG, CITY_CONFIG } from './constants.js';
import { getHeightAt } from './terrain-generator.js';
import { stateStore } from '../bridge/state-store.js';
import { sendBuildWall } from '../bridge/command-sender.js';
import { toWorldXZ } from './world-space.js';
import { getScene } from './scene-manager.js';

let isBuilding = false;
let currentCityNodeId = null;
let wallPoints = []; // [{ x, z }] 已确认的城墙点
let previewLine = null;
let pointMarkers = [];
let scene = null;

/**
 * 进入城墙绘制模式
 * @param {string} cityNodeId - 所属城市节点 ID
 */
export function startWallBuild(cityNodeId) {
  if (isBuilding) cancelWallBuild();

  isBuilding = true;
  currentCityNodeId = cityNodeId;
  wallPoints = [];
  scene = getScene();

  document.body.style.cursor = 'crosshair';
}

/**
 * 添加一个城墙点
 * @param {number} worldX - 3D 世界 X
 * @param {number} worldZ - 3D 世界 Z
 * @param {boolean} [isDoubleClick] - 双击表示封闭
 */
export function addWallPoint(worldX, worldZ, isDoubleClick = false) {
  if (!isBuilding) return;

  const node = stateStore.nodes[currentCityNodeId];
  if (!node) return;

  const { x: cityWX, z: cityWZ } = toWorldXZ(node.x, node.y);
  const localX = worldX - cityWX;
  const localZ = worldZ - cityWZ;

  // 检查是否点击了起点附近（封闭环）
  if (wallPoints.length >= 3 && !isDoubleClick) {
    const first = wallPoints[0];
    const dist = Math.sqrt((localX - first.x) ** 2 + (localZ - first.z) ** 2);
    if (dist < 0.5) {
      closeWallLoop();
      return;
    }
  }

  wallPoints.push({ x: localX, z: localZ });

  // 添加点标记
  const markerGeo = new THREE.SphereGeometry(0.15, 8, 8);
  const markerMat = new THREE.MeshStandardMaterial({ color: 0xff6600, emissive: 0xff6600, emissiveIntensity: 0.3 });
  const marker = new THREE.Mesh(markerGeo, markerMat);
  marker.position.set(worldX, getHeightAt(worldX, worldZ) + 0.3, worldZ);
  scene.add(marker);
  pointMarkers.push(marker);

  // 如果有前一个点，发送城墙段命令
  if (wallPoints.length >= 2) {
    const prev = wallPoints[wallPoints.length - 2];
    const curr = wallPoints[wallPoints.length - 1];

    sendBuildWall(currentCityNodeId, prev.x, prev.z, curr.x, curr.z);
  }

  // 双击封闭
  if (isDoubleClick && wallPoints.length >= 3) {
    closeWallLoop();
  }
}

/**
 * 封闭城墙环
 */
function closeWallLoop() {
  if (wallPoints.length < 3) return;

  const first = wallPoints[0];
  const last = wallPoints[wallPoints.length - 1];

  sendBuildWall(currentCityNodeId, last.x, last.z, first.x, first.z);
  finishWallBuild();
}

/**
 * 结束城墙绘制（不封闭）
 */
export function finishWallBuild() {
  cleanup();
  isBuilding = false;
  currentCityNodeId = null;
  document.body.style.cursor = 'default';
}

/**
 * 取消城墙绘制（移除所有未确认标记）
 */
export function cancelWallBuild() {
  cleanup();
  isBuilding = false;
  currentCityNodeId = null;
  wallPoints = [];
  document.body.style.cursor = 'default';
}

/**
 * 更新预览线（鼠标移动时）
 * @param {number} worldX
 * @param {number} worldZ
 */
export function updateWallPreview(worldX, worldZ) {
  if (!isBuilding || wallPoints.length === 0) return;

  // 移除旧预览线
  if (previewLine) {
    scene.remove(previewLine);
    previewLine.geometry.dispose();
    previewLine.material.dispose();
  }

  const last = wallPoints[wallPoints.length - 1];
  const node = stateStore.nodes[currentCityNodeId];
  if (!node) return;

  const { x: cityWX, z: cityWZ } = toWorldXZ(node.x, node.y);
  const fromWorldX = cityWX + last.x;
  const fromWorldZ = cityWZ + last.z;

  const points = [
    new THREE.Vector3(fromWorldX, getHeightAt(fromWorldX, fromWorldZ) + WALL_CONFIG.heightBase / 2, fromWorldZ),
    new THREE.Vector3(worldX, getHeightAt(worldX, worldZ) + WALL_CONFIG.heightBase / 2, worldZ),
  ];

  const geo = new THREE.BufferGeometry().setFromPoints(points);
  const mat = new THREE.LineBasicMaterial({ color: 0xff6600, linewidth: 2 });
  previewLine = new THREE.Line(geo, mat);
  scene.add(previewLine);
}

/** 是否在城墙绘制模式 */
export function isWallBuildMode() { return isBuilding; }

/** 获取已确认的城墙点数 */
export function getWallPointCount() { return wallPoints.length; }

/** 清理临时对象 */
function cleanup() {
  if (previewLine) {
    scene.remove(previewLine);
    previewLine.geometry.dispose();
    previewLine.material.dispose();
    previewLine = null;
  }
  for (const marker of pointMarkers) {
    scene.remove(marker);
    marker.geometry.dispose();
    marker.material.dispose();
  }
  pointMarkers = [];
  wallPoints = [];
}
