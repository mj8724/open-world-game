/**
 * HUD — 顶部全局资源状态栏
 * 带资源变化趋势指示
 */
import i18n from '../i18n/i18n.js';
import { stateStore } from '../bridge/state-store.js';
import { sendSetSpeed } from '../bridge/command-sender.js';
import { eventBus } from './event-bus.js';

let currentSpeed = 1;
let prevTotals = { food: 0, iron: 0, ammo: 0, pop: 0 };

/** 初始化 HUD 交互 */
export function initHUD() {
  document.getElementById('btn-pause')?.addEventListener('click', () => setSpeed(0));
  document.getElementById('btn-play')?.addEventListener('click', () => setSpeed(1));
  document.getElementById('btn-fast')?.addEventListener('click', () => setSpeed(5));

  document.getElementById('btn-settings')?.addEventListener('click', () => {
    const overlay = document.getElementById('settings-overlay');
    if (overlay) overlay.style.display = overlay.style.display === 'none' ? 'flex' : 'none';
  });

  document.getElementById('settings-close')?.addEventListener('click', () => {
    const overlay = document.getElementById('settings-overlay');
    if (overlay) overlay.style.display = 'none';
  });

  document.getElementById('settings-overlay')?.addEventListener('click', (e) => {
    if (e.target.id === 'settings-overlay') e.target.style.display = 'none';
  });
}

function setSpeed(speed) {
  currentSpeed = speed;
  sendSetSpeed(speed);
  document.querySelectorAll('.speed-btn').forEach(btn => btn.classList.remove('active'));
  if (speed === 0) document.getElementById('btn-pause')?.classList.add('active');
  else if (speed === 1) document.getElementById('btn-play')?.classList.add('active');
  else document.getElementById('btn-fast')?.classList.add('active');
}

/** 刷新 HUD 数值与趋势 */
export function updateHUD() {
  const totals = stateStore.getPlayerTotals();

  updateResource('food', totals.food, prevTotals.food);
  updateResource('iron', totals.iron, prevTotals.iron);
  updateResource('ammo', totals.ammo, prevTotals.ammo);
  updateResource('pop',  totals.pop,  prevTotals.pop);

  const tickEl = document.getElementById('tick-counter');
  if (tickEl) tickEl.textContent = stateStore.currentTick;

  // 记录前值用于下次比较
  prevTotals = { ...totals };
}

/**
 * 更新单个资源显示，带变化趋势
 */
function updateResource(key, value, prev) {
  const el = document.getElementById(`res-${key}`);
  if (!el) return;

  el.textContent = formatNumber(value);

  // 变化趋势指示
  const diff = value - prev;
  const parent = el.closest('.hud-resource');
  if (!parent || prev === 0) return;

  // 移除之前的趋势样式
  parent.classList.remove('res-up', 'res-down', 'res-critical');

  if (diff > 0) {
    parent.classList.add('res-up');
  } else if (diff < 0) {
    parent.classList.add('res-down');
  }

  // 粮食低于 100 时警告
  if (key === 'food' && value < 100) {
    parent.classList.add('res-critical');
  }
}

function formatNumber(n) {
  if (n >= 10000) return (n / 1000).toFixed(1) + 'K';
  return n.toLocaleString();
}
