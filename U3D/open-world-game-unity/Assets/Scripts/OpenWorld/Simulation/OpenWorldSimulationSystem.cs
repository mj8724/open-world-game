using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace OpenWorld
{
    public class OpenWorldSimulationSystem : MonoBehaviour, ISimulation
    {
        public string PressureSummary { get; private set; } = "Stable";
        public string ProductionSummary => _production?.Summary ?? "Production idle";
        public IReadOnlyList<string> ProductionLines => _production?.ProductionLines ?? System.Array.Empty<string>();
        public string ResearchSummary => _production?.ResearchSummary ?? "Research idle";
        public string DiplomacySummary { get; private set; } = "Neutral trade open";
        public float UnityProgress { get; private set; }
        public bool GameOver { get; private set; }
        public string GameOverText { get; private set; } = "";
        public bool IsVictory { get; private set; }
#if UNITY_EDITOR
        public static bool TestBotIsActive;
#endif

        private OpenWorldState _world;
        private SurfaceTerrainSystem _terrain;
        private BuildingSystem _buildings;
        private UnitSystem _units;
        private BlueprintSystem _blueprints;
        private VehicleSystem _vehicles;
        private OpenWorldCombatSystem _combat;
        private OpenWorldProductionSystem _production;
        private float _economyTimer;
        private float _cleanupTimer;
        private float _aiTimer;
        private float _combatTimer;
        private OpenWorldEnemyAISystem _enemyAi;

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain, BuildingSystem buildings, UnitSystem units, BlueprintSystem blueprints, VehicleSystem vehicles)
        {
            _world = world;
            _terrain = terrain;
            _buildings = buildings;
            _units = units;
            _blueprints = blueprints;
            _vehicles = vehicles;
            _combat = new OpenWorldCombatSystem(_world);
            _production = new OpenWorldProductionSystem(_world, _units, _vehicles);
            _enemyAi = new OpenWorldEnemyAISystem(_world, _units, _terrain, _blueprints);
        }

        private void Update()
        {
            if (_world == null) return;
            _economyTimer += Time.deltaTime;
            _aiTimer += Time.deltaTime;
            _combatTimer += Time.deltaTime;
            _cleanupTimer += Time.deltaTime;

            if (_economyTimer >= 1.0f)
            {
                _economyTimer = 0f;
                _production.TickEconomy();
                _production.TickResearch();
                TickMoraleMedical();
                UpdateRegionControl();
                _world.InvalidateResourceCache();
            }

            if (_aiTimer >= 2.0f)
            {
                _aiTimer = 0f;
                _enemyAi.Tick();
                if (_enemyAi.PressureSummary != "Stable")
                    PressureSummary = _enemyAi.PressureSummary;
            }

            if (_combatTimer >= 0.25f)
            {
                _combatTimer = 0f;
                _combat.TickCombat(_units);
                CheckWinLose();
            }

            if (_cleanupTimer >= 15.0f)
            {
                _cleanupTimer = 0f;
                _world.TrimExcess();
            }
        }

        public void TickEconomyNow() => _production.TickEconomy();

        public ProductionOrder QueueProduction(int buildingId, string recipeId, int cycles, int priority)
            => _production.QueueProduction(buildingId, recipeId, cycles, priority);

        public ResearchOrder QueueResearch(string techId, int priority)
            => _production.QueueResearch(techId, priority);

        public ProductionOrder QueueUnitTraining(int barracksId, UnitKind kind, int priority)
            => _production.QueueUnitTraining(barracksId, kind, priority);

        public void AssignWorkers(int buildingId, int workers)
            => _production.AssignWorkers(buildingId, workers);

        public void SetDiplomacy(int factionId, DiplomacyStance stance)
        {
            foreach (var relation in _world.Diplomacy)
            {
                if (relation.FactionA != OpenWorldConstants.PlayerFactionId || relation.FactionB != factionId) continue;
                relation.Stance = stance;
                relation.Trust = Mathf.Clamp(relation.Trust + (stance == DiplomacyStance.Allied ? 10 : stance == DiplomacyStance.Hostile ? -30 : 2), 0, 100);
                return;
            }
            _world.Diplomacy.Add(new DiplomacyRecord { FactionA = OpenWorldConstants.PlayerFactionId, FactionB = factionId, Stance = stance, Trust = stance == DiplomacyStance.Hostile ? 0 : 45 });
        }

        public TradeContract QueueTrade(int partnerFactionId, ResourceKind exportKind, ResourceKind importKind, int amount, int priority)
        {
            int nextId = 1;
            foreach (var contract in _world.TradeContracts) nextId = Mathf.Max(nextId, contract.Id + 1);
            var trade = new TradeContract
            {
                Id = nextId,
                PartnerFactionId = partnerFactionId,
                ExportKind = exportKind,
                ImportKind = importKind,
                Amount = Mathf.Max(1, amount),
                Priority = Mathf.Clamp(priority, 1, 5),
                Status = "Queued"
            };
            _world.TradeContracts.Add(trade);
            return trade;
        }

        public void DeclareHostilityForTarget(int targetEntityId)
        {
            if (targetEntityId <= 0 || !_world.Units.TryGetValue(targetEntityId, out var target)) return;
            if (target.FactionId == OpenWorldConstants.PlayerFactionId || target.FactionId == OpenWorldConstants.EnemyFactionId) return;
            SetDiplomacy(target.FactionId, DiplomacyStance.Hostile);
            foreach (var faction in _world.Factions)
                if (faction.Id == target.FactionId) faction.Reputation = Mathf.Max(0, faction.Reputation - 30);
            foreach (var contract in _world.TradeContracts)
                if (contract.PartnerFactionId == target.FactionId)
                {
                    contract.Active = false;
                    contract.Status = "Cancelled by hostilities";
                }
        }

        private void TickMoraleMedical()
        {
            PressureSummary = "Stable";
            int houses = 0;
            foreach (var building in _world.Buildings.Values)
                if (building.FactionId == OpenWorldConstants.PlayerFactionId && building.Kind == BuildableKind.House) houses++;
            int populationCapacity = 16 + houses * 6;
            _world.Population.Homeless = Mathf.Max(0, _world.Population.Residents - populationCapacity);

            int foodNeed = Mathf.Max(1, _world.Population.Residents / 8);
            if (_world.Inventory.Food >= foodNeed)
            {
                _world.Inventory.Food -= foodNeed;
                _world.Population.CityMorale = Mathf.Min(100f, _world.Population.CityMorale + 0.35f);
            }
            else
            {
                _world.Population.CityMorale = Mathf.Max(0f, _world.Population.CityMorale - 2.0f);
                PressureSummary = "Food shortage";
            }

            _world.Population.MedicalPressure = Mathf.Clamp01(_world.Population.Wounded / Mathf.Max(1f, _world.Population.Residents * 0.2f));
            if (_world.Population.MedicalPressure > 0.45f)
                PressureSummary = "Medical pressure";

            foreach (var unit in _world.Units.Values)
            {
                if (unit.Task == UnitTask.Moving || unit.Task == UnitTask.Attacking || unit.Task == UnitTask.Digging || unit.Task == UnitTask.Building)
                    unit.Fatigue = Mathf.Min(100f, unit.Fatigue + 0.5f);
                else if (unit.Task == UnitTask.Idle)
                {
                    unit.Fatigue = Mathf.Max(0f, unit.Fatigue - 1.5f);
                    if (unit.Hp < unit.MaxHp && !unit.Wounded)
                        unit.Hp = Mathf.Min(unit.MaxHp, unit.Hp + 1);
                    if (unit.Hp >= unit.MaxHp * 0.75f)
                        unit.Wounded = false;
                }
                else
                    unit.Fatigue = Mathf.Max(0f, unit.Fatigue - 0.8f);

                if (unit.Supplies <= 5f || unit.Fatigue > 80f || unit.Wounded)
                    unit.Morale = Mathf.Max(0f, unit.Morale - 0.6f);
                else
                    unit.Morale = Mathf.Min(100f, unit.Morale + 0.2f);
            }

            HealUnitsAtClinics();
            TickServiceOrders();
            TickTradeContracts();
        }

        private void UpdateRegionControl()
        {
            int controlled = 0;
            foreach (var region in _world.Regions)
            {
                int playerPresence = 0;
                int enemyPresence = 0;
                foreach (var building in _world.Buildings.Values)
                {
                    if ((building.Origin - region.Center).sqrMagnitude > region.Radius * region.Radius) continue;
                    if (building.FactionId == OpenWorldConstants.PlayerFactionId) playerPresence += building.Kind == BuildableKind.ControlPoint ? 12 : 3;
                    if (building.FactionId == OpenWorldConstants.EnemyFactionId) enemyPresence += 4;
                }

                foreach (var unit in _world.Units.Values)
                {
                    if ((unit.Cell - region.Center).sqrMagnitude > region.Radius * region.Radius) continue;
                    if (unit.FactionId == OpenWorldConstants.PlayerFactionId) playerPresence += 1;
                    if (unit.FactionId == OpenWorldConstants.EnemyFactionId) enemyPresence += 1;
                }

                foreach (var route in _world.LogisticsRoutes)
                {
                    if (!route.Status.Contains("moving") && !route.Status.Contains("delivered") && !route.Status.Contains("Waiting")) continue;
                    if ((route.Target - region.Center).sqrMagnitude <= region.Radius * region.Radius) playerPresence += 2;
                }

                if (playerPresence > enemyPresence)
                {
                    region.Control = Mathf.Min(100, region.Control + 2);
                    if (region.Control >= 55) region.OwnerFactionId = OpenWorldConstants.PlayerFactionId;
                }
                else if (enemyPresence > playerPresence)
                {
                    region.Control = Mathf.Max(-100, region.Control - 2);
                    if (region.Control <= -40) region.OwnerFactionId = OpenWorldConstants.EnemyFactionId;
                }

                if (region.OwnerFactionId == OpenWorldConstants.PlayerFactionId)
                    controlled++;
            }

            UnityProgress = _world.Regions.Count == 0 ? 0f : controlled / (float)_world.Regions.Count;
            DiplomacySummary = DiplomacyLine();
        }


        private void CheckWinLose()
        {
            if (GameOver) return;
            int playerBuildings = 0, playerUnits = 0;
            int enemyBuildings = 0, enemyUnits = 0;
            bool playerHasTownCenter = false, enemyHasTownCenter = false;
            foreach (var b in _world.Buildings.Values)
            {
                if (b.FactionId == OpenWorldConstants.PlayerFactionId)
                {
                    playerBuildings++;
                    if (b.Kind == BuildableKind.TownCenter) playerHasTownCenter = true;
                }
                if (b.FactionId == OpenWorldConstants.EnemyFactionId)
                {
                    enemyBuildings++;
                    if (b.Kind == BuildableKind.TownCenter) enemyHasTownCenter = true;
                }
            }
            foreach (var u in _world.Units.Values)
            {
                if (u.FactionId == OpenWorldConstants.PlayerFactionId) playerUnits++;
                if (u.FactionId == OpenWorldConstants.EnemyFactionId) enemyUnits++;
            }

            float totalBuildings = playerBuildings + enemyBuildings;
            UnityProgress = totalBuildings > 0 ? (float)playerBuildings / totalBuildings : 0.5f;

            if (enemyBuildings == 0 && enemyUnits == 0)
            {
                GameOver = true;
                IsVictory = true;
                GameOverText = "胜利！钢铁领主已被击败！\n边疆联盟取得了最终胜利！";
                Debug.Log("[OpenWorld] VICTORY! Enemy faction eliminated.");
            }
            else if ((playerBuildings == 0 || !playerHasTownCenter) && playerUnits == 0)
            {
                GameOver = true;
                IsVictory = false;
                GameOverText = "战败！边疆联盟已被摧毁！\n钢铁领主统治了这片土地！";
                Debug.Log("[OpenWorld] DEFEAT! Player faction eliminated.");
            }
        }

        private void HealUnitsAtClinics()
        {
            foreach (var clinic in _world.Buildings.Values)
            {
                if (clinic.FactionId != OpenWorldConstants.PlayerFactionId || clinic.Kind != BuildableKind.Clinic) continue;
                foreach (var unit in _world.Units.Values)
                {
                    if (unit.FactionId != clinic.FactionId || (!unit.Wounded && unit.Hp >= unit.MaxHp)) continue;
                    if ((unit.Cell - clinic.Origin).sqrMagnitude > 64) continue;
                    var medicineSource = _production.FindInputStorage(clinic, ResourceKind.Medicine, 1);
                    if (medicineSource == null) break;
                    medicineSource.Storage.Spend(ResourceKind.Medicine, 1);
                    unit.Hp = Mathf.Min(unit.MaxHp, unit.Hp + 18);
                    unit.Wounded = unit.Hp < unit.MaxHp * 0.7f;
                    unit.Morale = Mathf.Min(100f, unit.Morale + 4f);
                    break;
                }
            }
        }

        private void TickServiceOrders()
        {
            foreach (var order in _world.ServiceOrders)
            {
                if (!_world.Vehicles.TryGetValue(order.VehicleId, out var vehicle)) { order.Status = "Vehicle missing"; continue; }
                if (!_world.Buildings.TryGetValue(order.ServiceBuildingId, out var service) || service.Kind is not (BuildableKind.Garage or BuildableKind.Station)) { order.Status = "Service point missing"; continue; }
                if ((vehicle.Cell - service.Origin).sqrMagnitude > 16) { order.Status = "Travelling to service"; continue; }
                if (order.Refuel && vehicle.Fuel < 100f) { var fuel = _production.FindInputStorage(service, ResourceKind.Fuel, 1); if (fuel != null) { fuel.Storage.Spend(ResourceKind.Fuel, 1); vehicle.Fuel = Mathf.Min(100f, vehicle.Fuel + 20f); } }
                if (order.Repair && vehicle.Condition < 100f) { var parts = _production.FindInputStorage(service, ResourceKind.MachineParts, 1); if (parts != null) { parts.Storage.Spend(ResourceKind.MachineParts, 1); vehicle.Condition = Mathf.Min(100f, vehicle.Condition + 15f); } }
                bool fuelComplete = !order.Refuel || vehicle.Fuel >= 99f;
                bool repairComplete = !order.Repair || vehicle.Condition >= 99f;
                order.Status = fuelComplete && repairComplete ? "Complete" : "Servicing";
                if (order.Status == "Complete")
                {
                    vehicle.Task = VehicleTask.Idle;
                    vehicle.StatusText = "Service complete";
                }
            }
        }

        private void TickTradeContracts()
        {
            foreach (var contract in _world.TradeContracts)
            {
                if (!contract.Active) continue;
                if (!HasDiplomacy(contract.PartnerFactionId, DiplomacyStance.Trade) && !HasDiplomacy(contract.PartnerFactionId, DiplomacyStance.Allied))
                {
                    contract.Status = "No trade agreement";
                    continue;
                }
                int amount = Mathf.Max(1, contract.Amount);
                if (_world.Inventory.Get(contract.ExportKind) < amount) { contract.Status = $"Missing {contract.ExportKind}"; continue; }
                _world.Inventory.Add(contract.ExportKind, -amount);
                _world.Inventory.Add(contract.ImportKind, amount);
                _world.InvalidateResourceCache();
                contract.Status = $"Traded {amount} {contract.ExportKind}";
            }
        }

        private bool HasDiplomacy(int factionId, DiplomacyStance stance)
        {
            foreach (var relation in _world.Diplomacy)
                if (relation.FactionA == OpenWorldConstants.PlayerFactionId && relation.FactionB == factionId && relation.Stance == stance) return true;
            return false;
        }

        private string DiplomacyLine()
        {
            foreach (var relation in _world.Diplomacy)
            {
                if (relation.FactionA == OpenWorldConstants.PlayerFactionId && relation.FactionB == OpenWorldConstants.NeutralFactionId)
                    return $"Free Towns {relation.Stance} trust {relation.Trust}";
            }
            return "Diplomacy unknown";
        }

    }
}
