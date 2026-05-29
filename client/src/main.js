/**
 * 《文明模拟器》Demo — 前端入口
 */

// ─── 样式导入 ───
import './styles/index.css';
import './styles/layout.css';
import './styles/hud.css';
import './styles/map.css';
import './styles/panels.css';
import './styles/battle-log.css';
import './styles/animations.css';
import './styles/tech-panel.css';
import './styles/logistics-panel.css';
import './styles/build-panel.css';

// ─── 模块导入 ───
import i18n from './i18n/i18n.js';
import zhCN from './i18n/zh-CN.json';
import enUS from './i18n/en-US.json';
import { gameBridge } from './bridge/game-bridge.js';
import { stateStore } from './bridge/state-store.js';
import { eventBus } from './ui/event-bus.js';
import { initMap, loadMapData, refreshArmyVisuals, refreshLabels, refreshLogisticsVisuals, updateNodeVisual } from './ui/map-view.js';
import { initHUD, updateHUD } from './ui/hud.js';
import { initInfoPanel, refreshCurrentPanel } from './ui/info-panel.js';
import { initBattleLog, addEntry } from './ui/battle-log.js';
import { initSettingsPanel } from './ui/settings-panel.js';
import { initTechPanel } from './ui/tech-panel.js';
import { initLogisticsPanel } from './ui/logistics-panel.js';
import { initBuildPanel } from './ui/build-panel.js';
import { initManagementDock, refreshManagementDock } from './ui/management-dock.js';

// ─── 初始化 ───
async function init() {
  console.log('[App] 文明模拟器 Demo 启动...');

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
  initManagementDock();
  initMap('cy');

  // 3. 连接服务器（或降级到 mock）
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

  // 5. 语言切换时刷新所有 UI
  i18n.onChange(() => {
    updateHUD();
    refreshCurrentPanel();
    refreshLabels();
    refreshLogisticsVisuals();
    refreshArmyVisuals();
    refreshManagementDock();
  });

  console.log('[App] 初始化完成');
}

// 启动
init().catch(console.error);
