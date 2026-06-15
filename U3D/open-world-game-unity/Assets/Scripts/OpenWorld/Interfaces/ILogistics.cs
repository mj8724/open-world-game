using System.Collections.Generic;
using UnityEngine;

namespace OpenWorld
{
    public interface ILogistics
    {
        string LastStatus { get; }
        IReadOnlyList<string> RouteLines { get; }
        IReadOnlyList<string> VehicleLines { get; }

        LogisticsRoute EnsureStarterRoute(Vector2Int source, Vector2Int target);
        LogisticsRoute EnsureStarterRoute(int sourceBuildingId, int targetBuildingId);
        void TickNow();
        LogisticsRoute CreateRoute(int sourceBuildingId, int targetBuildingId, ResourceKind cargo, VehicleKind vehicleKind, int priority, LogisticsMode mode);
        void ToggleRouteMode(int routeId);
        void AdjustRoutePriority(int routeId, int delta);
        void AdjustRouteTargetStock(int routeId, int delta);
        void CycleRouteCargo(int routeId);
        void CompleteDeliveryIfArrived(VehicleAgent vehicle);
    }
}
