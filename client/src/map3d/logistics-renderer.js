/**
 * 物流渲染器 — 运输车模型 + 路线可视化
 */
import * as THREE from 'three';
import { FACTION_COLORS_3D } from './constants.js';
import { toWorldXZ } from './world-space.js';
import { getHeightAt } from './terrain-generator.js';
import { stateStore } from '../bridge/state-store.js';

const logisticsMeshes = new Map(); // entityId → { cart, route, label }
let scene = null;

export function initLogisticsRenderer(sceneRef) { scene = sceneRef; }

/**
 * 刷新物流可视化
 */
export function refreshLogisticsVisuals() {
  const logistics = stateStore.logistics || {};

  // 移除已消失的路线
  for (const [entityId, data] of logisticsMeshes) {
    if (!logistics[entityId]) {
      removeLogisticsMesh(entityId);
    }
  }

  // 更新或创建
  for (const route of Object.values(logistics)) {
    let data = logisticsMeshes.get(route.entityId);

    if (!data) {
      data = createLogisticsMesh(route);
      if (data) {
        scene.add(data.group);
        logisticsMeshes.set(route.entityId, data);
      }
    }

    // 更新运输车位置
    if (data) {
      updateCartPosition(data, route);
    }
  }
}

/**
 * 创建物流路线可视化
 */
function createLogisticsMesh(route) {
  const group = new THREE.Group();
  group.userData = { type: 'logistics', entityId: route.entityId };

  // 路线线
  const nodes = stateStore.nodes;
  const pathPoints = [];
  if (route.pathNodeIds) {
    for (const nodeId of route.pathNodeIds) {
      const node = nodes[nodeId];
      if (node) {
        const { x, z } = toWorldXZ(node.x, node.y);
        pathPoints.push(new THREE.Vector3(x, getHeightAt(x, z) + 0.05, z));
      }
    }
  }

  let routeLine = null;
  if (pathPoints.length >= 2) {
    const lineGeo = new THREE.BufferGeometry().setFromPoints(pathPoints);
    const lineColor = route.mode === 'AUTO' ? 0x16A34A : 0x2563EB;
    const lineMat = new THREE.LineBasicMaterial({ color: lineColor, linewidth: 2, transparent: true, opacity: 0.6 });
    routeLine = new THREE.Line(lineGeo, lineMat);
    group.add(routeLine);
  }

  // 运输车
  const cart = createCartMesh(route);
  group.add(cart);

  return { group, cart, routeLine };
}

/**
 * 创建运输车模型
 */
function createCartMesh(route) {
  const group = new THREE.Group();

  // 车体
  const bodyGeo = new THREE.BoxGeometry(0.3, 0.15, 0.2);
  const bodyMat = new THREE.MeshStandardMaterial({ color: 0x8B7355, roughness: 0.8 });
  const body = new THREE.Mesh(bodyGeo, bodyMat);
  body.position.y = 0.12;
  group.add(body);

  // 车轮
  const wheelGeo = new THREE.CylinderGeometry(0.06, 0.06, 0.02, 8);
  const wheelMat = new THREE.MeshStandardMaterial({ color: 0x333333 });
  for (const [x, z] of [[-0.1, -0.12], [-0.1, 0.12], [0.1, -0.12], [0.1, 0.12]]) {
    const wheel = new THREE.Mesh(wheelGeo, wheelMat);
    wheel.rotation.x = Math.PI / 2;
    wheel.position.set(x, 0.06, z);
    group.add(wheel);
  }

  // 货物颜色标记
  const cargoGeo = new THREE.BoxGeometry(0.15, 0.08, 0.1);
  let cargoColor = 0x90EE90; // FOOD=绿
  if (route.cargoType === 'IRON') cargoColor = 0x808080;
  if (route.cargoType === 'AMMO') cargoColor = 0xDC2626;
  const cargoMat = new THREE.MeshStandardMaterial({ color: cargoColor });
  const cargo = new THREE.Mesh(cargoGeo, cargoMat);
  cargo.position.y = 0.23;
  group.add(cargo);

  group.castShadow = true;
  return group;
}

/**
 * 更新运输车位置
 */
function updateCartPosition(data, route) {
  const nodes = stateStore.nodes;
  const cart = data.cart;

  // 简化：使用 route 的当前边和进度
  if (route.currentEdgeId && stateStore.edges[route.edgeId || route.currentEdgeId]) {
    const edge = stateStore.edges[route.currentEdgeId];
    if (edge) {
      const srcNode = nodes[edge.sourceNodeId];
      const tgtNode = nodes[edge.targetNodeId];
      if (srcNode && tgtNode) {
        const src = toWorldXZ(srcNode.x, srcNode.y);
        const tgt = toWorldXZ(tgtNode.x, tgtNode.y);
        const t = route.edgeProgress || 0;
        const wx = src.x + (tgt.x - src.x) * t;
        const wz = src.z + (tgt.z - src.z) * t;
        const wy = getHeightAt(wx, wz);
        cart.position.set(wx, wy + 0.05, wz);
      }
    }
  }
}

function removeLogisticsMesh(entityId) {
  const data = logisticsMeshes.get(entityId);
  if (data) {
    scene.remove(data.group);
    data.group.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) obj.material.dispose();
    });
    logisticsMeshes.delete(entityId);
  }
}

/** 清理 */
export function clearAllLogistics() {
  for (const [id, data] of logisticsMeshes) {
    scene.remove(data.group);
    data.group.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) obj.material.dispose();
    });
  }
  logisticsMeshes.clear();
}
