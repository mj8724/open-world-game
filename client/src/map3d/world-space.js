/**
 * 坐标空间转换 — 后端 (x, y) ↔ 3D 世界 (x, y, z)
 */
import { WORLD_SCALE, ELEVATION_SCALE, TERRAIN_CONFIG } from './constants.js';

/** 后端坐标 → 3D 世界坐标（不含高度） */
export function toWorldXZ(backendX, backendY) {
  return {
    x: backendX * WORLD_SCALE,
    z: backendY * WORLD_SCALE,
  };
}

/** 3D 世界坐标 → 后端坐标 */
export function toBackendXZ(worldX, worldZ) {
  return {
    x: worldX / WORLD_SCALE,
    y: worldZ / WORLD_SCALE,
  };
}

/**
 * 后端坐标 → 3D 世界坐标（含高度）
 * @param {number} backendX
 * @param {number} backendY - 后端 Y 对应 3D Z
 * @param {Function} getHeightAt - (worldX, worldZ) => height
 * @returns {{ x: number, y: number, z: number }}
 */
export function toWorldPos(backendX, backendY, getHeightAt) {
  const { x, z } = toWorldXZ(backendX, backendY);
  return {
    x,
    y: getHeightAt ? getHeightAt(x, z) : 0,
    z,
  };
}

/** 后端距离 → 3D 世界距离 */
export function toWorldDistance(backendDist) {
  return backendDist * WORLD_SCALE;
}

/** 3D 世界距离 → 后端距离 */
export function toBackendDistance(worldDist) {
  return worldDist / WORLD_SCALE;
}
