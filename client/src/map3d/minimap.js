/**
 * 迷你地图 — 正交相机小地图 + 视口指示器
 */
import * as THREE from 'three';
import { MINIMAP_CONFIG, TERRAIN_GEN } from './constants.js';
import { getScene, getCamera } from './scene-manager.js';
import { focusOnPosition } from './camera-controller.js';

let minimapRenderer = null;
let minimapCamera = null;
let minimapContainer = null;
let viewportRect = null;

/**
 * 初始化迷你地图
 * @param {HTMLElement} container - 放置小地图的 DOM 容器
 */
export function initMinimap(container) {
  minimapContainer = container;

  const cfg = MINIMAP_CONFIG;

  // 正交相机，从正上方俯视
  const size = TERRAIN_GEN.worldSize;
  const aspect = cfg.width / cfg.height;
  minimapCamera = new THREE.OrthographicCamera(
    -size * aspect / 2, size * aspect / 2,
    size / 2, -size / 2,
    0.1, 200
  );
  minimapCamera.position.set(15, 50, 12);
  minimapCamera.lookAt(15, 0, 12);

  // 渲染器
  minimapRenderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
  minimapRenderer.setSize(cfg.width, cfg.height);
  minimapRenderer.setClearColor(cfg.backgroundColor, 0.8);
  container.appendChild(minimapRenderer.domElement);

  // 视口矩形
  viewportRect = document.createElement('div');
  viewportRect.className = 'minimap-viewport';
  viewportRect.style.cssText = `
    position: absolute;
    border: ${cfg.borderWidth}px solid ${cfg.borderColor};
    pointer-events: none;
    transition: all 0.1s;
  `;
  container.appendChild(viewportRect);

  // 点击跳转
  minimapRenderer.domElement.addEventListener('click', onMinimapClick);
}

/**
 * 更新迷你地图（每几帧调用一次）
 */
export function updateMinimap() {
  if (!minimapRenderer || !minimapCamera) return;

  minimapRenderer.render(getScene(), minimapCamera);
  updateViewportRect();
}

/**
 * 更新视口矩形位置
 */
function updateViewportRect() {
  if (!viewportRect || !minimapContainer) return;

  const camera = getCamera();
  if (!camera) return;

  // 简化：基于相机位置计算视口在小地图上的位置
  const size = TERRAIN_GEN.worldSize;
  const cfg = MINIMAP_CONFIG;

  // 相机 → 小地图坐标
  const camPos = camera.position;
  const mapX = ((camPos.x - (-size/2)) / size) * cfg.width;
  const mapY = ((size/2 - camPos.z) / size) * cfg.height;

  // 视口大小（基于缩放）
  const zoom = camera.zoom || 1;
  const vpW = cfg.width / zoom * 0.3;
  const vpH = cfg.height / zoom * 0.3;

  viewportRect.style.left = `${mapX - vpW/2}px`;
  viewportRect.style.top = `${mapY - vpH/2}px`;
  viewportRect.style.width = `${vpW}px`;
  viewportRect.style.height = `${vpH}px`;
}

/**
 * 点击小地图跳转
 */
function onMinimapClick(event) {
  const rect = minimapRenderer.domElement.getBoundingClientRect();
  const x = event.clientX - rect.left;
  const y = event.clientY - rect.top;

  const size = TERRAIN_GEN.worldSize;
  const cfg = MINIMAP_CONFIG;

  const worldX = (x / cfg.width) * size - size/2 + 15; // 偏移到场景中心
  const worldZ = size/2 - (y / cfg.height) * size + 12;

  // 使用 camera-controller 聚焦
  focusOnPosition(worldX, worldZ);
}

/** 销毁迷你地图 */
export function disposeMinimap() {
  if (minimapRenderer) {
    minimapRenderer.dispose();
    minimapRenderer.domElement?.remove();
  }
  if (viewportRect) viewportRect.remove();
  minimapRenderer = null;
  minimapCamera = null;
  minimapContainer = null;
  viewportRect = null;
}
