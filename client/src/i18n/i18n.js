/**
 * i18n Engine — Internationalization system
 * Supports zh-CN and en-US with dynamic switching
 */

class I18nEngine {
  constructor() {
    /** @type {string} */
    this.locale = 'zh-CN';
    /** @type {Record<string, Record<string, string>>} */
    this.packs = {};
    /** @type {Set<Function>} */
    this.listeners = new Set();
  }

  /**
   * Load a language pack
   * @param {string} locale
   * @param {Record<string, string>} pack
   */
  loadPack(locale, pack) {
    this.packs[locale] = pack;
  }

  /**
   * Set the active locale
   * @param {string} locale
   */
  setLocale(locale) {
    if (!this.packs[locale]) {
      console.warn(`[i18n] Language pack not found: ${locale}`);
      return;
    }
    this.locale = locale;
    document.documentElement.lang = locale;
    localStorage.setItem('civ-locale', locale);
    this._updateDOM();
    this._notifyListeners();
  }

  /**
   * Get the active locale
   * @returns {string}
   */
  getLocale() {
    return this.locale;
  }

  /**
   * Translate a key with optional parameter interpolation
   * @param {string} key - dot-notation key e.g. "hud.food"
   * @param {Record<string, string|number>} [params] - interpolation params
   * @returns {string}
   */
  t(key, params) {
    const pack = this.packs[this.locale];
    if (!pack) return key;

    let value = this._resolve(pack, key);
    if (value === undefined) {
      // Fallback to zh-CN
      const fallback = this.packs['zh-CN'];
      if (fallback) {
        value = this._resolve(fallback, key);
      }
      if (value === undefined) return key;
    }

    // Interpolate {param} placeholders
    if (params) {
      for (const [k, v] of Object.entries(params)) {
        value = value.replace(new RegExp(`\\{${k}\\}`, 'g'), String(v));
      }
    }

    return value;
  }

  /**
   * Resolve a dot-notation key from a flat or nested object
   * @param {object} obj
   * @param {string} key
   * @returns {string|undefined}
   */
  _resolve(obj, key) {
    // Try flat key first
    if (obj[key] !== undefined) return obj[key];

    // Try dot-path resolution
    const parts = key.split('.');
    let current = obj;
    for (const part of parts) {
      if (current === undefined || current === null) return undefined;
      current = current[part];
    }
    return typeof current === 'string' ? current : undefined;
  }

  /**
   * Update all DOM elements with data-i18n attribute
   */
  _updateDOM() {
    const elements = document.querySelectorAll('[data-i18n]');
    elements.forEach(el => {
      const key = el.getAttribute('data-i18n');
      const text = this.t(key);
      if (text !== key) {
        el.textContent = text;
      }
    });

    // Update title
    document.title = this.t('app.title') + ' | Civilization Simulator';
  }

  /**
   * Register a listener for locale changes
   * @param {Function} fn
   */
  onChange(fn) {
    this.listeners.add(fn);
    return () => this.listeners.delete(fn);
  }

  /**
   * Notify all change listeners
   */
  _notifyListeners() {
    for (const fn of this.listeners) {
      try {
        fn(this.locale);
      } catch (e) {
        console.error('[i18n] Listener error:', e);
      }
    }
  }

  /**
   * Initialize with saved locale preference
   */
  init() {
    const saved = localStorage.getItem('civ-locale');
    if (saved && this.packs[saved]) {
      this.locale = saved;
    }
    document.documentElement.lang = this.locale;
    this._updateDOM();
  }
}

// Singleton instance
const i18n = new I18nEngine();
export default i18n;
