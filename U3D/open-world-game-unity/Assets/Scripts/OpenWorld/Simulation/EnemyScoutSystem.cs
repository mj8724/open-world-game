using UnityEngine;
using System.Collections.Generic;

namespace OpenWorld
{
    public class EnemyScoutSystem
    {
        private readonly List<Vector2Int> _discoveredResources = new List<Vector2Int>();
        private readonly List<Vector2Int> _scoutedCells = new List<Vector2Int>();
        public int ThreatLevel { get; private set; }

        public IReadOnlyList<Vector2Int> DiscoveredResources => _discoveredResources;

        public void TickScouting(UnitSystem units, OpenWorldState world)
        {
            int scoutCount = 0;
            foreach (var unit in world.Units.Values)
            {
                if (unit.FactionId == OpenWorldConstants.EnemyFactionId && unit.Kind == UnitKind.Scout)
                {
                    scoutCount++;
                    var agent = units.GetAgent(unit.Id);
                    if (agent != null && unit.Task == UnitTask.Idle)
                    {
                        var cell = FindUnexploredCell(world);
                        if (cell.HasValue)
                        {
                            agent.IssueOrder(new UnitOrder { Kind = UnitOrderKind.Move, TargetCell = cell.Value, Priority = 2 });
                            _scoutedCells.Add(cell.Value);
                        }
                    }

                    for (int z = -2; z <= 2; z++)
                    {
                        for (int x = -2; x <= 2; x++)
                        {
                            var check = unit.Cell + new Vector2Int(x, z);
                            if (!world.InBounds(check)) continue;
                            var surface = world.GetCell(check);
                            if (surface.ResourceRichness >= 2 && !_discoveredResources.Contains(check))
                            {
                                _discoveredResources.Add(check);
                            }
                            if (surface.Occupied && surface.BuildingId > 0)
                            {
                                if (world.Buildings.TryGetValue(surface.BuildingId, out var b) && b.FactionId == OpenWorldConstants.PlayerFactionId)
                                {
                                    ThreatLevel = Mathf.Min(3, ThreatLevel + 1);
                                }
                            }
                        }
                    }
                }
            }
            
            if (scoutCount < 2)
            {
                BuildingEntity tc = null;
                foreach (var b in world.Buildings.Values)
                {
                    if (b.FactionId == OpenWorldConstants.EnemyFactionId && b.Kind == BuildableKind.TownCenter)
                    {
                        tc = b;
                        break;
                    }
                }
                if (tc != null)
                {
                    units.Spawn(UnitKind.Scout, tc.Origin + new Vector2Int(0, -2), OpenWorldConstants.EnemyFactionId);
                }
            }
        }

        private Vector2Int? FindUnexploredCell(OpenWorldState world)
        {
            int halfMap = world.MapSize / 2;
            for (int i = 0; i < 50; i++)
            {
                var cell = new Vector2Int(Random.Range(halfMap + 10, world.MapSize), Random.Range(0, world.MapSize));
                if (!_scoutedCells.Contains(cell) && world.GetCell(cell).MoveCost < 9999f)
                {
                    return cell;
                }
            }
            return null;
        }
    }
}
