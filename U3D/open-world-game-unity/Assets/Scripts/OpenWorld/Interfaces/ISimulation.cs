using System.Collections.Generic;

namespace OpenWorld
{
    public interface ISimulation
    {
        string PressureSummary { get; }
        string ProductionSummary { get; }
        IReadOnlyList<string> ProductionLines { get; }
        string ResearchSummary { get; }
        string DiplomacySummary { get; }
        float UnityProgress { get; }
        bool GameOver { get; }
        string GameOverText { get; }
        bool IsVictory { get; }

        void TickEconomyNow();
        ProductionOrder QueueProduction(int buildingId, string recipeId, int cycles, int priority);
        ResearchOrder QueueResearch(string techId, int priority);
        ProductionOrder QueueUnitTraining(int barracksId, UnitKind kind, int priority);
        void AssignWorkers(int buildingId, int workers);
        void SetDiplomacy(int factionId, DiplomacyStance stance);
        TradeContract QueueTrade(int partnerFactionId, ResourceKind exportKind, ResourceKind importKind, int amount, int priority);
        void DeclareHostilityForTarget(int targetEntityId);
    }
}
