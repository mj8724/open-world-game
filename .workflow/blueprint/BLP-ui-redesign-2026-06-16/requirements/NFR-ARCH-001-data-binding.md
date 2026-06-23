---
id: NFR-ARCH-001
title: 响应式数据绑定底座 (MVVM)
priority: must
session_id: BLP-ui-redesign-2026-06-16
status: complete
---

# NFR-ARCH-001: 响应式数据绑定底座 (MVVM)

## 1. Description
彻底废弃现有的通过 MonoBehavior `Update()` 每帧轮询底层 `StateStore` 数据的做法，改为全面拥抱 MVVM（Model-View-ViewModel）架构。

## 2. Rationale
UI 每帧轮询会造成极大的 CPU 资源浪费，且导致视图与游戏底层逻辑代码紧密耦合，极难维护和独立测试。事件驱动加绑定的方式能保证只有在数据真实变化时，UI 才重绘。

## 3. Constraints & Rules
- 所有核心 UI 组件 MUST 取消对 MonoBehavior `Update()` 的依赖。
- ViewModel 层 MUST 作为视图与 `WorldEvents` 之间的防腐层，不包含任何直接的 Unity 渲染代码。
- 内存管理：MUST 妥善处理所有可观察对象 (Observable) 和事件订阅的生命周期（如在 `OnDestroy` 中 `Dispose`），坚决防止内存泄漏 (Memory Leak)。

## 4. Acceptance Criteria
1. **性能基准**: 场景中包含 1000 个活跃实体时，UI 系统造成的 CPU 耗时每帧不超过 1.5ms。
2. **无内存泄漏**: 打开与关闭建设抽屉面板 100 次，通过 Profiler 确认没有产生遗留的内存堆积或游离的事件委托。
3. **架构验证**: 修改一个基础资源（如增加 100 金币），仅触发相关的 `OnResourceChanged` 事件并渲染，无其他冗余调用。
