/**
 * 3D 地图模块 — 对外 API（与原 map-view.js 接口一致 + 新增功能）
 */
import { stateStore } from '../bridge/state-store.js';
import { eventBus } from '../ui/event-bus.js';
import { initScene, startLoop, stopLoop, onBeforeRender, getScene, getCamera, getRenderer, dispose as disposeScene } from './scene-manager.js';
import { initCameraController, updateCamera, focusOnNodeByData, fitAll, setControlsEnabled } from './camera-controller.js';
import { generateTerrain, getHeightAt } from './terrain-generator.js';
import { initNodeRenderer, createAllSettlements, updateNodeSettlement, setNodeSelected, getNodePosition3D, clearAllSettlements } from './node-renderer.js';
import { initEdgeRenderer, createAllEdges, refreshLogisticsVisuals as refreshEdgeLogistics, clearAllEdges } from './edge-renderer.js';
import { initArmyRenderer, createAllArmies, refreshArmyVisuals as updateArmyVisuals, clearAllArmies } from './army-renderer.js';
import { initLogisticsRenderer, refreshLogisticsVisuals as updateLogisticsMeshes, clearAllLogistics } from './logistics-renderer.js';
import { initWildRenderer, createAllWildEntities, refreshWildEntities, clearAllWildEntities } from './wild-renderer.js';
import { initSelectionHandler } from './selection-handler.js';
import { initMinimap, updateMinimap, disposeMinimap } from './minimap.js';
import { initMapLabels, createAllLabels, getAllLabels, refreshLabels as refreshLabelTexts, renderLabels, clearAllLabels } from './map-labels.js';
import { startPlacement, cancelPlacement, isPlacingMode } from './building-placer.js';
import { startWallBuild as enterWallBuildMode, cancelWallBuild, isWallBuildMode } from './wall-builder.js';

let initialized = false;
let minimapFrameCount = 0;

// ─── 对外 API ───

/**
 * 初始化 3D 地图
 * @param {string} containerId - DOM 容器 ID
 */
export function initMap(containerId) {
  const container = document.getElementById(containerId);
  if (!container) {
    console.error(`[Map3D] 容器 #${containerId} 不存在`);
    return;
  }

  // 初始化 Three.js 场景
  const { scene, camera, renderer } = initScene(container);

  // 初始化相机控制器
  initCameraController(camera, renderer.domElement);

  // 初始化各渲染器
  initNodeRenderer(scene);
  initEdgeRenderer(scene);
  initArmyRenderer(scene);
  initLogisticsRenderer(scene);
  initWildRenderer(scene);

  // 初始化交互
  initSelectionHandler();

  // 初始化标签
  initMapLabels(container);

  // 初始化迷你地图
  const minimapContainer = document.getElementById('minimap-container');
  if (minimapContainer) {
    initMinimap(minimapContainer);
  }

  // 注册每帧回调
  onBeforeRender(onFrame);

  initialized = true;
  console.log('[Map3D] 3D 地图初始化完成');
}

/**
 * 加载地图数据（从 stateStore 读取）
 */
export function loadMapData() {
  if (!initialized) return;

  const nodes = stateStore.nodes || {};
  const edges = stateStore.edges || {};

  // 清除旧数据
  clearAllSettlements();
  clearAllEdges();
  clearAllArmies();
  clearAllLogistics();
  clearAllWildEntities();
  clearAllLabels();

  const scene = getScene();

  // 生成地形
  const { groundMesh, waterMesh, decorationsGroup } = generateTerrain(nodes);
  // 移除旧地形
  const oldGround = scene.getObjectByName('terrain');
  const oldWater = scene.getObjectByName('water');
  const oldDeco = scene.getObjectByName('decorations');
  if (oldGround) scene.remove(oldGround);
  if (oldWater) scene.remove(oldWater);
  if (oldDeco) scene.remove(oldDeco);

  scene.add(groundMesh);
  scene.add(waterMesh);
  scene.add(decorationsGroup);

  // 创建城市
  createAllSettlements(nodes);

  // 创建道路
  createAllEdges(edges, nodes);

  // 创建军队
  const armies = stateStore.armies || {};
  createAllArmies(armies);

  // 创建野外实体
  createAllWildEntities();

  // 创建标签
  createAllLabels(nodes);
  const sceneRef = getScene();
  for (const [id, label] of getAllLabels()) {
    sceneRef.add(label);
  }

  // 启动动画循环
  startLoop();

  // 框选全地图
  fitAll();

  console.log('[Map3D] 地图数据加载完成');
}

/**
 * 选中节点
 */
export function selectNode(nodeId) {
  setNodeSelected(nodeId, true);
  const node = stateStore.nodes[nodeId];
  if (node) {
    focusOnNodeByData(node.x, node.y);
  }
}

/**
 * 取消所有选中
 */
export function deselectAll() {
  setNodeSelected(null, false);
}

/**
 * 更新单个节点视觉
 */
export function updateNodeVisual(nodeId) {
  updateNodeSettlement(nodeId);
}

/**
 * 刷新标签（i18n 切换后）
 */
export function refreshLabels() {
  refreshLabelTexts(stateStore.nodes);
}

/**
 * 刷新物流可视化
 */
export function refreshLogisticsVisuals() {
  refreshEdgeLogistics(stateStore.logistics);
  updateLogisticsMeshes();
}

/**
 * 刷新军队可视化
 */
export function refreshArmyVisuals() {
  updateArmyVisuals();
}

/**
 * 刷新所有地图实体
 */
export function refreshMapEntities() {
  updateArmyVisuals();
  updateLogisticsMeshes();
  refreshWildEntities();
}

/**
 * 兼容旧接口 — 返回 null
 */
export function getCy() { return null; }

/**
 * 新增：开始建筑放置
 */
export function startBuildingPlacement(buildingType, cityNodeId) {
  startPlacement(buildingType, cityNodeId);
}

/**
 * 新增：开始城墙绘制
 */
export function startWallBuild(cityNodeId) {
  enterWallBuildMode(cityNodeId);
}

/**
 * 新增：聚焦节点
 */
export function focusOnNode(nodeId) {
  const node = stateStore.nodes[nodeId];
  if (node) focusOnNodeByData(node.x, node.y);
}

// ─── 内部 ───

/**
 * 每帧回调
 */
function onFrame() {
  // 更新相机
  updateCamera();

  // 更新军队位置（插值）
  updateArmyVisuals();

  // 更新物流
  updateLogisticsMeshes();

  // 渲染标签
  renderLabels();

  // 迷你地图（每 5 帧更新一次）
  minimapFrameCount++;
  if (minimapFrameCount % 5 === 0) {
    updateMinimap();
  }
}
