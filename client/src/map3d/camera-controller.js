/**
 * 相机控制器 — OrbitControls + 焦点动画
 */
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { CAMERA_DEFAULTS, WORLD_SCALE } from './constants.js';
import { toWorldXZ } from './world-space.js';

let controls = null;
let focusTarget = null;
let focusLerpSpeed = 0;

/**
 * 初始化轨道控制器
 * @param {THREE.PerspectiveCamera} camera
 * @param {HTMLElement} domElement
 * @returns {OrbitControls}
 */
export function initCameraController(camera, domElement) {
  const cam = CAMERA_DEFAULTS;

  controls = new OrbitControls(camera, domElement);
  controls.minPolarAngle = cam.minPolarAngle;
  controls.maxPolarAngle = cam.maxPolarAngle;
  controls.minDistance = cam.minDistance;
  controls.maxDistance = cam.maxDistance;
  controls.enableDamping = true;
  controls.dampingFactor = cam.dampingFactor;
  controls.enablePan = true;
  controls.panSpeed = 1.0;
  controls.enableRotate = true;
  controls.rotateSpeed = 0.5;

  // 默认视角
  camera.position.set(15, 20, 25);
  controls.target.set(15, 0, 12);
  controls.update();

  return controls;
}

/**
 * 聚焦到 3D 世界坐标
 */
export function focusOnPosition(worldX, worldZ, distance) {
  focusTarget = { x: worldX, z: worldZ };
  focusLerpSpeed = CAMERA_DEFAULTS.focusLerpSpeed;

  if (distance && controls) {
    const cam = controls.object;
    const dir = new THREE.Vector3().subVectors(cam.position, controls.target).normalize();
    cam.position.copy(new THREE.Vector3(worldX, 0, worldZ).add(dir.multiplyScalar(distance)));
  }
}

/**
 * 聚焦到节点（后端坐标）
 */
export function focusOnNodeByData(backendX, backendY) {
  const { x, z } = toWorldXZ(backendX, backendY);
  focusOnPosition(x, z);
}

/** 框选全地图 */
export function fitAll() {
  if (!controls) return;
  const cam = controls.object;
  cam.position.set(15, 30, 35);
  controls.target.set(15, 0, 12);
  controls.update();
}

/** 每帧更新（在 beforeRender 中调用） */
export function updateCamera() {
  if (!controls) return;

  if (focusTarget) {
    const target = controls.target;
    target.x += (focusTarget.x - target.x) * focusLerpSpeed;
    target.z += (focusTarget.z - target.z) * focusLerpSpeed;

    if (Math.abs(focusTarget.x - target.x) < 0.01 &&
        Math.abs(focusTarget.z - target.z) < 0.01) {
      target.x = focusTarget.x;
      target.z = focusTarget.z;
      focusTarget = null;
    }
  }

  controls.update();
}

/** 获取 OrbitControls 实例 */
export function getControls() { return controls; }

/** 启用/禁用控制器 */
export function setControlsEnabled(enabled) {
  if (controls) controls.enabled = enabled;
}
