using UnityEngine;
using System.Collections.Generic;

namespace OpenWorld
{
    /// <summary>
    /// 生产与经济系统：管理配方生产、单位训练、研究、石油开采、物流
    /// </summary>
    public class OpenWorldProductionSystem
    {
        private readonly OpenWorldState _world;
        private readonly UnitSystem _units;
        private readonly VehicleSystem _vehicles;
        private readonly List<string> _lines = new();
        private List<ProductionOrder> _sortedOrders = new();
        private bool _ordersDirty = true;

        public IReadOnlyList<string> ProductionLines => _lines;
        public string Summary { get; private set; } = "Production idle";
        public string ResearchSummary { get; private set; } = "Research idle";

        public OpenWorldProductionSystem(OpenWorldState world, UnitSystem units, VehicleSystem vehicles)
        {
            _world = world;
            _units = units;
            _vehicles = vehicles;
        }

        public ProductionOrder QueueProduction(int buildingId, string recipeId, int cycles, int priority)
        {
            if (!_world.Buildings.TryGetValue(buildingId, out var building)) return null;
            var recipe = OpenWorldDataCatalog.GetRecipe(recipeId);
            if (recipe == null || recipe.Building != building.Kind) return null;
            building.ActiveRecipeId = recipeId;
            if (building.AssignedWorkers <= 0) AssignWorkers(buildingId, recipe.Workers);
            _ordersDirty = true;
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
            _ordersDirty = true;
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

        public void TickEconomy()
        {
            TickOilExtraction();
            int clinics = 0;
            _lines.Clear();
            int activeRecipes = 0;
            int blockedRecipes = 0;
            string firstBlocked = "";

            if (_ordersDirty || _sortedOrders.Count != _world.ProductionOrders.Count)
            {
                _sortedOrders = new List<ProductionOrder>(_world.ProductionOrders);
                _sortedOrders.Sort((a, b) => b.Priority != a.Priority ? b.Priority.CompareTo(a.Priority) : a.Id.CompareTo(b.Id));
                _ordersDirty = false;
            }
            foreach (var order in _sortedOrders)
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
                if (_lines.Count < 10)
                    _lines.Add($"#{building.Id} {recipe.Id}: {(string.IsNullOrEmpty(blockedReason) ? "running" : blockedReason)} | {building.Storage.Total}/{building.StorageCapacity}");
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
                    if (_lines.Count < 8)
                        _lines.Add($"#{building.Id} Clinic: healed 1 from storage #{medicineSource.Id}");
                }
            }

            Summary = activeRecipes > 0
                ? $"Active recipes {activeRecipes}, blocked {blockedRecipes}"
                : blockedRecipes > 0 ? $"Production blocked x{blockedRecipes}: {firstBlocked}" : "Production idle";
            if (_lines.Count == 0)
                _lines.Add(Summary);
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

        public BuildingEntity FindInputStorage(BuildingEntity producer, ResourceKind kind, int amount)
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

        public void TickResearch()
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
