using System.Linq;
using UnityEngine;

namespace OpenWorld.Testing
{
    /// <summary>
    /// Additional monitoring system that runs in the simulation to detect broader design issues.
    /// Attached to the same GameObject as TestBotManager to run passive checks.
    /// </summary>
    public class TestMonitor : MonoBehaviour
    {
        private OpenWorldState _world;
        private OpenWorldSimulationSystem _simulation;

        private float _checkTimer;
        private const float CheckInterval = 5f; // Check every 5s
        private float _gameStartTime;

        public void Initialize(OpenWorldState world, OpenWorldSimulationSystem simulation)
        {
            _world = world;
            _simulation = simulation;
            _gameStartTime = Time.time;
        }

        private void Update()
        {
            if (_world == null) return;

            _checkTimer += Time.deltaTime;
            if (_checkTimer < CheckInterval) return;
            _checkTimer = 0f;

            CheckCombatImbalance();
            CheckAmmoStarvation();
            CheckConstructionDelays();
            CheckVictoryStalemate();
            CheckProductionStalls();
        }

        private void CheckCombatImbalance()
        {
            var player1Units = 0;
            var player2Units = 0;

            foreach (var unit in _world.Units.Values)
            {
                if (unit.Hp <= 0) continue;
                if (unit.FactionId == OpenWorldConstants.PlayerFactionId) player1Units++;
                else if (unit.FactionId == OpenWorldConstants.EnemyFactionId) player2Units++;
            }

            if (player1Units == 0 || player2Units == 0) return;

            var ratio = Mathf.Max((float)player1Units / player2Units, (float)player2Units / player1Units);
            if (ratio >= 3f)
            {
                var stronger = player1Units > player2Units ? OpenWorldConstants.PlayerFactionId : OpenWorldConstants.EnemyFactionId;
                var weaker = player1Units > player2Units ? OpenWorldConstants.EnemyFactionId : OpenWorldConstants.PlayerFactionId;
                DiagnosticProbes.LogCombatImbalance(stronger, weaker, Mathf.Max(player1Units, player2Units),
                    Mathf.Min(player1Units, player2Units), ratio);
            }
        }

        private void CheckAmmoStarvation()
        {
            foreach (var factionId in new[] { OpenWorldConstants.PlayerFactionId, OpenWorldConstants.EnemyFactionId })
            {
                var rangedUnits = 0;
                var outOfAmmo = 0;

                foreach (var unit in _world.Units.Values)
                {
                    if (unit.FactionId != factionId || unit.Hp <= 0) continue;

                    var unitDef = OpenWorldDataCatalog.GetUnit(unit.Kind);
                    if (unitDef != null && unitDef.IsRanged)
                    {
                        rangedUnits++;
                        if (unit.Ammo <= 0) outOfAmmo++;
                    }
                }

                if (rangedUnits > 0 && outOfAmmo >= rangedUnits / 2)
                {
                    DiagnosticProbes.LogAmmoStarvation(factionId, rangedUnits, outOfAmmo);
                }
            }
        }

        private void CheckConstructionDelays()
        {
            foreach (var blueprint in _world.Blueprints)
            {
                // Blueprint doesn't have timestamp - simplified check
                if (blueprint.Status == BlueprintStatus.Active && blueprint.WorkRemaining > 1.5f)
                {
                    DiagnosticProbes.LogConstructionDelay(blueprint.FactionId, blueprint.BuildKind, blueprint.WorkRemaining * 30f);
                }
            }
        }

        private void CheckVictoryStalemate()
        {
            var gameTime = Time.time - _gameStartTime;
            if (gameTime < 600f) return; // Only check after 10 minutes

            // Check if both factions still have viable bases
            var player1Buildings = 0;
            var player2Buildings = 0;

            foreach (var building in _world.Buildings.Values)
            {
                if (building.Kind == BuildableKind.TownCenter)
                {
                    if (building.FactionId == OpenWorldConstants.PlayerFactionId) player1Buildings++;
                    else if (building.FactionId == OpenWorldConstants.EnemyFactionId) player2Buildings++;
                }
            }

            if (player1Buildings > 0 && player2Buildings > 0)
            {
                DiagnosticProbes.LogVictoryConditionStall(gameTime);
            }
        }

        private void CheckProductionStalls()
        {
            // Simplified - jobs don't have direct recipe mapping
            foreach (var order in _world.ProductionOrders)
            {
                {
                    if (order.Status == "Working" || order.Status == "Waiting" ||
                        order.Status.StartsWith("Workers") || order.Status.StartsWith("Trained") ||
                        order.Status.StartsWith("Produced"))
                        continue;
                }

                var building = _world.Buildings.Values.FirstOrDefault(b => b.Id == order.BuildingId);
                if (building == null) continue;

                DiagnosticProbes.LogProductionStall(building.FactionId, building.Kind, order.Status);
            }
        }
    }
}
