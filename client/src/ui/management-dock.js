import { eventBus } from './event-bus.js';

let selectedNodeId = null;

export function initManagementDock() {
  document.getElementById('btn-open-build')?.addEventListener('click', () => {
    eventBus.emit('open-build-panel', selectedNodeId);
  });
  document.getElementById('btn-open-logistics')?.addEventListener('click', () => {
    eventBus.emit('open-logistics-panel', selectedNodeId);
  });
  document.getElementById('btn-open-research')?.addEventListener('click', () => {
    eventBus.emit('open-tech-panel');
  });

  eventBus.on('node-selected', (nodeId) => {
    selectedNodeId = nodeId;
  });
  eventBus.on('node-deselected', () => {
    selectedNodeId = null;
  });
}

export function refreshManagementDock() {}
