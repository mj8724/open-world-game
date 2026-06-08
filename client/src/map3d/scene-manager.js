/**
 * Three.js 场景管理器 — Scene / Renderer / 灯光 / 动画循环
 */
import * as THREE from 'three';
import { CAMERA_DEFAULTS, ENV_CONFIG } from './constants.js';

let scene, camera, renderer;
let animating = false;
let frameId = null;
const beforeRenderCallbacks = [];

/**
 * 初始化 Three.js 场景
 * @param {HTMLElement} container - 渲染目标 DOM 元素
 * @returns {{ scene: THREE.Scene, camera: THREE.PerspectiveCamera, renderer: THREE.WebGLRenderer }}
 */
export function initScene(container) {
  const cam = CAMERA_DEFAULTS;
  const env = ENV_CONFIG;

  // 场景
  scene = new THREE.Scene();
  scene.background = new THREE.Color(env.skyColor);
  scene.fog = new THREE.Fog(env.skyColor, env.fogNear, env.fogFar);

  // 相机
  camera = new THREE.PerspectiveCamera(
    cam.fov,
    container.clientWidth / container.clientHeight,
    cam.near,
    cam.far
  );
  camera.position.set(15, 20, 25);
  camera.lookAt(15, 0, 12);

  // 渲染器
  renderer = new THREE.WebGLRenderer({ antialias: true });
  renderer.setSize(container.clientWidth, container.clientHeight);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.0;
  container.appendChild(renderer.domElement);

  // 灯光
  setupLights(scene);

  // 窗口 resize
  const onResize = () => {
    const w = container.clientWidth;
    const h = container.clientHeight;
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
    renderer.setSize(w, h);
  };
  window.addEventListener('resize', onResize);

  return { scene, camera, renderer };
}

function setupLights(scene) {
  const env = ENV_CONFIG;

  // 环境光
  const ambient = new THREE.AmbientLight(0xffffff, env.ambientLightIntensity);
  scene.add(ambient);

  // 方向光（太阳）+ 阴影
  const sun = new THREE.DirectionalLight(0xfff4e6, env.directionalLightIntensity);
  sun.position.set(20, 30, 15);
  sun.castShadow = true;
  sun.shadow.mapSize.width = env.shadowMapSize;
  sun.shadow.mapSize.height = env.shadowMapSize;
  sun.shadow.camera.near = 0.5;
  sun.shadow.camera.far = 100;
  sun.shadow.camera.left = -env.shadowCameraSize;
  sun.shadow.camera.right = env.shadowCameraSize;
  sun.shadow.camera.top = env.shadowCameraSize;
  sun.shadow.camera.bottom = -env.shadowCameraSize;
  sun.shadow.bias = -0.001;
  scene.add(sun);

  // 半球光
  const hemi = new THREE.HemisphereLight(
    env.hemisphereSkyColor,
    env.hemisphereGroundColor,
    env.hemisphereIntensity
  );
  scene.add(hemi);
}

/** 启动动画循环 */
export function startLoop() {
  if (animating) return;
  animating = true;

  function animate() {
    if (!animating) return;
    frameId = requestAnimationFrame(animate);

    // 执行所有 beforeRender 回调
    for (const cb of beforeRenderCallbacks) {
      try { cb(); } catch (e) { console.error('[Scene] beforeRender error:', e); }
    }

    renderer.render(scene, camera);
  }
  animate();
}

/** 停止动画循环 */
export function stopLoop() {
  animating = false;
  if (frameId) {
    cancelAnimationFrame(frameId);
    frameId = null;
  }
}

/** 注册每帧回调 */
export function onBeforeRender(callback) {
  beforeRenderCallbacks.push(callback);
}

/** 移除每帧回调 */
export function offBeforeRender(callback) {
  const idx = beforeRenderCallbacks.indexOf(callback);
  if (idx >= 0) beforeRenderCallbacks.splice(idx, 1);
}

/** 获取场景引用 */
export function getScene() { return scene; }
/** 获取相机引用 */
export function getCamera() { return camera; }
/** 获取渲染器引用 */
export function getRenderer() { return renderer; }

/** 销毁场景 */
export function dispose() {
  stopLoop();
  if (renderer) {
    renderer.dispose();
    renderer.domElement?.remove();
  }
  if (scene) {
    scene.traverse((obj) => {
      if (obj.geometry) obj.geometry.dispose();
      if (obj.material) {
        if (Array.isArray(obj.material)) obj.material.forEach(m => m.dispose());
        else obj.material.dispose();
      }
    });
  }
  beforeRenderCallbacks.length = 0;
}
