/**
 * InfoPanel — 右侧节点详情面板
 * 含建造队列进度显示
 */
import i18n from '../i18n/i18n.js';
import { stateStore } from '../bridge/state-store.js';
import { sendAttack, sendAttackNode, sendCreateCompany, sendMoveUnit, sendRetreatUnit, sendBuild } from '../bridge/command-sender.js';
import { eventBus } from './event-bus.js';
import { getAllBuildings, getAllUnits, getUnit } from '../dict/client-dict.js';
import { renderCitySketch } from './city-sketch.js';

let currentNodeId = null;

/** 初始化面板 */
export function initInfoPanel() {
  document.getElementById('panel-close')?.addEventListener('click', () => {
    showPlaceholder();
    eventBus.emit('panel-closed');
  });

  eventBus.on('node-selected', (nodeId) => showNodeDetails(nodeId));
  eventBus.on('node-deselected', () => showPlaceholder());
  eventBus.on('army-selected', (entityId) => showArmyDetails(entityId));
  eventBus.on('wild-resource-selected', (id) => showWildResourceDetails(id));
  eventBus.on('neutral-structure-selected', (id) => showNeutralStructureDetails(id));
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

  const sketchEl = document.getElementById('detail-city-sketch');
  if (sketchEl) sketchEl.innerHTML = renderCitySketch(node);

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
  renderCompanyControls(node);
  renderAttackControls(node);
  renderArmyControls(node);
}


function renderCompanyControls(node) {
  const container = document.getElementById('detail-buildings');
  if (!container) return;
  document.getElementById('company-controls')?.remove();

  const companies = Object.values(stateStore.armies || {})
    .filter(army => army.currentNodeId === node.id && !army.currentEdgeId && army.state === 'IDLE')
    .sort((a, b) => Number(a.entityId) - Number(b.entityId));
  const playerCompanies = companies.filter(army => army.factionId === 'PLAYER');
  const adjacent = getAdjacentNodes(node.id);
  const unitOptions = Object.values(getAllUnits()).map(unit => `<option value="${unit.id}">${unit.icon} ${i18n.t(`unit.${unit.id}.name`, {}) !== `unit.${unit.id}.name` ? i18n.t(`unit.${unit.id}.name`) : unit.id}</option>`).join('');

  container.insertAdjacentHTML('afterend', `
    <div class="company-controls" id="company-controls">
      <h4>${i18n.t('panel.companies')}</h4>
      ${companies.length ? companies.map(army => companyRowHtml(army, adjacent)).join('') : `<div class="attack-empty">${i18n.t('panel.noCompanies')}</div>`}
      ${node.factionId === 'PLAYER' ? `
        <div class="company-create">
          <select id="company-unit-type">${unitOptions}</select>
          <button class="action-btn" id="btn-create-company">${i18n.t('panel.createCompany')}</button>
        </div>
      ` : ''}
    </div>
  `);

  document.getElementById('btn-create-company')?.addEventListener('click', () => {
    sendCreateCompany(node.id, document.getElementById('company-unit-type')?.value || 'MILITIA');
  });

  document.querySelectorAll('.company-move').forEach(select => {
    select.addEventListener('change', () => {
      const targetNodeId = select.value;
      if (!targetNodeId) return;
      const entityId = Number(select.dataset.armyId);
      const target = stateStore.nodes[targetNodeId];
      if (target?.factionId === 'PLAYER') sendMoveUnit(entityId, targetNodeId);
      else sendAttackNode(entityId, targetNodeId);
      select.value = '';
    });
  });
}

function companyRowHtml(army, adjacent) {
  const unit = getUnit(army.unitDefId || 'MILITIA') || { icon: '⚔️' };
  const canCommand = army.factionId === 'PLAYER';
  const moveOptions = adjacent.map(target => `<option value="${target.id}">${nodeName(target.id)}</option>`).join('');
  const supplyFood = army.supplyFood ?? army.carryFood ?? 0;
  const supplyAmmo = army.supplyAmmo ?? army.carryAmmo ?? 0;
  return `
    <div class="company-row faction-${(army.factionId || 'neutral').toLowerCase()}">
      <div class="company-title"><span>${unit.icon}</span><strong>${army.name || `${army.unitDefId || 'MILITIA'} #${army.entityId}`}</strong></div>
      <div class="company-stats">
        <span>${i18n.t('panel.strength')}: ${army.strength ?? army.troopCount ?? 0}/${army.maxStrength || army.strength || army.troopCount || 0}</span>
        <span>${i18n.t('panel.morale')}: ${Math.round((army.morale || 1) * 100)}%</span>
        <span>${i18n.t('panel.foodAmmo')}: ${supplyFood}/${supplyAmmo}</span>
      </div>
      ${canCommand ? `<select class="company-move" data-army-id="${army.entityId}"><option value="">${i18n.t('panel.move')}</option>${moveOptions}</select>` : ''}
    </div>
  `;
}

function getAdjacentNodes(nodeId) {
  const ids = new Set();
  Object.values(stateStore.edges || {}).forEach(edge => {
    if (edge.sourceNodeId === nodeId) ids.add(edge.targetNodeId);
    if (edge.targetNodeId === nodeId) ids.add(edge.sourceNodeId);
  });
  return [...ids].map(id => stateStore.nodes[id]).filter(Boolean);
}

function renderAttackControls(node) {
  const container = document.getElementById('detail-buildings');
  if (!container) return;
  const existing = document.getElementById('attack-controls');
  if (existing) existing.remove();
  if (node.factionId !== 'PLAYER' || Object.values(stateStore.armies || {}).some(army => army.factionId === 'PLAYER' && army.currentNodeId === node.id && !army.currentEdgeId && army.state === 'IDLE')) return;

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
      sendRetreatUnit(Number(btn.dataset.armyId));
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
      actionHtml = `<button class="building-upgrade-btn" data-building="${type}" data-node="${node.id}">↗</button>`;
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

  container.querySelectorAll('.building-upgrade-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const buildingType = btn.dataset.building;
      const nodeId = btn.dataset.node;
      sendBuild(nodeId, buildingType);
    });
  });
}

/** 刷新当前选中的节点 */
export function refreshCurrentPanel() {
  if (currentNodeId) showNodeDetails(currentNodeId);
}

function showEntityDetails(data, icon, title, fields) {
  currentNodeId = null;
  const placeholder = document.getElementById('panel-placeholder');
  const details = document.getElementById('panel-details');
  const titleEl = document.getElementById('panel-title');
  if (placeholder) placeholder.style.display = 'none';
  if (details) details.style.display = 'block';
  if (titleEl) titleEl.textContent = `${icon} ${title}`;

  const badge = document.getElementById('faction-badge');
  if (badge) badge.textContent = data.factionId || '-';

  const sketchEl = document.getElementById('detail-city-sketch');
  if (sketchEl) sketchEl.innerHTML = '';

  document.getElementById('detail-pop') && (document.getElementById('detail-pop').textContent = '');
  document.getElementById('detail-food') && (document.getElementById('detail-food').textContent = '');
  document.getElementById('detail-iron') && (document.getElementById('detail-iron').textContent = '');
  document.getElementById('detail-ammo') && (document.getElementById('detail-ammo').textContent = '');

  const container = document.getElementById('detail-buildings');
  if (!container) return;
  document.getElementById('company-controls')?.remove();
  document.getElementById('attack-controls')?.remove();
  document.getElementById('army-controls')?.remove();

  container.innerHTML = `
    <div class="entity-detail">
      ${fields.map(f => `<div class="entity-field"><span class="entity-field-label">${f.label}</span><span class="entity-field-value">${f.value}</span></div>`).join('')}
    </div>
  `;
}

export function showArmyDetails(entityId) {
  const army = stateStore.armies?.[entityId];
  if (!army) return;
  const unit = getUnit(army.unitDefId || 'MILITIA') || { icon: '⚔️' };
  const nodeName = army.currentNodeId ? (stateStore.nodes[army.currentNodeId]?.name || army.currentNodeId) : '-';
  const targetName = army.targetNodeId ? (stateStore.nodes[army.targetNodeId]?.name || army.targetNodeId) : '-';
  const factionLabel = army.factionId === 'PLAYER' ? '玩家' : army.factionId === 'AI' ? 'AI' : '中立';

  showEntityDetails(army, unit.icon, army.name || `军队 #${entityId}`, [
    { label: '势力', value: factionLabel },
    { label: '兵力', value: `${army.strength ?? army.troopCount ?? 0} / ${army.maxStrength || army.strength || army.troopCount || 0}` },
    { label: '士气', value: `${Math.round((army.morale || 1) * 100)}%` },
    { label: '补给 (粮/弹)', value: `${army.supplyFood ?? army.carryFood ?? 0} / ${army.supplyAmmo ?? army.carryAmmo ?? 0}` },
    { label: '当前位置', value: nodeName },
    { label: '目标', value: army.targetNodeId ? targetName : '-' },
    { label: '状态', value: army.state === 'MOVING' ? '移动中' : army.state === 'IDLE' ? '待命' : army.state },
  ]);
}

export function showWildResourceDetails(id) {
  const wr = stateStore.wildResources?.[id];
  if (!wr) return;

  showEntityDetails(wr, '🌿', `野外资源 #${id}`, [
    { label: '资源类型', value: wr.resourceType || '-' },
    { label: '产量', value: `${wr.yield ?? 0}` },
    { label: '坐标', value: `(${wr.x?.toFixed(0)}, ${wr.z?.toFixed(0)})` },
  ]);
}

export function showNeutralStructureDetails(id) {
  const ns = stateStore.neutralStructures?.[id];
  if (!ns) return;

  showEntityDetails(ns, '🏛️', `中立建筑 #${id}`, [
    { label: '建筑类型', value: ns.structureType || '-' },
    { label: '坐标', value: `(${ns.x?.toFixed(0)}, ${ns.z?.toFixed(0)})` },
  ]);
}
