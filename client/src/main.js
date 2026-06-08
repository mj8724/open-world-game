/**
 * 《文明模拟器》Demo — 前端入口（3D 版）
 */

// ─── 样式导入 ───
import './styles/index.css';
import './styles/layout.css';
import './styles/hud.css';
import './styles/map3d.css';
import './styles/panels.css';
import './styles/battle-log.css';
import './styles/animations.css';
import './styles/tech-panel.css';
import './styles/logistics-panel.css';
import './styles/build-panel.css';
import './styles/action-bar.css';

// ─── 模块导入 ───
import i18n from './i18n/i18n.js';
import zhCN from './i18n/zh-CN.json';
import enUS from './i18n/en-US.json';
import { gameBridge } from './bridge/game-bridge.js';
import { stateStore } from './bridge/state-store.js';
import { eventBus } from './ui/event-bus.js';
import { initMap, loadMapData, refreshArmyVisuals, refreshLabels, refreshLogisticsVisuals, updateNodeVisual, selectNode, deselectAll } from './map3d/index.js';
import { initHUD, updateHUD } from './ui/hud.js';
import { initInfoPanel, refreshCurrentPanel } from './ui/info-panel.js';
import { initBattleLog, addEntry } from './ui/battle-log.js';
import { initSettingsPanel } from './ui/settings-panel.js';
import { initTechPanel } from './ui/tech-panel.js';
import { initLogisticsPanel } from './ui/logistics-panel.js';
import { initBuildPanel } from './ui/build-panel.js';

// ─── 初始化 ───
async function init() {
  console.log('[App] 文明模拟器 3D 启动...');

  // 1. i18n 初始化
  i18n.loadPack('zh-CN', zhCN);
  i18n.loadPack('en-US', enUS);
  i18n.init();

  // 2. UI 模块初始化
  initHUD();
  initInfoPanel();
  initBattleLog();
  initSettingsPanel();
  initTechPanel();
  initLogisticsPanel();
  initBuildPanel();
  initMap('map-container');

  // 3. 连接服务器
  gameBridge.connect();

  // 4. 监听状态变化
  eventBus.on('state-full-update', () => {
    console.log('[App] 收到完整状态，加载地图...');
    loadMapData();
    updateHUD();
    addEntry(i18n.t('log.connected'), 'success');
  });

  eventBus.on('state-tick-update', ({ delta }) => {
    updateHUD();
    Object.keys(delta?.nodes || {}).forEach(updateNodeVisual);
    refreshCurrentPanel();
    refreshLogisticsVisuals();
  });

  eventBus.on('connection-changed', (connected) => {
    const dot = document.querySelector('.status-dot');
    const text = document.querySelector('#connection-status span[data-i18n]');
    if (dot) {
      dot.classList.toggle('online', connected);
      dot.classList.toggle('offline', !connected);
    }
    if (text) {
      text.setAttribute('data-i18n', connected ? 'settings.online' : 'settings.offline');
      text.textContent = i18n.t(connected ? 'settings.online' : 'settings.offline');
    }
  });

  // 5. 节点选中 → Info Panel 滑入
  eventBus.on('node-selected', (nodeId) => {
    selectNode(nodeId);
    const panel = document.getElementById('info-panel');
    if (panel) panel.classList.add('open');
  });

  eventBus.on('node-deselected', () => {
    deselectAll();
    const panel = document.getElementById('info-panel');
    if (panel) panel.classList.remove('open');
  });

  // 6. Action Bar 按钮绑定
  setupActionBar();

  // 7. 语言切换时刷新所有 UI
  i18n.onChange(() => {
    updateHUD();
    refreshCurrentPanel();
    refreshLabels();
    refreshLogisticsVisuals();
    refreshArmyVisuals();
  });

  console.log('[App] 初始化完成');
}

/**
 * Action Bar 按钮绑定
 */
function setupActionBar() {
  // 建造按钮
  const btnBuild = document.getElementById('btn-build');
  if (btnBuild) btnBuild.addEventListener('click', () => {
    // 打开建筑选择面板
    const buildPanel = document.querySelector('.build-overlay');
    if (buildPanel) buildPanel.style.display = buildPanel.style.display === 'none' ? 'flex' : 'none';
  });

  // 城墙按钮
  const btnWall = document.getElementById('btn-wall');
  if (btnWall) btnWall.addEventListener('click', () => {
    // TODO: 进入城墙绘制模式（需要先选中城市）
    console.log('[Action] 城墙绘制模式 — 请先选中一个城市');
  });

  // 物流按钮
  const btnLogistics = document.getElementById('btn-logistics');
  if (btnLogistics) btnLogistics.addEventListener('click', () => {
    const logisticsPanel = document.querySelector('.logistics-overlay');
    if (logisticsPanel) logisticsPanel.style.display = logisticsPanel.style.display === 'none' ? 'flex' : 'none';
  });

  // 研究按钮
  const btnResearch = document.getElementById('btn-research');
  if (btnResearch) btnResearch.addEventListener('click', () => {
    const techPanel = document.querySelector('.tech-overlay');
    if (techPanel) techPanel.style.display = techPanel.style.display === 'none' ? 'flex' : 'none';
  });

  // 日志按钮
  const btnLogToggle = document.getElementById('btn-log-toggle');
  if (btnLogToggle) btnLogToggle.addEventListener('click', () => {
    const battleLog = document.getElementById('battle-log');
    if (battleLog) {
      battleLog.style.display = battleLog.style.display === 'none' ? 'block' : 'none';
    }
  });

  // 快捷键
  document.addEventListener('keydown', (e) => {
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
    switch (e.key.toLowerCase()) {
      case 'b': btnBuild?.click(); break;
      case 'w': btnWall?.click(); break;
      case 'l': btnLogistics?.click(); break;
      case 't': btnResearch?.click(); break;
      case 'j': btnLogToggle?.click(); break;
      case 'escape':
        // 关闭所有面板
        const panels = document.querySelectorAll('.build-overlay, .logistics-overlay, .tech-overlay, .settings-overlay');
        panels.forEach(p => p.style.display = 'none');
        break;
    }
  });
}

// 启动
init().catch(console.error);
