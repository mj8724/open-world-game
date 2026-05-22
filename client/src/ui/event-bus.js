/**
 * EventBus — 全局发布-订阅事件总线
 */
class EventBus {
  constructor() {
    /** @type {Map<string, Set<Function>>} */
    this._listeners = new Map();
  }

  /**
   * @param {string} event
   * @param {Function} callback
   */
  on(event, callback) {
    if (!this._listeners.has(event)) this._listeners.set(event, new Set());
    this._listeners.get(event).add(callback);
    return () => this.off(event, callback);
  }

  /**
   * @param {string} event
   * @param {Function} callback
   */
  off(event, callback) {
    this._listeners.get(event)?.delete(callback);
  }

  /**
   * @param {string} event
   * @param {*} data
   */
  emit(event, data) {
    this._listeners.get(event)?.forEach(fn => {
      try { fn(data); } catch (e) { console.error(`[EventBus] Error in ${event}:`, e); }
    });
  }
}

export const eventBus = new EventBus();
export default eventBus;
