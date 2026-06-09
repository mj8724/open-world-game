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
        private float _economyTimer;
        private float _aiTimer;
        private int _aiStep;

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain, BuildingSystem buildings, UnitSystem units, BlueprintSystem blueprints)
        {
            _world = world;
            _terrain = terrain;
            _buildings = buildings;
            _units = units;
            _blueprints = blueprints;
        }

        private void Update()
        {
            if (_world == null) return;
            _economyTimer += Time.deltaTime;
            _aiTimer += Time.deltaTime;

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
        }

        private void TickEconomy()
        {
            int clinics = 0;
            var counts = new Dictionary<BuildableKind, int>();
            _productionLines.Clear();

            foreach (var building in _world.Buildings.Values)
            {
                if (building.FactionId != OpenWorldConstants.PlayerFactionId) continue;
                counts.TryGetValue(building.Kind, out int current);
                counts[building.Kind] = current + 1;
                if (building.Kind == BuildableKind.Clinic) clinics++;
            }

            int activeRecipes = 0;
            int blockedRecipes = 0;
            string firstBlocked = "";
            foreach (var recipe in OpenWorldDataCatalog.ProductionRecipes)
            {
                if (!OpenWorldDataCatalog.EraUnlocked(_world.Tech.Era, recipe.RequiredEra)) continue;
                counts.TryGetValue(recipe.Building, out int buildingCount);
                if (buildingCount <= 0) continue;

                int activeForRecipe = 0;
                int blockedForRecipe = 0;
                string blockedReason = "";
                for (int i = 0; i < buildingCount; i++)
                {
                    if (_world.Population.Workers < recipe.Workers)
                    {
                        blockedRecipes++;
                        blockedForRecipe++;
                        if (string.IsNullOrEmpty(blockedReason)) blockedReason = $"workers {_world.Population.Workers}/{recipe.Workers}";
                        if (string.IsNullOrEmpty(firstBlocked)) firstBlocked = $"{recipe.Id}: workers";
                        continue;
                    }

                    if (!OpenWorldDataCatalog.CanSpend(_world.Inventory, recipe.Inputs, out var missing))
                    {
                        blockedRecipes++;
                        blockedForRecipe++;
                        if (string.IsNullOrEmpty(blockedReason)) blockedReason = missing;
                        if (string.IsNullOrEmpty(firstBlocked)) firstBlocked = $"{recipe.Id}: {missing}";
                        continue;
                    }

                    OpenWorldDataCatalog.Spend(_world.Inventory, recipe.Inputs);
                    OpenWorldDataCatalog.Add(_world.Inventory, recipe.Outputs);
                    activeRecipes++;
                    activeForRecipe++;
                }

                if (_productionLines.Count < 6)
                {
                    string line = activeForRecipe > 0
                        ? $"{recipe.Building} {recipe.Id}: run {activeForRecipe}/{buildingCount} -> {FormatAmounts(recipe.Outputs)}"
                        : $"{recipe.Building} {recipe.Id}: blocked {blockedForRecipe}/{buildingCount} ({blockedReason})";
                    if (activeForRecipe > 0 && blockedForRecipe > 0)
                        line += $" / blocked {blockedForRecipe} ({blockedReason})";
                    _productionLines.Add(line);
                }
            }

            if (clinics > 0 && _world.Inventory.Medicine > 0 && _world.Population.Wounded > 0)
            {
                int healed = Mathf.Min(clinics, Mathf.Min(_world.Inventory.Medicine, _world.Population.Wounded));
                _world.Population.Wounded -= healed;
                _world.Inventory.Medicine -= healed;
                activeRecipes++;
                if (_productionLines.Count < 6)
                    _productionLines.Add($"Clinic medical: healed {healed}, medicine left {_world.Inventory.Medicine}");
            }

            ProductionSummary = activeRecipes > 0
                ? $"Active recipes {activeRecipes}, blocked {blockedRecipes}"
                : blockedRecipes > 0 ? $"Production blocked x{blockedRecipes}: {firstBlocked}" : "Production idle";
            if (_productionLines.Count == 0)
                _productionLines.Add(ProductionSummary);
        }

        private void TickResearch()
        {
            if (string.IsNullOrEmpty(_world.Tech.CurrentResearch))
            {
                ResearchSummary = "Research complete";
                return;
            }

            var tech = OpenWorldDataCatalog.GetTech(_world.Tech.CurrentResearch);
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

            OpenWorldDataCatalog.Spend(_world.Inventory, tech.CostPerTick);
            _world.Tech.ResearchProgress += Mathf.Max(1, _world.Population.Engineers);
            if (_world.Tech.ResearchProgress >= tech.ResearchTicks)
            {
                _world.Tech.Era = tech.UnlockEra;
                _world.Tech.CurrentResearch = tech.NextResearch;
                _world.Tech.ResearchProgress = 0;
                ResearchSummary = string.IsNullOrEmpty(tech.NextResearch)
                    ? $"Unlocked {tech.UnlockEra}; research complete"
                    : $"Unlocked {tech.UnlockEra}; next {tech.NextResearch}";
                return;
            }

            ResearchSummary = $"{tech.DisplayName} {_world.Tech.ResearchProgress}/{tech.ResearchTicks}";
        }

        private void TickMoraleMedical()
        {
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

            if (_aiStep % 2 == 0)
                _units.Spawn(UnitKind.Militia, pressureCell, OpenWorldConstants.EnemyFactionId);

            if (_aiStep % 3 == 0)
                _blueprints.QueueBuilding(BuildableKind.Roadblock, pressureCell + new Vector2Int(2, 0), OpenWorldConstants.EnemyFactionId, 2);

            PressureSummary = _aiStep % 2 == 0 ? "Enemy scouting pressure" : "Logistics stable";
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
