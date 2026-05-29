import i18n from '../i18n/i18n.js';
import { stateStore } from '../bridge/state-store.js';
import { sendBuild } from '../bridge/command-sender.js';
import { getAllBuildings } from '../dict/client-dict.js';
import { eventBus } from './event-bus.js';

let visible = false;
let selectedNodeId = null;

const BUILDING_FIELDS = {
  FARM: 'farmLevel',
  MINE: 'mineLevel',
  ARSENAL: 'arsenalLevel',
  WALL: 'wallLevel',
  ORACLE_BEACON: 'beaconLevel',
};

export function initBuildPanel() {
  const overlay = document.createElement('div');
  overlay.id = 'build-overlay';
  overlay.className = 'build-overlay';
  overlay.style.display = 'none';
  overlay.innerHTML = `
    <div class="build-panel">
      <div class="build-header">
        <h2>🏗️ ${i18n.t('build.title')}</h2>
        <button id="build-close" class="panel-close-btn">✕</button>
      </div>
      <div class="build-body" id="build-body"></div>
    </div>
  `;
  document.body.appendChild(overlay);

  document.getElementById('build-close')?.addEventListener('click', hideBuildPanel);
  overlay.addEventListener('click', (event) => { if (event.target === overlay) hideBuildPanel(); });
  eventBus.on('open-build-panel', showBuildPanel);
  eventBus.on('state-full-update', () => { if (visible) renderBuildPanel(); });
  eventBus.on('state-tick-update', () => { if (visible) renderBuildPanel(); });
}

export function showBuildPanel(nodeId = null) {
  visible = true;
  selectedNodeId = nodeId || selectedNodeId;
  const overlay = document.getElementById('build-overlay');
  if (overlay) overlay.style.display = 'flex';
  renderBuildPanel();
}

export function hideBuildPanel() {
  visible = false;
  const overlay = document.getElementById('build-overlay');
  if (overlay) overlay.style.display = 'none';
}

function renderBuildPanel() {
  const body = document.getElementById('build-body');
  if (!body) return;

  const playerNodes = getPlayerNodes();
  const selected = selectedNodeId && stateStore.nodes[selectedNodeId]?.factionId === 'PLAYER'
    ? selectedNodeId
    : playerNodes[0]?.id;
  selectedNodeId = selected || null;

  if (!playerNodes.length || !selectedNodeId) {
    body.innerHTML = `<div class="build-empty">${i18n.t('management.noPlayerNodes')}</div>`;
    return;
  }

  const node = stateStore.nodes[selectedNodeId];
  body.innerHTML = `
    <aside class="build-city-list">
      <h3>${i18n.t('build.playerCities')}</h3>
      ${playerNodes.map(item => `
        <button class="build-city-btn ${item.id === selectedNodeId ? 'active' : ''}" data-node="${item.id}">
          <span>${nodeName(item.id)}</span>
          <small>👥 ${item.popCount || 0}</small>
        </button>
      `).join('')}
    </aside>
    <section class="build-city-detail">
      <div class="build-city-summary">
        <h3>${i18n.t('build.currentCity')}: ${nodeName(node.id)}</h3>
        <span>🌾 ${node.invFood || 0}</span>
        <span>⛏️ ${node.invIron || 0}</span>
        <span>💥 ${node.invAmmo || 0}</span>
      </div>
      <div class="build-card-grid">
        ${Object.values(getAllBuildings()).map(def => buildCardHtml(node, def)).join('')}
      </div>
    </section>
  `;

  body.querySelectorAll('.build-city-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      selectedNodeId = btn.dataset.node;
      renderBuildPanel();
    });
  });

  body.querySelectorAll('.build-upgrade-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      sendBuild(selectedNodeId, btn.dataset.building);
      btn.disabled = true;
      btn.textContent = '⏳';
      setTimeout(renderBuildPanel, 500);
    });
  });
}

function buildCardHtml(node, def) {
  const level = node[BUILDING_FIELDS[def.id]] || 0;
  const queued = (stateStore.buildQueue || []).find(item => item.nodeId === node.id && item.buildingType === def.id);
  const name = i18n.t(`building.${def.id}.name`, {}) !== `building.${def.id}.name` ? i18n.t(`building.${def.id}.name`) : def.id;
  const desc = i18n.t(`building.${def.id}.desc`, {}) !== `building.${def.id}.desc` ? i18n.t(`building.${def.id}.desc`) : '';
  const canBuild = level < def.maxLevel && !queued;
  const pct = queued?.totalTicks > 0 ? Math.round((1 - queued.remainingTicks / queued.totalTicks) * 100) : 0;

  return `
    <div class="build-card">
      <div class="build-card-icon">${def.icon}</div>
      <div class="build-card-main">
        <div class="build-card-title"><strong>${name}</strong><span>Lv.${level} / ${def.maxLevel}</span></div>
        <p>${desc}</p>
        ${queued ? `
          <div class="build-progress build-card-progress">
            <div class="build-progress-bar" style="width:${pct}%"></div>
            <span class="build-progress-text">${i18n.t('build.inProgress')} · ${queued.remainingTicks}t</span>
          </div>
        ` : `<button class="action-btn build-upgrade-btn" data-building="${def.id}" ${canBuild ? '' : 'disabled'}>${level >= def.maxLevel ? i18n.t('build.max') : i18n.t('build.upgrade')}</button>`}
      </div>
    </div>
  `;
}

function getPlayerNodes() {
  const faction = stateStore.getPlayerFaction();
  return (faction?.ownedNodeIds || []).map(id => stateStore.nodes[id]).filter(Boolean);
}

function nodeName(id) {
  const node = stateStore.nodes[id];
  return i18n.t(`map.node.${id}`, {}) !== `map.node.${id}` ? i18n.t(`map.node.${id}`) : (node?.name || id);
}
