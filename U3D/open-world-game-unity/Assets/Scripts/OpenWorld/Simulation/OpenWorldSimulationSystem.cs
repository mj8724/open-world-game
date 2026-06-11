using UnityEngine;
using System.Collections.Generic;

namespace OpenWorld
{
    public class OpenWorldSimulationSystem : MonoBehaviour
    {
        public string PressureSummary { get; private set; } = "Stable";
        public string ProductionSummary { get; private set; } = "Production idle";
        public IReadOnlyList<string> ProductionLines => _productionLines;
        public string ResearchSummary { get; private set; } = "Research idle";
        public string DiplomacySummary { get; private set; } = "Neutral trade open";
        public float UnityProgress { get; private set; }

        private readonly List<string> _productionLines = new();
        private OpenWorldState _world;
        private SurfaceTerrainSystem _terrain;
        private BuildingSystem _buildings;
        private UnitSystem _units;
        private BlueprintSystem _blueprints;
        private VehicleSystem _vehicles;
        private float _economyTimer;
        private float _aiTimer;
        private float _combatTimer;
        private int _aiStep;
        private const int EnemyUnitBudget = 48;
        private const int EnemyRoadblockBudget = 8;
        private const int EnemyRaidSize = 2;

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain, BuildingSystem buildings, UnitSystem units, BlueprintSystem blueprints, VehicleSystem vehicles)
        {
            _world = world;
            _terrain = terrain;
            _buildings = buildings;
            _units = units;
            _blueprints = blueprints;
            _vehicles = vehicles;
        }

        private void Update()
        {
            if (_world == null) return;
            _economyTimer += Time.deltaTime;
            _aiTimer += Time.deltaTime;
            _combatTimer += Time.deltaTime;

            if (_economyTimer >= 1.0f)
            {
                _economyTimer = 0f;
                TickEconomy();
                TickResearch();
                TickMoraleMedical();
                UpdateRegionControl();
            }

            if (_aiTimer >= 4.0f)
            {
                _aiTimer = 0f;
                TickEnemyAi();
            }

            if (_combatTimer >= 0.25f)
            {
                _combatTimer = 0f;
                TickCombat();
            }
        }

        public void TickEconomyNow() => TickEconomy();

        public ProductionOrder QueueProduction(int buildingId, string recipeId, int cycles, int priority)
        {
            if (!_world.Buildings.TryGetValue(buildingId, out var building)) return null;
            var recipe = OpenWorldDataCatalog.GetRecipe(recipeId);
            if (recipe == null || recipe.Building != building.Kind) return null;
            building.ActiveRecipeId = recipeId;
            if (building.AssignedWorkers <= 0) AssignWorkers(buildingId, recipe.Workers);
            return _world.AddProductionOrder(buildingId, recipeId, cycles, priority);
        }

        public ResearchOrder QueueResearch(string techId, int priority)
        {
            if (OpenWorldDataCatalog.GetTech(techId) == null) return null;
            _world.Tech.CurrentResearch = techId;
            _world.Tech.ResearchProgress = 0;
            return _world.AddResearchOrder(techId, priority);
        }

        public ProductionOrder QueueUnitTraining(int barracksId, UnitKind kind, int priority)
        {
            if (!_world.Buildings.TryGetValue(barracksId, out var building) || building.Kind != BuildableKind.Barracks) return null;
            return _world.AddProductionOrder(barracksId, $"unit:{kind}", 1, priority);
        }

        public void AssignWorkers(int buildingId, int workers)
        {
            if (!_world.Buildings.TryGetValue(buildingId, out var building)) return;
            int assignedElsewhere = 0;
            foreach (var assignment in _world.WorkerAssignments)
                if (assignment.BuildingId != buildingId) assignedElsewhere += assignment.Workers;
            int available = Mathf.Max(0, _world.Population.Workers - assignedElsewhere);
            workers = Mathf.Clamp(workers, 0, available);
            var existing = _world.WorkerAssignments.Find(a => a.BuildingId == buildingId);
            if (existing == null)
            {
                existing = new WorkerAssignment { BuildingId = buildingId };
                _world.WorkerAssignments.Add(existing);
            }
            existing.Workers = workers;
            building.AssignedWorkers = workers;
        }

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

        private void TickEconomy()
        {
            TickOilExtraction();
            int clinics = 0;
            _productionLines.Clear();
            int activeRecipes = 0;
            int blockedRecipes = 0;
            string firstBlocked = "";

            var orders = new List<ProductionOrder>(_world.ProductionOrders);
            orders.Sort((a, b) => b.Priority != a.Priority ? b.Priority.CompareTo(a.Priority) : a.Id.CompareTo(b.Id));
            foreach (var order in orders)
            {
                if (order.Paused || order.RemainingCycles <= 0) continue;
                if (!_world.Buildings.TryGetValue(order.BuildingId, out var building))
                {
                    order.Status = "Building missing";
                    blockedRecipes++;
                    continue;
                }
                _world.EnsureBuildingStorage(building);
                if (order.RecipeId.StartsWith("unit:"))
                {
                    if (TickTraining(order, building)) activeRecipes++; else blockedRecipes++;
                    continue;
                }

                var recipe = OpenWorldDataCatalog.GetRecipe(order.RecipeId);
                if (recipe == null || recipe.Building != building.Kind)
                {
                    order.Status = "Invalid recipe";
                    blockedRecipes++;
                    continue;
                }

                string blockedReason = TickRecipe(order, building, recipe);
                if (string.IsNullOrEmpty(blockedReason)) activeRecipes++;
                else
                {
                    blockedRecipes++;
                    if (string.IsNullOrEmpty(firstBlocked)) firstBlocked = $"{recipe.Id}: {blockedReason}";
                }
                if (_productionLines.Count < 10)
                    _productionLines.Add($"#{building.Id} {recipe.Id}: {(string.IsNullOrEmpty(blockedReason) ? "running" : blockedReason)} | {building.Storage.Total}/{building.StorageCapacity}");
            }

            for (int i = _world.ProductionOrders.Count - 1; i >= 0; i--)
                if (_world.ProductionOrders[i].RemainingCycles <= 0) _world.ProductionOrders.RemoveAt(i);

            foreach (var building in _world.Buildings.Values)
                if (building.FactionId == OpenWorldConstants.PlayerFactionId && building.Kind == BuildableKind.Clinic) clinics++;

            if (clinics > 0 && _world.Population.Wounded > 0)
            {
                foreach (var building in _world.Buildings.Values)
                {
                    if (building.FactionId != OpenWorldConstants.PlayerFactionId || building.Kind != BuildableKind.Clinic) continue;
                    var medicineSource = FindInputStorage(building, ResourceKind.Medicine, 1);
                    if (medicineSource == null) continue;
                    medicineSource.Storage.Spend(ResourceKind.Medicine, 1);
                    _world.Population.Wounded--;
                    activeRecipes++;
                    if (_productionLines.Count < 8)
                        _productionLines.Add($"#{building.Id} Clinic: healed 1 from storage #{medicineSource.Id}");
                }
            }

            ProductionSummary = activeRecipes > 0
                ? $"Active recipes {activeRecipes}, blocked {blockedRecipes}"
                : blockedRecipes > 0 ? $"Production blocked x{blockedRecipes}: {firstBlocked}" : "Production idle";
            if (_productionLines.Count == 0)
                _productionLines.Add(ProductionSummary);
        }

        private string TickRecipe(ProductionOrder order, BuildingEntity building, ProductionRecipeDef recipe)
        {
            if (!OpenWorldDataCatalog.EraUnlocked(_world.Tech.Era, recipe.RequiredEra)) return order.Status = $"Locked: {recipe.RequiredEra}";
            if (building.AssignedWorkers < recipe.Workers) return order.Status = $"Workers {building.AssignedWorkers}/{recipe.Workers}";
            var powerSource = RequiresPower(building.Kind) ? FindInputStorage(building, ResourceKind.Power, 1) : null;
            if (RequiresPower(building.Kind) && powerSource == null) return order.Status = "No power";
            if (!CanStoreOutputs(building, recipe.Outputs)) return order.Status = "Output full";
            if (!TryConsumeLocalInputs(building, recipe.Inputs, out var missing)) return order.Status = missing;
            powerSource?.Storage.Spend(ResourceKind.Power, 1);

            float efficiency = Mathf.Clamp01(_world.Population.CityMorale / 100f);
            building.ProductionProgress += Mathf.Max(0.25f, efficiency);
            if (building.ProductionProgress < 1f) return order.Status = "Working";
            building.ProductionProgress = 0f;
            if (recipe.Id.StartsWith("vehicle:") && System.Enum.TryParse(recipe.Id.Substring(8), out VehicleKind vehKind))
            {
                var spawnCell = _world.FindBuildingAccessCell(building, building.Origin + Vector2Int.down * 6);
                var veh = _vehicles?.SpawnScenarioVehicle(vehKind, spawnCell, building.FactionId);
                if (veh != null) building.ProductionStatus = $"Produced {vehKind} as #{veh.Entity.Id}";
                else building.ProductionStatus = $"Failed to produce {vehKind} - missing factory?";
            }
            else
            {
                foreach (var output in recipe.Outputs) _world.AddToStorage(building, output.Kind, output.Amount);
            }
            order.RemainingCycles--;
            string statusText = recipe.Id.StartsWith("vehicle:")
                ? $"Produced {recipe.Id.Substring(8)}"
                : $"Produced {FormatAmounts(recipe.Outputs)}";
            building.ProductionStatus = order.Status = statusText;
            return "";
        }

        private bool TickTraining(ProductionOrder order, BuildingEntity barracks)
        {
            if (!System.Enum.TryParse(order.RecipeId.Substring(5), out UnitKind kind))
            {
                order.Status = "Invalid unit";
                return false;
            }
            var requiredEra = kind switch
            {
                UnitKind.Musketeer or UnitKind.Artillery => TechEra.Gunpowder,
                UnitKind.Rifleman or UnitKind.MachineGunner => TechEra.Industrial,
                _ => TechEra.WoodStone
            };
            if (!OpenWorldDataCatalog.EraUnlocked(_world.Tech.Era, requiredEra)) { order.Status = $"Locked: {requiredEra}"; return false; }
            if (_world.Population.Residents <= 1) { order.Status = "No recruitable resident"; return false; }

            // Resource costs per unit type
            int foodCost = kind is UnitKind.MachineGunner or UnitKind.Artillery ? 6 :
                          kind is UnitKind.Rifleman or UnitKind.Musketeer ? 5 :
                          kind is UnitKind.Melee or UnitKind.Spearman or UnitKind.Ranged ? 4 : 3;
            int weaponCost = kind is UnitKind.Worker or UnitKind.Medic or UnitKind.Engineer or UnitKind.Hauler ? 0 :
                            kind is UnitKind.Militia or UnitKind.Scout ? 1 : 2;
            int ammoGrant = kind is UnitKind.Rifleman ? 20 :
                           kind is UnitKind.MachineGunner ? 40 :
                           kind is UnitKind.Musketeer or UnitKind.Ranged or UnitKind.Artillery ? 15 : 0;

            var foodSource = FindInputStorage(barracks, ResourceKind.Food, foodCost);
            var weaponSource = weaponCost > 0 ? FindInputStorage(barracks, ResourceKind.Weapons, weaponCost) : null;
            if (foodSource == null) { order.Status = $"No food ({foodCost})"; return false; }
            if (weaponCost > 0 && weaponSource == null) { order.Status = "No weapons"; return false; }
            foodSource.Storage.Spend(ResourceKind.Food, foodCost);
            weaponSource?.Storage.Spend(ResourceKind.Weapons, weaponCost);

            _world.Population.Residents--;
            _world.Population.Soldiers++;
            var spawn = _world.FindBuildingAccessCell(barracks, barracks.Origin + Vector2Int.down * 4);
            var unit = _units.Spawn(kind, spawn, barracks.FactionId);
            if (unit != null && ammoGrant > 0) unit.Entity.Ammo = ammoGrant;
            order.RemainingCycles--;
            order.Status = $"Trained {kind}";
            return true;
        }

        private bool ConsumeNearby(BuildingEntity producer, ResourceKind kind, int amount)
        {
            var source = FindInputStorage(producer, kind, amount);
            if (source == null) return false;
            source.Storage.Spend(kind, amount);
            return true;
        }

        private static bool RequiresPower(BuildableKind kind) => kind is BuildableKind.Steelworks or BuildableKind.MachineShop or BuildableKind.VehicleFactory or BuildableKind.TrainFactory or BuildableKind.Refinery;

        private void TickOilExtraction()
        {
            foreach (var derrick in _world.Buildings.Values)
            {
                if (derrick.Kind != BuildableKind.OilDerrick || derrick.UnderConstruction || derrick.AssignedWorkers <= 0) continue;
                var survey = _world.GetSurvey(derrick.Origin);
                if (survey == null || survey.State != SurveyState.Drilled || survey.EstimatedMaterial != GroundMaterial.Oil)
                {
                    derrick.ProductionStatus = "Requires drilled oil report";
                    continue;
                }
                if (derrick.Storage.Total >= derrick.StorageCapacity)
                {
                    derrick.ProductionStatus = "Oil storage full";
                    continue;
                }

                var cell = _world.GetCell(derrick.Origin);
                OpenWorldState.NormalizeLayers(ref cell);
                bool extracted = false;
                for (int i = 0; i < cell.Layers.Length; i++)
                {
                    var layer = cell.Layers[i];
                    if (layer.Material != GroundMaterial.Oil || layer.RemainingAmount <= 0) continue;
                    int amount = Mathf.Max(1, Mathf.RoundToInt(layer.Grade * Mathf.Max(1, derrick.AssignedWorkers)));
                    amount = Mathf.Min(amount, layer.RemainingAmount);
                    int stored = _world.AddToStorage(derrick, ResourceKind.Oil, amount);
                    layer.RemainingAmount -= stored;
                    cell.Layers[i] = layer;
                    _world.SetCell(derrick.Origin, cell);
                    derrick.ProductionStatus = stored > 0 ? $"Pumped {stored} Oil" : "Oil storage full";
                    if (layer.RemainingAmount <= 0)
                    {
                        survey.State = SurveyState.Exhausted;
                        survey.MinReserve = survey.MaxReserve = 0;
                    }
                    extracted = stored > 0;
                    break;
                }
                if (!extracted && survey.State != SurveyState.Exhausted)
                    derrick.ProductionStatus = "No oil at well cell";
            }
        }

        private bool TryConsumeLocalInputs(BuildingEntity producer, IReadOnlyList<ResourceAmount> inputs, out string missing)
        {
            missing = "";
            if (inputs == null || inputs.Count == 0) return true;
            var sources = new List<BuildingEntity>(inputs.Count);
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                var source = FindInputStorage(producer, input.Kind, input.Amount);
                if (source == null)
                {
                    missing = $"local {input.Kind} 0/{input.Amount}";
                    return false;
                }
                sources.Add(source);
            }

            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                sources[i].Storage.Spend(input.Kind, input.Amount);
                sources[i].LastStorageStatus = $"Supplied {input.Amount} {input.Kind} to #{producer.Id}";
            }
            return true;
        }

        private BuildingEntity FindInputStorage(BuildingEntity producer, ResourceKind kind, int amount)
        {
            BuildingEntity best = null;
            int bestDistance = int.MaxValue;
            foreach (var candidate in _world.Buildings.Values)
            {
                if (candidate.FactionId != producer.FactionId) continue;
                _world.EnsureBuildingStorage(candidate);
                if (candidate.StorageCapacity <= 0 || candidate.Storage.Get(kind) < amount) continue;
                int distance = (candidate.Origin - producer.Origin).sqrMagnitude;
                if (distance > 36 || distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private static bool CanStoreOutputs(BuildingEntity building, IReadOnlyList<ResourceAmount> outputs)
        {
            int amount = 0;
            for (int i = 0; i < outputs.Count; i++) amount += outputs[i].Amount;
            return building.StorageCapacity > 0 && building.Storage.Total + amount <= building.StorageCapacity;
        }

        private void TickResearch()
        {
            ResearchOrder order = null;
            foreach (var candidate in _world.ResearchOrders)
            {
                if (candidate.Paused) continue;
                if (order == null || candidate.Priority > order.Priority) order = candidate;
            }

            if (order == null)
            {
                ResearchSummary = "Research queue empty";
                return;
            }

            _world.Tech.CurrentResearch = order.TechId;
            var tech = OpenWorldDataCatalog.GetTech(order.TechId);
            if (tech == null)
            {
                ResearchSummary = "No research selected";
                return;
            }

            if (!OpenWorldDataCatalog.EraUnlocked(_world.Tech.Era, tech.RequiredEra))
            {
                ResearchSummary = $"Research locked: needs {tech.RequiredEra}";
                return;
            }

            if (_world.Population.Engineers < tech.Engineers)
            {
                ResearchSummary = $"Research blocked: engineers {_world.Population.Engineers}/{tech.Engineers}";
                return;
            }

            if (!OpenWorldDataCatalog.CanSpend(_world.Inventory, tech.CostPerTick, out var missing))
            {
                ResearchSummary = $"Research blocked: {missing}";
                return;
            }

            int assignedEngineers = 0;
            foreach (var assignment in _world.WorkerAssignments) assignedEngineers += assignment.Engineers;
            int researchEngineers = Mathf.Max(0, _world.Population.Engineers - assignedEngineers);
            if (researchEngineers < tech.Engineers)
            {
                order.Status = $"Engineers {researchEngineers}/{tech.Engineers}";
                ResearchSummary = $"Research blocked: {order.Status}";
                return;
            }

            OpenWorldDataCatalog.Spend(_world.Inventory, tech.CostPerTick);

            _world.Tech.ResearchProgress += Mathf.Max(1, researchEngineers);
            order.Progress = _world.Tech.ResearchProgress;
            if (_world.Tech.ResearchProgress >= tech.ResearchTicks)
            {
                _world.Tech.Era = tech.UnlockEra;
                if (!_world.Tech.CompletedResearch.Contains(tech.Id)) _world.Tech.CompletedResearch.Add(tech.Id);
                _world.Tech.CurrentResearch = "";
                _world.Tech.ResearchProgress = 0;
                _world.ResearchOrders.Remove(order);
                ResearchSummary = $"Unlocked {tech.UnlockEra}";
                return;
            }

            order.Status = $"Researching {order.Progress:0}/{tech.ResearchTicks}";
            ResearchSummary = $"{tech.DisplayName} {_world.Tech.ResearchProgress}/{tech.ResearchTicks}";
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

        private void TickEnemyAi()
        {
            if (_world.Buildings.Count == 0) return;
            _aiStep++;
            var center = new Vector2Int(_world.MapSize / 2, _world.MapSize / 2);
            var enemyCenter = center + new Vector2Int(95, 90);
            var pressureCell = enemyCenter + new Vector2Int((_aiStep % 5) * 2, (_aiStep % 3) * 2);

            int enemyUnits = 0;
            foreach (var unit in _world.Units.Values)
                if (unit.FactionId == OpenWorldConstants.EnemyFactionId) enemyUnits++;

            int enemyRoadblocks = 0;
            foreach (var building in _world.Buildings.Values)
                if (building.FactionId == OpenWorldConstants.EnemyFactionId && building.Kind == BuildableKind.Roadblock) enemyRoadblocks++;
            foreach (var blueprint in _world.Blueprints)
                if (blueprint.FactionId == OpenWorldConstants.EnemyFactionId && blueprint.BuildKind == BuildableKind.Roadblock && blueprint.Status != BlueprintStatus.Cancelled) enemyRoadblocks++;

            if (_aiStep % 2 == 0 && enemyUnits < EnemyUnitBudget)
                _units.Spawn(UnitKind.Militia, pressureCell, OpenWorldConstants.EnemyFactionId);

            if (_aiStep % 3 == 0 && enemyRoadblocks < EnemyRoadblockBudget)
                _blueprints.QueueBuilding(BuildableKind.Roadblock, pressureCell + new Vector2Int(2, 0), OpenWorldConstants.EnemyFactionId, 2);

            UnitEntity playerTarget = null;
            foreach (var unit in _world.Units.Values)
            {
                if (unit.FactionId != OpenWorldConstants.PlayerFactionId) continue;
                playerTarget = unit;
                break;
            }
            int activeRaiders = 0;
            foreach (var agent in _units.AllAgents())
            {
                if (agent.Entity.FactionId != OpenWorldConstants.EnemyFactionId || agent.Entity.CurrentOrder?.Kind != UnitOrderKind.Attack) continue;
                activeRaiders++;
                if (activeRaiders > EnemyRaidSize) agent.ClearOrder();
            }
            activeRaiders = Mathf.Min(activeRaiders, EnemyRaidSize);

            if (playerTarget != null && _aiStep >= 30 && _world.LogisticsRoutes.Count > 0)
            {
                int issued = activeRaiders;
                foreach (var agent in _units.AllAgents())
                {
                    if (agent.Entity.FactionId != OpenWorldConstants.EnemyFactionId || agent.Entity.CurrentOrder?.Kind == UnitOrderKind.Attack) continue;
                    agent.IssueOrder(new UnitOrder { Kind = UnitOrderKind.Attack, TargetCell = playerTarget.Cell, TargetEntityId = playerTarget.Id, Priority = 4 });
                    issued++;
                    if (issued >= EnemyRaidSize) break;
                }
            }

            PressureSummary = _aiStep % 2 == 0 ? "Enemy scouting pressure" : "Logistics stable";
        }

        private void TickCombat()
        {
            var dead = new List<int>();
            var units = new List<UnitEntity>(_world.Units.Values);
            foreach (var attacker in units)
            {
                if (attacker.Hp <= 0) { dead.Add(attacker.Id); continue; }
                if (attacker.SimulationTier == SimulationTier.Dormant) continue;
                if (attacker.SimulationTier == SimulationTier.LowFrequency && attacker.CurrentOrder?.Kind != UnitOrderKind.Attack) continue;
                UnitEntity target = null;
                if (attacker.CurrentOrder != null && attacker.CurrentOrder.Kind == UnitOrderKind.Attack && attacker.CurrentOrder.TargetEntityId > 0)
                    _world.Units.TryGetValue(attacker.CurrentOrder.TargetEntityId, out target);
                if (target == null) target = FindNearestHostile(attacker, Mathf.Max(5f, attacker.VisionRange));
                if (target == null) continue;

                float distance = Vector2Int.Distance(attacker.Cell, target.Cell);
                if (distance > attacker.AttackRange)
                {
                    if (attacker.CurrentOrder != null && attacker.CurrentOrder.Kind == UnitOrderKind.Attack)
                    {
                        var agent = _units.GetAgent(attacker.Id);
                        if (agent != null && attacker.Task != UnitTask.Moving)
                            agent.IssueOrder(new UnitOrder { Kind = UnitOrderKind.Attack, TargetCell = target.Cell, TargetEntityId = target.Id, Priority = 5 });
                    }
                    continue;
                }

                bool ranged = attacker.AttackRange > 2f;
                if (ranged && attacker.Ammo < 1f) { attacker.Morale = Mathf.Max(0f, attacker.Morale - 0.5f); continue; }
                if (ranged) attacker.Ammo -= 1f;
                float efficiency = Mathf.Clamp(attacker.Morale / 100f, 0.2f, 1f) * Mathf.Clamp(1f - attacker.Fatigue / 120f, 0.25f, 1f);
                float damage = Mathf.Max(1f, attacker.AttackPower * attacker.Accuracy * efficiency * 0.32f - target.Armor * 0.15f);
                target.Hp -= Mathf.RoundToInt(damage);
                target.Suppression = Mathf.Min(100f, target.Suppression + damage * 1.5f);
                target.Morale = Mathf.Max(0f, target.Morale - damage * 0.35f);
                if (target.Hp <= 0) dead.Add(target.Id);
                else if (target.Hp < target.MaxHp * 0.45f) target.Wounded = true;
            }

            foreach (int id in dead) _units.RemoveUnit(id);
        }

        private UnitEntity FindNearestHostile(UnitEntity attacker, float radius)
        {
            UnitEntity best = null;
            float bestDistance = radius * radius;
            foreach (var candidate in _world.Units.Values)
            {
                if (candidate.Id == attacker.Id || candidate.Hp <= 0 || !AreHostile(attacker.FactionId, candidate.FactionId)) continue;
                float distance = (candidate.Cell - attacker.Cell).sqrMagnitude;
                if (distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private bool AreHostile(int a, int b)
        {
            if (a == b) return false;
            foreach (var relation in _world.Diplomacy)
                if ((relation.FactionA == a && relation.FactionB == b) || (relation.FactionA == b && relation.FactionB == a))
                    return relation.Stance == DiplomacyStance.Hostile;
            return a == OpenWorldConstants.EnemyFactionId || b == OpenWorldConstants.EnemyFactionId;
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
                    if (!ConsumeNearby(clinic, ResourceKind.Medicine, 1)) break;
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
                if (order.Refuel && vehicle.Fuel < 100f && ConsumeNearby(service, ResourceKind.Fuel, 1)) vehicle.Fuel = Mathf.Min(100f, vehicle.Fuel + 20f);
                if (order.Repair && vehicle.Condition < 100f && ConsumeNearby(service, ResourceKind.MachineParts, 1)) vehicle.Condition = Mathf.Min(100f, vehicle.Condition + 15f);
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

        private static string FormatAmounts(IReadOnlyList<ResourceAmount> amounts)
        {
            if (amounts == null || amounts.Count == 0) return "nothing";
            var text = "";
            for (int i = 0; i < amounts.Count; i++)
            {
                if (i > 0) text += ", ";
                text += $"+{amounts[i].Amount} {amounts[i].Kind}";
            }
            return text;
        }
    }
}
