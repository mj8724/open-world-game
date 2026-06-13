using UnityEngine;

namespace OpenWorld.Testing
{
    /// <summary>
    /// Diagnostic warning system that logs design flaws in red to expose issues.
    /// Triggered when the bot simulation encounters deadlocks, starvation, or imbalances.
    /// </summary>
    public static class DiagnosticProbes
    {
        private static readonly string RedColor = "<color=red>";
        private static readonly string EndColor = "</color>";

        public static void LogWorkerStarvation(int factionId, int workerCount, int blueprintCount)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Faction {factionId} Worker Starvation: " +
                           $"{workerCount} workers all busy, {blueprintCount} blueprints queued for 30s+. " +
                           $"Design issue: need worker training or auto-reassignment system.{EndColor}");
        }

        public static void LogFoodShortage(int factionId, int currentFood, int population)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Faction {factionId} Food Crisis: " +
                           $"{currentFood} food for {population} population (ratio: {(float)currentFood / population:F1}). " +
                           $"Design issue: food production rate too low or consumption too high.{EndColor}");
        }

        public static void LogResourceDeadlock(int factionId, BuildableKind blockedBuilding, ResourceInventory inventory)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Faction {factionId} Resource Deadlock: " +
                           $"Cannot build {blockedBuilding} for 5+ attempts. " +
                           $"Current resources: Wood={inventory.Get(ResourceKind.Wood)}, " +
                           $"Stone={inventory.Get(ResourceKind.Stone)}, IronOre={inventory.Get(ResourceKind.IronOre)}. " +
                           $"Design issue: resource generation bottleneck or excessive cost.{EndColor}");
        }

        public static void LogLogisticsDeadlock(int factionId, int blockedCount, int totalRoutes)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Faction {factionId} Logistics Congestion: " +
                           $"{blockedCount}/{totalRoutes} routes stuck for 30s+. " +
                           $"Design issue: pathfinding failure, vehicle collision, or route overload.{EndColor}");
        }

        public static void LogProductionStall(int factionId, BuildableKind building, string missingResource)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Faction {factionId} Production Stalled: " +
                           $"{building} cannot produce, missing: {missingResource}. " +
                           $"Design issue: input resource chain broken or recipe imbalanced.{EndColor}");
        }

        public static void LogCombatImbalance(int faction1, int faction2, int units1, int units2, float ratio)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Combat Imbalance: " +
                           $"Faction {faction1} has {units1} units vs Faction {faction2} with {units2} units (ratio {ratio:F1}x). " +
                           $"Design issue: snowball effect too strong or unit training too slow.{EndColor}");
        }

        public static void LogAmmoStarvation(int factionId, int rangedUnits, int unitsOutOfAmmo)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Faction {factionId} Ammo Starvation: " +
                           $"{unitsOutOfAmmo}/{rangedUnits} ranged units out of ammo. " +
                           $"Design issue: ammo production insufficient or resupply system missing.{EndColor}");
        }

        public static void LogStorageOverflow(int factionId, ResourceKind resource, int amount, int capacity)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Faction {factionId} Storage Overflow: " +
                           $"{resource} at {amount}/{capacity} ({(float)amount / capacity * 100:F0}% full). " +
                           $"Design issue: overproduction or logistics not distributing resources.{EndColor}");
        }

        public static void LogConstructionDelay(int factionId, BuildableKind building, float elapsedTime)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Faction {factionId} Construction Delay: " +
                           $"{building} blueprint active for {elapsedTime:F0}s without completion. " +
                           $"Design issue: insufficient workers or construction speed too slow.{EndColor}");
        }

        public static void LogVictoryConditionStall(float gameTime)
        {
            Debug.LogWarning($"{RedColor}[PROBE] Victory Condition Stalemate: " +
                           $"Game running for {gameTime:F0}s with no decisive outcome. " +
                           $"Design issue: victory conditions too strict or combat ineffective.{EndColor}");
        }
    }
}
