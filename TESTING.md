# MCP 测试指南 (TESTING.md)

本项目通过 Unity MCP (Model Context Protocol) 工具进行自动化测试和性能诊断。

## 前置条件

1. Unity Editor 已安装 `com.coplaydev.unity-mcp` 包
2. Window → MCP for Unity 窗口已打开并显示 "Connected"
3. Unity Bridge 状态为 "Running"

## 测试场景

### 1. 内存泄漏检测

**目的**：验证 Material/Mesh 泄漏已修复，长时间运行内存不持续增长

```csharp
// 步骤 1：启动游戏并重置 profiler
// MCP: manage_profiler → profiler_start (log_file 可选)
profiler_start: enable recording

// 步骤 2：运行 5 分钟
// MCP: manage_editor → play
manage_editor play

// 步骤 3：等待后暂停，对比内存快照
// MCP: manage_profiler → memory_take_snapshot (snapshot_1)
manage_profiler memory_take_snapshot snapshot_1

// 再运行 5 分钟...

// 步骤 4：第二次快照
manage_profiler memory_take_snapshot snapshot_2

// 步骤 5：对比
manage_profiler memory_compare_snapshots snapshot_1 snapshot_2

// 预期：材质数量(Material count)不增长，Mesh 数量稳定
```

### 2. 实体膨胀检测

**目的**：确认 `TrimExcess()` 正常工作，数据不会无限增长

```csharp
// 步骤 1：进入 Play 模式
manage_editor play

// 步骤 2：运行 1 分钟后执行 C# 代码检查
execute_code action=execute code=@"
var bootstrap = FindObjectOfType<OpenWorld.OpenWorldBootstrap>();
var world = bootstrap.World;
return $""Buildings:{world.Buildings.Count} Units:{world.Units.Count} 
  Vehicles:{world.Vehicles.Count} Intel:{world.IntelSnapshots.Count}
  Surveys:{world.Surveys.Count} ModifiedCells:{world.ModifiedCells.Count}
  Commands:{world.Commands.Count}""
"

// 步骤 3：等待 5 分钟后再执行
// 预期：Buildings/Units 数量稳定，Intel/Surveys 有上限
```

### 3. 帧率/性能监控

**目的**：确认 O(n²) 优化后帧率稳定

```csharp
// 步骤 1：启动 profiler
manage_profiler profiler_start

// 步骤 2：运行中读取渲染计数
manage_profiler get_counters category="Render"

// 步骤 3：读取帧时间
manage_profiler get_frame_timing

// 步骤 4：读取脚本耗时
manage_profiler get_counters category="Scripts"

// 预期：draw calls < 500, batches < 300, frame time < 33ms (30fps)
```

### 4. 控制台错误检查

**目的**：确认没有运行时异常

```csharp
// 读取最近 50 条控制台消息
read_console count=50 types=["error","warning"] include_stacktrace=true

// 预期：无 NullReferenceException, OutOfMemoryException, MissingReferenceException
```

### 5. 材质缓存验证

**目的**：确认 MaterialCache 正常工作

```csharp
// 执行 C# 代码
execute_code action=execute code=@"
var stats = Rendering.MaterialCache.Stats;
return stats;
"

// 预期：材质数量在 10-50 之间，不随运行时间增加
```

### 6. 卡死复现测试

**目的**：确认修复后不再卡死

```csharp
// 步骤 1：进入 Play 模式并等待 10 分钟
manage_editor play

// 步骤 2：10 分钟后检查
manage_editor telemetry_ping   // 返回 true 表示编辑器仍在响应

// 步骤 3：停止
manage_editor stop

// 预期：10 分钟后编辑器仍正常响应，可以停止
```

### 7. ScriptableObject 配置验证

**目的**：确认 GameDataConfig 正确加载

```csharp
execute_code code=@"
var config = FindObjectOfType<OpenWorld.GameDataConfig>();
if (config == null) return ""ERROR: GameDataConfig not found"";
return $""Buildings:{config.BuildingDefs.Count} Units:{config.UnitDefs.Count} 
  Recipes:{config.RecipeDefs.Count} Techs:{config.TechDefs.Count} Vehicles:{config.VehicleDefs.Count}""
"
```

### 8. 边界测试场景

| 场景 | 操作 | 预期 |
|------|------|------|
| 空世界 | 不放置任何实体 | 帧率 60fps，内存 < 200MB |
| 大量建筑 | 放置 500+ 建筑 | 帧率 > 20fps，不卡死 |
| 大量单位 | 生成 200+ 单位战斗 | 帧率 > 15fps，O(n²) 已优化 |
| 长时运行 | 连续运行 30 分钟 | 内存稳定，不持续增长 |
| 快速切换 | 反复 Play/Stop | 无残留 GameObject/材质 |
| 边界坐标 | 点击地图边缘 | 不崩溃，InBounds 正常 |

### 9. 回归测试检查清单

- [ ] `MaterialCache.Stats` 材质数不随运行时间增长
- [ ] `manage_profiler memory_compare_snapshots` 无泄漏
- [ ] `read_console` 无 OutOfMemory 错误
- [ ] `manage_editor telemetry_ping` 10 分钟后仍响应
- [ ] `GameDataConfig` 正确加载所有配置
- [ ] `OpenWorldState.TrimExcess()` 定期执行（检查日志）
- [ ] 战斗 O(n²) 已用空间哈希替代（检查代码）
- [ ] `SurfaceChunkView.Rebuild()` 旧 mesh 已销毁

## 目录

### 文档文件

| 文件 | 说明 |
|------|------|
| `DEVLOG.md` | 开发日志（修改记录） |
| `DEVELOPMENT.md` | 开发必读（架构、规则、字典索引） |
| `TESTING.md` | 本文件（MCP 测试指南） |

### 新增代码文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Rendering/MaterialCache.cs` | 材质缓存池 |
| `Assets/Scripts/Rendering/WorldRendererBridge.cs` | 事件到渲染桥接器 |
| `Assets/Scripts/OpenWorld/World/WorldEvents.cs` | 数据变更事件系统 |
| `Assets/Scripts/OpenWorld/Data/GameDataConfig.cs` | ScriptableObject 配置 |

### 修改代码文件

| 文件 | 修改内容 |
|------|----------|
| `ArmyRenderer.cs` | 6 处 `new Material` → `MaterialCache` |
| `NodeRenderer.cs` | 10 处 `new Material` → `MaterialCache` |
| `EdgeRenderer.cs` | 3 处 `new Material` → `MaterialCache` + mesh MarkDynamic |
| `WildRenderer.cs` | 11 处 `new Material` → `MaterialCache` |
| `LogisticsRenderer.cs` | 4 处 `new Material` → `MaterialCache` |
| `TerrainGenerator.cs` | 2 处 `new Material` → `MaterialCache.GetNamed()` |
| `SurfaceChunkView.cs` | mesh 重建前销毁旧 mesh |
| `BuildingSystem.cs` | MaterialFor → MaterialCache + GameDataConfig |
| `BlueprintSystem.cs` | MaterialFor → MaterialCache |
| `UnitAgent.cs` | MaterialFor → MaterialCache |
| `VehicleAgent.cs` | MaterialFor → MaterialCache |
| `SurfaceTerrainSystem.cs` | terrain material → MaterialCache.GetNamed() |
| `OpenWorldState.cs` | 添加 TrimExcess() + WorldEvents 注入 + ApplyUnitDefaults 重构 |
| `OpenWorldSimulationSystem.cs` | 空间哈希战斗索引 + 定期 TrimExcess |
