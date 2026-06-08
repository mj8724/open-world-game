/**
 * 野外渲染器 — 野外资源点 + 中立建筑
 */
import * as THREE from 'three';
import { WILD_ENTITY_CONFIG, FACTION_COLORS_3D } from './constants.js';
import { getHeightAt } from './terrain-generator.js';
import { stateStore } from '../bridge/state-store.js';

const wildMeshes = new Map(); // id → THREE.Group
const structureMeshes = new Map(); // id → THREE.Group
let scene = null;

export function initWildRenderer(sceneRef) { scene = sceneRef; }

/**
 * 创建所有野外实体
 */
export function createAllWildEntities() {
  // 野外资源点
  const wildResources = stateStore.wildResources || {};
  for (const wr of Object.values(wildResources)) {
    const group = createWildResourceMesh(wr);
    if (group) {
      scene.add(group);
      wildMeshes.set(wr.id, group);
    }
  }

  // 中立建筑
  const neutralStructures = stateStore.neutralStructures || {};
  for (const ns of Object.values(neutralStructures)) {
    const group = createNeutralStructureMesh(ns);
    if (group) {
      scene.add(group);
      structureMeshes.set(ns.id, group);
    }
  }
}

/**
 * 创建野外资源点
 */
function createWildResourceMesh(wr) {
  const cfg = WILD_ENTITY_CONFIG.RESOURCE[wr.resourceType] || WILD_ENTITY_CONFIG.RESOURCE.FOOD;
  const group = new THREE.Group();
  group.userData = { type: 'wildResource', id: wr.id, resourceType: wr.resourceType };

  const y = getHeightAt(wr.x * 0.05, wr.z * 0.05); // 后端坐标→世界坐标
  const wx = wr.x * 0.05;
  const wz = wr.z * 0.05;

  switch (wr.resourceType) {
    case 'IRON':
      // 铁矿石堆
      for (let i = 0; i < 3; i++) {
        const geo = new THREE.DodecahedronGeometry(cfg.size * (0.7 + Math.random() * 0.5), 0);
        const mat = new THREE.MeshStandardMaterial({ color: cfg.color, roughness: 0.8, metalness: 0.3 });
        const mesh = new THREE.Mesh(geo, mat);
        mesh.position.set(
          (Math.random() - 0.5) * 0.5,
          cfg.size * 0.3,
          (Math.random() - 0.5) * 0.5
        );
        mesh.castShadow = true;
        group.add(mesh);
      }
      break;

    case 'FOOD':
      // 小块田地
      for (let r = 0; r < cfg.rows; r++) {
        for (let c = 0; c < cfg.cols; c++) {
          const geo = new THREE.BoxGeometry(cfg.size, cfg.size * 0.6, cfg.size);
          const mat = new THREE.MeshStandardMaterial({ color: cfg.color, roughness: 0.9 });
          const mesh = new THREE.Mesh(geo, mat);
          mesh.position.set(
            (r - cfg.rows / 2) * cfg.size * 1.2,
            cfg.size * 0.3,
            (c - cfg.cols / 2) * cfg.size * 1.2
          );
          group.add(mesh);
        }
      }
      break;

    case 'AMMO':
      // 弹药箱
      const boxGeo = new THREE.BoxGeometry(cfg.size * 1.5, cfg.size, cfg.size);
      const boxMat = new THREE.MeshStandardMaterial({ color: cfg.color, roughness: 0.85 });
      const box = new THREE.Mesh(boxGeo, boxMat);
      box.position.y = cfg.size / 2;
      box.castShadow = true;
      group.add(box);
      break;
  }

  // 占领旗帜
  if (wr.ownerFactionId) {
    const flagColor = FACTION_COLORS_3D[wr.ownerFactionId] || 0x9CA3AF;
    addFlag(group, flagColor);
  }

  group.position.set(wx, y, wz);
  return group;
}

/**
 * 创建中立建筑
 */
function createNeutralStructureMesh(ns) {
  const cfg = WILD_ENTITY_CONFIG.STRUCTURE[ns.structureType] || WILD_ENTITY_CONFIG.STRUCTURE.RUINS;
  const group = new THREE.Group();
  group.userData = { type: 'neutralStructure', id: ns.id, structureType: ns.structureType };

  const y = getHeightAt(ns.x * 0.05, ns.z * 0.05);
  const wx = ns.x * 0.05;
  const wz = ns.z * 0.05;

  switch (ns.structureType) {
    case 'RUINS':
      // 断墙 + 石柱
      const wallGeo = new THREE.BoxGeometry(1.5, cfg.height * 0.6, 0.15);
      const wallMat = new THREE.MeshStandardMaterial({ color: cfg.color, roughness: 0.95 });
      const wall = new THREE.Mesh(wallGeo, wallMat);
      wall.position.y = cfg.height * 0.3;
      wall.castShadow = true;
      group.add(wall);

      for (let i = 0; i < cfg.pillarCount; i++) {
        const pillarGeo = new THREE.CylinderGeometry(0.08, 0.1, cfg.height * (0.4 + Math.random() * 0.4), 6);
        const pillar = new THREE.Mesh(pillarGeo, wallMat);
        pillar.position.set(
          (Math.random() - 0.5) * 1.5,
          cfg.height * 0.2,
          (Math.random() - 0.5) * 0.5
        );
        pillar.castShadow = true;
        group.add(pillar);
      }

      // 发光符文
      const glowGeo = new THREE.SphereGeometry(0.1, 8, 8);
      const glowMat = new THREE.MeshStandardMaterial({
        color: 0xFFD700, emissive: 0xFFD700, emissiveIntensity: 0.5, transparent: true, opacity: 0.6,
      });
      const glow = new THREE.Mesh(glowGeo, glowMat);
      glow.position.y = cfg.height * 0.5;
      group.add(glow);
      break;

    case 'OUTPOST':
      // 木制瞭望塔
      const towerGeo = new THREE.CylinderGeometry(0.3, 0.4, cfg.height, 6);
      const towerMat = new THREE.MeshStandardMaterial({ color: cfg.color, roughness: 0.9 });
      const tower = new THREE.Mesh(towerGeo, towerMat);
      tower.position.y = cfg.height / 2;
      tower.castShadow = true;
      group.add(tower);

      // 顶部平台
      const platGeo = new THREE.CylinderGeometry(0.5, 0.5, 0.1, 6);
      const plat = new THREE.Mesh(platGeo, towerMat);
      plat.position.y = cfg.height;
      group.add(plat);
      break;

    case 'SHRINE':
      // 石祭台
      const baseGeo = new THREE.CylinderGeometry(0.6, 0.7, cfg.height * 0.5, 8);
      const baseMat = new THREE.MeshStandardMaterial({ color: cfg.color, roughness: 0.85, metalness: 0.2 });
      const base = new THREE.Mesh(baseGeo, baseMat);
      base.position.y = cfg.height * 0.25;
      base.castShadow = true;
      group.add(base);

      // 光柱
      const beamGeo = new THREE.CylinderGeometry(0.05, 0.05, 3, 6);
      const beamMat = new THREE.MeshBasicMaterial({
        color: cfg.beamColor, transparent: true, opacity: 0.3,
      });
      const beam = new THREE.Mesh(beamGeo, beamMat);
      beam.position.y = cfg.height + 1.5;
      group.add(beam);
      break;
  }

  // 占领旗帜
  if (ns.ownerFactionId) {
    const flagColor = FACTION_COLORS_3D[ns.ownerFactionId] || 0x9CA3AF;
    addFlag(group, flagColor);
  }

  group.position.set(wx, y, wz);
  return group;
}

/**
 * 添加小旗帜
 */
function addFlag(group, color) {
  const poleGeo = new THREE.CylinderGeometry(0.02, 0.02, 1.0, 4);
  const poleMat = new THREE.MeshStandardMaterial({ color: 0x8B4513 });
  const pole = new THREE.Mesh(poleGeo, poleMat);
  pole.position.y = 0.5;
  group.add(pole);

  const flagGeo = new THREE.PlaneGeometry(0.3, 0.2);
  const flagMat = new THREE.MeshStandardMaterial({ color, side: THREE.DoubleSide });
  const flag = new THREE.Mesh(flagGeo, flagMat);
  flag.position.set(0.15, 0.9, 0);
  group.add(flag);
}

/**
 * 刷新野外实体
 */
export function refreshWildEntities() {
  // 简化：全量重建
  clearAllWildEntities();
  createAllWildEntities();
}

/** 清理 */
export function clearAllWildEntities() {
  for (const [id, group] of wildMeshes) {
    scene.remove(group);
    group.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) obj.material.dispose();
    });
  }
  wildMeshes.clear();

  for (const [id, group] of structureMeshes) {
    scene.remove(group);
    group.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) obj.material.dispose();
    });
  }
  structureMeshes.clear();
}
