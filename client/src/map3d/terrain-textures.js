/**
 * 程序化地形纹理生成 — Canvas 绘制，无外部贴图
 */
import * as THREE from 'three';
import { TERRAIN_CONFIG } from './constants.js';

const TEX_SIZE = 256;
const cache = new Map();

/**
 * 获取地形纹理
 * @param {string} terrainType
 * @returns {THREE.CanvasTexture}
 */
export function getTerrainTexture(terrainType) {
  if (cache.has(terrainType)) return cache.get(terrainType);

  const canvas = document.createElement('canvas');
  canvas.width = TEX_SIZE;
  canvas.height = TEX_SIZE;
  const ctx = canvas.getContext('2d');

  const cfg = TERRAIN_CONFIG[terrainType];
  const baseColor = cfg?.cssColor || '#4a7c3f';
  const r = parseInt(baseColor.slice(1, 3), 16);
  const g = parseInt(baseColor.slice(3, 5), 16);
  const b = parseInt(baseColor.slice(5, 7), 16);

  switch (terrainType) {
    case 'PLAINS':   drawPlainsTexture(ctx, r, g, b); break;
    case 'MOUNTAIN': drawMountainTexture(ctx, r, g, b); break;
    case 'HILLS':    drawHillsTexture(ctx, r, g, b); break;
    case 'COASTAL':  drawCoastalTexture(ctx, r, g, b); break;
    case 'FOREST':   drawForestTexture(ctx, r, g, b); break;
    case 'DESERT':   drawDesertTexture(ctx, r, g, b); break;
    case 'RUINS':    drawRuinsTexture(ctx, r, g, b); break;
    case 'SWAMP':    drawSwampTexture(ctx, r, g, b); break;
    default:         drawPlainsTexture(ctx, r, g, b);
  }

  const texture = new THREE.CanvasTexture(canvas);
  texture.wrapS = THREE.RepeatWrapping;
  texture.wrapT = THREE.RepeatWrapping;
  texture.repeat.set(4, 4);

  cache.set(terrainType, texture);
  return texture;
}

/** 道路纹理 */
export function getRoadTexture(roadType = 'ROAD') {
  const key = `road_${roadType}`;
  if (cache.has(key)) return cache.get(key);

  const canvas = document.createElement('canvas');
  canvas.width = 64;
  canvas.height = 64;
  const ctx = canvas.getContext('2d');

  if (roadType === 'RAILWAY') {
    ctx.fillStyle = '#4a4a4a';
    ctx.fillRect(0, 0, 64, 64);
    for (let x = 0; x < 64; x += 8) {
      ctx.fillStyle = '#6b4f3a';
      ctx.fillRect(x, 10, 4, 44);
    }
  } else {
    ctx.fillStyle = roadType === 'ROAD' ? '#8B7355' : '#A0937D';
    ctx.fillRect(0, 0, 64, 64);
    for (let i = 0; i < 50; i++) {
      ctx.fillStyle = Math.random() > 0.5 ? 'rgba(0,0,0,0.05)' : 'rgba(255,255,255,0.05)';
      ctx.fillRect(Math.random() * 64, Math.random() * 64, 3, 3);
    }
  }

  const texture = new THREE.CanvasTexture(canvas);
  texture.wrapS = THREE.RepeatWrapping;
  texture.wrapT = THREE.RepeatWrapping;
  cache.set(key, texture);
  return texture;
}

// ─── 各地形纹理绘制函数 ───

function drawPlainsTexture(ctx, r, g, b) {
  ctx.fillStyle = `rgb(${r},${g},${b})`;
  ctx.fillRect(0, 0, TEX_SIZE, TEX_SIZE);
  for (let i = 0; i < 800; i++) {
    const x = Math.random() * TEX_SIZE, y = Math.random() * TEX_SIZE;
    const s = (Math.random() - 0.5) * 30;
    ctx.fillStyle = `rgb(${clamp(r+s)},${clamp(g+s+10)},${clamp(b+s)})`;
    ctx.fillRect(x, y, 2, 4);
  }
}

function drawMountainTexture(ctx, r, g, b) {
  ctx.fillStyle = `rgb(${r},${g},${b})`;
  ctx.fillRect(0, 0, TEX_SIZE, TEX_SIZE);
  for (let i = 0; i < 30; i++) {
    ctx.beginPath();
    ctx.moveTo(Math.random() * TEX_SIZE, Math.random() * TEX_SIZE);
    ctx.lineTo(Math.random() * TEX_SIZE, Math.random() * TEX_SIZE);
    ctx.strokeStyle = `rgba(60,60,60,${0.2 + Math.random() * 0.3})`;
    ctx.lineWidth = 1 + Math.random() * 2;
    ctx.stroke();
  }
  for (let i = 0; i < 200; i++) {
    const x = Math.random() * TEX_SIZE, y = Math.random() * TEX_SIZE;
    const s = (Math.random() - 0.5) * 40;
    ctx.fillStyle = `rgb(${clamp(r+s)},${clamp(g+s)},${clamp(b+s)})`;
    ctx.fillRect(x, y, 2 + Math.random() * 4, 2 + Math.random() * 4);
  }
}

function drawHillsTexture(ctx, r, g, b) {
  ctx.fillStyle = `rgb(${r},${g},${b})`;
  ctx.fillRect(0, 0, TEX_SIZE, TEX_SIZE);
  for (let y = 0; y < TEX_SIZE; y += 32) {
    ctx.beginPath();
    for (let x = 0; x <= TEX_SIZE; x += 4) {
      const o = Math.sin(x * 0.05 + y * 0.02) * 8;
      x === 0 ? ctx.moveTo(x, y + o) : ctx.lineTo(x, y + o);
    }
    ctx.strokeStyle = 'rgba(80,120,60,0.15)';
    ctx.lineWidth = 1;
    ctx.stroke();
  }
  for (let i = 0; i < 400; i++) {
    const x = Math.random() * TEX_SIZE, y = Math.random() * TEX_SIZE;
    const s = (Math.random() - 0.5) * 25;
    ctx.fillStyle = `rgb(${clamp(r+s)},${clamp(g+s+8)},${clamp(b+s)})`;
    ctx.fillRect(x, y, 2, 3);
  }
}

function drawCoastalTexture(ctx, r, g, b) {
  ctx.fillStyle = `rgb(${r},${g},${b})`;
  ctx.fillRect(0, 0, TEX_SIZE, TEX_SIZE);
  for (let y = 0; y < TEX_SIZE; y += 40) {
    ctx.beginPath();
    for (let x = 0; x <= TEX_SIZE; x += 3) {
      const w = Math.sin(x * 0.08 + y * 0.1) * 5;
      x === 0 ? ctx.moveTo(x, y + w) : ctx.lineTo(x, y + w);
    }
    ctx.strokeStyle = 'rgba(100,180,220,0.2)';
    ctx.lineWidth = 2;
    ctx.stroke();
  }
  for (let i = 0; i < 500; i++) {
    ctx.fillStyle = `rgba(210,190,140,${0.3 + Math.random() * 0.3})`;
    ctx.fillRect(Math.random() * TEX_SIZE, Math.random() * TEX_SIZE, 1, 1);
  }
}

function drawForestTexture(ctx, r, g, b) {
  ctx.fillStyle = `rgb(${r},${g},${b})`;
  ctx.fillRect(0, 0, TEX_SIZE, TEX_SIZE);
  for (let i = 0; i < 300; i++) {
    const x = Math.random() * TEX_SIZE, y = Math.random() * TEX_SIZE;
    const s = (Math.random() - 0.5) * 35;
    ctx.fillStyle = `rgb(${clamp(r+s)},${clamp(g+s+15)},${clamp(b+s)})`;
    ctx.beginPath();
    ctx.arc(x, y, (3 + Math.random() * 6) / 2, 0, Math.PI * 2);
    ctx.fill();
  }
  for (let i = 0; i < 20; i++) {
    ctx.fillStyle = 'rgba(0,30,0,0.1)';
    ctx.beginPath();
    ctx.arc(Math.random() * TEX_SIZE, Math.random() * TEX_SIZE, 8 + Math.random() * 8, 0, Math.PI * 2);
    ctx.fill();
  }
}

function drawDesertTexture(ctx, r, g, b) {
  ctx.fillStyle = `rgb(${r},${g},${b})`;
  ctx.fillRect(0, 0, TEX_SIZE, TEX_SIZE);
  for (let y = 0; y < TEX_SIZE; y += 16) {
    ctx.beginPath();
    for (let x = 0; x <= TEX_SIZE; x += 2) {
      const w = Math.sin(x * 0.06 + y * 0.04) * 3;
      x === 0 ? ctx.moveTo(x, y + w) : ctx.lineTo(x, y + w);
    }
    ctx.strokeStyle = 'rgba(200,170,80,0.25)';
    ctx.lineWidth = 1.5;
    ctx.stroke();
  }
  for (let i = 0; i < 100; i++) {
    ctx.fillStyle = 'rgba(255,240,200,0.15)';
    ctx.fillRect(Math.random() * TEX_SIZE, Math.random() * TEX_SIZE, 2, 1);
  }
}

function drawRuinsTexture(ctx, r, g, b) {
  ctx.fillStyle = `rgb(${r},${g},${b})`;
  ctx.fillRect(0, 0, TEX_SIZE, TEX_SIZE);
  for (let i = 0; i < 60; i++) {
    const x = Math.random() * TEX_SIZE, y = Math.random() * TEX_SIZE;
    const s = (Math.random() - 0.5) * 30;
    ctx.fillStyle = `rgb(${clamp(r+s+20)},${clamp(g+s+10)},${clamp(b+s)})`;
    const w = 8 + Math.random() * 16, h = 4 + Math.random() * 8;
    ctx.fillRect(x, y, w, h);
    ctx.strokeStyle = 'rgba(40,30,20,0.2)';
    ctx.lineWidth = 0.5;
    ctx.strokeRect(x, y, w, h);
  }
  for (let i = 0; i < 5; i++) {
    ctx.fillStyle = 'rgba(200,180,100,0.15)';
    ctx.beginPath();
    ctx.arc(Math.random() * TEX_SIZE, Math.random() * TEX_SIZE, 4 + Math.random() * 6, 0, Math.PI * 2);
    ctx.fill();
  }
}

function drawSwampTexture(ctx, r, g, b) {
  ctx.fillStyle = `rgb(${r},${g},${b})`;
  ctx.fillRect(0, 0, TEX_SIZE, TEX_SIZE);
  for (let i = 0; i < 15; i++) {
    const x = Math.random() * TEX_SIZE, y = Math.random() * TEX_SIZE;
    ctx.fillStyle = `rgba(40,80,80,${0.2 + Math.random() * 0.2})`;
    ctx.beginPath();
    ctx.ellipse(x, y, 8 + Math.random() * 12, 5 + Math.random() * 8, Math.random(), 0, Math.PI * 2);
    ctx.fill();
  }
  for (let i = 0; i < 200; i++) {
    ctx.fillStyle = 'rgba(80,100,60,0.2)';
    ctx.fillRect(Math.random() * TEX_SIZE, Math.random() * TEX_SIZE, 2, 2);
  }
}

function clamp(v) { return Math.max(0, Math.min(255, Math.round(v))); }
