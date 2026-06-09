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

        public void Initialize(OpenWorldState world, SurfaceTerrainSystem terrain, UnitSystem units, VehicleSystem vehicles, BlueprintSystem blueprints)
        {
            _world = world;
            _terrain = terrain;
            _units = units;
            _vehicles = vehicles;
            _blueprints = blueprints;
        }

        private void Update()
        {
            if (_world == null) return;
            int guard = 64;
            while (_world.Commands.Count > 0 && guard-- > 0)
            {
                Execute(_world.Commands.Dequeue());
            }
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

        public void SubmitLoadSelected(ResourceKind cargo)
        {
            var command = _world.EnqueueCommand(CommandKind.LoadCargo, OpenWorldConstants.PlayerFactionId);
            command.ResourceKind = cargo;
        }

        public void SubmitUnloadSelected()
        {
            _world.EnqueueCommand(CommandKind.UnloadCargo, OpenWorldConstants.PlayerFactionId);
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
            }
        }
    }
}
