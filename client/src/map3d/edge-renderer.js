/**
 * 道路渲染器 — ROAD / TRAIL / RAILWAY 3D 路径 + 物流可视化
 */
import * as THREE from 'three';
import { ROAD_CONFIG, WORLD_SCALE } from './constants.js';
import { toWorldXZ } from './world-space.js';
import { getHeightAt } from './terrain-generator.js';

const edgeMeshes = new Map(); // edgeId → THREE.Group
let logisticsParticles = new Map(); // routeId → particle system
let scene = null;

export function initEdgeRenderer(sceneRef) { scene = sceneRef; }

/**
 * 创建所有道路
 * @param {object} edges - stateStore.edges
 * @param {object} nodes - stateStore.nodes
 */
export function createAllEdges(edges, nodes) {
  for (const edge of Object.values(edges)) {
    const group = createEdgeMesh(edge, nodes);
    if (group) {
      scene.add(group);
      edgeMeshes.set(edge.id, group);
    }
  }
}

/**
 * 创建单条道路 3D 网格
 */
function createEdgeMesh(edge, nodes) {
  const srcNode = nodes[edge.sourceNodeId];
  const tgtNode = nodes[edge.targetNodeId];
  if (!srcNode || !tgtNode) return null;

  const src = toWorldXZ(srcNode.x, srcNode.y);
  const tgt = toWorldXZ(tgtNode.x, tgtNode.y);
  const edgeType = edge.edgeType || 'ROAD';
  const config = ROAD_CONFIG[edgeType] || ROAD_CONFIG.ROAD;

  // 沿地形表面生成路径点
  const pathPoints = [];
  const segments = config.segments;
  for (let i = 0; i <= segments; i++) {
    const t = i / segments;
    const wx = src.x + (tgt.x - src.x) * t;
    const wz = src.z + (tgt.z - src.z) * t;
    const wy = getHeightAt(wx, wz) + 0.02; // 略微高于地面
    pathPoints.push(new THREE.Vector3(wx, wy, wz));
  }

  const group = new THREE.Group();
  group.userData = { type: 'edge', edgeId: edge.id, edgeType };

  if (edgeType === 'RAILWAY') {
    // 铁路：两条铁轨 + 枕木
    createRailwayMesh(group, pathPoints, config);
  } else {
    // 普通道路：贴地条带
    createRoadMesh(group, pathPoints, config);
  }

  return group;
}

function createRoadMesh(group, pathPoints, config) {
  // 使用 TubeGeometry 沿路径生成道路
  const curve = new THREE.CatmullRomCurve3(pathPoints);
  const tubeGeo = new THREE.TubeGeometry(curve, config.segments, config.width / 2, 4, false);
  const tubeMat = new THREE.MeshStandardMaterial({
    color: config.color, roughness: 0.9, metalness: 0.0,
  });
  const mesh = new THREE.Mesh(tubeGeo, tubeMat);
  mesh.scale.y = 0.15; // 压扁成路面
  mesh.receiveShadow = true;
  group.add(mesh);
}

function createRailwayMesh(group, pathPoints, config) {
  const curve = new THREE.CatmullRomCurve3(pathPoints);

  // 两条铁轨
  const offset = config.width / 2;
  for (const side of [-1, 1]) {
    const railPoints = [];
    for (let i = 0; i <= config.segments; i++) {
      const t = i / config.segments;
      const pt = curve.getPoint(t);
      const tangent = curve.getTangent(t);
      const normal = new THREE.Vector3(-tangent.z, 0, tangent.x).normalize();
      railPoints.push(pt.clone().add(normal.multiplyScalar(side * offset)));
    }
    const railCurve = new THREE.CatmullRomCurve3(railPoints);
    const railGeo = new THREE.TubeGeometry(railCurve, config.segments, config.railWidth, 3, false);
    const railMat = new THREE.MeshStandardMaterial({ color: 0x666666, metalness: 0.8, roughness: 0.3 });
    const rail = new THREE.Mesh(railGeo, railMat);
    group.add(rail);
  }

  // 枕木
  const tieCount = Math.floor(curve.getLength() / config.tieSpacing);
  const tieMat = new THREE.MeshStandardMaterial({ color: 0x6b4f3a, roughness: 0.9 });
  for (let i = 0; i <= tieCount; i++) {
    const t = i / tieCount;
    const pt = curve.getPoint(t);
    const tangent = curve.getTangent(t);
    const normal = new THREE.Vector3(-tangent.z, 0, tangent.x).normalize();

    const tieGeo = new THREE.BoxGeometry(config.width * 1.2, 0.04, 0.08);
    const tie = new THREE.Mesh(tieGeo, tieMat);
    tie.position.copy(pt);
    tie.position.y += 0.02;
    tie.lookAt(pt.clone().add(tangent));
    group.add(tie);
  }
}

/**
 * 更新物流路线可视化
 * @param {object} logistics - stateStore.logistics
 */
export function refreshLogisticsVisuals(logistics) {
  // 移除旧粒子
  for (const [id, particles] of logisticsParticles) {
    scene.remove(particles);
    particles.geometry.dispose();
    particles.material.dispose();
  }
  logisticsParticles.clear();

  if (!logistics) return;

  // 为每条活跃路线添加发光粒子
  for (const route of Object.values(logistics)) {
    if (!route.enabled) continue;
    // 简化：在路线的每条边上添加移动点
    if (route.pathEdgeIds) {
      for (const edgeId of route.pathEdgeIds) {
        const edgeGroup = edgeMeshes.get(edgeId);
        if (!edgeGroup) continue;
        // 添加发光点
        addLogisticsGlow(edgeId, route);
      }
    }
  }
}

function addLogisticsGlow(edgeId, route) {
  const key = `${edgeId}_${route.entityId}`;
  if (logisticsParticles.has(key)) return;

  const geo = new THREE.SphereGeometry(0.08, 6, 6);
  const color = route.mode === 'AUTO' ? 0x16A34A : 0x2563EB;
  const mat = new THREE.MeshBasicMaterial({ color });
  const mesh = new THREE.Mesh(geo, mat);
  mesh.userData = { routeId: route.entityId, edgeId, progress: Math.random() };
  scene.add(mesh);
  logisticsParticles.set(key, mesh);
}

/**
 * 沿边获取 3D 位置（0-1 进度）
 */
export function getEdgePosition3D(edgeId, progress, nodes) {
  const edge = stateStore?.edges?.[edgeId];
  if (!edge) return null;

  const src = nodes?.[edge.sourceNodeId];
  const tgt = nodes?.[edge.targetNodeId];
  if (!src || !tgt) return null;

  const srcW = toWorldXZ(src.x, src.y);
  const tgtW = toWorldXZ(tgt.x, tgt.y);

  const wx = srcW.x + (tgtW.x - srcW.x) * progress;
  const wz = srcW.z + (tgtW.z - srcW.z) * progress;
  const wy = getHeightAt(wx, wz);

  return new THREE.Vector3(wx, wy + 0.1, wz);
}

/** 清理 */
export function clearAllEdges() {
  for (const [id, group] of edgeMeshes) {
    scene.remove(group);
    group.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) obj.material.dispose();
    });
  }
  edgeMeshes.clear();
  for (const [id, mesh] of logisticsParticles) {
    scene.remove(mesh);
    mesh.geometry.dispose();
    mesh.material.dispose();
  }
  logisticsParticles.clear();
}

import { stateStore } from '../bridge/state-store.js';
