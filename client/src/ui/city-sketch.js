import i18n from '../i18n/i18n.js';
import { stateStore } from '../bridge/state-store.js';

const BUILDINGS = [
  { type: 'FARM', field: 'farmLevel', x: 38, y: 112, labelX: 34, labelY: 142 },
  { type: 'MINE', field: 'mineLevel', x: 50, y: 42, labelX: 34, labelY: 24 },
  { type: 'ARSENAL', field: 'arsenalLevel', x: 184, y: 110, labelX: 166, labelY: 142 },
  { type: 'ORACLE_BEACON', field: 'beaconLevel', x: 186, y: 42, labelX: 160, labelY: 24 },
];

export function renderCitySketch(node) {
  const stationed = stationedTroops(node.id);
  const troopCount = stationed.total || node.garrisonCount || 0;
  const wallLevel = node.wallLevel || 0;
  const wallHp = node.wallHpCurrent || 0;
  const wallClass = wallLevel > 0 ? 'active' : 'inactive';

  return `
    <svg class="city-sketch" viewBox="0 0 260 160" role="img" aria-label="${nodeName(node.id)} city overview">
      <rect class="city-sketch-bg" x="1" y="1" width="258" height="158" rx="14" />
      <path class="city-sketch-line muted" d="M22 128 C64 114 91 138 132 122 S208 112 238 126" />
      <path class="city-sketch-line muted" d="M24 78 C66 64 98 86 132 74 S202 68 236 80" />
      ${renderWall(wallLevel, wallHp, wallClass)}
      ${BUILDINGS.map(item => renderBuilding(node, item)).join('')}
      ${renderTroops(troopCount, stationed.companies, wallLevel > 0)}
      <text class="city-sketch-title" x="130" y="18" text-anchor="middle">${nodeName(node.id)}</text>
    </svg>
  `;
}

function renderWall(level, hp, className) {
  const wallName = i18n.t('building.WALL.name', {}) !== 'building.WALL.name' ? i18n.t('building.WALL.name') : 'Wall';
  const label = level > 0 ? `${wallName} Lv.${level} · HP ${hp}` : `${wallName} —`;
  return `
    <rect class="city-sketch-wall ${className}" x="76" y="38" width="108" height="78" rx="8" />
    <path class="city-sketch-wall ${className}" d="M88 38 v-9 h14 v9 M116 38 v-9 h14 v9 M144 38 v-9 h14 v9 M172 38 v-9 h14" />
    <text class="city-sketch-label" x="130" y="130" text-anchor="middle">${label}</text>
  `;
}

function renderBuilding(node, item) {
  const level = node[item.field] || 0;
  const active = level > 0 ? 'active' : 'inactive';
  const name = i18n.t(`building.${item.type}.name`, {}) !== `building.${item.type}.name`
    ? i18n.t(`building.${item.type}.name`)
    : item.type;
  const label = `${shortName(name)} ${level > 0 ? `Lv.${level}` : '—'}`;

  if (item.type === 'FARM') {
    return `
      <g class="city-sketch-building ${active}">
        <path d="M${item.x - 18} ${item.y - 12} h42 l-8 28 h-42 z" />
        <path d="M${item.x - 12} ${item.y - 5} h30 M${item.x - 16} ${item.y + 4} h30 M${item.x - 20} ${item.y + 13} h30" />
        <text class="city-sketch-label" x="${item.labelX}" y="${item.labelY}" text-anchor="middle">${label}</text>
      </g>
    `;
  }

  if (item.type === 'MINE') {
    return `
      <g class="city-sketch-building ${active}">
        <path d="M${item.x - 22} ${item.y + 18} l18 -32 l16 22 l10 -14 l22 32 z" />
        <path d="M${item.x - 2} ${item.y + 19} q10 -18 22 0" />
        <text class="city-sketch-label" x="${item.labelX}" y="${item.labelY}" text-anchor="middle">${label}</text>
      </g>
    `;
  }

  if (item.type === 'ARSENAL') {
    return `
      <g class="city-sketch-building ${active}">
        <path d="M${item.x - 22} ${item.y + 18} v-28 l13 10 l13 -10 l13 10 l13 -10 v28 z" />
        <path d="M${item.x + 16} ${item.y - 17} v-18 h10 v18" />
        <path d="M${item.x + 24} ${item.y - 42} q10 -8 18 0" />
        <text class="city-sketch-label" x="${item.labelX}" y="${item.labelY}" text-anchor="middle">${label}</text>
      </g>
    `;
  }

  return `
    <g class="city-sketch-building ${active}">
      <path d="M${item.x} ${item.y + 28} v-54" />
      <path d="M${item.x - 12} ${item.y + 28} h24" />
      <path d="M${item.x - 10} ${item.y - 8} h20 l-10 -18 z" />
      <path d="M${item.x - 24} ${item.y - 22} q24 -18 48 0 M${item.x - 16} ${item.y - 34} q16 -12 32 0" />
      <text class="city-sketch-label" x="${item.labelX}" y="${item.labelY}" text-anchor="middle">${label}</text>
    </g>
  `;
}

function renderTroops(total, companies, hasWall) {
  if (total <= 0) return '';
  const visible = Math.min(5, Math.max(1, companies || Math.ceil(total / 5)));
  const baseY = hasWall ? 88 : 96;
  const startX = 130 - (visible - 1) * 7;
  const troops = Array.from({ length: visible }, (_, i) => {
    const x = startX + i * 14;
    return `<g class="city-sketch-troop"><circle cx="${x}" cy="${baseY - 9}" r="3" /><path d="M${x} ${baseY - 6} v12 M${x - 5} ${baseY} h10 M${x} ${baseY + 6} l-5 7 M${x} ${baseY + 6} l5 7" /></g>`;
  }).join('');
  return `
    <g>
      ${troops}
      <text class="city-sketch-label troop-label" x="130" y="${baseY + 30}" text-anchor="middle">${i18n.t('panel.companies')}: ${companies || 1} · ${total}</text>
    </g>
  `;
}

function stationedTroops(nodeId) {
  const companies = Object.values(stateStore.armies || {})
    .filter(army => army.currentNodeId === nodeId && !army.currentEdgeId && army.state === 'IDLE');
  return {
    companies: companies.length,
    total: companies.reduce((sum, army) => sum + Number(army.strength ?? army.troopCount ?? 0), 0),
  };
}

function nodeName(id) {
  const node = stateStore.nodes[id];
  return i18n.t(`map.node.${id}`, {}) !== `map.node.${id}` ? i18n.t(`map.node.${id}`) : (node?.name || id);
}

function shortName(name) {
  return name.length > 5 ? name.slice(0, 5) : name;
}
