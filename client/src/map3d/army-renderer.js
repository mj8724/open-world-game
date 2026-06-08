/**
 * 军队渲染器 — 士兵模型 + 方阵 + 移动插值
 */
import * as THREE from 'three';
import { ARMY_CONFIG, FACTION_COLORS_3D } from './constants.js';
import { toWorldXZ } from './world-space.js';
import { getHeightAt } from './terrain-generator.js';
import { stateStore } from '../bridge/state-store.js';

const armyGroups = new Map(); // entityId → THREE.Group
let scene = null;

export function initArmyRenderer(sceneRef) { scene = sceneRef; }

/**
 * 创建所有军队
 */
export function createAllArmies(armies) {
  for (const army of Object.values(armies)) {
    const group = createArmyGroup(army);
    if (group) {
      scene.add(group);
      armyGroups.set(army.entityId, group);
    }
  }
}

/**
 * 创建单个军队 3D 模型
 */
function createArmyGroup(army) {
  const group = new THREE.Group();
  group.userData = { type: 'army', entityId: army.entityId, factionId: army.factionId };

  const factionColor = FACTION_COLORS_3D[army.factionId] || FACTION_COLORS_3D.NEUTRAL;
  const strength = army.strength || army.troopCount || 1;

  // 士兵数量（限制可见数）
  const soldierCount = Math.min(strength, ARMY_CONFIG.maxVisibleSoldiers);

  for (let i = 0; i < soldierCount; i++) {
    const soldier = createSoldier(factionColor, army.unitDefId);
    // 方阵排列
    const cols = Math.ceil(Math.sqrt(soldierCount));
    const row = Math.floor(i / cols);
    const col = i % cols;
    const offsetX = (col - cols / 2) * ARMY_CONFIG.formationSpacing;
    const offsetZ = (row - Math.ceil(soldierCount / cols) / 2) * ARMY_CONFIG.formationSpacing;
    soldier.position.set(offsetX, 0, offsetZ);
    group.add(soldier);
  }

  // 血条
  const healthBar = createHealthBar(army);
  group.add(healthBar);

  // 强度标签
  const label = createStrengthLabel(strength);
  group.add(label);

  // 设置初始位置
  updateArmyPositionFromData(group, army);

  return group;
}

/**
 * 创建士兵模型
 */
function createSoldier(factionColor, unitDefId) {
  const soldierGroup = new THREE.Group();
  const h = ARMY_CONFIG.soldierHeight;
  const r = ARMY_CONFIG.soldierRadius;

  // 身体
  const bodyGeo = new THREE.CylinderGeometry(r, r * 1.2, h * 0.6, 6);
  const bodyMat = new THREE.MeshStandardMaterial({ color: factionColor, roughness: 0.7 });
  const body = new THREE.Mesh(bodyGeo, bodyMat);
  body.position.y = h * 0.3;
  soldierGroup.add(body);

  // 头
  const headGeo = new THREE.SphereGeometry(r * 0.8, 6, 6);
  const headMat = new THREE.MeshStandardMaterial({ color: 0xf5d0a9, roughness: 0.8 });
  const head = new THREE.Mesh(headGeo, headMat);
  head.position.y = h * 0.7;
  soldierGroup.add(head);

  // 武器
  if (unitDefId === 'MUSKETEER' || unitDefId === 'MAXIM_GUN') {
    // 枪
    const gunGeo = new THREE.BoxGeometry(0.02, h * 0.5, 0.02);
    const gunMat = new THREE.MeshStandardMaterial({ color: 0x4a4a4a });
    const gun = new THREE.Mesh(gunGeo, gunMat);
    gun.position.set(r * 1.5, h * 0.4, 0);
    soldierGroup.add(gun);
  } else {
    // 剑
    const swordGeo = new THREE.BoxGeometry(0.02, h * 0.4, 0.04);
    const swordMat = new THREE.MeshStandardMaterial({ color: 0xc0c0c0, metalness: 0.8 });
    const sword = new THREE.Mesh(swordGeo, swordMat);
    sword.position.set(r * 1.5, h * 0.3, 0);
    soldierGroup.add(sword);
  }

  soldierGroup.castShadow = true;
  return soldierGroup;
}

/**
 * 血条
 */
function createHealthBar(army) {
  const group = new THREE.Group();
  group.name = 'healthBar';

  const w = ARMY_CONFIG.healthBarWidth;
  const h = ARMY_CONFIG.healthBarHeight;
  const y = ARMY_CONFIG.soldierHeight + 0.3;

  // 背景
  const bgGeo = new THREE.PlaneGeometry(w, h);
  const bgMat = new THREE.MeshBasicMaterial({ color: 0x333333, side: THREE.DoubleSide });
  const bg = new THREE.Mesh(bgGeo, bgMat);
  bg.position.y = y;
  group.add(bg);

  // 前景（HP 比例）
  const hpRatio = army.morale || 1.0;
  const fgGeo = new THREE.PlaneGeometry(w * hpRatio, h);
  const fgMat = new THREE.MeshBasicMaterial({ color: hpRatio > 0.5 ? 0x00ff00 : 0xff0000, side: THREE.DoubleSide });
  const fg = new THREE.Mesh(fgGeo, fgMat);
  fg.position.set(-(w * (1 - hpRatio)) / 2, y, 0.001);
  group.add(fg);

  return group;
}

/**
 * 强度标签（使用 Sprite）
 */
function createStrengthLabel(strength) {
  const canvas = document.createElement('canvas');
  canvas.width = 64;
  canvas.height = 32;
  const ctx = canvas.getContext('2d');
  ctx.fillStyle = 'rgba(0,0,0,0.6)';
  ctx.fillRect(0, 0, 64, 32);
  ctx.fillStyle = '#ffffff';
  ctx.font = 'bold 16px sans-serif';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillText(String(strength), 32, 16);

  const texture = new THREE.CanvasTexture(canvas);
  const spriteMat = new THREE.SpriteMaterial({ map: texture });
  const sprite = new THREE.Sprite(spriteMat);
  sprite.scale.set(0.6, 0.3, 1);
  sprite.position.y = ARMY_CONFIG.soldierHeight + 0.5;
  sprite.name = 'strengthLabel';

  return sprite;
}

/**
 * 从数据更新军队位置
 */
function updateArmyPositionFromData(group, army) {
  const nodes = stateStore.nodes;

  if (army.currentEdgeId) {
    // 在边上移动
    const edge = stateStore.edges[army.currentEdgeId];
    if (edge) {
      const srcNode = nodes[edge.sourceNodeId];
      const tgtNode = nodes[edge.targetNodeId];
      if (srcNode && tgtNode) {
        const src = toWorldXZ(srcNode.x, srcNode.y);
        const tgt = toWorldXZ(tgtNode.x, tgtNode.y);

        // 确定方向
        const progress = army.edgeProgress || 0;
        const isReverse = army.targetNodeId === edge.sourceNodeId;
        const t = isReverse ? (1 - progress) : progress;

        const wx = src.x + (tgt.x - src.x) * t;
        const wz = src.z + (tgt.z - src.z) * t;
        const wy = getHeightAt(wx, wz);

        group.position.set(wx, wy + 0.05, wz);

        // 朝向
        const dx = tgt.x - src.x;
        const dz = tgt.z - src.z;
        group.rotation.y = Math.atan2(dx, dz) + (isReverse ? Math.PI : 0);
      }
    }
  } else if (army.currentNodeId) {
    // 在节点上
    const node = nodes[army.currentNodeId];
    if (node) {
      const { x, z } = toWorldXZ(node.x, node.y);
      const y = getHeightAt(x, z);

      // 多军队同节点时分散
      const armiesAtNode = Object.values(stateStore.armies || {})
        .filter(a => a.currentNodeId === army.currentNodeId && !a.currentEdgeId);
      const idx = armiesAtNode.findIndex(a => a.entityId === army.entityId);
      const count = armiesAtNode.length;

      if (count > 1) {
        const angle = (idx / count) * Math.PI * 2;
        const radius = 1.5;
        group.position.set(
          x + Math.cos(angle) * radius,
          y + 0.05,
          z + Math.sin(angle) * radius
        );
      } else {
        group.position.set(x, y + 0.05, z);
      }
    }
  }
}

/**
 * 刷新所有军队位置
 */
export function refreshArmyVisuals() {
  const armies = stateStore.armies || {};

  // 移除已消失的军队
  for (const [entityId, group] of armyGroups) {
    if (!armies[entityId]) {
      scene.remove(group);
      group.traverse((obj) => {
        if (obj.geometry) obj.geometry.dispose();
        if (obj.material) obj.material.dispose();
      });
      armyGroups.delete(entityId);
    }
  }

  // 更新或创建军队
  for (const army of Object.values(armies)) {
    let group = armyGroups.get(army.entityId);
    if (!group) {
      group = createArmyGroup(army);
      if (group) {
        scene.add(group);
        armyGroups.set(army.entityId, group);
      }
    } else {
      updateArmyPositionFromData(group, army);
    }
  }
}

/** 获取军队 Group */
export function getArmyGroup(entityId) { return armyGroups.get(entityId); }

/** 清理 */
export function clearAllArmies() {
  for (const [id, group] of armyGroups) {
    scene.remove(group);
    group.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) obj.material.dispose();
    });
  }
  armyGroups.clear();
}
