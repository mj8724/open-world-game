/**
 * SettingsPanel — 设置面板（语言切换）
 */
import i18n from '../i18n/i18n.js';
import { refreshLabels } from '../map3d/index.js';
import { eventBus } from './event-bus.js';

/** 初始化设置面板 */
export function initSettingsPanel() {
  const toggle = document.getElementById('language-toggle');
  if (!toggle) return;

  toggle.addEventListener('click', (e) => {
    const btn = e.target.closest('.lang-btn');
    if (!btn) return;

    const lang = btn.dataset.lang;
    i18n.setLocale(lang);

    // 更新按钮状态
    toggle.querySelectorAll('.lang-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');

    // 刷新地图标签
    refreshLabels();

    // 通知其他模块
    eventBus.emit('language-changed', lang);
  });
}
