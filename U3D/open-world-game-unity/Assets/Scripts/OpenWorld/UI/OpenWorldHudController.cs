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
        private OpenWorldGeologySystem _geology;
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
        private VisualElement _overviewPanel;
        private VisualElement _productionPanel;
        private VisualElement _transportPanel;
        private VisualElement _blueprintPanel;
        private VisualElement _geologyPanel;
        private VisualElement _populationPanel;
        private VisualElement _militaryPanel;
        private VisualElement _diplomacyPanel;
        private VisualElement _productionList;
        private VisualElement _transportList;
        private VisualElement _blueprintList;
        private VisualElement _geologyList;
        private VisualElement _populationList;
        private VisualElement _militaryList;
        private VisualElement _diplomacyList;

        private Button _opsOverview;
        private Button _opsProduction;
        private Button _opsTransport;
        private Button _opsGeology;
        private Button _opsPopulation;
        private Button _opsMilitary;
        private Button _opsDiplomacy;
        private Button _opsBlueprints;
        private string _currentOpsTab = "Overview";

        private Button _tabCivilian;
        private Button _tabIndustry;
        private Button _tabLogistics;
        private Button _tabMilitary;
        private Button _tabInfrastructure;
        private Label _buildSummaryLabel;
        private Label _buildHintLabel;
        private ScrollView _buildContent;
        private string _currentBuildTab = "Civilian";

        private float _fps;
        private float _accum;
        private int _frames;
        private float _timeLeft = 0.5f;
        private float _refreshTimer;
        private const float RefreshInterval = 0.25f;

        public void Initialize(OpenWorldState world, OpenWorldInputController input, UnitSystem units, VehicleSystem vehicles, WorldKnowledgeSystem knowledge, OpenWorldLogisticsSystem logistics, OpenWorldSimulationSystem simulation, OpenWorldGeologySystem geology, OpenWorldCommandSystem commands)
        {
            _world = world;
            _input = input;
            _units = units;
            _vehicles = vehicles;
            _knowledge = knowledge;
            _logistics = logistics;
            _simulation = simulation;
            _geology = geology;
            _commands = commands;
            BindDocument();
            Refresh();

            I18nSystem.OnLanguageChanged -= OnLanguageChanged;
            I18nSystem.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            I18nSystem.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            // 先统一翻译静态文本，再让各 C# 渲染逻辑对动态文本做最终覆盖
            var root = GetComponent<UIDocument>()?.rootVisualElement;
            I18nSystem.LocalizeTree(root);

            UpdateBuildDeckText();
            PopulateBuildMenu();
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

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer < RefreshInterval) return;
            _refreshTimer = 0f;
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
            _overviewPanel = root.Q<VisualElement>("overview-panel");
            _saveButton = root.Q<Button>("save-button");
            EnsureSaveButton();
            EnsureOperationalPanels();
            EnsureBlueprintPanel();
            BindOpsTabs(root);
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

            BindBuildMenu(root);

            // 翻译所有静态 UXML 文本（资源栏/页签/指标名/帮助等）；
            // 动态文本由下方各刷新逻辑（UpdateBuildDeckText/PopulateBuildMenu/Refresh）覆盖。
            I18nSystem.LocalizeTree(root);
        }

        private void BindOpsTabs(VisualElement root)
        {
            _opsOverview = root.Q<Button>("ops-overview");
            _opsProduction = root.Q<Button>("ops-production");
            _opsTransport = root.Q<Button>("ops-transport");
            _opsGeology = root.Q<Button>("ops-geology");
            _opsPopulation = root.Q<Button>("ops-population");
            _opsMilitary = root.Q<Button>("ops-military");
            _opsDiplomacy = root.Q<Button>("ops-diplomacy");
            _opsBlueprints = root.Q<Button>("ops-blueprints");

            BindOpsButton(_opsOverview, SwitchOpsTabOverview);
            BindOpsButton(_opsProduction, SwitchOpsTabProduction);
            BindOpsButton(_opsTransport, SwitchOpsTabTransport);
            BindOpsButton(_opsGeology, SwitchOpsTabGeology);
            BindOpsButton(_opsPopulation, SwitchOpsTabPopulation);
            BindOpsButton(_opsMilitary, SwitchOpsTabMilitary);
            BindOpsButton(_opsDiplomacy, SwitchOpsTabDiplomacy);
            BindOpsButton(_opsBlueprints, SwitchOpsTabBlueprints);

            SwitchOpsTab(_currentOpsTab);
        }

        private static void BindOpsButton(Button button, System.Action handler)
        {
            if (button == null) return;
            button.clicked -= handler;
            button.clicked += handler;
        }

        private void BindBuildMenu(VisualElement root)
        {
            _tabCivilian = root.Q<Button>("tab-civilian");
            _tabIndustry = root.Q<Button>("tab-industry");
            _tabLogistics = root.Q<Button>("tab-logistics");
            _tabMilitary = root.Q<Button>("tab-military");
            _tabInfrastructure = root.Q<Button>("tab-infrastructure");
            _buildSummaryLabel = root.Q<Label>("build-summary");
            _buildHintLabel = root.Q<Label>("build-hint");
            _buildContent = root.Q<ScrollView>("build-content");

            BindBuildButton(_tabCivilian, SwitchBuildTabCivilian);
            BindBuildButton(_tabIndustry, SwitchBuildTabIndustry);
            BindBuildButton(_tabLogistics, SwitchBuildTabLogistics);
            BindBuildButton(_tabMilitary, SwitchBuildTabMilitary);
            BindBuildButton(_tabInfrastructure, SwitchBuildTabInfrastructure);

            SwitchBuildTab(_currentBuildTab);
        }

        private static void BindBuildButton(Button button, System.Action handler)
        {
            if (button == null) return;
            button.clicked -= handler;
            button.clicked += handler;
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
            _blueprintPanel = _statusPanel.Q<VisualElement>("blueprint-panel");
            _blueprintList = _blueprintPanel?.Q<VisualElement>("blueprint-list");
            if (_blueprintList != null)
            {
                _cancelAllBlueprintsButton = _blueprintPanel.Q<Button>("blueprint-cancel-all");
                return;
            }

            var panel = new VisualElement { name = "blueprint-panel" };
            panel.AddToClassList("queue-panel");
            panel.AddToClassList("command-panel");

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
            _blueprintPanel = panel;
        }

        private void EnsureOperationalPanels()
        {
            if (_statusPanel == null) return;
            _productionPanel = _statusPanel.Q<VisualElement>("production-panel");
            _transportPanel = _statusPanel.Q<VisualElement>("transport-panel");
            _geologyPanel = _statusPanel.Q<VisualElement>("geology-panel");
            _populationPanel = _statusPanel.Q<VisualElement>("population-panel");
            _militaryPanel = _statusPanel.Q<VisualElement>("military-panel");
            _diplomacyPanel = _statusPanel.Q<VisualElement>("diplomacy-panel");
            _productionList = _productionPanel?.Q<VisualElement>("production-list");
            _transportList = _transportPanel?.Q<VisualElement>("transport-list");
            _geologyList = _geologyPanel?.Q<VisualElement>("geology-list");
            _populationList = _populationPanel?.Q<VisualElement>("population-list");
            _militaryList = _militaryPanel?.Q<VisualElement>("military-list");
            _diplomacyList = _diplomacyPanel?.Q<VisualElement>("diplomacy-list");
            if (_productionList != null && _transportList != null && _geologyList != null && _populationList != null && _militaryList != null && _diplomacyList != null) return;

            if (_productionList == null)
                _productionList = CreateStatusPanel("production-panel", "Production", "production-list");
            if (_transportList == null)
                _transportList = CreateStatusPanel("transport-panel", "Transport", "transport-list");
            if (_geologyList == null)
                _geologyList = CreateStatusPanel("geology-panel", "Geology & Mining", "geology-list");
            if (_populationList == null)
                _populationList = CreateStatusPanel("population-panel", "Population & Medical", "population-list");
            if (_militaryList == null)
                _militaryList = CreateStatusPanel("military-panel", "Military & Intel", "military-list");
            if (_diplomacyList == null)
                _diplomacyList = CreateStatusPanel("diplomacy-panel", "Diplomacy & Trade", "diplomacy-list");
        }

        private VisualElement CreateStatusPanel(string panelName, string title, string listName)
        {
            var panel = new VisualElement { name = panelName };
            panel.AddToClassList("queue-panel");
            panel.AddToClassList("command-panel");

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
            switch (panelName)
            {
                case "production-panel": _productionPanel = panel; break;
                case "transport-panel": _transportPanel = panel; break;
                case "geology-panel": _geologyPanel = panel; break;
                case "population-panel": _populationPanel = panel; break;
                case "military-panel": _militaryPanel = panel; break;
                case "diplomacy-panel": _diplomacyPanel = panel; break;
            }
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

        private void SwitchOpsTabOverview() => SwitchOpsTab("Overview");
        private void SwitchOpsTabProduction() => SwitchOpsTab("Production");
        private void SwitchOpsTabTransport() => SwitchOpsTab("Transport");
        private void SwitchOpsTabGeology() => SwitchOpsTab("Geology");
        private void SwitchOpsTabPopulation() => SwitchOpsTab("Population");
        private void SwitchOpsTabMilitary() => SwitchOpsTab("Military");
        private void SwitchOpsTabDiplomacy() => SwitchOpsTab("Diplomacy");
        private void SwitchOpsTabBlueprints() => SwitchOpsTab("Blueprints");

        private void SwitchOpsTab(string tabName)
        {
            _currentOpsTab = tabName;
            SetPanelVisible(_overviewPanel, tabName == "Overview");
            SetPanelVisible(_productionPanel, tabName == "Production");
            SetPanelVisible(_transportPanel, tabName == "Transport");
            SetPanelVisible(_geologyPanel, tabName == "Geology");
            SetPanelVisible(_populationPanel, tabName == "Population");
            SetPanelVisible(_militaryPanel, tabName == "Military");
            SetPanelVisible(_diplomacyPanel, tabName == "Diplomacy");
            SetPanelVisible(_blueprintPanel, tabName == "Blueprints");

            ToggleOpsButton(_opsOverview, tabName == "Overview");
            ToggleOpsButton(_opsProduction, tabName == "Production");
            ToggleOpsButton(_opsTransport, tabName == "Transport");
            ToggleOpsButton(_opsGeology, tabName == "Geology");
            ToggleOpsButton(_opsPopulation, tabName == "Population");
            ToggleOpsButton(_opsMilitary, tabName == "Military");
            ToggleOpsButton(_opsDiplomacy, tabName == "Diplomacy");
            ToggleOpsButton(_opsBlueprints, tabName == "Blueprints");
        }

        private static void SetPanelVisible(VisualElement panel, bool visible)
        {
            if (panel != null) panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void ToggleOpsButton(Button button, bool active)
        {
            if (button != null) button.EnableInClassList("ops-tab-active", active);
        }

        private void SwitchBuildTabCivilian() => SwitchBuildTab("Civilian");
        private void SwitchBuildTabIndustry() => SwitchBuildTab("Industry");
        private void SwitchBuildTabLogistics() => SwitchBuildTab("Logistics");
        private void SwitchBuildTabMilitary() => SwitchBuildTab("Military");
        private void SwitchBuildTabInfrastructure() => SwitchBuildTab("Infrastructure");

        private void SwitchBuildTab(string tabName)
        {
            _currentBuildTab = tabName;
            if (_tabCivilian != null) _tabCivilian.EnableInClassList("build-tab-active", tabName == "Civilian");
            if (_tabIndustry != null) _tabIndustry.EnableInClassList("build-tab-active", tabName == "Industry");
            if (_tabLogistics != null) _tabLogistics.EnableInClassList("build-tab-active", tabName == "Logistics");
            if (_tabMilitary != null) _tabMilitary.EnableInClassList("build-tab-active", tabName == "Military");
            if (_tabInfrastructure != null) _tabInfrastructure.EnableInClassList("build-tab-active", tabName == "Infrastructure");

            UpdateBuildDeckText();
            PopulateBuildMenu();
        }

        private void UpdateBuildDeckText()
        {
            if (_buildSummaryLabel != null) _buildSummaryLabel.text = I18nSystem.Get(_currentBuildTab switch
            {
                "Industry" => "Industry deck: extraction, smelting, power, and manufacturing",
                "Logistics" => "Logistics deck: depots, vehicles, rail hubs, docks, and bridges",
                "Military" => "Military deck: defense lines, vision, barracks, and fortifications",
                "Infrastructure" => "Infrastructure deck: terrain tools, roads, rails, bridges, and mines",
                _ => "Civilian deck: housing, food, storage, trade, and command"
            });

            if (_buildHintLabel != null) _buildHintLabel.text = I18nSystem.Get(_currentBuildTab switch
            {
                "Infrastructure" => "Issue terrain commands directly. Brush size and queue behavior follow current input modifiers.",
                "Military" => "Place defensive assets near routes, chokepoints, and exposed production chains.",
                "Logistics" => "Build storage and vehicle infrastructure before scaling production throughput.",
                "Industry" => "Prioritize upstream inputs first: ore, coal, smelting, then advanced factories.",
                _ => "Establish food, housing, storage, and command capacity before heavy expansion."
            });
        }

        private void PopulateBuildMenu()
        {
            if (_buildContent == null || _world == null) return;
            _buildContent.Clear();

            if (_currentBuildTab == "Infrastructure")
            {
                AddTerrainToolButton(TerrainTool.Road, "Road", new Color(0.76f, 0.61f, 0.34f));
                AddTerrainToolButton(TerrainTool.Rail, "Rail", new Color(0.45f, 0.45f, 0.50f));
                AddTerrainToolButton(TerrainTool.Bridge, "Bridge", new Color(0.75f, 0.43f, 0.22f));
                AddTerrainToolButton(TerrainTool.Dig, "Dig", new Color(0.6f, 0.5f, 0.4f));
                AddTerrainToolButton(TerrainTool.Fill, "Fill", new Color(0.6f, 0.5f, 0.4f));
                AddTerrainToolButton(TerrainTool.Flatten, "Flatten", new Color(0.6f, 0.5f, 0.4f));
                AddTerrainToolButton(TerrainTool.Trench, "Trench", new Color(0.3f, 0.25f, 0.2f));
                AddTerrainToolButton(TerrainTool.Mine, "Mine", new Color(0.40f, 0.48f, 0.55f));
            }
            else
            {
                var defs = BuildableDef.Defaults();
                foreach (var def in defs)
                {
                    if (MatchesBuildTab(def, _currentBuildTab))
                    {
                        AddBuildableButton(def);
                    }
                }
            }
        }

        private static bool MatchesBuildTab(BuildableDef def, string tabName) => tabName switch
        {
            "Industry" => IsIndustrialBuildable(def),
            "Logistics" => def.IsTransport,
            "Military" => IsMilitaryBuildable(def),
            "Civilian" => !IsIndustrialBuildable(def) && !def.IsTransport && !IsMilitaryBuildable(def),
            _ => false
        };

        private static bool IsIndustrialBuildable(BuildableDef def)
        {
            return def.IsIndustrial
                || def.Kind is BuildableKind.MinePost or BuildableKind.Quarry or BuildableKind.LumberCamp;
        }

        private static bool IsMilitaryBuildable(BuildableDef def)
        {
            return def.IsDefense
                || def.Kind is BuildableKind.Barracks or BuildableKind.Armory;
        }

        private void AddBuildableButton(BuildableDef def)
        {
            var btn = new Button { name = $"build-item-{def.Kind}" };
            btn.AddToClassList("build-item-btn");
            btn.AddToClassList($"build-card-{BuildCategoryClass(def)}");

            var cardTop = new VisualElement();
            cardTop.AddToClassList("build-card-top");
            var colorBox = new VisualElement();
            colorBox.AddToClassList("build-item-color");
            colorBox.style.backgroundColor = def.Color;
            cardTop.Add(colorBox);
            var tagLabel = new Label { text = BuildCategoryLabel(def) };
            tagLabel.AddToClassList("build-item-tag");
            cardTop.Add(tagLabel);
            btn.Add(cardTop);

            var nameLabel = new Label { text = I18nSystem.Get(def.DisplayName) };
            nameLabel.AddToClassList("build-item-name");
            btn.Add(nameLabel);

            var metaLabel = new Label { text = $"{def.Size.x}x{def.Size.y}  {OpenWorldDataCatalog.RequiredEraFor(def.Kind)}" };
            metaLabel.AddToClassList("build-item-meta");
            btn.Add(metaLabel);

            string costText = "";
            if (def.Cost.Wood > 0) costText += $"{I18nSystem.Get("W:")}{def.Cost.Wood} ";
            if (def.Cost.Stone > 0) costText += $"{I18nSystem.Get("S:")}{def.Cost.Stone} ";
            if (def.Cost.IronOre > 0) costText += $"{I18nSystem.Get("Ore:")}{def.Cost.IronOre} ";
            if (def.Cost.Food > 0) costText += $"{I18nSystem.Get("F:")}{def.Cost.Food} ";
            if (def.Cost.Dirt > 0) costText += $"{I18nSystem.Get("D:")}{def.Cost.Dirt} ";

            var costLabel = new Label { text = costText.Trim() };
            costLabel.AddToClassList("build-item-cost");
            btn.Add(costLabel);

            bool unlocked = _world == null || OpenWorldDataCatalog.EraUnlocked(_world.Tech.Era, OpenWorldDataCatalog.RequiredEraFor(def.Kind));
            if (!unlocked)
            {
                btn.SetEnabled(false);
                btn.AddToClassList("build-item-locked");
                btn.tooltip = $"Requires {OpenWorldDataCatalog.RequiredEraFor(def.Kind)} era";
            }

            btn.clicked += () =>
            {
                if (_input != null) _input.SetBuildable(def.Kind);
            };

            _buildContent.Add(btn);
        }

        private void AddTerrainToolButton(TerrainTool tool, string displayName, Color color)
        {
            var btn = new Button { name = $"build-tool-{tool}" };
            btn.AddToClassList("build-item-btn");
            btn.AddToClassList("build-card-infrastructure");

            var cardTop = new VisualElement();
            cardTop.AddToClassList("build-card-top");
            var colorBox = new VisualElement();
            colorBox.AddToClassList("build-item-color");
            colorBox.style.backgroundColor = color;
            cardTop.Add(colorBox);
            var tagLabel = new Label { text = "Tool" };
            tagLabel.AddToClassList("build-item-tag");
            cardTop.Add(tagLabel);
            btn.Add(cardTop);

            var nameLabel = new Label { text = I18nSystem.Get(displayName) };
            nameLabel.AddToClassList("build-item-name");
            btn.Add(nameLabel);

            var metaLabel = new Label { text = TerrainToolMeta(tool) };
            metaLabel.AddToClassList("build-item-meta");
            btn.Add(metaLabel);

            btn.clicked += () =>
            {
                if (_input != null) _input.SetTerrainTool(tool);
            };

            _buildContent.Add(btn);
        }

        private static string BuildCategoryClass(BuildableDef def)
        {
            if (IsMilitaryBuildable(def)) return "military";
            if (def.IsTransport) return "logistics";
            if (IsIndustrialBuildable(def)) return "industry";
            if (def.IsMedical) return "civilian";
            return "civilian";
        }

        private static string BuildCategoryLabel(BuildableDef def)
        {
            if (IsMilitaryBuildable(def)) return "Military";
            if (def.IsTransport) return "Logistics";
            if (IsIndustrialBuildable(def)) return "Industry";
            if (def.IsMedical) return "Medical";
            if (def.ProvidesVision) return "Intel";
            return "Civil";
        }

        private static string TerrainToolMeta(TerrainTool tool) => tool switch
        {
            TerrainTool.Road => "surface route",
            TerrainTool.Rail => "heavy route",
            TerrainTool.Bridge => "gap crossing",
            TerrainTool.Dig => "lower ground",
            TerrainTool.Fill => "raise ground",
            TerrainTool.Flatten => "grade terrain",
            TerrainTool.Trench => "defense work",
            TerrainTool.Mine => "mining zone",
            _ => "terrain"
        };

        private void Refresh()
        {
            if (_world == null || _input == null) return;

            SetText(_fpsLabel, $"{_fps:0}");
            SetText(_mapLabel, $"{_world.MapSize} x {_world.MapSize} / chunk {_world.ChunkSize} / overlay {_knowledge?.CurrentOverlay}");
            SetText(_resFood, _world.TotalResource(ResourceKind.Food).ToString());
            SetText(_resWood, _world.TotalResource(ResourceKind.Wood).ToString());
            SetText(_resStone, _world.TotalResource(ResourceKind.Stone).ToString());
            SetText(_resOre, _world.TotalResource(ResourceKind.IronOre).ToString());
            SetText(_resCoal, _world.TotalResource(ResourceKind.Coal).ToString());
            SetText(_resIron, _world.TotalResource(ResourceKind.IronIngot).ToString());
            SetText(_resSteel, _world.TotalResource(ResourceKind.Steel).ToString());
            SetText(_resParts, _world.TotalResource(ResourceKind.MachineParts).ToString());
            SetText(_resRail, _world.TotalResource(ResourceKind.RailParts).ToString());
            SetText(_resPowder, _world.TotalResource(ResourceKind.Gunpowder).ToString());
            SetText(_resFuel, _world.TotalResource(ResourceKind.Fuel).ToString());
            SetText(_resAmmo, _world.TotalResource(ResourceKind.Ammo).ToString());

            SetText(_entityLabel, $"{I18nSystem.Get("Buildings")} {_world.Buildings.Count}  {I18nSystem.Get("Units")} {_world.Units.Count}  {I18nSystem.Get("Vehicles")} {_world.Vehicles.Count}  {I18nSystem.Get("Blueprints")} {_world.Blueprints.Count}  {I18nSystem.Get("Routes")} {_world.LogisticsRoutes.Count}  {I18nSystem.Get("Selected")} {_units?.SelectedUnits.Count ?? 0}/{_vehicles?.SelectedVehicles.Count ?? 0}");
            SetText(_toolLabel, $"{_input.CurrentTool} / {I18nSystem.Get("brush")} {_input.BrushRadius} / {I18nSystem.Get("build")} {_input.CurrentBuildable} / {I18nSystem.Get("vehicle")} {_input.CurrentVehicle}");

            string researchText = _simulation?.ResearchSummary ?? _world.Tech.CurrentResearch;
            if (_simulation != null && _simulation.ResearchSummary != null)
            {
                // Translate the tech name part if we can split it. For now just show it.
            }

            SetText(_systemsLabel, $"{I18nSystem.Get("Pop")} {_world.Population.Residents}  {I18nSystem.Get("Workers")} {_world.Population.Workers}  {I18nSystem.Get("Soldiers")} {_world.Population.Soldiers}  {I18nSystem.Get("Wounded")} {_world.Population.Wounded}  {I18nSystem.Get("Morale")} {_world.Population.CityMorale:0}  {I18nSystem.Get("Era")} {_world.Tech.Era}  {I18nSystem.Get("Research")} {researchText}  {I18nSystem.Get("Production")} {_simulation?.ProductionSummary ?? "-"}");
            SetText(_logisticsLabel, $"{_logistics?.LastStatus ?? "-"}  {_geology?.StatusSummary ?? "-"}  {I18nSystem.Get("Explored")} {_knowledge?.ExploredCells ?? 0:n0}  {I18nSystem.Get("Visible")} {_knowledge?.VisibleCells ?? 0:n0}");
            SetText(_goalLabel, $"{I18nSystem.Get("Unification")} {((_simulation?.UnityProgress ?? 0f) * 100f):0}%  {I18nSystem.Get("Pressure")} {_simulation?.PressureSummary ?? I18nSystem.Get("Stable")}  {I18nSystem.Get("Diplomacy")} {_simulation?.DiplomacySummary ?? "-"}");

            // Keep controls English for now or add localized string later
            SetText(_controlsLabel, "1-9 tools  B build  F1-F8 buildings  V cycle vehicle  G+click survey  K+click drill  A/P/D+right-click combat orders  L/U load-unload  M map  O overlay  X+click cancel  +/- priority  Shift+click queue  F9 save  T language");
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
                AddStatusLines(_productionList, _simulation?.ProductionLines, 5);
                AddProductionControls(_productionList, 4);
                AddResearchControls(_productionList);
                AddStorageLines(_productionList, 4);
            }

            if (_transportList != null)
            {
                _transportList.Clear();
                AddStatusLine(_transportList, _vehicles?.LastProductionStatus ?? "No vehicle production", "status-summary");
                var serviceRow = new VisualElement();
                serviceRow.AddToClassList("queue-row");
                serviceRow.Add(new Label { text = $"Selected vehicles {_vehicles?.SelectedVehicles.Count ?? 0}" });
                serviceRow.Add(MakeRouteButton("Refuel", () => _commands?.SubmitVehicleService(true, false)));
                serviceRow.Add(MakeRouteButton("Repair", () => _commands?.SubmitVehicleService(false, true)));
                serviceRow.Add(MakeRouteButton("Full service", () => _commands?.SubmitVehicleService(true, true)));
                _transportList.Add(serviceRow);
                AddRouteRows(_transportList, 4);
                AddStatusLines(_transportList, _logistics?.VehicleLines, 4);
            }

            if (_geologyList != null)
            {
                _geologyList.Clear();
                AddStatusLine(_geologyList, _geology?.StatusSummary ?? "Geology idle", "status-summary");
                int shown = 0;
                for (int i = _world.DrillReports.Count - 1; i >= 0 && shown < 3; i--, shown++)
                {
                    var report = _world.DrillReports[i];
                    var material = report.Layers.Count > 0 ? report.Layers[report.Layers.Count - 1].Material.ToString() : "Unknown";
                    AddStatusLine(_geologyList, $"Drill #{report.Id} {report.Cell.x},{report.Cell.y}: {material} layers {report.Layers.Count}", "status-line");
                }
                foreach (var zone in _world.MiningZones)
                {
                    AddStatusLine(_geologyList, $"Mine #{zone.Id} {zone.TargetMaterial} P{zone.Priority}: {zone.Status} / extracted {zone.ExtractedAmount}", "status-line");
                    if (++shown >= 6) break;
                }
            }

            RefreshPopulationPanel();
            RefreshMilitaryPanel();
            RefreshDiplomacyPanel();
        }

        private void RefreshPopulationPanel()
        {
            if (_populationList == null) return;
            _populationList.Clear();
            var population = _world.Population;
            AddStatusLine(_populationList, $"Residents {population.Residents} Workers {population.Workers} Soldiers {population.Soldiers}", "status-summary");
            AddStatusLine(_populationList, $"Engineers {population.Engineers} Drivers {population.Drivers} Doctors {population.Doctors}", "status-line");
            AddStatusLine(_populationList, $"Morale {population.CityMorale:0} Medical pressure {population.MedicalPressure:0} Wounded {population.Wounded}", "status-line");
        }

        private void RefreshMilitaryPanel()
        {
            if (_militaryList == null) return;
            _militaryList.Clear();
            AddStatusLine(_militaryList, $"Known enemy contacts {_world.IntelSnapshots.Count}", "status-summary");

            if (_units != null && _units.SelectedUnits.Count > 0)
            {
                int closestVehicleId = -1;
                float closestDist = float.MaxValue;
                Vector2Int groupCenter = _units.SelectedUnits[0].Entity.Cell;
                foreach (var v in _world.Vehicles.Values)
                {
                    if (v.FactionId == OpenWorldConstants.PlayerFactionId)
                    {
                        float dist = Vector2Int.Distance(groupCenter, v.Cell);
                        if (dist <= 20f && dist < closestDist)
                        {
                            closestDist = dist;
                            closestVehicleId = v.Id;
                        }
                    }
                }

                if (closestVehicleId != -1)
                {
                    var row = new VisualElement();
                    row.AddToClassList("queue-row");
                    row.Add(new Label { text = $"Escort Vehicle #{closestVehicleId}" });
                    row.Add(MakeRouteButton("Escort", () => _commands?.SubmitEscortVehicle(0, closestVehicleId)));
                    _militaryList.Add(row);
                }
            }

            int shown = 0;
            foreach (var unit in _world.Units.Values)
            {
                if (unit.FactionId != OpenWorldConstants.PlayerFactionId) continue;
                AddStatusLine(_militaryList, $"#{unit.Id} {unit.Kind} HP {unit.Hp}/{unit.MaxHp} ammo {unit.Ammo:0} morale {unit.Morale:0} fatigue {unit.Fatigue:0} {unit.Task}", "status-line");
                if (++shown >= 4) break;
            }
        }

        private void RefreshDiplomacyPanel()
        {
            if (_diplomacyList == null) return;
            _diplomacyList.Clear();
            foreach (var relation in _world.Diplomacy)
            {
                if (relation.FactionA != OpenWorldConstants.PlayerFactionId) continue;
                var row = new VisualElement();
                row.AddToClassList("queue-row");
                row.Add(new Label { text = $"Faction {relation.FactionB}: {relation.Stance} trust {relation.Trust}" });
                int factionId = relation.FactionB;
                row.Add(MakeRouteButton("Trade", () => _commands?.SubmitDiplomacy(factionId, DiplomacyStance.Trade)));
                row.Add(MakeRouteButton("Ally", () => _commands?.SubmitDiplomacy(factionId, DiplomacyStance.Allied)));
                row.Add(MakeRouteButton("Hostile", () => _commands?.SubmitDiplomacy(factionId, DiplomacyStance.Hostile)));
                row.Add(MakeRouteButton("Food->Med", () => _commands?.SubmitTrade(factionId, ResourceKind.Food, ResourceKind.Medicine, 5)));
                _diplomacyList.Add(row);
            }
            foreach (var trade in _world.TradeContracts)
                AddStatusLine(_diplomacyList, $"Trade #{trade.Id} {trade.ExportKind}->{trade.ImportKind} x{trade.Amount}: {trade.Status}", "status-line");
        }

        private void AddProductionControls(VisualElement parent, int limit)
        {
            int shown = 0;
            foreach (var building in _world.Buildings.Values)
            {
                if (building.FactionId != OpenWorldConstants.PlayerFactionId) continue;
                foreach (var recipe in OpenWorldDataCatalog.ProductionRecipes)
                {
                    if (recipe.Building != building.Kind) continue;
                    var row = new VisualElement();
                    row.AddToClassList("queue-row");
                    var label = new Label { text = $"#{building.Id} {recipe.Id} W{building.AssignedWorkers}/{recipe.Workers}" };
                    label.AddToClassList("queue-label");
                    row.Add(label);
                    int buildingId = building.Id;
                    string recipeId = recipe.Id;
                    row.Add(MakeRouteButton("Queue", () => _commands?.SubmitProduction(buildingId, recipeId, 5)));
                    row.Add(MakeRouteButton("W+", () => _commands?.SubmitAssignWorkers(buildingId, building.AssignedWorkers + 1)));
                    row.Add(MakeRouteButton("W-", () => _commands?.SubmitAssignWorkers(buildingId, building.AssignedWorkers - 1)));
                    parent.Add(row);
                    if (++shown >= limit) return;
                }
                if (building.Kind == BuildableKind.Barracks && shown < limit)
                {
                    var row = new VisualElement();
                    row.AddToClassList("queue-row");
                    row.Add(new Label { text = $"#{building.Id} Barracks" });
                    int barracksId = building.Id;
                    Button MakeTrainButton(string text, UnitKind kind)
                    {
                        var btn = MakeRouteButton(text, () => _commands?.SubmitTrainUnit(barracksId, kind));
                        if (!OpenWorldDataCatalog.IsUnitUnlocked(kind, _world.Tech.Era))
                        {
                            btn.SetEnabled(false);
                            var def = OpenWorldDataCatalog.GetUnit(kind);
                            if (def != null)
                                btn.tooltip = $"需要 {def.RequiredEra} 时代";
                        }
                        return btn;
                    }

                    row.Add(MakeTrainButton("Mil", UnitKind.Militia));
                    row.Add(MakeTrainButton("Mel", UnitKind.Melee));
                    row.Add(MakeTrainButton("Spear", UnitKind.Spearman));
                    row.Add(MakeTrainButton("Rngd", UnitKind.Ranged));
                    row.Add(MakeTrainButton("Musk", UnitKind.Musketeer));
                    row.Add(MakeTrainButton("Scout", UnitKind.Scout));
                    row.Add(MakeTrainButton("Eng", UnitKind.Engineer));
                    row.Add(MakeTrainButton("Med", UnitKind.Medic));
                    parent.Add(row);
                    var row2 = new VisualElement();
                    row2.AddToClassList("queue-row");
                    row2.Add(new Label { text = "Advanced:" });
                    row2.Add(MakeTrainButton("Rifle", UnitKind.Rifleman));
                    row2.Add(MakeTrainButton("MG", UnitKind.MachineGunner));
                    row2.Add(MakeTrainButton("Art", UnitKind.Artillery));
                    row2.Add(MakeTrainButton("Wkr", UnitKind.Worker));
                    row2.Add(MakeTrainButton("Haul", UnitKind.Hauler));
                    parent.Add(row2);
                    shown += 2;
                }
            }
        }

        private void AddResearchControls(VisualElement parent)
        {
            var row = new VisualElement();
            row.AddToClassList("queue-row");
            row.Add(new Label { text = _simulation?.ResearchSummary ?? "Research" });
            foreach (var tech in OpenWorldDataCatalog.Techs)
            {
                if (_world.Tech.CompletedResearch.Contains(tech.Id)) continue;
                string techId = tech.Id;
                row.Add(MakeRouteButton(tech.DisplayName, () => _commands?.SubmitResearch(techId)));
                break;
            }
            parent.Add(row);
        }

        private void AddRouteRows(VisualElement parent, int limit)
        {
            if (parent == null || _world == null) return;
            int shown = 0;
            var routes = new List<LogisticsRoute>(_world.LogisticsRoutes);
            routes.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            foreach (var route in routes)
            {
                if (shown++ >= limit) break;

                var row = new VisualElement();
                row.AddToClassList("queue-row");
                row.AddToClassList("route-row");

                var label = new Label { text = _logistics != null ? _logistics.RouteLine(route) : route.Status };
                label.AddToClassList("queue-label");
                label.AddToClassList("route-label");
                label.style.color = new Color(0.93f, 0.95f, 0.90f);
                label.style.fontSize = 10;
                label.style.whiteSpace = WhiteSpace.Normal;
                row.Add(label);

                row.Add(MakeRouteButton("Mode", () => _commands?.SubmitToggleRouteMode(route.Id)));
                row.Add(MakeRouteButton("Cargo", () => _commands?.SubmitCycleRouteCargo(route.Id)));
                row.Add(MakeRouteButton("P+", () => _commands?.SubmitRoutePriority(route.Id, 1)));
                row.Add(MakeRouteButton("P-", () => _commands?.SubmitRoutePriority(route.Id, -1)));
                row.Add(MakeRouteButton("S+", () => _commands?.SubmitRouteTargetStock(route.Id, 10)));
                row.Add(MakeRouteButton("S-", () => _commands?.SubmitRouteTargetStock(route.Id, -10)));
                parent.Add(row);
            }

            if (shown == 0)
                AddStatusLine(parent, "No logistics routes configured", "status-line");
        }

        private void AddStorageLines(VisualElement parent, int limit)
        {
            int shown = 0;
            foreach (var building in _world.Buildings.Values)
            {
                _world.EnsureBuildingStorage(building);
                if (building.FactionId != OpenWorldConstants.PlayerFactionId || building.StorageCapacity <= 0) continue;
                AddStatusLine(parent, $"Store #{building.Id} {building.Kind} {building.Storage.Total}/{building.StorageCapacity}: {building.LastStorageStatus}", "status-line");
                if (++shown >= limit) break;
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

        private static Button MakeRouteButton(string text, System.Action action)
        {
            var button = MakeBlueprintButton(text, action);
            button.AddToClassList("route-button");
            button.style.height = 22;
            button.style.minWidth = 44;
            button.style.fontSize = 11;
            button.style.color = new Color(0.93f, 0.96f, 0.93f);
            button.style.backgroundColor = new Color(0.23f, 0.30f, 0.33f, 0.92f);
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
