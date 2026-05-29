import i18n from '../i18n/i18n.js';
import { stateStore } from '../bridge/state-store.js';
import {
  sendCancelRoute,
  sendClearRallyPoint,
  sendCreateRoute,
  sendProduceTransport,
  sendSetRallyPoint,
  sendUpdateRoute,
} from '../bridge/command-sender.js';
import { eventBus } from './event-bus.js';
import { getAllResources, getAllTransports, getResource, getTransport } from '../dict/client-dict.js';

let visible = false;
let selectedNodeId = null;

export function initLogisticsPanel() {
  const overlay = document.createElement('div');
  overlay.id = 'logistics-overlay';
  overlay.className = 'logistics-overlay';
  overlay.style.display = 'none';
  overlay.innerHTML = `
    <div class="logistics-panel">
      <div class="logistics-header">
        <h2>${i18n.t('logistics.title')}</h2>
        <button id="logistics-close" class="panel-close-btn">✕</button>
      </div>
      <div class="logistics-body" id="logistics-body"></div>
    </div>
  `;
  document.body.appendChild(overlay);
  const body = document.getElementById('logistics-body');
  if (!body) return;
  bindLogisticsActions(body, () => renderLogisticsPanel());
  document.getElementById('logistics-close')?.addEventListener('click', hideLogisticsPanel);
  overlay.addEventListener('click', (e) => { if (e.target === overlay) hideLogisticsPanel(); });
  eventBus.on('open-logistics-panel', (nodeId) => showLogisticsPanel(nodeId));
  let renderQueued = false;
  const requestRender = () => {
    if (!visible || renderQueued || isEditingLogisticsForm()) return;
    renderQueued = true;
    requestAnimationFrame(() => {
      renderQueued = false;
      if (visible && !isEditingLogisticsForm()) renderLogisticsPanel();
    });
  };
  eventBus.on('state-full-update', requestRender);
  eventBus.on('state-tick-update', requestRender);
}

function isEditingLogisticsForm() {
  const active = document.activeElement;
  return !!active?.matches?.('#logistics-body input, #logistics-body select, #logistics-body textarea');
}

export function showLogisticsPanel(nodeId = null) {
  visible = true;
  selectedNodeId = nodeId || selectedNodeId;
  const overlay = document.getElementById('logistics-overlay');
  if (overlay) overlay.style.display = 'flex';
  renderLogisticsPanel();
}

export function hideLogisticsPanel() {
  visible = false;
  const overlay = document.getElementById('logistics-overlay');
  if (overlay) overlay.style.display = 'none';
}

function renderLogisticsPanel() {
  const body = document.getElementById('logistics-body');
  if (!body) return;
  renderLogisticsInto(body, selectedNodeId, (nextNodeId) => {
    selectedNodeId = nextNodeId || selectedNodeId;
    renderLogisticsPanel();
  }, { bindActions: false });
}

export function renderLogisticsInto(body, nodeId = null, refresh = null, options = {}) {
  if (!body) return;

  const playerNodes = getPlayerNodes();
  const selected = nodeId && stateStore.nodes[nodeId]?.factionId === 'PLAYER'
    ? nodeId
    : playerNodes[0]?.id;

  if (!selected) {
    body.innerHTML = `<div class="logistics-empty">${i18n.t('management.noPlayerNodes')}</div>`;
    return;
  }

  const rerender = (nextNodeId = selected) => {
    if (refresh) refresh(nextNodeId);
    else renderLogisticsInto(body, nextNodeId, refresh);
  };

  body.innerHTML = `
    <div class="logistics-grid">
      <section class="logistics-section">
        <h3>${i18n.t('logistics.rally_points')}</h3>
        ${renderRallyEditor(playerNodes, selected)}
      </section>
      <section class="logistics-section">
        <h3>${i18n.t('logistics.manual_route')}</h3>
        ${renderManualRouteForm(playerNodes, selected)}
      </section>
      <section class="logistics-section logistics-section-wide">
        <h3>${i18n.t('logistics.routes')}</h3>
        ${renderRoutes()}
      </section>
      <section class="logistics-section logistics-section-wide">
        <h3>${i18n.t('logistics.transport_stock')}</h3>
        ${renderTransportStock(playerNodes)}
      </section>
    </div>
  `;

  bindRallyEditor(body, rerender);
  bindManualRouteForm(body, rerender);
  if (options.bindActions !== false) bindLogisticsActions(body, rerender, { once: true });
}

function renderRallyEditor(nodes, selected) {
  const rally = stateStore.rallyPoints[selected] || { cargoPolicies: {} };
  const nodeOptions = renderNodeOptions(nodes, selected);
  const rows = Object.keys(getAllResources()).map((cargo) => {
    const policy = rally.cargoPolicies?.[cargo] || {};
    return `
      <div class="logistics-policy-row" data-cargo="${cargo}">
        <label><input type="checkbox" class="rally-enabled" ${policy.enabled ? 'checked' : ''}> ${cargoLabel(cargo)}</label>
        <input class="rally-target" type="number" min="1" value="${policy.targetQuantity ?? 500}" ${policy.unlimited ? 'disabled' : ''}>
        <label><input type="checkbox" class="rally-unlimited" ${policy.unlimited ? 'checked' : ''}> ${i18n.t('logistics.unlimited')}</label>
        <input class="rally-priority" type="number" min="0" max="100" value="${policy.priority ?? 50}">
      </div>
    `;
  }).join('');

  return `
    <div class="logistics-form">
      <label>${i18n.t('logistics.rally_node')}<select id="rally-node">${nodeOptions}</select></label>
      <div class="logistics-policy-head"><span>${i18n.t('logistics.cargo')}</span><span>${i18n.t('logistics.target_quantity')}</span><span>${i18n.t('logistics.priority')}</span></div>
      ${rows}
      <div class="logistics-actions">
        <button class="action-btn" id="save-rally">${i18n.t('logistics.save_rally')}</button>
        <button class="action-btn action-btn-danger" id="clear-rally">${i18n.t('logistics.clear_rally')}</button>
      </div>
    </div>
  `;
}

function renderManualRouteForm(nodes, selected) {
  const target = nodes.find(n => n.id !== selected)?.id || selected;
  const fromStock = stateStore.transportStocks[selected]?.stock || {};
  const selectedTransport = Object.keys(getAllTransports()).find(t => (fromStock[t]?.idle || 0) > 0) || 'PORTER';
  const estimate = estimateRoute(selected, target, selectedTransport);

  return `
    <div class="logistics-form">
      <label>${i18n.t('logistics.from')}<select id="route-from">${renderNodeOptions(nodes, selected)}</select></label>
      <label>${i18n.t('logistics.to')}<select id="route-to">${renderNodeOptions(nodes, target)}</select></label>
      <label>${i18n.t('logistics.cargo')}<select id="route-cargo">${Object.keys(getAllResources()).map(r => `<option value="${r}">${cargoLabel(r)}</option>`).join('')}</select></label>
      <label>${i18n.t('logistics.transport')}<select id="route-transport">${Object.keys(getAllTransports()).map(t => `<option value="${t}" ${t === selectedTransport ? 'selected' : ''}>${transportLabel(t)} (${fromStock[t]?.idle || 0})</option>`).join('')}</select></label>
      <label>${i18n.t('logistics.assigned')}<input id="route-count" type="number" min="1" value="1"></label>
      <label>${i18n.t('logistics.priority')}<input id="route-priority" type="number" min="0" max="100" value="50"></label>
      <div class="logistics-estimate">${estimate.text}</div>
      <button class="action-btn" id="create-route" ${estimate.available ? '' : 'disabled'}>${i18n.t('logistics.create')}</button>
    </div>
  `;
}

function renderRoutes() {
  const routes = Object.values(stateStore.logistics || {});
  if (!routes.length) return `<div class="logistics-empty">${i18n.t('logistics.no_routes')}</div>`;
  return routes.map(route => {
    const pct = Math.round(((route.edgeProgress || route.trips?.[0]?.edgeProgress || 0) * 100));
    return `
      <div class="logistics-route-card ${route.mode === 'AUTO' ? 'route-auto' : 'route-manual'}" data-route-id="${route.entityId}">
        <div class="route-main">
          <strong>${route.mode || 'MANUAL'}</strong>
          <span>${nodeName(route.fromNodeId)} → ${nodeName(route.toNodeId)}</span>
          <span>${cargoLabel(route.cargoType)} · ${transportLabel(route.transportType)} × ${route.assignedTransportCount || 1}</span>
        </div>
        <div class="route-meta">
          <span>${i18n.t('logistics.progress')}: ${pct}%</span>
          <span>${i18n.t('logistics.recommended')}: ${route.estimatedRequiredTransportCount || 1}</span>
          <span>${i18n.t('logistics.throughput')}: ${(route.estimatedThroughputPerTick || 0).toFixed(1)}/t</span>
          <span>${route.returning ? i18n.t('logistics.returning') : i18n.t('logistics.outbound')}</span>
        </div>
        <div class="logistics-actions">
          ${route.mode === 'AUTO' ? `<button class="btn-manual" data-id="${route.entityId}">${i18n.t('logistics.manual_override')}</button>` : ''}
          <button class="btn-toggle-route" data-id="${route.entityId}" data-enabled="${route.enabled ? 'false' : 'true'}">${route.enabled ? 'Pause' : 'Resume'}</button>
          <button class="btn-cancel-route action-btn-danger" data-id="${route.entityId}">${i18n.t('logistics.cancel')}</button>
        </div>
      </div>
    `;
  }).join('');
}

function renderTransportStock(nodes) {
  return nodes.map(node => {
    const stock = stateStore.transportStocks[node.id]?.stock || {};
    const rows = Object.keys(getAllTransports()).map(type => {
      const entry = stock[type] || {};
      const def = getTransport(type);
      return `
        <div class="transport-row">
          <span>${transportLabel(type)}</span>
          <span>${i18n.t('logistics.idle')}: ${entry.idle || 0}</span>
          <span>${i18n.t('logistics.assigned')}: ${entry.assigned || 0}</span>
          <span>${i18n.t('logistics.total')}: ${entry.total || 0}</span>
          <span>${i18n.t('logistics.maintenance')}: 🌾${def.maintenanceFood}/t ⛏️${def.maintenanceIron}/t</span>
          <button class="btn-produce" data-node="${node.id}" data-type="${type}">${i18n.t('logistics.produce')}</button>
        </div>
      `;
    }).join('');
    return `<div class="transport-node"><h4>${nodeName(node.id)}</h4>${rows}</div>`;
  }).join('');
}

function bindRallyEditor(container = document, refresh = () => renderLogisticsPanel()) {
  container.querySelector('#rally-node')?.addEventListener('change', (e) => {
    selectedNodeId = e.target.value;
    refresh(e.target.value);
  });
  container.querySelectorAll('.rally-unlimited').forEach(input => {
    input.addEventListener('change', () => {
      const target = input.closest('.logistics-policy-row')?.querySelector('.rally-target');
      if (target) target.disabled = input.checked;
    });
  });
  container.querySelector('#save-rally')?.addEventListener('click', () => {
    const nodeId = container.querySelector('#rally-node')?.value;
    const policies = {};
    container.querySelectorAll('.logistics-policy-row').forEach(row => {
      const cargo = row.dataset.cargo;
      const unlimited = row.querySelector('.rally-unlimited')?.checked || false;
      policies[cargo] = {
        enabled: row.querySelector('.rally-enabled')?.checked || false,
        unlimited,
        targetQuantity: unlimited ? null : Number(row.querySelector('.rally-target')?.value || 0),
        priority: Number(row.querySelector('.rally-priority')?.value || 50),
      };
    });
    sendSetRallyPoint(nodeId, policies);
    refresh(nodeId);
  });
  container.querySelector('#clear-rally')?.addEventListener('click', () => {
    const nodeId = container.querySelector('#rally-node')?.value;
    sendClearRallyPoint(nodeId);
    refresh(nodeId);
  });
}

function bindManualRouteForm(container = document, refresh = () => renderLogisticsPanel()) {
  ['route-from', 'route-to', 'route-transport'].forEach(id => {
    container.querySelector(`#${id}`)?.addEventListener('change', () => refresh(container.querySelector('#route-from')?.value));
  });
  container.querySelector('#create-route')?.addEventListener('click', () => {
    sendCreateRoute({
      fromNodeId: container.querySelector('#route-from')?.value,
      targetNodeId: container.querySelector('#route-to')?.value,
      cargoType: container.querySelector('#route-cargo')?.value,
      transportType: container.querySelector('#route-transport')?.value,
      transportCount: Number(container.querySelector('#route-count')?.value || 1),
      priority: Number(container.querySelector('#route-priority')?.value || 50),
    });
    refresh(container.querySelector('#route-from')?.value);
  });
}

export function bindLogisticsActions(container, refresh = () => renderLogisticsPanel(), options = {}) {
  container?.addEventListener('click', (event) => handleLogisticsClick(event, container, refresh), options.once ? { once: true } : undefined);
}

function handleLogisticsClick(event, container = document, refresh = () => renderLogisticsPanel()) {
  const cancel = event.target.closest('.btn-cancel-route');
  if (cancel) {
    sendCancelRoute(Number(cancel.dataset.id));
    refresh();
    return;
  }

  const manual = event.target.closest('.btn-manual');
  if (manual) {
    sendUpdateRoute(Number(manual.dataset.id), { priority: 50, enabled: true });
    refresh();
    return;
  }

  const toggle = event.target.closest('.btn-toggle-route');
  if (toggle) {
    const routeId = Number(toggle.dataset.id);
    const enabled = toggle.dataset.enabled === 'true';
    sendUpdateRoute(routeId, { enabled });
    if (stateStore.logistics[routeId]) stateStore.logistics[routeId].enabled = enabled;
    refresh();
    return;
  }

  const produce = event.target.closest('.btn-produce');
  if (produce) {
    sendProduceTransport(produce.dataset.node, produce.dataset.type, 1);
    refresh(produce.dataset.node);
  }
}

function getPlayerNodes() {
  const faction = stateStore.getPlayerFaction();
  return (faction?.ownedNodeIds || []).map(id => stateStore.nodes[id]).filter(Boolean);
}

function renderNodeOptions(nodes, selected) {
  return nodes.map(node => `<option value="${node.id}" ${node.id === selected ? 'selected' : ''}>${nodeName(node.id)}</option>`).join('');
}

function nodeName(id) {
  const node = stateStore.nodes[id];
  return i18n.t(`map.node.${id}`, {}) !== `map.node.${id}` ? i18n.t(`map.node.${id}`) : (node?.name || id);
}

function cargoLabel(cargo) {
  const res = getResource(cargo);
  return `${res?.icon || ''} ${i18n.t(`hud.${cargo.toLowerCase()}`, {}) !== `hud.${cargo.toLowerCase()}` ? i18n.t(`hud.${cargo.toLowerCase()}`) : cargo}`;
}

function transportLabel(type) {
  const t = getTransport(type);
  return `${t?.icon || ''} ${i18n.t(`logistics.transport_${type}`, {}) !== `logistics.transport_${type}` ? i18n.t(`logistics.transport_${type}`) : type}`;
}

function estimateRoute(fromId, toId, transportType) {
  const def = getTransport(transportType);
  if (!fromId || !toId || !def) return { available: false, text: '' };
  const path = findCompatiblePath(fromId, toId, transportType);
  if (path.edgeIds.length === 0) {
    return { available: false, text: `${i18n.t('logistics.round_trip')}: -` };
  }
  const distance = path.edgeIds.reduce((sum, id) => sum + (stateStore.edges[id]?.length || 0), 0);
  const roundTrip = Math.round((distance / Math.max(1, def.speed)) * 2);
  return {
    available: true,
    text: `${i18n.t('logistics.round_trip')}: ~${roundTrip}t · ${i18n.t('logistics.capacity')}: ${def.capacity} · ${i18n.t('logistics.throughput')}: ${(def.capacity / Math.max(1, roundTrip)).toFixed(1)}/t`,
  };
}

function findCompatiblePath(fromId, toId, transportType) {
  const queue = [{ id: fromId, edgeIds: [] }];
  const seen = new Set([fromId]);
  while (queue.length) {
    const current = queue.shift();
    if (current.id === toId) return current;
    for (const edge of Object.values(stateStore.edges)) {
      if (!isEdgeCompatible(transportType, edge.edgeType)) continue;
      const next = edge.sourceNodeId === current.id
        ? edge.targetNodeId
        : edge.targetNodeId === current.id ? edge.sourceNodeId : null;
      if (!next || seen.has(next)) continue;
      seen.add(next);
      queue.push({ id: next, edgeIds: [...current.edgeIds, edge.id] });
    }
  }
  return { edgeIds: [] };
}

function isEdgeCompatible(transportType, edgeType) {
  if (transportType === 'TRAIN') return edgeType === 'RAILWAY';
  if (transportType === 'CARRIAGE') return edgeType === 'ROAD' || edgeType === 'RAILWAY';
  return edgeType === 'TRAIL' || edgeType === 'ROAD' || edgeType === 'RAILWAY';
}
