using UnityEngine;

namespace OpenWorld
{
    public class OpenWorldCommandSystem : MonoBehaviour
    {
        private OpenWorldState _world;
        private SurfaceTerrainSystem _terrain;
        private UnitSystem _units;
        private VehicleSystem _vehicles;
        private BlueprintSystem _blueprints;
        private OpenWorldLogisticsSystem _logistics;
        private OpenWorldGeologySystem _geology;
        private OpenWorldSimulationSystem _simulation;

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain, UnitSystem units, VehicleSystem vehicles, BlueprintSystem blueprints, OpenWorldLogisticsSystem logistics, OpenWorldGeologySystem geology, OpenWorldSimulationSystem simulation)
        {
            _world = world;
            _terrain = terrain;
            _units = units;
            _vehicles = vehicles;
            _blueprints = blueprints;
            _logistics = logistics;
            _geology = geology;
            _simulation = simulation;
        }

        private void Update()
        {
            if (_world == null) return;
            FlushPendingCommands();
        }

        public int FlushPendingCommands(int maxCommands = 64)
        {
            if (_world == null) return 0;
            int processed = 0;
            int guard = Mathf.Max(1, maxCommands);
            while (_world.Commands.Count > 0 && guard-- > 0)
            {
                Execute(_world.Commands.Dequeue());
                processed++;
            }
            return processed;
        }

        public void SubmitTerrain(TerrainTool tool, Vector2Int cell, int radius, bool queue)
        {
            var command = _world.EnqueueCommand(CommandKind.TerrainBrush, OpenWorldConstants.PlayerFactionId);
            command.TerrainTool = tool;
            command.Cell = cell;
            command.Priority = queue ? 4 : 3;
            command.EntityId = radius;
        }

        public void SubmitBuild(BuildableKind kind, Vector2Int cell)
        {
            var command = _world.EnqueueCommand(CommandKind.BuildBlueprint, OpenWorldConstants.PlayerFactionId);
            command.BuildKind = kind;
            command.Cell = cell;
            command.Priority = 5;
        }

        public void SubmitMoveSelected(Vector2Int cell)
        {
            var command = _world.EnqueueCommand(CommandKind.Move, OpenWorldConstants.PlayerFactionId);
            command.TargetCell = cell;
        }

        public void SubmitCancelBlueprint(Vector2Int cell)
        {
            var command = _world.EnqueueCommand(CommandKind.CancelBlueprint, OpenWorldConstants.PlayerFactionId);
            command.Cell = cell;
        }

        public void SubmitCancelBlueprint(int blueprintId)
        {
            var command = _world.EnqueueCommand(CommandKind.CancelBlueprint, OpenWorldConstants.PlayerFactionId);
            command.EntityId = blueprintId;
        }

        public void SubmitCancelAllBlueprints()
        {
            _world.EnqueueCommand(CommandKind.CancelAllBlueprints, OpenWorldConstants.PlayerFactionId);
        }

        public void SubmitBlueprintPriority(Vector2Int cell, int delta)
        {
            var command = _world.EnqueueCommand(CommandKind.SetBlueprintPriority, OpenWorldConstants.PlayerFactionId);
            command.Cell = cell;
            command.Priority = delta;
        }

        public void SubmitBlueprintPriority(int blueprintId, int delta)
        {
            var command = _world.EnqueueCommand(CommandKind.SetBlueprintPriority, OpenWorldConstants.PlayerFactionId);
            command.EntityId = blueprintId;
            command.Priority = delta;
        }

        public void SubmitPauseBlueprint(int blueprintId)
        {
            var command = _world.EnqueueCommand(CommandKind.PauseBlueprint, OpenWorldConstants.PlayerFactionId);
            command.EntityId = blueprintId;
        }

        public void SubmitResumeBlueprint(int blueprintId)
        {
            var command = _world.EnqueueCommand(CommandKind.ResumeBlueprint, OpenWorldConstants.PlayerFactionId);
            command.EntityId = blueprintId;
        }

        public void SubmitProduceVehicle(VehicleKind kind, Vector2Int cell)
        {
            var command = _world.EnqueueCommand(CommandKind.ProduceVehicle, OpenWorldConstants.PlayerFactionId);
            command.VehicleKind = kind;
            command.Cell = cell;
        }

        public void SubmitScout(Vector2Int cell)
        {
            var command = _world.EnqueueCommand(CommandKind.Scout, OpenWorldConstants.PlayerFactionId);
            command.TargetCell = cell;
            command.Priority = 4;
        }

        public void SubmitGeologicalSurvey(Vector2Int cell, int radius = 6)
        {
            var command = _world.EnqueueCommand(CommandKind.GeologicalSurvey, OpenWorldConstants.PlayerFactionId);
            command.TargetCell = cell;
            command.Amount = radius;
            command.Priority = 4;
        }

        public void SubmitCoreDrill(Vector2Int cell)
        {
            var command = _world.EnqueueCommand(CommandKind.CoreDrill, OpenWorldConstants.PlayerFactionId);
            command.TargetCell = cell;
            command.Priority = 4;
        }

        public void SubmitMiningZone(Vector2Int cell, int radius, GroundMaterial material)
        {
            var command = _world.EnqueueCommand(CommandKind.AssignMiningZone, OpenWorldConstants.PlayerFactionId);
            command.TargetCell = cell;
            command.Amount = radius;
            command.ResourceKind = ToResource(material);
            command.Priority = 4;
        }

        public void SubmitProduction(int buildingId, string recipeId, int cycles = 1)
        {
            var command = _world.EnqueueCommand(CommandKind.Produce, OpenWorldConstants.PlayerFactionId);
            command.EntityId = buildingId;
            command.Text = recipeId;
            command.Amount = cycles;
        }

        public void SubmitResearch(string techId)
        {
            var command = _world.EnqueueCommand(CommandKind.Research, OpenWorldConstants.PlayerFactionId);
            command.Text = techId;
        }

        public void SubmitTrainUnit(int barracksId, UnitKind kind)
        {
            var command = _world.EnqueueCommand(CommandKind.TrainUnit, OpenWorldConstants.PlayerFactionId);
            command.EntityId = barracksId;
            command.UnitKind = kind;
        }

        public void SubmitAttackSelected(Vector2Int cell, int targetEntityId = 0)
        {
            var command = _world.EnqueueCommand(CommandKind.Attack, OpenWorldConstants.PlayerFactionId);
            command.TargetCell = cell;
            command.EntityId = targetEntityId;
        }

        public void SubmitPatrolSelected(Vector2Int cell)
        {
            var command = _world.EnqueueCommand(CommandKind.Patrol, OpenWorldConstants.PlayerFactionId);
            command.TargetCell = cell;
        }

        public void SubmitDefenseArea(Vector2Int cell, int radius)
        {
            var command = _world.EnqueueCommand(CommandKind.SetDefenseArea, OpenWorldConstants.PlayerFactionId);
            command.TargetCell = cell;
            command.Amount = radius;
        }

        public void SubmitAssignWorkers(int buildingId, int workers)
        {
            var command = _world.EnqueueCommand(CommandKind.AssignWorkers, OpenWorldConstants.PlayerFactionId);
            command.EntityId = buildingId;
            command.Amount = workers;
        }

        public void SubmitDiplomacy(int factionId, DiplomacyStance stance)
        {
            var command = _world.EnqueueCommand(CommandKind.Diplomacy, OpenWorldConstants.PlayerFactionId);
            command.EntityId = factionId;
            command.Amount = (int)stance;
        }

        public void SubmitTrade(int factionId, ResourceKind exportKind, ResourceKind importKind, int amount = 5)
        {
            var command = _world.EnqueueCommand(CommandKind.Trade, OpenWorldConstants.PlayerFactionId);
            command.EntityId = factionId;
            command.ResourceKind = exportKind;
            command.Text = importKind.ToString();
            command.Amount = amount;
            command.Priority = 3;
        }

        public void SubmitLoadSelected(ResourceKind cargo)
        {
            var command = _world.EnqueueCommand(CommandKind.LoadCargo, OpenWorldConstants.PlayerFactionId);
            command.ResourceKind = cargo;
        }

        public void SubmitUnloadSelected()
        {
            _world.EnqueueCommand(CommandKind.UnloadCargo, OpenWorldConstants.PlayerFactionId);
        }

        public void SubmitVehicleService(bool refuel, bool repair)
        {
            var command = _world.EnqueueCommand(refuel ? CommandKind.RefuelVehicle : CommandKind.RepairVehicle, OpenWorldConstants.PlayerFactionId);
            command.Amount = repair ? 1 : 0;
        }

        public void SubmitToggleRouteMode(int routeId)
        {
            var command = _world.EnqueueCommand(CommandKind.ToggleRouteMode, OpenWorldConstants.PlayerFactionId);
            command.EntityId = routeId;
        }

        public void SubmitRoutePriority(int routeId, int delta)
        {
            var command = _world.EnqueueCommand(CommandKind.AdjustRoutePriority, OpenWorldConstants.PlayerFactionId);
            command.EntityId = routeId;
            command.Priority = delta;
        }

        public void SubmitRouteTargetStock(int routeId, int delta)
        {
            var command = _world.EnqueueCommand(CommandKind.AdjustRouteTargetStock, OpenWorldConstants.PlayerFactionId);
            command.EntityId = routeId;
            command.Amount = delta;
        }

        public void SubmitCycleRouteCargo(int routeId)
        {
            var command = _world.EnqueueCommand(CommandKind.CycleRouteCargo, OpenWorldConstants.PlayerFactionId);
            command.EntityId = routeId;
        }

        public void SubmitCreateRoute(int sourceBuildingId, int targetBuildingId, ResourceKind cargo, VehicleKind vehicleKind, int priority = 3)
        {
            var command = _world.EnqueueCommand(CommandKind.CreateRoute, OpenWorldConstants.PlayerFactionId);
            command.EntityId = sourceBuildingId;
            command.SecondaryEntityId = targetBuildingId;
            command.ResourceKind = cargo;
            command.VehicleKind = vehicleKind;
            command.Priority = priority;
        }

        private void Execute(OpenWorldCommand command)
        {
            switch (command.Kind)
            {
                case CommandKind.TerrainBrush:
                    if (command.Priority >= 4)
                        _blueprints.QueueTerrain(command.TerrainTool, command.Cell, command.EntityId, command.FactionId, command.Priority);
                    else
                        _terrain.ApplyBrush(command.TerrainTool, command.Cell, command.EntityId, 0.5f);
                    break;
                case CommandKind.BuildBlueprint:
                    _blueprints.QueueBuilding(command.BuildKind, command.Cell, command.FactionId, command.Priority);
                    break;
                case CommandKind.Move:
                    _units.MoveSelected(command.TargetCell);
                    _vehicles.MoveSelected(command.TargetCell);
                    break;
                case CommandKind.CancelBlueprint:
                    if (command.EntityId > 0) _blueprints.CancelById(command.EntityId);
                    else _blueprints.CancelNearest(command.Cell);
                    break;
                case CommandKind.CancelAllBlueprints:
                    _blueprints.CancelAll();
                    break;
                case CommandKind.SetBlueprintPriority:
                    if (command.EntityId > 0) _blueprints.AdjustPriorityById(command.EntityId, command.Priority);
                    else _blueprints.AdjustNearestPriority(command.Cell, command.Priority);
                    break;
                case CommandKind.PauseBlueprint:
                    _blueprints.Pause(command.EntityId);
                    break;
                case CommandKind.ResumeBlueprint:
                    _blueprints.Resume(command.EntityId);
                    break;
                case CommandKind.ProduceVehicle:
                    _vehicles.Spawn(command.VehicleKind, command.Cell, command.FactionId);
                    break;
                case CommandKind.Scout:
                    _units.MoveBestScoutTo(command.TargetCell);
                    break;
                case CommandKind.LoadCargo:
                    _vehicles.LoadSelected(command.ResourceKind);
                    break;
                case CommandKind.UnloadCargo:
                    _vehicles.UnloadSelected();
                    break;
                case CommandKind.RefuelVehicle:
                    _vehicles.QueueServiceForSelected(true, command.Amount > 0);
                    break;
                case CommandKind.RepairVehicle:
                    _vehicles.QueueServiceForSelected(false, true);
                    break;
                case CommandKind.ToggleRouteMode:
                    _logistics.ToggleRouteMode(command.EntityId);
                    break;
                case CommandKind.AdjustRoutePriority:
                    _logistics.AdjustRoutePriority(command.EntityId, command.Priority);
                    break;
                case CommandKind.AdjustRouteTargetStock:
                    _logistics.AdjustRouteTargetStock(command.EntityId, command.Amount);
                    break;
                case CommandKind.CycleRouteCargo:
                    _logistics.CycleRouteCargo(command.EntityId);
                    break;
                case CommandKind.CreateRoute:
                    _logistics.CreateRoute(command.EntityId, command.SecondaryEntityId, command.ResourceKind, command.VehicleKind, command.Priority, LogisticsMode.Automatic);
                    break;
                case CommandKind.GeologicalSurvey:
                    _geology.QueueSurvey(command.TargetCell, command.Amount, command.Priority);
                    break;
                case CommandKind.CoreDrill:
                    _geology.QueueCoreDrill(command.TargetCell, command.Priority);
                    break;
                case CommandKind.AssignMiningZone:
                    _geology.AssignMiningZone(command.TargetCell, command.Amount, ToMaterial(command.ResourceKind), command.Priority);
                    break;
                case CommandKind.Produce:
                    _simulation.QueueProduction(command.EntityId, command.Text, command.Amount, command.Priority);
                    break;
                case CommandKind.Research:
                    _simulation.QueueResearch(command.Text, command.Priority);
                    break;
                case CommandKind.TrainUnit:
                    _simulation.QueueUnitTraining(command.EntityId, command.UnitKind, command.Priority);
                    break;
                case CommandKind.Attack:
                    _simulation.DeclareHostilityForTarget(command.EntityId);
                    _units.AttackSelected(command.TargetCell, command.EntityId);
                    break;
                case CommandKind.Patrol:
                    _units.PatrolSelected(command.TargetCell);
                    break;
                case CommandKind.SetDefenseArea:
                    _units.DefendSelected(command.TargetCell, command.Amount);
                    break;
                case CommandKind.AssignWorkers:
                    _simulation.AssignWorkers(command.EntityId, command.Amount);
                    break;
                case CommandKind.Diplomacy:
                    _simulation.SetDiplomacy(command.EntityId, (DiplomacyStance)command.Amount);
                    break;
                case CommandKind.Trade:
                    if (System.Enum.TryParse(command.Text, out ResourceKind importKind))
                        _simulation.QueueTrade(command.EntityId, command.ResourceKind, importKind, command.Amount, command.Priority);
                    break;
            }
        }

        private static ResourceKind ToResource(GroundMaterial material) => material switch
        {
            GroundMaterial.Dirt => ResourceKind.Dirt,
            GroundMaterial.Stone => ResourceKind.Stone,
            GroundMaterial.IronOre => ResourceKind.IronOre,
            GroundMaterial.Coal => ResourceKind.Coal,
            GroundMaterial.Clay => ResourceKind.Clay,
            GroundMaterial.Wood => ResourceKind.Wood,
            GroundMaterial.Food => ResourceKind.Food,
            GroundMaterial.Sulfur => ResourceKind.Sulfur,
            GroundMaterial.Nitrate => ResourceKind.Nitrate,
            GroundMaterial.Oil => ResourceKind.Oil,
            _ => ResourceKind.Stone
        };

        private static GroundMaterial ToMaterial(ResourceKind resource) => resource switch
        {
            ResourceKind.IronOre => GroundMaterial.IronOre,
            ResourceKind.Coal => GroundMaterial.Coal,
            ResourceKind.Clay => GroundMaterial.Clay,
            ResourceKind.Sulfur => GroundMaterial.Sulfur,
            ResourceKind.Nitrate => GroundMaterial.Nitrate,
            ResourceKind.Oil => GroundMaterial.Oil,
            ResourceKind.Dirt => GroundMaterial.Dirt,
            _ => GroundMaterial.Stone
        };
    }
}
