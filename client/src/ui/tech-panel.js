/**
 * TechPanel — 科技树面板
 * 4 列科技树布局，支持研发操作
 */
import i18n from '../i18n/i18n.js';
import { stateStore } from '../bridge/state-store.js';
import { sendResearch } from '../bridge/command-sender.js';
import { eventBus } from './event-bus.js';

const TECH_TREE = {
  BUILD:  [{ id: 'BUILD_MASONRY', tier: 1 }, { id: 'BUILD_ARCHITECTURE', tier: 2 }, { id: 'BUILD_ENGINEERING', tier: 3 }],
  TRANSIT:[{ id: 'TRANSIT_ROADS', tier: 1 }, { id: 'TRANSIT_WAGONS', tier: 2 }, { id: 'TRANSIT_RAILWAY', tier: 3 }],
  FOOD:   [{ id: 'FOOD_IRRIGATION', tier: 1 }, { id: 'FOOD_FERTILIZER', tier: 2 }, { id: 'FOOD_MECHANIZATION', tier: 3 }],
  WEAPON: [{ id: 'WEAPON_BLADES', tier: 1 }, { id: 'WEAPON_GUNPOWDER', tier: 2 }, { id: 'WEAPON_MACHINEGUN', tier: 3 }],
};

const TECH_META = {
  BUILD_MASONRY:     { icon: '🧱', ticks: 15,  cost: 50  },
  BUILD_ARCHITECTURE:{ icon: '🏛️', ticks: 30,  cost: 150 },
  BUILD_ENGINEERING: { icon: '⚙️', ticks: 60,  cost: 400 },
  TRANSIT_ROADS:     { icon: '🛤️', ticks: 10,  cost: 30  },
  TRANSIT_WAGONS:    { icon: '🐎', ticks: 25,  cost: 100 },
  TRANSIT_RAILWAY:   { icon: '🚂', ticks: 50,  cost: 350 },
  FOOD_IRRIGATION:   { icon: '💧', ticks: 12,  cost: 40  },
  FOOD_FERTILIZER:   { icon: '🧪', ticks: 28,  cost: 120 },
  FOOD_MECHANIZATION:{ icon: '🚜', ticks: 55,  cost: 380 },
  WEAPON_BLADES:     { icon: '⚔️', ticks: 15,  cost: 60  },
  WEAPON_GUNPOWDER:  { icon: '💣', ticks: 35,  cost: 200 },
  WEAPON_MACHINEGUN: { icon: '🔫', ticks: 65,  cost: 500 },
};

let visible = false;

export function initTechPanel() {
  // 创建科技面板 DOM
  const overlay = document.createElement('div');
  overlay.id = 'tech-overlay';
  overlay.className = 'tech-overlay';
  overlay.style.display = 'none';
  overlay.innerHTML = `
    <div class="tech-panel">
      <div class="tech-header">
        <h2>🔬 ${i18n.t('tech.title')}</h2>
        <button id="tech-close" class="panel-close-btn">✕</button>
      </div>
      <div class="tech-research-status" id="tech-research-status"></div>
      <div class="tech-grid" id="tech-grid"></div>
    </div>
  `;
  document.body.appendChild(overlay);

  document.getElementById('tech-close')?.addEventListener('click', hideTechPanel);
  overlay.addEventListener('click', (e) => { if (e.target === overlay) hideTechPanel(); });

  // InfoPanel 的研究按钮触发
  eventBus.on('open-tech-panel', showTechPanel);
}

export function showTechPanel() {
  visible = true;
  const overlay = document.getElementById('tech-overlay');
  if (overlay) overlay.style.display = 'flex';
  renderTechTree();
}

export function hideTechPanel() {
  visible = false;
  const overlay = document.getElementById('tech-overlay');
  if (overlay) overlay.style.display = 'none';
}

function renderTechTree() {
  const grid = document.getElementById('tech-grid');
  const statusEl = document.getElementById('tech-research-status');
  if (!grid) return;
  renderTechTreeInto(grid, statusEl, () => renderTechTree());
}

export function renderTechTreeInto(grid, statusEl = null, refresh = null) {
  if (!grid) return;

  const faction = stateStore.getPlayerFaction();
  if (!faction) return;

  const unlocked = new Set(faction.unlockedTechs || []);
  const researching = faction.researchingTechId;
  const progress = faction.researchProgress || 0;

  // 研发状态栏
  if (statusEl) {
    if (researching) {
      const meta = TECH_META[researching];
      const pct = meta ? Math.round((progress / meta.ticks) * 100) : 0;
      const name = i18n.t(`tech.${researching}.name`, {}) !== `tech.${researching}.name`
        ? i18n.t(`tech.${researching}.name`) : researching;
      statusEl.innerHTML = `
        <div class="research-active">
          <span>${meta?.icon || '🔬'} ${i18n.t('tech.researching')}: <strong>${name}</strong></span>
          <div class="research-progress">
            <div class="research-progress-bar" style="width:${pct}%"></div>
            <span class="research-progress-text">${progress}/${meta?.ticks || '?'} (${pct}%)</span>
          </div>
        </div>
      `;
    } else {
      statusEl.innerHTML = `<div class="research-idle">${i18n.t('tech.idle')}</div>`;
    }
  }

  // 科技树网格
  const categories = Object.entries(TECH_TREE);
  grid.innerHTML = categories.map(([cat, techs]) => {
    const catName = i18n.t(`tech.category.${cat}`, {}) !== `tech.category.${cat}`
      ? i18n.t(`tech.category.${cat}`) : cat;
    return `
      <div class="tech-column">
        <div class="tech-column-header">${catName}</div>
        ${techs.map(({ id, tier }) => {
          const meta = TECH_META[id];
          const name = i18n.t(`tech.${id}.name`, {}) !== `tech.${id}.name`
            ? i18n.t(`tech.${id}.name`) : id;
          const desc = i18n.t(`tech.${id}.desc`, {}) !== `tech.${id}.desc`
            ? i18n.t(`tech.${id}.desc`) : '';
          const isUnlocked = unlocked.has(id);
          const isResearching = researching === id;
          const canResearch = !isUnlocked && !researching && checkPrereqs(id, unlocked);

          let statusClass = 'locked';
          if (isUnlocked) statusClass = 'unlocked';
          else if (isResearching) statusClass = 'researching';
          else if (canResearch) statusClass = 'available';

          return `
            <div class="tech-node tech-${statusClass}" data-tech-id="${id}">
              <div class="tech-node-icon">${meta?.icon || '❓'}</div>
              <div class="tech-node-info">
                <div class="tech-node-name">${name}</div>
                <div class="tech-node-desc">${desc}</div>
                <div class="tech-node-cost">⛏️ ${meta?.cost || '?'} | ⏱️ ${meta?.ticks || '?'}t</div>
              </div>
              ${canResearch ? `<button class="tech-research-btn" data-tech="${id}">${i18n.t('tech.research_btn')}</button>` : ''}
              ${isResearching ? `<div class="tech-researching-badge">${i18n.t('tech.researching_badge')}</div>` : ''}
              ${isUnlocked ? `<div class="tech-unlocked-badge">✓</div>` : ''}
            </div>
          `;
        }).join('')}
      </div>
    `;
  }).join('');

  // 绑定研发按钮
  grid.querySelectorAll('.tech-research-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      sendResearch(btn.dataset.tech);
      btn.disabled = true;
      btn.textContent = '⏳';
      setTimeout(refresh || (() => renderTechTreeInto(grid, statusEl, refresh)), 500);
    });
  });
}

function checkPrereqs(techId, unlocked) {
  const prereqs = {
    BUILD_MASONRY: [], BUILD_ARCHITECTURE: ['BUILD_MASONRY'], BUILD_ENGINEERING: ['BUILD_ARCHITECTURE'],
    TRANSIT_ROADS: [], TRANSIT_WAGONS: ['TRANSIT_ROADS'], TRANSIT_RAILWAY: ['TRANSIT_WAGONS'],
    FOOD_IRRIGATION: [], FOOD_FERTILIZER: ['FOOD_IRRIGATION'], FOOD_MECHANIZATION: ['FOOD_FERTILIZER'],
    WEAPON_BLADES: [], WEAPON_GUNPOWDER: ['WEAPON_BLADES'], WEAPON_MACHINEGUN: ['WEAPON_GUNPOWDER'],
  };
  return (prereqs[techId] || []).every(p => unlocked.has(p));
}

// 自动刷新
eventBus.on('state-tick-update', () => { if (visible) renderTechTree(); });
