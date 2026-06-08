/**
 * 城市节点渲染器 — 3D 建筑实例 + 城墙段 + 旗帜 + 选中高亮
 */
import * as THREE from 'three';
import { BUILDING_MODELS, WALL_CONFIG, CITY_CONFIG, FACTION_COLORS_3D } from './constants.js';
import { toWorldXZ } from './world-space.js';
import { getHeightAt } from './terrain-generator.js';
import { stateStore } from '../bridge/state-store.js';

const nodeGroups = new Map(); // nodeId → THREE.Group
let selectedNodeId = null;
let scene = null;

/**
 * 初始化节点渲染器
 */
export function initNodeRenderer(sceneRef) {
  scene = sceneRef;
}

/**
 * 为所有节点创建 3D 定居点
 * @param {object} nodes - stateStore.nodes
 */
export function createAllSettlements(nodes) {
  for (const node of Object.values(nodes)) {
    const group = createSettlement(node);
    if (group) {
      scene.add(group);
      nodeGroups.set(node.id, group);
    }
  }
}

/**
 * 创建单个城市定居点
 */
function createSettlement(node) {
  const { x, z } = toWorldXZ(node.x, node.y);
  const y = getHeightAt(x, z);
  const group = new THREE.Group();
  group.userData = { type: 'node', nodeId: node.id };
  group.position.set(x, y, z);

  // 1. 城市平台（势力色圆盘）
  const platformGeo = new THREE.CylinderGeometry(
    CITY_CONFIG.influenceRadius, CITY_CONFIG.influenceRadius, CITY_CONFIG.platformHeight, 8
  );
  const factionColor = FACTION_COLORS_3D[node.factionId] || FACTION_COLORS_3D.NEUTRAL;
  const platformMat = new THREE.MeshStandardMaterial({
    color: factionColor,
    transparent: true,
    opacity: 0.15,
    roughness: 0.9,
  });
  const platform = new THREE.Mesh(platformGeo, platformMat);
  platform.position.y = CITY_CONFIG.platformHeight / 2;
  group.add(platform);

  // 2. 建筑实例
  if (node.placedBuildings && node.placedBuildings.length > 0) {
    node.placedBuildings.forEach((bld, idx) => {
      const bldMesh = createBuildingMesh(bld);
      if (bldMesh) group.add(bldMesh);
    });
  } else {
    // 兼容旧数据：从 farmLevel/mineLevel 等生成默认布局
    addLegacyBuildings(group, node);
  }

  // 3. 城墙段
  if (node.wallSegments && node.wallSegments.length > 0) {
    node.wallSegments.forEach((seg, idx) => {
      const wallMesh = createWallSegmentMesh(seg, node);
      if (wallMesh) group.add(wallMesh);
    });
  } else if (node.wallLevel > 0) {
    // 兼容旧数据：默认圆形城墙
    addLegacyWall(group, node);
  }

  // 4. 势力旗帜
  const flag = createFlag(factionColor);
  group.add(flag);

  // 5. 选中高亮环
  const ring = createSelectionRing();
  ring.visible = false;
  ring.name = 'selectionRing';
  group.add(ring);

  return group;
}

/**
 * 创建建筑 3D 网格
 */
function createBuildingMesh(bld) {
  const model = BUILDING_MODELS[bld.buildingType] || BUILDING_MODELS.FARM;
  const level = bld.level || 1;
  const h = model.hBase + model.hPerLevel * (level - 1);

  const group = new THREE.Group();
  group.userData = { type: 'building', buildingType: bld.buildingType, level };

  // 底座
  const baseGeo = new THREE.BoxGeometry(model.w, h, model.d);
  const baseMat = new THREE.MeshStandardMaterial({ color: model.color, roughness: 0.8 });
  const base = new THREE.Mesh(baseGeo, baseMat);
  base.position.y = h / 2;
  base.castShadow = true;
  base.receiveShadow = true;
  group.add(base);

  // 屋顶
  if (model.roofColor) {
    const roofGeo = new THREE.ConeGeometry(
      Math.max(model.w, model.d) * 0.7,
      h * 0.4,
      4
    );
    const roofMat = new THREE.MeshStandardMaterial({ color: model.roofColor, roughness: 0.7 });
    const roof = new THREE.Mesh(roofGeo, roofMat);
    roof.position.y = h + h * 0.2;
    roof.rotation.y = Math.PI / 4;
    roof.castShadow = true;
    group.add(roof);
  }

  // 特殊装饰
  if (bld.buildingType === 'ORACLE_BEACON') {
    // 顶部光球
    const glowGeo = new THREE.SphereGeometry(0.15, 8, 8);
    const glowMat = new THREE.MeshStandardMaterial({
      color: 0xFFD700, emissive: 0xFFD700, emissiveIntensity: 0.5,
    });
    const glow = new THREE.Mesh(glowGeo, glowMat);
    glow.position.y = h + 0.3;
    group.add(glow);
  }

  if (bld.buildingType === 'ARSENAL') {
    // 烟囱
    const chimneyGeo = new THREE.CylinderGeometry(0.05, 0.05, 0.3, 6);
    const chimneyMat = new THREE.MeshStandardMaterial({ color: 0x333333 });
    const chimney = new THREE.Mesh(chimneyGeo, chimneyMat);
    chimney.position.set(model.w * 0.3, h + 0.15, 0);
    group.add(chimney);
  }

  // 放置位置
  group.position.set(bld.localX, 0, bld.localZ);
  group.rotation.y = (bld.rotation || 0) * Math.PI / 180;

  return group;
}

/**
 * 创建城墙段网格
 */
function createWallSegmentMesh(seg, node) {
  const group = new THREE.Group();

  const dx = seg.toX - seg.fromX;
  const dz = seg.toZ - seg.fromZ;
  const length = Math.sqrt(dx * dx + dz * dz);
  const angle = Math.atan2(dx, dz);

  const level = seg.level || 1;
  const h = WALL_CONFIG.heightBase + WALL_CONFIG.heightPerLevel * (level - 1);
  const factionColor = FACTION_COLORS_3D[node.factionId] || FACTION_COLORS_3D.NEUTRAL;

  // 城墙主体
  const wallGeo = new THREE.BoxGeometry(WALL_CONFIG.thickness, h, length);
  const wallMat = new THREE.MeshStandardMaterial({
    color: 0xc0c0c0, roughness: 0.85, metalness: 0.1,
  });
  const wall = new THREE.Mesh(wallGeo, wallMat);
  wall.position.y = h / 2;
  wall.castShadow = true;
  group.add(wall);

  // 垛口
  const battlementCount = Math.floor(length / WALL_CONFIG.battlementSpacing);
  for (let i = 0; i < battlementCount; i++) {
    const bGeo = new THREE.BoxGeometry(
      WALL_CONFIG.thickness + 0.04,
      WALL_CONFIG.battlementHeight,
      WALL_CONFIG.battlementWidth
    );
    const b = new THREE.Mesh(bGeo, wallMat);
    const z = -length / 2 + (i + 0.5) * (length / battlementCount);
    b.position.set(0, h + WALL_CONFIG.battlementHeight / 2, z);
    group.add(b);
  }

  // 中点位置
  const midX = (seg.fromX + seg.toX) / 2;
  const midZ = (seg.fromZ + seg.toZ) / 2;
  group.position.set(midX, 0, midZ);
  group.rotation.y = angle;

  return group;
}

/**
 * 创建势力旗帜
 */
function createFlag(color) {
  const group = new THREE.Group();

  // 旗杆
  const poleGeo = new THREE.CylinderGeometry(0.02, 0.02, CITY_CONFIG.flagPoleHeight, 4);
  const poleMat = new THREE.MeshStandardMaterial({ color: 0x8B4513 });
  const pole = new THREE.Mesh(poleGeo, poleMat);
  pole.position.y = CITY_CONFIG.flagPoleHeight / 2;
  group.add(pole);

  // 旗面
  const flagGeo = new THREE.PlaneGeometry(CITY_CONFIG.flagSize, CITY_CONFIG.flagSize * 0.6);
  const flagMat = new THREE.MeshStandardMaterial({
    color, side: THREE.DoubleSide, roughness: 0.6,
  });
  const flag = new THREE.Mesh(flagGeo, flagMat);
  flag.position.set(CITY_CONFIG.flagSize / 2, CITY_CONFIG.flagPoleHeight - CITY_CONFIG.flagSize * 0.3, 0);
  flag.name = 'flag';
  group.add(flag);

  return group;
}

/**
 * 选中高亮环
 */
function createSelectionRing() {
  const geo = new THREE.RingGeometry(
    CITY_CONFIG.selectionRingRadius - 0.1,
    CITY_CONFIG.selectionRingRadius,
    32
  );
  geo.rotateX(-Math.PI / 2);
  const mat = new THREE.MeshBasicMaterial({
    color: 0x2563EB, transparent: true, opacity: 0.5, side: THREE.DoubleSide,
  });
  const ring = new THREE.Mesh(geo, mat);
  ring.position.y = 0.1;
  return ring;
}

/**
 * 兼容旧数据：从 farmLevel/mineLevel 等生成默认布局建筑
 */
function addLegacyBuildings(group, node) {
  const buildings = [];
  if (node.farmLevel > 0) buildings.push({ type: 'FARM', level: node.farmLevel, pos: [-1.5, 0, 1.5] });
  if (node.mineLevel > 0) buildings.push({ type: 'MINE', level: node.mineLevel, pos: [1.5, 0, 1.5] });
  if (node.arsenalLevel > 0) buildings.push({ type: 'ARSENAL', level: node.arsenalLevel, pos: [-1.5, 0, -1.5] });
  if (node.beaconLevel > 0) buildings.push({ type: 'ORACLE_BEACON', level: node.beaconLevel, pos: [1.5, 0, -1.5] });

  // 市政厅（首都有）
  if (node.isCapital) {
    const hall = BUILDING_MODELS.HALL;
    const hGeo = new THREE.BoxGeometry(hall.w, hall.hBase, hall.d);
    const hMat = new THREE.MeshStandardMaterial({ color: hall.color, roughness: 0.8 });
    const hMesh = new THREE.Mesh(hGeo, hMat);
    hMesh.position.set(0, hall.hBase / 2, 0);
    hMesh.castShadow = true;
    group.add(hMesh);
    // 屋顶
    const rGeo = new THREE.ConeGeometry(Math.max(hall.w, hall.d) * 0.6, hall.hBase * 0.4, 4);
    const rMat = new THREE.MeshStandardMaterial({ color: hall.roofColor, roughness: 0.7 });
    const rMesh = new THREE.Mesh(rGeo, rMat);
    rMesh.position.y = hall.hBase + hall.hBase * 0.2;
    rMesh.rotation.y = Math.PI / 4;
    rMesh.castShadow = true;
    group.add(rMesh);
  }

  for (const b of buildings) {
    const model = BUILDING_MODELS[b.type];
    if (!model) continue;
    const h = model.hBase + model.hPerLevel * (b.level - 1);
    const geo = new THREE.BoxGeometry(model.w, h, model.d);
    const mat = new THREE.MeshStandardMaterial({ color: model.color, roughness: 0.8 });
    const mesh = new THREE.Mesh(geo, mat);
    mesh.position.set(b.pos[0], h / 2, b.pos[2]);
    mesh.castShadow = true;
    mesh.receiveShadow = true;
    group.add(mesh);

    // 屋顶
    if (model.roofColor) {
      const rGeo = new THREE.ConeGeometry(Math.max(model.w, model.d) * 0.7, h * 0.4, 4);
      const rMat = new THREE.MeshStandardMaterial({ color: model.roofColor, roughness: 0.7 });
      const rMesh = new THREE.Mesh(rGeo, rMat);
      rMesh.position.set(b.pos[0], h + h * 0.2, b.pos[2]);
      rMesh.rotation.y = Math.PI / 4;
      rMesh.castShadow = true;
      group.add(rMesh);
    }
  }
}

/**
 * 兼容旧数据：默认圆形城墙
 */
function addLegacyWall(group, node) {
  const level = node.wallLevel || 1;
  const h = WALL_CONFIG.heightBase + WALL_CONFIG.heightPerLevel * (level - 1);
  const radius = 3.0;
  const segments = 12;

  for (let i = 0; i < segments; i++) {
    const angle = (i / segments) * Math.PI * 2;
    const nextAngle = ((i + 1) / segments) * Math.PI * 2;
    const x1 = Math.cos(angle) * radius, z1 = Math.sin(angle) * radius;
    const x2 = Math.cos(nextAngle) * radius, z2 = Math.sin(nextAngle) * radius;

    const dx = x2 - x1, dz = z2 - z1;
    const len = Math.sqrt(dx * dx + dz * dz);
    const midAngle = Math.atan2(dx, dz);

    const wallGeo = new THREE.BoxGeometry(WALL_CONFIG.thickness, h, len);
    const wallMat = new THREE.MeshStandardMaterial({ color: 0xc0c0c0, roughness: 0.85 });
    const wall = new THREE.Mesh(wallGeo, wallMat);
    wall.position.set((x1 + x2) / 2, h / 2, (z1 + z2) / 2);
    wall.rotation.y = midAngle;
    wall.castShadow = true;
    group.add(wall);
  }
}

/**
 * 更新单个节点的 3D 渲染
 */
export function updateNodeSettlement(nodeId) {
  const node = stateStore.nodes[nodeId];
  if (!node) return;

  const existing = nodeGroups.get(nodeId);
  if (existing) {
    // 移除旧的
    scene.remove(existing);
    existing.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) {
        if (Array.isArray(obj.material)) obj.material.forEach(m => m.dispose());
        else obj.material.dispose();
      }
    });
  }

  // 重建
  const group = createSettlement(node);
  if (group) {
    scene.add(group);
    nodeGroups.set(nodeId, group);

    // 恢复选中状态
    if (nodeId === selectedNodeId) {
      const ring = group.getObjectByName('selectionRing');
      if (ring) ring.visible = true;
    }
  }
}

/**
 * 设置节点选中/取消
 */
export function setNodeSelected(nodeId, selected) {
  selectedNodeId = selected ? nodeId : null;

  // 更新所有节点的选中环
  for (const [id, group] of nodeGroups) {
    const ring = group.getObjectByName('selectionRing');
    if (ring) ring.visible = (id === nodeId && selected);
  }
}

/**
 * 获取节点 3D 位置
 */
export function getNodePosition3D(nodeId) {
  const group = nodeGroups.get(nodeId);
  if (group) return group.position.clone();
  return null;
}

/** 获取节点 Group */
export function getNodeGroup(nodeId) { return nodeGroups.get(nodeId); }

/** 获取所有节点 Group */
export function getAllNodeGroups() { return nodeGroups; }

/** 清理 */
export function clearAllSettlements() {
  for (const [id, group] of nodeGroups) {
    scene.remove(group);
    group.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) {
        if (Array.isArray(obj.material)) obj.material.forEach(m => m.dispose());
        else obj.material.dispose();
      }
    });
  }
  nodeGroups.clear();
  selectedNodeId = null;
}
