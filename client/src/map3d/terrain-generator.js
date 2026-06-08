/**
 * 大陆地形生成器 — 高度图 + 地面网格 + 水面 + 装饰
 *
 * 核心思路：
 * 1. 基于 17 个城市节点位置 + terrain 类型，用 Inverse-Distance Weighting (IDW) 插值生成高度图
 * 2. 顶点着色按最近城市地形类型
 * 3. 水面在 Y < waterLevel 处
 * 4. 地表装饰：岩石/树木/沙纹/水洼/碎石
 */
import * as THREE from 'three';
import { TERRAIN_CONFIG, TERRAIN_GEN, WORLD_SCALE, ELEVATION_SCALE } from './constants.js';
import { toWorldXZ } from './world-space.js';
import { getTerrainTexture } from './terrain-textures.js';

let groundMesh = null;
let waterMesh = null;
let decorationsGroup = null;
let heightData = null; // Float32Array for getHeightAt queries

// 存储节点 3D 位置用于 IDW
let nodePositions3D = []; // [{ x, z, terrain, elevation }]

/**
 * 生成大陆地形
 * @param {object} nodes - stateStore.nodes
 * @returns {{ groundMesh: THREE.Mesh, waterMesh: THREE.Mesh, decorationsGroup: THREE.Group, getHeightAt: Function }}
 */
export function generateTerrain(nodes) {
  const cfg = TERRAIN_GEN;

  // 准备节点 3D 位置
  nodePositions3D = [];
  for (const node of Object.values(nodes)) {
    const { x, z } = toWorldXZ(node.x, node.y);
    const terrainCfg = TERRAIN_CONFIG[node.terrain] || TERRAIN_CONFIG.PLAINS;
    nodePositions3D.push({
      x, z,
      terrain: node.terrain || 'PLAINS',
      elevation: terrainCfg.elevation * ELEVATION_SCALE,
      factionId: node.factionId,
    });
  }

  // 生成高度图
  const { positions, colors, indices, heightMap } = generateHeightMap(cfg);

  // 创建地面网格
  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
  geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
  geometry.setIndex(indices);
  geometry.computeVertexNormals();

  const material = new THREE.MeshStandardMaterial({
    vertexColors: true,
    roughness: 0.85,
    metalness: 0.05,
    flatShading: false,
  });

  groundMesh = new THREE.Mesh(geometry, material);
  groundMesh.receiveShadow = true;
  groundMesh.name = 'terrain';

  // 水面
  waterMesh = createWaterMesh(cfg);

  // 装饰物
  decorationsGroup = createDecorations(nodes, cfg);

  // 高度查询数据
  heightData = heightMap;

  return {
    groundMesh,
    waterMesh,
    decorationsGroup,
    getHeightAt,
  };
}

/**
 * IDW 高度图生成
 */
function generateHeightMap(cfg) {
  const seg = cfg.gridSize;
  const size = cfg.worldSize;
  const half = size / 2;

  const vertCount = (seg + 1) * (seg + 1);
  const positions = new Float32Array(vertCount * 3);
  const colors = new Float32Array(vertCount * 3);
  const heightMap = new Float32Array(vertCount);

  // 为每个顶点计算高度和颜色
  for (let iz = 0; iz <= seg; iz++) {
    for (let ix = 0; ix <= seg; ix++) {
      const idx = iz * (seg + 1) + ix;
      const wx = -half + (ix / seg) * size;
      const wz = -half + (iz / seg) * size;

      // IDW 插值高度
      let height = 0;
      let weightSum = 0;
      let dominantTerrain = 'PLAINS';
      let maxWeight = 0;

      for (const node of nodePositions3D) {
        const dx = wx - node.x;
        const dz = wz - node.z;
        const dist = Math.sqrt(dx * dx + dz * dz);

        if (dist < 0.01) {
          height = node.elevation;
          weightSum = 1;
          dominantTerrain = node.terrain;
          maxWeight = 1;
          break;
        }

        const w = 1 / Math.pow(dist, cfg.heightBlendPower);
        height += w * node.elevation;
        weightSum += w;

        if (w > maxWeight) {
          maxWeight = w;
          dominantTerrain = node.terrain;
        }
      }

      if (weightSum > 0) height /= weightSum;

      // 添加噪声让地形更自然
      height += simplexNoise(wx * 0.3, wz * 0.3) * 0.3;

      heightMap[idx] = height;

      const vi = idx * 3;
      positions[vi] = wx;
      positions[vi + 1] = height;
      positions[vi + 2] = wz;

      // 顶点着色
      const terrainCfg = TERRAIN_CONFIG[dominantTerrain] || TERRAIN_CONFIG.PLAINS;
      const col = new THREE.Color(terrainCfg.color);
      // 添加轻微随机变化
      const variation = simplexNoise(wx * 0.5, wz * 0.5) * 0.05;
      colors[vi] = Math.max(0, Math.min(1, col.r + variation));
      colors[vi + 1] = Math.max(0, Math.min(1, col.g + variation));
      colors[vi + 2] = Math.max(0, Math.min(1, col.b + variation));
    }
  }

  // 索引（使用普通数组，让 Three.js setIndex 自动包装为 BufferAttribute）
  const indices = [];
  for (let iz = 0; iz < seg; iz++) {
    for (let ix = 0; ix < seg; ix++) {
      const a = iz * (seg + 1) + ix;
      const b = a + 1;
      const c = a + (seg + 1);
      const d = c + 1;
      indices.push(a, c, b);
      indices.push(b, c, d);
    }
  }

  return { positions, colors, indices, heightMap };
}

/**
 * 创建水面
 */
function createWaterMesh(cfg) {
  const size = cfg.worldSize;
  const waterGeo = new THREE.PlaneGeometry(size * 1.5, size * 1.5);
  waterGeo.rotateX(-Math.PI / 2);
  const waterMat = new THREE.MeshStandardMaterial({
    color: cfg.waterColor,
    transparent: true,
    opacity: cfg.waterOpacity,
    roughness: 0.1,
    metalness: 0.3,
    side: THREE.DoubleSide,
  });
  const mesh = new THREE.Mesh(waterGeo, waterMat);
  mesh.position.y = cfg.waterLevel;
  mesh.name = 'water';
  return mesh;
}

/**
 * 创建地表装饰（岩石、树木等）
 */
function createDecorations(nodes, cfg) {
  const group = new THREE.Group();
  group.name = 'decorations';

  const density = cfg.decorationDensity;

  for (const node of nodePositions3D) {
    const count = density[node.terrain] || 0;
    if (count <= 0) continue;

    for (let i = 0; i < count; i++) {
      // 在节点周围随机位置
      const angle = Math.random() * Math.PI * 2;
      const radius = 1 + Math.random() * (CITY_INFLUENCE_RADIUS || 5) * 0.8;
      const wx = node.x + Math.cos(angle) * radius;
      const wz = node.z + Math.sin(angle) * radius;
      const wy = getHeightAt(wx, wz);

      let deco;
      switch (node.terrain) {
        case 'MOUNTAIN':
          deco = createRock(wx, wy, wz);
          break;
        case 'FOREST':
          deco = createTree(wx, wy, wz);
          break;
        case 'DESERT':
          deco = createSandDune(wx, wy, wz);
          break;
        case 'SWAMP':
          deco = createPuddle(wx, wy, wz);
          break;
        case 'RUINS':
          deco = createRubble(wx, wy, wz);
          break;
        default:
          continue;
      }
      if (deco) group.add(deco);
    }
  }

  return group;
}

// ─── 装饰物工厂函数 ───

function createRock(x, y, z) {
  const size = 0.15 + Math.random() * 0.3;
  const geo = new THREE.DodecahedronGeometry(size, 0);
  const mat = new THREE.MeshStandardMaterial({
    color: 0x808080,
    roughness: 0.9,
    metalness: 0.1,
  });
  const mesh = new THREE.Mesh(geo, mat);
  mesh.position.set(x, y + size * 0.4, z);
  mesh.rotation.set(Math.random(), Math.random(), Math.random());
  mesh.castShadow = true;
  return mesh;
}

function createTree(x, y, z) {
  const group = new THREE.Group();
  // 树干
  const trunkH = 0.3 + Math.random() * 0.3;
  const trunk = new THREE.Mesh(
    new THREE.CylinderGeometry(0.03, 0.04, trunkH, 5),
    new THREE.MeshStandardMaterial({ color: 0x5c4033 })
  );
  trunk.position.y = trunkH / 2;
  group.add(trunk);
  // 树冠
  const crownH = 0.3 + Math.random() * 0.2;
  const crown = new THREE.Mesh(
    new THREE.ConeGeometry(0.15 + Math.random() * 0.1, crownH, 6),
    new THREE.MeshStandardMaterial({ color: 0x2d5a1e })
  );
  crown.position.y = trunkH + crownH / 2;
  group.add(crown);
  group.position.set(x, y, z);
  group.castShadow = true;
  return group;
}

function createSandDune(x, y, z) {
  const size = 0.2 + Math.random() * 0.2;
  const geo = new THREE.SphereGeometry(size, 6, 4, 0, Math.PI * 2, 0, Math.PI / 2);
  const mat = new THREE.MeshStandardMaterial({ color: 0xd4a855, roughness: 1.0 });
  const mesh = new THREE.Mesh(geo, mat);
  mesh.position.set(x, y, z);
  mesh.scale.y = 0.4;
  return mesh;
}

function createPuddle(x, y, z) {
  const size = 0.3 + Math.random() * 0.3;
  const geo = new THREE.CircleGeometry(size, 8);
  geo.rotateX(-Math.PI / 2);
  const mat = new THREE.MeshStandardMaterial({
    color: 0x2a4a4a, transparent: true, opacity: 0.5, roughness: 0.1, metalness: 0.2,
  });
  const mesh = new THREE.Mesh(geo, mat);
  mesh.position.set(x, y + 0.02, z);
  return mesh;
}

function createRubble(x, y, z) {
  const size = 0.1 + Math.random() * 0.15;
  const geo = new THREE.CylinderGeometry(size * 0.6, size, size * 2, 5);
  const mat = new THREE.MeshStandardMaterial({ color: 0x6b5b4f, roughness: 0.95 });
  const mesh = new THREE.Mesh(geo, mat);
  mesh.position.set(x, y + size, z);
  mesh.rotation.z = (Math.random() - 0.5) * 0.8;
  return mesh;
}

/**
 * 查询地面高度（双线性插值）
 * @param {number} wx - 世界 X 坐标
 * @param {number} wz - 世界 Z 坐标
 * @returns {number} 地面高度 Y
 */
export function getHeightAt(wx, wz) {
  if (!heightData) return 0;

  const cfg = TERRAIN_GEN;
  const seg = cfg.gridSize;
  const size = cfg.worldSize;
  const half = size / 2;

  // 世界坐标 → 网格坐标
  const gx = ((wx + half) / size) * seg;
  const gz = ((wz + half) / size) * seg;

  // 超出范围返回 0
  if (gx < 0 || gx >= seg || gz < 0 || gz >= seg) return 0;

  const ix = Math.floor(gx);
  const iz = Math.floor(gz);
  const fx = gx - ix;
  const fz = gz - iz;

  const ix1 = Math.min(ix + 1, seg);
  const iz1 = Math.min(iz + 1, seg);

  const w = seg + 1;
  const h00 = heightData[iz * w + ix] || 0;
  const h10 = heightData[iz * w + ix1] || 0;
  const h01 = heightData[iz1 * w + ix] || 0;
  const h11 = heightData[iz1 * w + ix1] || 0;

  // 双线性插值
  const h0 = h00 * (1 - fx) + h10 * fx;
  const h1 = h01 * (1 - fx) + h11 * fx;
  return h0 * (1 - fz) + h1 * fz;
}

/**
 * 简单 2D simplex-like 噪声（用于地形自然变化）
 */
function simplexNoise(x, y) {
  const n = Math.sin(x * 12.9898 + y * 78.233) * 43758.5453;
  return (n - Math.floor(n)) * 2 - 1;
}

// 需要从 constants 导入
const CITY_INFLUENCE_RADIUS = 5.0;
