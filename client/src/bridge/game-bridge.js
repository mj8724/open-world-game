/**
 * GameBridge — WebSocket 通信层
 * 连接后端，收发消息，断连自动降级到 mock 模式
 */
import { stateStore } from './state-store.js';
import { MOCK_FULL_STATE } from './mock-data.js';
import { eventBus } from '../ui/event-bus.js';

class GameBridge {
  constructor() {
    /** @type {WebSocket|null} */
    this._ws = null;
    this._url = 'ws://localhost:5000/ws';
    this._connected = false;
    this._seq = 0;
    this._reconnectDelay = 1000;
    this._maxReconnectDelay = 10000;
    this._shouldReconnect = true;
  }

  /** 尝试连接服务器，失败则使用 mock 数据 */
  connect() {
    try {
      this._ws = new WebSocket(this._url);

      this._ws.onopen = () => {
        this._connected = true;
        this._reconnectDelay = 1000;
        console.log('[Bridge] 已连接到服务器');
        eventBus.emit('connection-changed', true);
      };

      this._ws.onmessage = (event) => {
        try {
          const msg = JSON.parse(event.data);
          this._handleMessage(msg);
        } catch (e) {
          console.error('[Bridge] 消息解析失败:', e);
        }
      };

      this._ws.onclose = () => {
        this._connected = false;
        console.log('[Bridge] 连接已断开');
        eventBus.emit('connection-changed', false);
        if (this._shouldReconnect) this._scheduleReconnect();
      };

      this._ws.onerror = () => {
        // onclose will fire after this
      };
    } catch (e) {
      console.warn('[Bridge] WebSocket 不可用，使用演示模式');
      this._useMockData();
    }

    // 超时降级：2秒内未连接则使用 mock
    setTimeout(() => {
      if (!this._connected && !stateStore.initialized) {
        console.log('[Bridge] 连接超时，切换到演示模式');
        this._useMockData();
      }
    }, 2000);
  }

  _handleMessage(msg) {
    switch (msg.type) {
      case 'FULL_STATE':
        stateStore.applyFullState(msg.data);
        break;
      case 'TICK_UPDATE':
        stateStore.applyDelta(msg.data);
        break;
    }
  }

  _useMockData() {
    stateStore.applyFullState(MOCK_FULL_STATE);
    // 模拟 Tick 推进
    this._mockInterval = setInterval(() => {
      stateStore.currentTick++;
      eventBus.emit('state-tick-update', { tick: stateStore.currentTick, delta: {} });
    }, 1000);
  }

  _scheduleReconnect() {
    setTimeout(() => {
      console.log(`[Bridge] 尝试重连... (${this._reconnectDelay}ms)`);
      this.connect();
      this._reconnectDelay = Math.min(this._reconnectDelay * 2, this._maxReconnectDelay);
    }, this._reconnectDelay);
  }

  /**
   * 发送指令到服务器
   * @param {string} action - 指令类型
   * @param {object} payload - 指令数据
   */
  sendCommand(action, payload = {}) {
    this._seq++;
    const msg = { type: 'COMMAND', action, payload, seq: this._seq };

    if (this._connected && this._ws?.readyState === WebSocket.OPEN) {
      this._ws.send(JSON.stringify(msg));
    } else {
      console.warn('[Bridge] 离线模式，指令未发送:', action);
    }
  }

  /** @returns {boolean} */
  isConnected() { return this._connected; }

  disconnect() {
    this._shouldReconnect = false;
    this._ws?.close();
    if (this._mockInterval) clearInterval(this._mockInterval);
  }
}

export const gameBridge = new GameBridge();
export default gameBridge;
