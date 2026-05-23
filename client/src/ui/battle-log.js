/**
 * BattleLog — 底部文字战报日志
 */
import i18n from '../i18n/i18n.js';
import { eventBus } from './event-bus.js';

const MAX_ENTRIES = 100;

/** 初始化战报日志 */
export function initBattleLog() {
  document.getElementById('btn-clear-log')?.addEventListener('click', clearLog);

  // 监听游戏事件
  eventBus.on('game-event', (evt) => {
    if (evt.type === 'LOG' && evt.textKey) {
      addEntry(evt.textKey, 'info');
    } else if (evt.type === 'COMBAT_LOG' || evt.type?.startsWith('COMBAT_')) {
      addEntry(i18n.t(evt.textKey, evt.params), 'combat');
    } else if (evt.type === 'BUILD_COMPLETE') {
      const text = i18n.t(evt.textKey, evt.params);
      addEntry(text, 'success');
    } else if (evt.type === 'TECH_COMPLETE') {
      addEntry(i18n.t('event.tech_complete', evt.params), 'success');
    } else if (evt.type === 'STARVATION') {
      addEntry(i18n.t('event.starvation', evt.params), 'warning');
    }
  });
}

/**
 * 添加日志条目
 * @param {string} text
 * @param {'info'|'warning'|'combat'|'success'} type
 */
export function addEntry(text, type = 'info') {
  const container = document.getElementById('log-entries');
  if (!container) return;

  const entry = document.createElement('div');
  entry.className = `log-entry log-${type}`;

  const time = document.createElement('span');
  time.className = 'log-time';
  time.textContent = `[${String(new Date().getHours()).padStart(2,'0')}:${String(new Date().getMinutes()).padStart(2,'0')}]`;

  const msg = document.createElement('span');
  msg.className = 'log-msg';
  msg.textContent = text;

  entry.appendChild(time);
  entry.appendChild(msg);
  container.appendChild(entry);

  // 限制条目数量
  while (container.children.length > MAX_ENTRIES) {
    container.removeChild(container.firstChild);
  }

  // 自动滚动
  container.scrollTop = container.scrollHeight;
}

/** 清空日志 */
function clearLog() {
  const container = document.getElementById('log-entries');
  if (container) container.innerHTML = '';
  addEntry(i18n.t('log.cleared'), 'info');
}
