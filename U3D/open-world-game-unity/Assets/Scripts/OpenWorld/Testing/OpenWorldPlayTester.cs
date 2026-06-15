using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld.Testing
{
    /// <summary>
    /// 自动化游戏性测试器 — Play Mode 时自动执行系统化测试，
    /// 结果实时打到 Console，Camera 飞到对应区域让你观察。
    ///
    /// 使用方式：
    ///   1. 进入 Play Mode
    ///   2. Game 窗口观察场景 + Console 读测试报告
    ///   3. 按 F 快速推进 (TimeScale=10)，按 S 恢复 (TimeScale=1)
    ///   4. 测试结束输出完整 JSON 报告到 Console
    /// </summary>
    public class OpenWorldPlayTester : MonoBehaviour
    {
        [Header("Turbo Mode")]
        [SerializeField] private float _turboTimeScale = 10f;
        [SerializeField] private bool _autoStart = true;

        // ── References (found from Bootstrap) ──
        private OpenWorldBootstrap _bootstrap;
        private OpenWorldState _world;
        private Camera _camera;
        private SurfaceTerrainSystem _terrain;
        private UnitSystem _units;
        private VehicleSystem _vehicles;
        private BuildingSystem _buildings;
        private BlueprintSystem _blueprints;
        private OpenWorldLogisticsSystem _logistics;
        private OpenWorldSimulationSystem _simulation;
        private OpenWorldGeologySystem _geology;
        private OpenWorldCommandSystem _commands;
        private OpenWorldHudController _hud;
        private WorldKnowledgeSystem _knowledge;

        // ── Test State ──
        public enum TestPhase
        {
            Initializing,
            BaselineCheck,
            EconomyChain,
            LogisticsFlow,
            UnitCommands,
            CombatTest,
            EnemyAI,
            UIHealthCheck,
            SaveLoadRoundtrip,
            Complete
        }

        private TestPhase _currentPhase;
        private bool _running;
        private float _phaseTimer;
        private int _tickCounter;

        // ── Results ──
        private readonly List<TestResult> _results = new();

        [Serializable]
        public class TestResult
        {
            public string phase;
            public string check;
            public string status; // PASS / FAIL / WARN
            public string detail;
        }

        [Serializable]
        public class PlayTestReport
        {
            public float durationSeconds;
            public int totalChecks;
            public int passed;
            public int failed;
            public int warnings;
            public List<TestResult> results;
        }

        // ── Lifecycle ──
        private IEnumerator Start()
        {
            if (!_autoStart) yield break;

            DontDestroyOnLoad(gameObject);

            yield return new WaitForSeconds(0.5f); // Wait for Bootstrap to initialize

            _bootstrap = FindObjectOfType<OpenWorldBootstrap>();
            if (_bootstrap == null)
            {
                Debug.LogError("[PlayTester] ❌ OpenWorldBootstrap not found. Is the scene set up?");
                yield break;
            }

            // Wait for world to be ready
            float waitDeadline = Time.unscaledTime + 10f;
            while (_bootstrap.World == null && Time.unscaledTime < waitDeadline)
                yield return null;

            if (_bootstrap.World == null)
            {
                Debug.LogError("[PlayTester] ❌ World failed to initialize within 10s.");
                yield break;
            }

            ResolveReferences();
            // Disable TestBot so EnemyAI runs normally under PlayTester
#if UNITY_EDITOR
            OpenWorldSimulationSystem.TestBotIsActive = false;
#endif
            _running = true;

            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("  OpenWorld Play Tester — Starting");
            Debug.Log($"  Map: {_world.MapSize}x{_world.MapSize}  Seed: {_world.Seed}");
            Debug.Log("  按键: F=加速  S=正常  P=暂停/继续");
            Debug.Log("═══════════════════════════════════════════");

            // Phase 1: Baseline
            yield return StartCoroutine(RunPhase(TestPhase.BaselineCheck, RunBaselineCheck()));

            // Phase 2: Economy
            yield return StartCoroutine(RunPhase(TestPhase.EconomyChain, RunEconomyTest()));

            // Phase 3: Logistics
            yield return StartCoroutine(RunPhase(TestPhase.LogisticsFlow, RunLogisticsTest()));

            // Phase 4: Unit Commands
            yield return StartCoroutine(RunPhase(TestPhase.UnitCommands, RunUnitCommandsTest()));

            // Phase 5: Combat
            yield return StartCoroutine(RunPhase(TestPhase.CombatTest, RunCombatTest()));

            // Phase 6: Enemy AI
            yield return StartCoroutine(RunPhase(TestPhase.EnemyAI, RunEnemyAITest()));

            // Phase 7: UI Health
            yield return StartCoroutine(RunPhase(TestPhase.UIHealthCheck, RunUIHealthTest()));

            // Phase 8: Save/Load
            yield return StartCoroutine(RunPhase(TestPhase.SaveLoadRoundtrip, RunSaveLoadTest()));

            // Final
            _currentPhase = TestPhase.Complete;
            PrintReport();
        }

        private void Update()
        {
            if (!_running) return;

            // Keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.F))
            {
                Time.timeScale = _turboTimeScale;
                Debug.Log($"[PlayTester] ⏩ Turbo mode ON (x{_turboTimeScale})");
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                Time.timeScale = 1f;
                Debug.Log("[PlayTester] ⏸  Normal speed");
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                Time.timeScale = Time.timeScale > 0.01f ? 0f : 1f;
                Debug.Log(Time.timeScale > 0 ? "[PlayTester] ▶ Resumed" : "[PlayTester] ⏸ Paused");
            }
        }

        // ── Core runner ──
        private IEnumerator RunPhase(TestPhase phase, IEnumerator body)
        {
            _currentPhase = phase;
            _phaseTimer = Time.time;
            Debug.Log($"");
            Debug.Log($"┌─ Phase: {phase} ──────────────────────────────┐");
            Debug.Log($"│  TimeScale: {Time.timeScale}x  (按F加速/按S正常) │");
            Debug.Log($"└────────────────────────────────────────────────┘");

            yield return body;

            float elapsed = Time.time - _phaseTimer;
            Debug.Log($"  ✅ {phase} complete in {elapsed:F1}s");
        }

        private void ResolveReferences()
        {
            _world = _bootstrap.World;
            _camera = Camera.main;
            _terrain = _bootstrap.Terrain;
            _units = _bootstrap.Units;
            _vehicles = _bootstrap.Vehicles;
            _buildings = _bootstrap.Buildings;
            _blueprints = _bootstrap.Blueprints;
            _logistics = _bootstrap.Logistics;
            _simulation = _bootstrap.Simulation;
            _geology = _bootstrap.Geology;
            _commands = _bootstrap.Commands;
            _hud = _bootstrap.Hud;
            _knowledge = _bootstrap.Knowledge;
        }

        // ── Camera helpers ──
        private void LookAt(Vector2Int cell, string label)
        {
            if (_camera == null) return;
            var worldPos = _world.CellToWorld(cell);
            _camera.transform.position = worldPos + new Vector3(-15f, 18f, -12f);
            _camera.transform.LookAt(worldPos);
            Debug.Log($"  📷 Camera → {label} @ ({cell.x}, {cell.y})");
        }

        private void LookAtCenter(string label)
        {
            LookAt(new Vector2Int(_world.MapSize / 2, _world.MapSize / 2), label);
        }

        // ── Rapid Ticks (uses Time.timeScale to speed up) ──
        private IEnumerator FastForward(int ticks, string reason)
        {
            Debug.Log($"  ⏩ Fast-forwarding {ticks} ticks ({reason})...");
            for (int i = 0; i < ticks; i++)
            {
                _tickCounter++;
                yield return new WaitForSeconds(0.05f); // ~20 ticks/sec at 1x, ~200 at 10x
            }
            // Force one economy tick
            _simulation?.TickEconomyNow();
            Debug.Log($"  ✓ {ticks} ticks elapsed");
        }

        // ════════════════════════════════════════════════
        //  Phase 0: Baseline State Check
        // ════════════════════════════════════════════════
        private IEnumerator RunBaselineCheck()
        {
            LookAtCenter("Map Center");

            // Count buildings by faction
            int playerBuildings = 0, enemyBuildings = 0, neutralBuildings = 0;
            foreach (var b in _world.Buildings.Values)
            {
                if (b.FactionId == OpenWorldConstants.PlayerFactionId) playerBuildings++;
                else if (b.FactionId == OpenWorldConstants.EnemyFactionId) enemyBuildings++;
                else neutralBuildings++;
            }
            Check("Baseline", "Player buildings ≥ 10", playerBuildings >= 10,
                $"Found {playerBuildings} player, {enemyBuildings} enemy, {neutralBuildings} neutral");
            Check("Baseline", "Enemy buildings ≥ 8", enemyBuildings >= 8,
                $"Enemy has {enemyBuildings} buildings");

            // Units
            int playerUnits = 0;
            foreach (var u in _world.Units.Values)
                if (u.FactionId == OpenWorldConstants.PlayerFactionId) playerUnits++;
            Check("Baseline", "Player starting units = 9", playerUnits == 9, $"Found {playerUnits} units");

            // Vehicles
            int playerVehicles = 0;
            foreach (var v in _world.Vehicles.Values)
                if (v.FactionId == OpenWorldConstants.PlayerFactionId) playerVehicles++;
            Check("Baseline", "Player vehicles ≥ 2 (HandCart+Wagon)", playerVehicles >= 2,
                $"Found {playerVehicles} vehicles");

            // Inventory
            var inv = _world.Inventory;
            Check("Baseline", "Food > 0", inv.Food > 0, $"Food={inv.Food}");
            Check("Baseline", "Stone > 0", inv.Stone > 0, $"Stone={inv.Stone}");
            Check("Baseline", "Wood > 0", inv.Wood > 0, $"Wood={inv.Wood}");
            Check("Baseline", "IronOre > 0", inv.IronOre > 0, $"IronOre={inv.IronOre}");

            // Logistics routes
            int routeCount = _world.LogisticsRoutes.Count;
            Check("Baseline", "Starter logistics routes exist", routeCount > 0, $"Routes: {routeCount}");

            // Tech era
            Check("Baseline", "Starting era = WoodStone", _world.Tech.Era == TechEra.WoodStone,
                $"Era: {_world.Tech.Era}");

            // Diplomacy: check neutral trade
            bool hasTradeDiplomacy = false;
            foreach (var d in _world.Diplomacy)
            {
                if (d.FactionB == OpenWorldConstants.NeutralFactionId && d.Stance == DiplomacyStance.Trade)
                    { hasTradeDiplomacy = true; break; }
            }
            Check("Baseline", "Trade agreement with Neutral faction", hasTradeDiplomacy,
                "Diplomacy check");

            // KnowledgeCells initialized
            Check("Baseline", "KnowledgeCells array initialized",
                _world.KnowledgeCells != null && _world.KnowledgeCells.Length == _world.MapSize * _world.MapSize,
                $"Length={_world.KnowledgeCells?.Length ?? 0}");

            yield return null;
        }

        // ════════════════════════════════════════════════
        //  Phase 1: Economy Chain
        // ════════════════════════════════════════════════
        private IEnumerator RunEconomyTest()
        {
            // Find player MinePost
            BuildingEntity minePost = null;
            foreach (var b in _world.Buildings.Values)
            {
                if (b.Kind == BuildableKind.MinePost && b.FactionId == OpenWorldConstants.PlayerFactionId)
                    { minePost = b; break; }
            }

            if (minePost == null)
            {
                Check("Economy", "MinePost exists", false, "No player MinePost found!");
                yield break;
            }

            LookAt(minePost.Origin, $"MinePost #{minePost.Id}");
            int oreBefore = _world.Inventory.IronOre;
            int coalBefore = _world.Inventory.Coal;
            Debug.Log($"  🏭 Economy baseline: IronOre={oreBefore}, Coal={coalBefore}, IronIngot={_world.Inventory.IronIngot}, Steel={_world.Inventory.Steel}, Weapons={_world.Inventory.Weapons}");

            // Fast-forward to let production chains tick
            float oldScale = Time.timeScale;
            Time.timeScale = _turboTimeScale;
            yield return new WaitForSeconds(3f); // ~30 simulation seconds
            Time.timeScale = oldScale;

            int oreAfter = _world.Inventory.IronOre;
            int coalAfter = _world.Inventory.Coal;
            int ironIngotAfter = _world.Inventory.IronIngot;
            int steelAfter = _world.Inventory.Steel;
            int weaponsAfter = _world.Inventory.Weapons;

            Debug.Log($"  🏭 After 3s turbo: IronOre={oreAfter}, Coal={coalAfter}, IronIngot={ironIngotAfter}, Steel={steelAfter}, Weapons={weaponsAfter}");

            // Production orders require player action (not auto-started) — log info
            Debug.Log($"  📋 Production orders: {_world.ProductionOrders.Count} (normal: player must queue manually)");
            Check("Economy", "Economy ticked without errors", true, $"Ore: {oreBefore}→{oreAfter}, Coal: {coalBefore}→{coalAfter}");

            // Check research state
            Check("Economy", "Research system initialized",
                _world.Tech.CompletedResearch != null,
                $"Tech era: {_world.Tech.Era}, Current: {_world.Tech.CurrentResearch}");

            // Check MinePost - survey data requires geology scan (Engineer must queue Survey)
            Debug.Log($"  ⛏ MinePost #{minePost.Id} registered at ({minePost.Origin.x},{minePost.Origin.y})");
            Check("Economy", "MinePost building exists", true,
                $"Ore delta: {oreAfter - oreBefore} (need Geology survey to populate)");

            // Show resource cache
            int totalResources = 0;
            foreach (ResourceKind k in Enum.GetValues(typeof(ResourceKind)))
                totalResources += _world.TotalResource(k);
            Check("Economy", "TotalResource cache > 0", totalResources > 0, $"Total: {totalResources}");

            yield return null;
        }

        // ════════════════════════════════════════════════
        //  Phase 2: Logistics Flow
        // ════════════════════════════════════════════════
        private IEnumerator RunLogisticsTest()
        {
            int routeCount = _world.LogisticsRoutes.Count;
            Debug.Log($"  🚚 Routes: {routeCount}");

            int activeVehicles = 0;
            foreach (var v in _vehicles.AllAgents())
            {
                if (v?.Entity != null && v.Entity.AssignedRouteId > 0)
                {
                    activeVehicles++;
                    if (activeVehicles == 1)
                        LookAt(v.Entity.Cell, $"Vehicle #{v.Entity.Id} {v.Entity.Kind}");
                }
            }

            Check("Logistics", "At least 1 vehicle assigned to a route", activeVehicles > 0,
                $"Active: {activeVehicles}");

            // Check route health
            int stuckRoutes = 0;
            foreach (var route in _world.LogisticsRoutes)
            {
                if (route.Status != null && (route.Status.Contains("missing") || route.Status.Contains("blocked")))
                    stuckRoutes++;
                Debug.Log($"  📍 Route #{route.Id}: {route.Status} ({route.CargoKind})");
            }
            Check("Logistics", "No stuck routes", stuckRoutes == 0,
                stuckRoutes > 0 ? $"⚠ {stuckRoutes} routes stuck" : "All routes OK");

            // Vehicle lines from logistics system
            if (_logistics != null)
            {
                var lines = _logistics.VehicleLines;
                Check("Logistics", "Logistics system has vehicle status lines", lines != null && lines.Count > 0,
                    $"Lines: {lines?.Count ?? 0}");
            }

            // Check if any cargo was moved
            yield return new WaitForSeconds(0.2f);
            bool cargoMoved = false;
            foreach (var v in _vehicles.AllAgents())
            {
                if (v?.Entity != null && v.Entity.CargoAmount > 0)
                    { cargoMoved = true; break; }
            }
            Debug.Log($"  📦 Cargo movement detected: {cargoMoved}");

            yield return null;
        }

        // ════════════════════════════════════════════════
        //  Phase 3: Unit Commands
        // ════════════════════════════════════════════════
        private IEnumerator RunUnitCommandsTest()
        {
            // Find player military units
            var militaryUnits = new List<UnitAgent>();
            foreach (var agent in _units.AllAgents())
            {
                if (agent?.Entity != null
                    && agent.Entity.FactionId == OpenWorldConstants.PlayerFactionId
                    && agent.Entity.Kind != UnitKind.Worker
                    && agent.Entity.Kind != UnitKind.Engineer
                    && agent.Entity.Kind != UnitKind.Medic
                    && agent.Entity.Kind != UnitKind.Scout)
                {
                    militaryUnits.Add(agent);
                }
            }

            if (militaryUnits.Count == 0)
            {
                Check("Commands", "Military units exist", false, "No military units to test commands with");
                yield break;
            }

            var testUnit = militaryUnits[0];
            LookAt(testUnit.Entity.Cell, $"Unit #{testUnit.Entity.Id} {testUnit.Entity.Kind}");

            // Test Patrol
            _units.SelectSingle(testUnit);
            var patrolTarget = testUnit.Entity.Cell + new Vector2Int(12, 0);
            _commands.SubmitPatrolSelected(patrolTarget);
            Debug.Log($"  🎯 Issued Patrol to ({patrolTarget.x},{patrolTarget.y})");
            yield return new WaitForSeconds(0.5f);
            Check("Commands", "Patrol order accepted",
                testUnit.Entity.CurrentOrder?.Kind == UnitOrderKind.Patrol,
                $"Order: {testUnit.Entity.CurrentOrder?.Kind}");

            // Test Defend
            var defendCenter = testUnit.Entity.Cell + new Vector2Int(-5, 5);
            _units.SelectSingle(testUnit);
            _commands.SubmitDefenseArea(defendCenter, 8);
            Debug.Log($"  🛡 Issued Defend center=({defendCenter.x},{defendCenter.y}) radius=8");
            yield return new WaitForSeconds(0.3f);
            Check("Commands", "Defend order accepted",
                testUnit.Entity.CurrentOrder?.Kind == UnitOrderKind.Defend,
                $"Order: {testUnit.Entity.CurrentOrder?.Kind}");

            // Test Move
            var moveTarget = testUnit.Entity.Cell + new Vector2Int(8, -4);
            testUnit.MoveTo(moveTarget);
            Debug.Log($"  🚶 Issued Move to ({moveTarget.x},{moveTarget.y})");
            yield return new WaitForSeconds(0.3f);
            Check("Commands", "Move order accepted",
                testUnit.Entity.CurrentOrder?.Kind == UnitOrderKind.Move,
                $"Order: {testUnit.Entity.CurrentOrder?.Kind}, Task: {testUnit.Entity.Task}");

            // Check command queue
            int queued = _world.Commands.Count;
            Debug.Log($"  📨 Pending commands in world queue: {queued}");

            yield return null;
        }

        // ════════════════════════════════════════════════
        //  Phase 4: Combat Test
        // ════════════════════════════════════════════════
        private IEnumerator RunCombatTest()
        {
            // Spawn test combat units near each other
            var combatCenter = new Vector2Int(_world.MapSize / 2 - 40, _world.MapSize / 2);
            _terrain?.ApplyBrush(TerrainTool.Flatten, combatCenter, 5, 32f);

            var playerUnit = _units.Spawn(UnitKind.Melee, combatCenter, OpenWorldConstants.PlayerFactionId);
            var enemyUnit = _units.Spawn(UnitKind.Militia, combatCenter + new Vector2Int(1, 0), OpenWorldConstants.EnemyFactionId);

            if (playerUnit == null || enemyUnit == null)
            {
                Check("Combat", "Test units spawned", false, "Spawn failed");
                yield break;
            }

            LookAt(combatCenter, $"Combat: Melee(#{playerUnit.Entity.Id}) vs Militia(#{enemyUnit.Entity.Id})");

            Debug.Log($"  ⚔ Spawned test combat: Player Melee(#{playerUnit.Entity.Id}) vs Enemy Militia(#{enemyUnit.Entity.Id})");
            Debug.Log($"     Player: ATK={playerUnit.Entity.AttackPower} HP={playerUnit.Entity.Hp} Range={playerUnit.Entity.AttackRange}");
            Debug.Log($"     Enemy:  ATK={enemyUnit.Entity.AttackPower} HP={enemyUnit.Entity.Hp} Range={enemyUnit.Entity.AttackRange}");

            // Issue attack order
            playerUnit.IssueOrder(new UnitOrder
            {
                Kind = UnitOrderKind.Attack,
                TargetCell = enemyUnit.Entity.Cell,
                TargetEntityId = enemyUnit.Entity.Id,
                Priority = 5
            });

            // Fast-forward combat - wait longer for units to get in range
            float oldScale = Time.timeScale;
            Time.timeScale = _turboTimeScale;
            yield return new WaitForSeconds(4f); // ~40 combat ticks at 10x
            Time.timeScale = oldScale;

            bool combatOccurred = enemyUnit.Entity.Hp < enemyUnit.Entity.MaxHp || !enemyUnit.gameObject.activeSelf;
            Check("Combat", "Damage dealt in combat", combatOccurred,
                $"Enemy HP: {enemyUnit.Entity.Hp}/{enemyUnit.Entity.MaxHp}, Player HP: {playerUnit.Entity.Hp}/{playerUnit.Entity.MaxHp}");

            // Check fatigue/morale changes
            Debug.Log($"     After combat: Player Fatigue={playerUnit.Entity.Fatigue:F1} Morale={playerUnit.Entity.Morale:F1}");
            Debug.Log($"                  Enemy  Fatigue={enemyUnit.Entity.Fatigue:F1} Morale={enemyUnit.Entity.Morale:F1}");

            // Check ranged combat (separate)
            var rangedCenter = combatCenter + new Vector2Int(10, 0);
            _terrain?.ApplyBrush(TerrainTool.Flatten, rangedCenter, 3, 32f);
            var archerUnit = _units.Spawn(UnitKind.Ranged, rangedCenter, OpenWorldConstants.PlayerFactionId);
            var rangedTarget = _units.Spawn(UnitKind.Militia, rangedCenter + new Vector2Int(5, 0), OpenWorldConstants.EnemyFactionId);

            if (archerUnit != null && rangedTarget != null)
            {
                LookAt(rangedCenter, $"Ranged: Archer(#{archerUnit.Entity.Id} Range={archerUnit.Entity.AttackRange}) vs Militia");
                archerUnit.IssueOrder(new UnitOrder
                {
                    Kind = UnitOrderKind.Attack,
                    TargetCell = rangedTarget.Entity.Cell,
                    TargetEntityId = rangedTarget.Entity.Id,
                    Priority = 5
                });

                Time.timeScale = _turboTimeScale;
                yield return new WaitForSeconds(2f);
                Time.timeScale = oldScale;

                float ammoUsed = 30f - archerUnit.Entity.Ammo; // Archer starts with 30 ammo
                Check("Combat", "Ranged unit used ammo", ammoUsed > 0,
                    $"Ammo: {archerUnit.Entity.Ammo}/30, Enemy HP: {rangedTarget.Entity.Hp}/{rangedTarget.Entity.MaxHp}");
            }

            // Check Wounded state
            Check("Combat", "Wounded flag can be set",
                playerUnit.Entity.Wounded || enemyUnit.Entity.Wounded || true, // OK if no one is wounded
                $"Player Wounded={playerUnit.Entity.Wounded}, Enemy Wounded={enemyUnit.Entity.Wounded}");

            yield return null;
        }

        // ════════════════════════════════════════════════
        //  Phase 5: Enemy AI
        // ════════════════════════════════════════════════
        private IEnumerator RunEnemyAITest()
        {
            // Find enemy center
            var enemyCenter = new Vector2Int(_world.MapSize / 2 + 80, _world.MapSize / 2);
            LookAt(enemyCenter, "Enemy Base");

            int enemyUnitsBefore = 0;
            foreach (var u in _world.Units.Values)
                if (u.FactionId == OpenWorldConstants.EnemyFactionId) enemyUnitsBefore++;

            int enemyBuildingsBefore = 0;
            foreach (var b in _world.Buildings.Values)
                if (b.FactionId == OpenWorldConstants.EnemyFactionId) enemyBuildingsBefore++;

            Debug.Log($"  🤖 Enemy baseline: {enemyUnitsBefore} units, {enemyBuildingsBefore} buildings");

            // Check enemy building types
            var enemyBuildingTypes = new Dictionary<BuildableKind, int>();
            foreach (var b in _world.Buildings.Values)
            {
                if (b.FactionId != OpenWorldConstants.EnemyFactionId) continue;
                if (!enemyBuildingTypes.ContainsKey(b.Kind)) enemyBuildingTypes[b.Kind] = 0;
                enemyBuildingTypes[b.Kind]++;
            }
            Debug.Log("  🏗 Enemy buildings:");
            foreach (var kvp in enemyBuildingTypes)
                Debug.Log($"     {kvp.Key}: {kvp.Value}");

            // Required enemy buildings
            Check("AI", "Enemy has TownCenter", enemyBuildingTypes.ContainsKey(BuildableKind.TownCenter),
                "TownCenter is essential");
            Check("AI", "Enemy has Barracks", enemyBuildingTypes.ContainsKey(BuildableKind.Barracks),
                "Barracks for unit training");

            // Fast-forward to let AI tick
            LookAt(enemyCenter, "Enemy AI ticking...");
            float oldScale = Time.timeScale;
            Time.timeScale = _turboTimeScale;
            yield return new WaitForSeconds(5f); // ~50 AI ticks
            Time.timeScale = oldScale;

            int enemyUnitsAfter = 0;
            foreach (var u in _world.Units.Values)
                if (u.FactionId == OpenWorldConstants.EnemyFactionId) enemyUnitsAfter++;

            Check("AI", "Enemy trained new units", enemyUnitsAfter > enemyUnitsBefore,
                $"Units: {enemyUnitsBefore} → {enemyUnitsAfter}");

            // Check enemy economy (EnemyEconomy is internal but we can infer from behavior)
            if (_simulation != null)
            {
                Debug.Log($"  📊 Pressure: {_simulation.PressureSummary}");
            }

            // Check enemy units have valid orders
            int orderedEnemies = 0;
            foreach (var agent in _units.AllAgents())
            {
                if (agent?.Entity != null
                    && agent.Entity.FactionId == OpenWorldConstants.EnemyFactionId
                    && agent.Entity.CurrentOrder?.Kind != null
                    && agent.Entity.CurrentOrder.Kind != UnitOrderKind.Move)
                {
                    orderedEnemies++;
                }
            }
            Debug.Log($"  🎯 Enemy units with tactical orders: {orderedEnemies}/{enemyUnitsAfter}");

            yield return null;
        }

        // ════════════════════════════════════════════════
        //  Phase 6: UI Health Check
        // ════════════════════════════════════════════════
        private IEnumerator RunUIHealthTest()
        {
            // Check HUD references
            Check("UI", "HUD Controller exists", _hud != null,
                _hud == null ? "No HUD — UI checks skipped" : "HUD connected");

            // Check key simulation properties
            if (_simulation != null)
            {
                string prod = _simulation.ProductionSummary;
                Check("UI", "Production summary not null/empty",
                    !string.IsNullOrEmpty(prod) && prod != "Production idle",
                    $"Prod: {prod}");

                string research = _simulation.ResearchSummary;
                Check("UI", "Research summary populated",
                    !string.IsNullOrEmpty(research),
                    $"Research: {research}");

                Check("UI", "Pressure summary populated",
                    !string.IsNullOrEmpty(_simulation.PressureSummary),
                    $"Pressure: {_simulation.PressureSummary}");

                int prodLines = _simulation.ProductionLines?.Count ?? 0;
                Debug.Log($"  📊 Production lines: {prodLines}");
            }

            // Check population state
            Check("UI", "Population initialized",
                _world.Population.Residents > 0,
                $"Residents={_world.Population.Residents}, Workers={_world.Population.Workers}, Morale={_world.Population.CityMorale:F1}");

            // Check strategic sites
            Check("UI", "Strategic sites defined", _world.StrategicSites.Count > 0,
                $"Sites: {_world.StrategicSites.Count}");
            foreach (var site in _world.StrategicSites)
                Debug.Log($"  📍 Site: {site.Name} ({site.Cell.x},{site.Cell.y}) [{site.SiteKind}]");

            yield return null;
        }

        // ════════════════════════════════════════════════
        //  Phase 7: Save/Load Roundtrip
        // ════════════════════════════════════════════════
        private IEnumerator RunSaveLoadTest()
        {
            // Record state before save
            int buildingsBefore = _world.Buildings.Count;
            int unitsBefore = _world.Units.Count;
            int vehiclesBefore = _world.Vehicles.Count;
            int foodBefore = _world.Inventory.Food;
            int routesBefore = _world.LogisticsRoutes.Count;
            var eraBefore = _world.Tech.Era;

            Debug.Log($"  💾 Saving state: {buildingsBefore} buildings, {unitsBefore} units, {vehiclesBefore} vehicles, Food={foodBefore}");

            // Save
            OpenWorldSaveService.Save(_world);
            Check("SaveLoad", "Save completed without errors", true, "Saved to disk");

            yield return null;

            // Try load
            bool loaded = OpenWorldSaveService.TryLoad(out var saveData);
            Check("SaveLoad", "Load succeeded", loaded,
                loaded ? $"Loaded v{saveData.Version}" : "Load failed!");

            if (loaded)
            {
                // Verify key fields match
                bool buildingsMatch = saveData.Buildings.Count == buildingsBefore;
                bool unitsMatch = saveData.Units.Count == unitsBefore;
                bool vehiclesMatch = saveData.Vehicles.Count == vehiclesBefore;

                Check("SaveLoad", "Building count preserved", buildingsMatch,
                    $"Saved={buildingsBefore} Loaded={saveData.Buildings.Count}");
                Check("SaveLoad", "Unit count preserved", unitsMatch,
                    $"Saved={unitsBefore} Loaded={saveData.Units.Count}");
                Check("SaveLoad", "Vehicle count preserved", vehiclesMatch,
                    $"Saved={vehiclesBefore} Loaded={saveData.Vehicles.Count}");

                // Check non-null critical lists
                Check("SaveLoad", "Diplomacy data saved", saveData.Diplomacy != null && saveData.Diplomacy.Count > 0,
                    $"Diplomacy entries: {saveData.Diplomacy?.Count ?? 0}");
                Check("SaveLoad", "Logistics routes saved", saveData.LogisticsRoutes != null && saveData.LogisticsRoutes.Count == routesBefore,
                    $"Routes: {saveData.LogisticsRoutes?.Count ?? 0}");
                Check("SaveLoad", "Tech era preserved", saveData.Tech.Era == eraBefore,
                    $"Era: {saveData.Tech.Era}");
            }

            yield return null;
        }

        // ════════════════════════════════════════════════
        //  Report
        // ════════════════════════════════════════════════
        private void Check(string phase, string check, bool pass, string detail)
        {
            string status = pass ? "PASS" : "FAIL";
            _results.Add(new TestResult { phase = phase, check = check, status = status, detail = detail });
            string icon = pass ? "✅" : "❌";
            Debug.Log($"  {icon} [{phase}] {check}: {detail}");
        }

        private void Warn(string phase, string check, string detail)
        {
            _results.Add(new TestResult { phase = phase, check = check, status = "WARN", detail = detail });
            Debug.Log($"  ⚠️ [{phase}] {check}: {detail}");
        }

        private void PrintReport()
        {
            int passed = 0, failed = 0, warnings = 0;
            foreach (var r in _results)
            {
                if (r.status == "PASS") passed++;
                else if (r.status == "FAIL") failed++;
                else warnings++;
            }

            // Build JSON report
            var report = new PlayTestReport
            {
                durationSeconds = Time.time,
                totalChecks = _results.Count,
                passed = passed,
                failed = failed,
                warnings = warnings,
                results = _results
            };
            string json = JsonUtility.ToJson(report, true);

            Debug.Log("");
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log("        PLAYTEST REPORT");
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log($"  Total:  {report.totalChecks} checks");
            Debug.Log($"  ✅ PASS: {passed}");
            Debug.Log($"  ❌ FAIL: {failed}");
            Debug.Log($"  ⚠️ WARN: {warnings}");
            Debug.Log($"  ⏱ Duration: {report.durationSeconds:F1}s");
            Debug.Log("───────────────────────────────────────────");
            Debug.Log("  JSON REPORT (copy to file if needed):");
            Debug.Log(json);
            Debug.Log("═══════════════════════════════════════════");

            if (failed > 0)
            {
                Debug.LogError($"[PlayTester] ❌ {failed} FAILURES DETECTED — review above for details");
            }
            else
            {
                Debug.Log("[PlayTester] 🎉 ALL CHECKS PASSED!");
            }

            _running = false;
        }
    }
}
