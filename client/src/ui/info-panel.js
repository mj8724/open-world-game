/**
 * InfoPanel — 右侧节点详情面板
 * 含建造队列进度显示
 */
import i18n from '../i18n/i18n.js';
import { stateStore } from '../bridge/state-store.js';
import { sendAttack, sendBuild, sendRetreat } from '../bridge/command-sender.js';
import { eventBus } from './event-bus.js';
import { getAllBuildings } from '../dict/client-dict.js';

let currentNodeId = null;

/** 初始化面板 */
export function initInfoPanel() {
  document.getElementById('panel-close')?.addEventListener('click', () => {
    showPlaceholder();
    eventBus.emit('panel-closed');
  });

  document.getElementById('btn-research')?.addEventListener('click', () => {
    eventBus.emit('open-tech-panel');
  });

  document.getElementById('btn-logistics')?.addEventListener('click', () => {
    eventBus.emit('open-logistics-panel', currentNodeId);
  });

  eventBus.on('node-selected', (nodeId) => showNodeDetails(nodeId));
  eventBus.on('node-deselected', () => showPlaceholder());
}

function showPlaceholder() {
  currentNodeId = null;
  const placeholder = document.getElementById('panel-placeholder');
  const details = document.getElementById('panel-details');
  const title = document.getElementById('panel-title');
  if (placeholder) placeholder.style.display = 'flex';
  if (details) details.style.display = 'none';
  if (title) title.textContent = i18n.t('panel.selectNode');
}

/**
 * 显示节点详情
 * @param {string} nodeId
 */
export function showNodeDetails(nodeId) {
  currentNodeId = nodeId;
  const node = stateStore.getNode(nodeId);
  if (!node) return;

  const placeholder = document.getElementById('panel-placeholder');
  const details = document.getElementById('panel-details');
  const title = document.getElementById('panel-title');

  if (placeholder) placeholder.style.display = 'none';
  if (details) details.style.display = 'block';

  // 节点名称
  const name = i18n.t(`map.node.${nodeId}`, {}) !== `map.node.${nodeId}`
    ? i18n.t(`map.node.${nodeId}`) : node.name;
  if (title) title.textContent = `📍 ${name}`;

  // 势力标签
  const badge = document.getElementById('faction-badge');
  if (badge) {
    const isPlayer = node.factionId === 'PLAYER';
    const isAI = node.factionId === 'AI';
    badge.textContent = isPlayer ? i18n.t('faction.player') : isAI ? i18n.t('faction.ai') : i18n.t('faction.neutral');
    badge.className = 'faction-badge ' + (isPlayer ? 'faction-player' : isAI ? 'faction-ai' : 'faction-neutral');
  }

  // 人口（含人口上限）
  const popEl = document.getElementById('detail-pop');
  if (popEl) {
    const popCap = 5 + (node.farmLevel || 0) * 25;
    popEl.textContent = `👥 ${node.popCount} / ${popCap}` + (node.garrisonCount > 0 ? ` (⚔️ ${node.garrisonCount})` : '');
  }

  // 资源
  const foodEl = document.getElementById('detail-food');
  const ironEl = document.getElementById('detail-iron');
  const ammoEl = document.getElementById('detail-ammo');
  if (foodEl) foodEl.textContent = node.invFood;
  if (ironEl) ironEl.textContent = node.invIron;
  if (ammoEl) ammoEl.textContent = node.invAmmo;

  // 建筑列表
  renderBuildings(node);
  renderAttackControls(node);
  renderArmyControls(node);
}


function renderAttackControls(node) {
  const container = document.getElementById('detail-buildings');
  if (!container) return;
  const existing = document.getElementById('attack-controls');
  if (existing) existing.remove();
  if (node.factionId !== 'PLAYER') return;

  const targets = getAdjacentAttackTargets(node.id);
  const garrison = node.garrisonCount || 0;
  const disabled = garrison <= 0 || targets.length === 0;
  const targetOptions = targets.map(target => `<option value="${target.id}">${nodeName(target.id)}</option>`).join('');
  const message = garrison <= 0
    ? i18n.t('panel.noGarrison')
    : targets.length === 0 ? i18n.t('panel.noAttackTargets') : '';

  container.insertAdjacentHTML('afterend', `
    <div class="attack-controls" id="attack-controls">
      <h4>${i18n.t('panel.attack')}</h4>
      ${message ? `<div class="attack-empty">${message}</div>` : `
        <label>${i18n.t('panel.attackTarget')}<select id="attack-target">${targetOptions}</select></label>
        <label>${i18n.t('panel.troopCount')}<input id="attack-count" type="number" min="1" max="${garrison}" value="${Math.min(1, garrison)}"></label>
      `}
      <button class="action-btn" id="btn-send-attack" ${disabled ? 'disabled' : ''}>${i18n.t('panel.attack')}</button>
    </div>
  `);

  document.getElementById('btn-send-attack')?.addEventListener('click', () => {
    const targetNodeId = document.getElementById('attack-target')?.value;
    const count = Math.max(1, Math.min(garrison, Number(document.getElementById('attack-count')?.value || 1)));
    if (!targetNodeId || count > garrison) return;
    sendAttack(node.id, targetNodeId, count);
    refreshCurrentPanel();
  });
}

function renderArmyControls(node) {
  const anchor = document.getElementById('attack-controls') || document.getElementById('detail-buildings');
  if (!anchor) return;
  const existing = document.getElementById('army-controls');
  if (existing) existing.remove();
  if (node.factionId !== 'PLAYER') return;

  const armies = Object.values(stateStore.armies || {})
    .filter(army => army.factionId === 'PLAYER' && army.currentNodeId === node.id && army.state === 'MOVING')
    .sort((a, b) => Number(a.entityId) - Number(b.entityId));
  if (!armies.length) return;

  anchor.insertAdjacentHTML('afterend', `
    <div class="army-controls" id="army-controls">
      <h4>${i18n.t('panel.activeArmies')}</h4>
      ${armies.map(army => `
        <div class="army-row">
          <span>${i18n.t('panel.armyTarget')}: ${nodeName(army.targetNodeId)}</span>
          <span>${i18n.t('panel.troopCount')}: ${army.troopCount}</span>
          <button class="action-btn action-btn-danger btn-retreat" data-army-id="${army.entityId}">${i18n.t('panel.retreat')}</button>
        </div>
      `).join('')}
    </div>
  `);

  document.querySelectorAll('.btn-retreat').forEach(btn => {
    btn.addEventListener('click', () => {
      sendRetreat(Number(btn.dataset.armyId));
      btn.disabled = true;
    });
  });
}

function getAdjacentAttackTargets(nodeId) {
  const ids = new Set();
  Object.values(stateStore.edges || {}).forEach(edge => {
    if (edge.sourceNodeId === nodeId) ids.add(edge.targetNodeId);
    if (edge.targetNodeId === nodeId) ids.add(edge.sourceNodeId);
  });
  return [...ids].map(id => stateStore.nodes[id]).filter(node => node && node.factionId !== 'PLAYER');
}

function nodeName(id) {
  const node = stateStore.nodes[id];
  return i18n.t(`map.node.${id}`, {}) !== `map.node.${id}` ? i18n.t(`map.node.${id}`) : (node?.name || id);
}

/** 渲染建筑列表和升级按钮 */
function renderBuildings(node) {
  const container = document.getElementById('detail-buildings');
  if (!container) return;

  const buildings = getAllBuildings();
  const isOwned = node.factionId === 'PLAYER';

  // 当前节点的建造队列
  const nodeQueue = (stateStore.buildQueue || []).filter(b => b.nodeId === node.id);

  const rows = [
    { type: 'FARM',          level: node.farmLevel },
    { type: 'MINE',          level: node.mineLevel },
    { type: 'ARSENAL',       level: node.arsenalLevel },
    { type: 'WALL',          level: node.wallLevel },
    { type: 'ORACLE_BEACON', level: node.beaconLevel },
  ];

  container.innerHTML = rows.map(({ type, level }) => {
    const def = buildings[type];
    const name = i18n.t(`building.${type}.name`, {}) !== `building.${type}.name`
      ? i18n.t(`building.${type}.name`) : type;
    const canUpgrade = isOwned && level < def.maxLevel;

    // 检查是否正在建造
    const inQueue = nodeQueue.find(b => b.buildingType === type);

    let actionHtml = '';
    if (inQueue) {
      const pct = inQueue.totalTicks > 0
        ? Math.round((1 - inQueue.remainingTicks / inQueue.totalTicks) * 100)
        : 50;
      actionHtml = `
        <div class="build-progress">
          <div class="build-progress-bar" style="width:${pct}%"></div>
          <span class="build-progress-text">${inQueue.remainingTicks}t</span>
        </div>`;
    } else if (canUpgrade) {
      actionHtml = `<button class="btn-upgrade" data-node="${node.id}" data-building="${type}">${i18n.t('panel.upgrade')}</button>`;
    } else {
      actionHtml = `<span class="building-maxed">${level >= def.maxLevel ? 'MAX' : '—'}</span>`;
    }

    return `
      <div class="building-row">
        <span class="building-icon">${def.icon}</span>
        <span class="building-name">${name}</span>
        <span class="building-level">Lv.${level}</span>
        ${actionHtml}
      </div>
    `;
  }).join('');

  // 绑定升级按钮
  container.querySelectorAll('.btn-upgrade').forEach(btn => {
    btn.addEventListener('click', () => {
      const nodeId = btn.dataset.node;
      const buildingType = btn.dataset.building;
      sendBuild(nodeId, buildingType);
      btn.disabled = true;
      btn.textContent = '⏳';
      // 短暂延迟后刷新面板以显示建造队列
      setTimeout(() => refreshCurrentPanel(), 500);
    });
  });
}

/** 刷新当前选中的节点 */
export function refreshCurrentPanel() {
  if (currentNodeId) showNodeDetails(currentNodeId);
}
