using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace OpenWorld
{
    [RequireComponent(typeof(UIDocument))]
    public class OpenWorldHudController : MonoBehaviour
    {
        private OpenWorldState _world;
        private OpenWorldInputController _input;
        private UnitSystem _units;
        private VehicleSystem _vehicles;
        private WorldKnowledgeSystem _knowledge;
        private OpenWorldLogisticsSystem _logistics;
        private OpenWorldSimulationSystem _simulation;
        private OpenWorldCommandSystem _commands;

        private Label _fpsLabel;
        private Label _mapLabel;
        private Label _resFood;
        private Label _resWood;
        private Label _resStone;
        private Label _resOre;
        private Label _resCoal;
        private Label _resIron;
        private Label _resSteel;
        private Label _resParts;
        private Label _resRail;
        private Label _resPowder;
        private Label _resFuel;
        private Label _resAmmo;
        private Label _entityLabel;
        private Label _toolLabel;
        private Label _systemsLabel;
        private Label _logisticsLabel;
        private Label _goalLabel;
        private Label _controlsLabel;
        private Button _saveButton;
        private Button _cancelAllBlueprintsButton;
        private VisualElement _statusPanel;
        private VisualElement _productionList;
        private VisualElement _transportList;
        private VisualElement _blueprintList;

        private float _fps;
        private float _accum;
        private int _frames;
        private float _timeLeft = 0.5f;

        public void Initialize(OpenWorldState world, OpenWorldInputController input, UnitSystem units, VehicleSystem vehicles, WorldKnowledgeSystem knowledge, OpenWorldLogisticsSystem logistics, OpenWorldSimulationSystem simulation, OpenWorldCommandSystem commands)
        {
            _world = world;
            _input = input;
            _units = units;
            _vehicles = vehicles;
            _knowledge = knowledge;
            _logistics = logistics;
            _simulation = simulation;
            _commands = commands;
            BindDocument();
            Refresh();
        }

        private void Awake()
        {
            BindDocument();
        }

        private void Update()
        {
            UpdateFps();

            var keyboard = OpenWorldInput.Keyboard;
            if (keyboard != null && OpenWorldInput.Pressed(keyboard.f9Key) && _world != null)
                OpenWorldSaveService.Save(_world);

            Refresh();
        }

        private void BindDocument()
        {
            var document = GetComponent<UIDocument>();
            var root = document != null ? document.rootVisualElement : null;
            if (root == null) return;

            _fpsLabel = root.Q<Label>("fps-value");
            _mapLabel = root.Q<Label>("map-value");
            _resFood = root.Q<Label>("res-food");
            _resWood = root.Q<Label>("res-wood");
            _resStone = root.Q<Label>("res-stone");
            _resOre = root.Q<Label>("res-ore");
            _resCoal = root.Q<Label>("res-coal");
            _resIron = root.Q<Label>("res-iron");
            _resSteel = root.Q<Label>("res-steel");
            _resParts = root.Q<Label>("res-parts");
            _resRail = root.Q<Label>("res-rail");
            _resPowder = root.Q<Label>("res-powder");
            _resFuel = root.Q<Label>("res-fuel");
            _resAmmo = root.Q<Label>("res-ammo");
            _entityLabel = root.Q<Label>("entity-value");
            _toolLabel = root.Q<Label>("tool-value");
            _systemsLabel = root.Q<Label>("systems-value");
            _logisticsLabel = root.Q<Label>("logistics-value");
            _goalLabel = root.Q<Label>("goal-value");
            _controlsLabel = root.Q<Label>("controls-value");
            _statusPanel = root.Q<VisualElement>("status-panel");
            _saveButton = root.Q<Button>("save-button");
            EnsureSaveButton();
            EnsureOperationalPanels();
            EnsureBlueprintPanel();
            if (_saveButton != null)
            {
                _saveButton.clicked -= SaveNow;
                _saveButton.clicked += SaveNow;
            }
            if (_cancelAllBlueprintsButton != null)
            {
                _cancelAllBlueprintsButton.clicked -= CancelAllBlueprints;
                _cancelAllBlueprintsButton.clicked += CancelAllBlueprints;
            }
        }

        private void EnsureSaveButton()
        {
            if (_saveButton != null || _statusPanel == null) return;
            _saveButton = new Button { text = "Save", name = "save-button" };
            _saveButton.AddToClassList("hud-button");
            _statusPanel.Insert(0, _saveButton);
        }

        private void EnsureBlueprintPanel()
        {
            if (_statusPanel == null) return;
            _blueprintList = _statusPanel.Q<VisualElement>("blueprint-list");
            if (_blueprintList != null)
            {
                _cancelAllBlueprintsButton = _statusPanel.Q<Button>("blueprint-cancel-all");
                return;
            }

            var panel = new VisualElement { name = "blueprint-panel" };
            panel.AddToClassList("queue-panel");

            var header = new VisualElement { name = "blueprint-header" };
            header.AddToClassList("queue-header");
            header.Add(new Label { text = "Blueprint Queue" });
            _cancelAllBlueprintsButton = new Button { text = "Cancel All", name = "blueprint-cancel-all" };
            _cancelAllBlueprintsButton.AddToClassList("hud-button");
            header.Add(_cancelAllBlueprintsButton);

            _blueprintList = new VisualElement { name = "blueprint-list" };
            _blueprintList.AddToClassList("queue-list");
            panel.Add(header);
            panel.Add(_blueprintList);

            int insertIndex = _statusPanel.childCount;
            if (_controlsLabel != null)
                insertIndex = Mathf.Max(0, _statusPanel.IndexOf(_controlsLabel));
            _statusPanel.Insert(insertIndex, panel);
        }

        private void EnsureOperationalPanels()
        {
            if (_statusPanel == null) return;
            _productionList = _statusPanel.Q<VisualElement>("production-list");
            _transportList = _statusPanel.Q<VisualElement>("transport-list");
            if (_productionList != null && _transportList != null) return;

            if (_productionList == null)
                _productionList = CreateStatusPanel("production-panel", "Production", "production-list");
            if (_transportList == null)
                _transportList = CreateStatusPanel("transport-panel", "Transport", "transport-list");
        }

        private VisualElement CreateStatusPanel(string panelName, string title, string listName)
        {
            var panel = new VisualElement { name = panelName };
            panel.AddToClassList("queue-panel");

            var header = new Label { text = title };
            header.AddToClassList("status-subtitle");
            header.style.color = new Color(0.95f, 0.96f, 0.91f);
            header.style.fontSize = 12;
            panel.Add(header);

            var list = new VisualElement { name = listName };
            list.AddToClassList("status-list");
            panel.Add(list);

            int insertIndex = _statusPanel.childCount;
            if (_controlsLabel != null)
                insertIndex = Mathf.Max(0, _statusPanel.IndexOf(_controlsLabel));
            _statusPanel.Insert(insertIndex, panel);
            return list;
        }

        private void UpdateFps()
        {
            _timeLeft -= Time.deltaTime;
            _accum += Time.timeScale / Mathf.Max(Time.deltaTime, 0.0001f);
            _frames++;
            if (_timeLeft > 0) return;

            _fps = _accum / Mathf.Max(_frames, 1);
            _timeLeft = 0.5f;
            _accum = 0;
            _frames = 0;
        }

        private void Refresh()
        {
            if (_world == null || _input == null) return;

            SetText(_fpsLabel, $"{_fps:0}");
            SetText(_mapLabel, $"{_world.MapSize} x {_world.MapSize} / chunk {_world.ChunkSize} / overlay {_knowledge?.CurrentOverlay}");
            SetText(_resFood, _world.Inventory.Food.ToString());
            SetText(_resWood, _world.Inventory.Wood.ToString());
            SetText(_resStone, _world.Inventory.Stone.ToString());
            SetText(_resOre, _world.Inventory.IronOre.ToString());
            SetText(_resCoal, _world.Inventory.Coal.ToString());
            SetText(_resIron, _world.Inventory.IronIngot.ToString());
            SetText(_resSteel, _world.Inventory.Steel.ToString());
            SetText(_resParts, _world.Inventory.MachineParts.ToString());
            SetText(_resRail, _world.Inventory.RailParts.ToString());
            SetText(_resPowder, _world.Inventory.Gunpowder.ToString());
            SetText(_resFuel, _world.Inventory.Fuel.ToString());
            SetText(_resAmmo, _world.Inventory.Ammo.ToString());
            SetText(_entityLabel, $"Buildings {_world.Buildings.Count}  Units {_world.Units.Count}  Vehicles {_world.Vehicles.Count}  Blueprints {_world.Blueprints.Count}  Routes {_world.LogisticsRoutes.Count}  Selected {_units?.SelectedUnits.Count ?? 0}/{_vehicles?.SelectedVehicles.Count ?? 0}");
            SetText(_toolLabel, $"{_input.CurrentTool} / brush {_input.BrushRadius} / build {_input.CurrentBuildable} / vehicle {_input.CurrentVehicle}");
            SetText(_systemsLabel, $"Pop {_world.Population.Residents}  Workers {_world.Population.Workers}  Soldiers {_world.Population.Soldiers}  Wounded {_world.Population.Wounded}  Morale {_world.Population.CityMorale:0}  Era {_world.Tech.Era}  Research {_simulation?.ResearchSummary ?? _world.Tech.CurrentResearch}  Production {_simulation?.ProductionSummary ?? "-"}");
            SetText(_logisticsLabel, $"{_logistics?.LastStatus ?? "-"}  Explored {_knowledge?.ExploredCells ?? 0:n0}  Visible {_knowledge?.VisibleCells ?? 0:n0}");
            SetText(_goalLabel, $"Unification {((_simulation?.UnityProgress ?? 0f) * 100f):0}%  Pressure {_simulation?.PressureSummary ?? "Stable"}  Diplomacy {_simulation?.DiplomacySummary ?? "-"}");
            SetText(_controlsLabel, "1-9 tools  B build  F1-F8 buildings  V cycle vehicle  V+click produce  L/U load-unload selected vehicle  M map  O overlay  X+click cancel  +/- priority  Shift+click queue  F9 save");
            RefreshOperationalPanels();
            RefreshBlueprintPanel();
        }

        private void RefreshOperationalPanels()
        {
            if (_world == null) return;

            if (_productionList != null)
            {
                _productionList.Clear();
                AddStatusLine(_productionList, _simulation?.ProductionSummary ?? "Production idle", "status-summary");
                AddStatusLines(_productionList, _simulation?.ProductionLines, 4);
            }

            if (_transportList != null)
            {
                _transportList.Clear();
                AddStatusLine(_transportList, _vehicles?.LastProductionStatus ?? "No vehicle production", "status-summary");
                AddStatusLines(_transportList, _logistics?.RouteLines, 3);
                AddStatusLines(_transportList, _logistics?.VehicleLines, 4);
            }
        }

        private void RefreshBlueprintPanel()
        {
            if (_blueprintList == null || _world == null) return;
            _blueprintList.Clear();
            int shown = 0;
            int active = 0;
            int paused = 0;
            BlueprintJob best = null;

            foreach (var blueprint in _world.Blueprints)
            {
                if (blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) continue;
                if (blueprint.Status == BlueprintStatus.Active) active++;
                if (blueprint.Status == BlueprintStatus.Paused) paused++;
                if (best == null || blueprint.Priority > best.Priority || (blueprint.Priority == best.Priority && blueprint.Id < best.Id))
                    best = blueprint;
            }

            var summary = new Label { text = best == null ? "No queued blueprints" : $"Active {active}  Paused {paused}  Top #{best.Id} P{best.Priority} {BlueprintName(best)} {best.WorkRemaining:0.0}s" };
            summary.AddToClassList("queue-summary");
            _blueprintList.Add(summary);

            foreach (var blueprint in _world.Blueprints)
            {
                if (blueprint.Status == BlueprintStatus.Cancelled || blueprint.Status == BlueprintStatus.Complete) continue;
                AddBlueprintRow(blueprint);
                shown++;
                if (shown >= 5) break;
            }
        }

        private void AddBlueprintRow(BlueprintJob blueprint)
        {
            var row = new VisualElement();
            row.AddToClassList("queue-row");

            var label = new Label { text = $"#{blueprint.Id} P{blueprint.Priority} {blueprint.Status} {BlueprintName(blueprint)} {blueprint.WorkRemaining:0.0}" };
            label.AddToClassList("queue-label");
            row.Add(label);

            row.Add(MakeBlueprintButton("+", () => _commands?.SubmitBlueprintPriority(blueprint.Id, 1)));
            row.Add(MakeBlueprintButton("-", () => _commands?.SubmitBlueprintPriority(blueprint.Id, -1)));
            if (blueprint.Status == BlueprintStatus.Paused)
                row.Add(MakeBlueprintButton("Run", () => _commands?.SubmitResumeBlueprint(blueprint.Id)));
            else
                row.Add(MakeBlueprintButton("Pause", () => _commands?.SubmitPauseBlueprint(blueprint.Id)));
            row.Add(MakeBlueprintButton("X", () => _commands?.SubmitCancelBlueprint(blueprint.Id)));
            _blueprintList.Add(row);
        }

        private static string BlueprintName(BlueprintJob blueprint)
        {
            return blueprint.Kind == BlueprintKind.Building
                ? blueprint.BuildKind.ToString()
                : blueprint.Kind == BlueprintKind.Terrain ? blueprint.Tool.ToString() : blueprint.Kind.ToString();
        }

        private static Button MakeBlueprintButton(string text, System.Action action)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("queue-button");
            return button;
        }

        private static void AddStatusLines(VisualElement parent, IReadOnlyList<string> lines, int limit)
        {
            if (parent == null || lines == null) return;
            int count = Mathf.Min(limit, lines.Count);
            for (int i = 0; i < count; i++)
                AddStatusLine(parent, lines[i], "status-line");
        }

        private static void AddStatusLine(VisualElement parent, string text, string className)
        {
            if (parent == null || string.IsNullOrEmpty(text)) return;
            var label = new Label { text = text };
            label.AddToClassList(className);
            if (className == "status-summary")
            {
                label.style.color = new Color(0.84f, 0.87f, 0.83f);
                label.style.fontSize = 11;
            }
            else
            {
                label.style.color = new Color(0.93f, 0.95f, 0.90f);
                label.style.fontSize = 10;
            }
            label.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(label);
        }

        private static void SetText(Label label, string value)
        {
            if (label != null) label.text = value;
        }

        private void SaveNow()
        {
            if (_world != null)
                OpenWorldSaveService.Save(_world);
        }

        private void CancelAllBlueprints()
        {
            _commands?.SubmitCancelAllBlueprints();
        }
    }
}
