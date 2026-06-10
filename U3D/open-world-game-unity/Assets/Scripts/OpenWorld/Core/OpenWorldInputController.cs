using UnityEngine;

namespace OpenWorld
{
    public class OpenWorldInputController : MonoBehaviour
    {
        public TerrainTool CurrentTool { get; private set; } = TerrainTool.Dig;
        public BuildableKind CurrentBuildable { get; private set; } = BuildableKind.Wall;
        public VehicleKind CurrentVehicle { get; private set; } = VehicleKind.HandCart;
        public int BrushRadius { get; private set; } = 1;
        public bool StrategicMapOpen { get; private set; } = true;

        private Camera _camera;
        private SurfaceTerrainSystem _terrain;
        private BuildingSystem _buildings;
        private UnitSystem _units;
        private VehicleSystem _vehicles;
        private OpenWorldJobSystem _jobs;
        private OpenWorldCommandSystem _commands;
        private OpenWorldStrategicMapController _strategicMap;
        private WorldKnowledgeSystem _knowledge;

        private Vector2 _dragStart;
        private bool _dragging;

        public void Initialize(Camera cam, SurfaceTerrainSystem terrain, BuildingSystem buildings, UnitSystem units, VehicleSystem vehicles, OpenWorldJobSystem jobs, OpenWorldCommandSystem commands, OpenWorldStrategicMapController strategicMap, WorldKnowledgeSystem knowledge)
        {
            _camera = cam;
            _terrain = terrain;
            _buildings = buildings;
            _units = units;
            _vehicles = vehicles;
            _jobs = jobs;
            _commands = commands;
            _strategicMap = strategicMap;
            _knowledge = knowledge;
        }

        public void SetStrategicMap(OpenWorldStrategicMapController strategicMap)
        {
            _strategicMap = strategicMap;
            if (_strategicMap != null)
                _strategicMap.SetOpen(StrategicMapOpen);
        }

        public void SetBuildable(BuildableKind kind)
        {
            CurrentTool = TerrainTool.None;
            CurrentBuildable = kind;
        }

        public void SetTerrainTool(TerrainTool tool)
        {
            CurrentTool = tool;
        }

        private void Update()
        {
            HandleHotkeys();
            HandleMouse();
        }

        private void HandleHotkeys()
        {
            var keyboard = OpenWorldInput.Keyboard;
            if (keyboard == null) return;

            if (OpenWorldInput.Pressed(keyboard.digit1Key)) CurrentTool = TerrainTool.Dig;
            if (OpenWorldInput.Pressed(keyboard.digit2Key)) CurrentTool = TerrainTool.Fill;
            if (OpenWorldInput.Pressed(keyboard.digit3Key)) CurrentTool = TerrainTool.Flatten;
            if (OpenWorldInput.Pressed(keyboard.digit4Key)) CurrentTool = TerrainTool.Ramp;
            if (OpenWorldInput.Pressed(keyboard.digit5Key)) CurrentTool = TerrainTool.Road;
            if (OpenWorldInput.Pressed(keyboard.digit6Key)) CurrentTool = TerrainTool.Trench;
            if (OpenWorldInput.Pressed(keyboard.digit7Key)) CurrentTool = TerrainTool.Rail;
            if (OpenWorldInput.Pressed(keyboard.digit8Key)) CurrentTool = TerrainTool.Bridge;
            if (OpenWorldInput.Pressed(keyboard.digit9Key)) CurrentTool = TerrainTool.Mine;
            if (OpenWorldInput.Pressed(keyboard.bKey)) CurrentTool = TerrainTool.None;
            if (OpenWorldInput.Pressed(keyboard.mKey)) ToggleStrategicMap();
            if (OpenWorldInput.Pressed(keyboard.oKey)) _knowledge?.NextOverlay();
            if (OpenWorldInput.Pressed(keyboard.leftBracketKey)) BrushRadius = Mathf.Max(0, BrushRadius - 1);
            if (OpenWorldInput.Pressed(keyboard.rightBracketKey)) BrushRadius = Mathf.Min(8, BrushRadius + 1);

            if (OpenWorldInput.Pressed(keyboard.f1Key)) CurrentBuildable = BuildableKind.Wall;
            if (OpenWorldInput.Pressed(keyboard.f2Key)) CurrentBuildable = BuildableKind.Tower;
            if (OpenWorldInput.Pressed(keyboard.f3Key)) CurrentBuildable = BuildableKind.Warehouse;
            if (OpenWorldInput.Pressed(keyboard.f4Key)) CurrentBuildable = BuildableKind.Barracks;
            if (OpenWorldInput.Pressed(keyboard.f5Key)) CurrentBuildable = BuildableKind.Farm;
            if (OpenWorldInput.Pressed(keyboard.f6Key)) CurrentBuildable = BuildableKind.Smelter;
            if (OpenWorldInput.Pressed(keyboard.f7Key)) CurrentBuildable = BuildableKind.VehicleFactory;
            if (OpenWorldInput.Pressed(keyboard.f8Key)) CurrentBuildable = BuildableKind.ScoutTower;

            if (OpenWorldInput.Pressed(keyboard.vKey)) CurrentVehicle = NextVehicle(CurrentVehicle);
            if (OpenWorldInput.Pressed(keyboard.lKey)) _commands.SubmitLoadSelected(ResourceKind.Food);
            if (OpenWorldInput.Pressed(keyboard.uKey)) _commands.SubmitUnloadSelected();

            if (OpenWorldInput.Pressed(keyboard.tKey)) I18nSystem.ToggleLanguage();
        }

        private void HandleMouse()
        {
            var mouse = OpenWorldInput.Mouse;
            if (_camera == null || _terrain == null || mouse == null) return;

            Vector2 pointerPosition = OpenWorldInput.PointerPosition;

            if (OpenWorldInput.Pressed(mouse.leftButton))
            {
                _dragStart = pointerPosition;
                _dragging = true;
            }

            if (OpenWorldInput.Released(mouse.leftButton))
            {
                _dragging = false;
                if ((_dragStart - pointerPosition).sqrMagnitude > 64f)
                {
                    SelectUnitsInScreenRect(_dragStart, pointerPosition);
                    return;
                }

                if (_terrain.TryRaycastCell(_camera, pointerPosition, out var cell))
                {
                    var keyboard = OpenWorldInput.Keyboard;
                    if (keyboard != null && OpenWorldInput.Held(keyboard.gKey))
                    {
                        _commands.SubmitGeologicalSurvey(cell, 6);
                        return;
                    }
                    if (keyboard != null && OpenWorldInput.Held(keyboard.kKey))
                    {
                        _commands.SubmitCoreDrill(cell);
                        return;
                    }
                    if (keyboard != null && OpenWorldInput.Held(keyboard.xKey))
                    {
                        _commands.SubmitCancelBlueprint(cell);
                        return;
                    }
                    if (keyboard != null && OpenWorldInput.Held(keyboard.equalsKey))
                    {
                        _commands.SubmitBlueprintPriority(cell, 1);
                        return;
                    }
                    if (keyboard != null && OpenWorldInput.Held(keyboard.minusKey))
                    {
                        _commands.SubmitBlueprintPriority(cell, -1);
                        return;
                    }
                    if (keyboard != null && OpenWorldInput.Held(keyboard.vKey))
                    {
                        _commands.SubmitProduceVehicle(CurrentVehicle, cell);
                        return;
                    }

                    if (CurrentTool == TerrainTool.None)
                    {
                        _commands.SubmitBuild(CurrentBuildable, cell);
                    }
                    else if (OpenWorldInput.ShiftHeld)
                    {
                        _commands.SubmitTerrain(CurrentTool, cell, BrushRadius, queue: true);
                    }
                    else
                    {
                        _commands.SubmitTerrain(CurrentTool, cell, BrushRadius, queue: false);
                    }
                }
            }

            if (OpenWorldInput.Pressed(mouse.rightButton))
            {
                if (_terrain.TryRaycastCell(_camera, pointerPosition, out var cell))
                {
                    var keyboard = OpenWorldInput.Keyboard;
                    if (keyboard != null && OpenWorldInput.Held(keyboard.aKey)) _commands.SubmitAttackSelected(cell);
                    else if (keyboard != null && OpenWorldInput.Held(keyboard.pKey)) _commands.SubmitPatrolSelected(cell);
                    else if (keyboard != null && OpenWorldInput.Held(keyboard.dKey)) _commands.SubmitDefenseArea(cell, BrushRadius);
                    else _commands.SubmitMoveSelected(cell);
                }
            }
        }

        private void SelectUnitsInScreenRect(Vector2 a, Vector2 b)
        {
            Vector2 min = Vector2.Min(a, b);
            Vector2 max = Vector2.Max(a, b);
            var bounds = new Bounds();
            bool initialized = false;

            foreach (var agent in _units.AllAgents())
            {
                Vector3 screen = _camera.WorldToScreenPoint(agent.transform.position);
                if (screen.z < 0) continue;
                if (screen.x >= min.x && screen.x <= max.x && screen.y >= min.y && screen.y <= max.y)
                {
                    if (!initialized)
                    {
                        bounds = new Bounds(agent.transform.position, Vector3.one);
                        initialized = true;
                    }
                    else bounds.Encapsulate(agent.transform.position);
                }
            }

            foreach (var agent in _vehicles.AllAgents())
            {
                Vector3 screen = _camera.WorldToScreenPoint(agent.transform.position);
                if (screen.z < 0) continue;
                if (screen.x >= min.x && screen.x <= max.x && screen.y >= min.y && screen.y <= max.y)
                {
                    if (!initialized)
                    {
                        bounds = new Bounds(agent.transform.position, Vector3.one);
                        initialized = true;
                    }
                    else bounds.Encapsulate(agent.transform.position);
                }
            }

            if (initialized)
            {
                _units.SelectInBounds(bounds);
                _vehicles.SelectInBounds(bounds, append: true);
            }
            else
            {
                _units.ClearSelection();
                _vehicles.ClearSelection();
            }
        }

        private void ToggleStrategicMap()
        {
            StrategicMapOpen = !StrategicMapOpen;
            if (_strategicMap != null)
                _strategicMap.SetOpen(StrategicMapOpen);
        }

        private static VehicleKind NextVehicle(VehicleKind current) => current switch
        {
            VehicleKind.HandCart => VehicleKind.Wagon,
            VehicleKind.Wagon => VehicleKind.Truck,
            VehicleKind.Truck => VehicleKind.ArmoredCar,
            VehicleKind.ArmoredCar => VehicleKind.HandCart,
            _ => VehicleKind.HandCart
        };

        private void OnGUI()
        {
            if (!_dragging) return;
            var current = OpenWorldInput.PointerPosition;
            var min = Vector2.Min(_dragStart, current);
            var max = Vector2.Max(_dragStart, current);
            var rect = new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
            GUI.Box(rect, "");
        }
    }
}
