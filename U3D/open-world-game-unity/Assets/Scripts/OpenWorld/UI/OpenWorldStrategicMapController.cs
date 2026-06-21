using UnityEngine;
using UnityEngine.UIElements;

namespace OpenWorld
{
    [RequireComponent(typeof(UIDocument))]
    public class OpenWorldStrategicMapController : MonoBehaviour
    {
        private OpenWorldState _world;
        private WorldKnowledgeSystem _knowledge;
        private OpenWorldCommandSystem _commands;
        private Camera _camera;
        private VisualElement _panel;
        private VisualElement _mapImage;
        private Label _title;
        private Label _details;
        private Label _legend;
        private Button _overlayButton;
        private Button _zoomInButton;
        private Button _zoomOutButton;
        private Button _cameraButton;
        private Button _scoutButton;
        private Button _roadButton;
        private Button _railButton;
        private Button _bridgeButton;
        private Button _surveyButton;
        private Button _drillButton;
        private Button _mineButton;
        private Texture2D _mapTexture;
        private float _refreshTimer;
        private bool _open;
        private float _zoom = 4f;
        private Vector2 _viewCenter = new(0.5f, 0.5f);
        private bool _dragging;
        private bool _callbacksBound;
        private bool _modeButtonsBound;
        private Vector2 _dragStart;
        private Vector2 _dragLast;
        private const int MapTextureSize = 384;
        private MapClickMode _clickMode = MapClickMode.Camera;

        private enum MapClickMode
        {
            Camera,
            Scout,
            Road,
            Rail,
            Bridge,
            Survey,
            Drill,
            Mine
        }

        public void Initialize(OpenWorldState world, WorldKnowledgeSystem knowledge, Camera camera, OpenWorldCommandSystem commands = null)
        {
            _world = world;
            _knowledge = knowledge;
            _commands = commands;
            _camera = camera;
            Bind();
            SetOpen(true);
            EnsureTexture();
            RefreshMap();

            I18nSystem.OnLanguageChanged -= OnLanguageChanged;
            I18nSystem.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            I18nSystem.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            if (_world != null && _knowledge != null) RefreshMap();
        }

        private void Awake()
        {
            Bind();
        }

        private void Update()
        {
            if (!_open || _world == null || _knowledge == null) return;
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer < 0.6f) return;
            _refreshTimer = 0f;
            RefreshMap();
        }

        public void SetOpen(bool open)
        {
            _open = open;
            if (_panel != null)
                _panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Bind()
        {
            var document = GetComponent<UIDocument>();
            var root = document != null ? document.rootVisualElement : null;
            if (root == null) return;
            _panel = root.Q<VisualElement>("strategic-map-panel");
            _mapImage = root.Q<VisualElement>("strategic-map-image");
            _title = root.Q<Label>("strategic-map-title");
            _details = root.Q<Label>("strategic-map-details");
            _legend = root.Q<Label>("strategic-map-legend");
            _overlayButton = root.Q<Button>("map-overlay-button");
            _zoomInButton = root.Q<Button>("map-zoom-in-button");
            _zoomOutButton = root.Q<Button>("map-zoom-out-button");
            _cameraButton = root.Q<Button>("map-camera-button");
            _scoutButton = root.Q<Button>("map-scout-button");
            _roadButton = root.Q<Button>("map-road-button");
            _railButton = root.Q<Button>("map-rail-button");
            _bridgeButton = root.Q<Button>("map-bridge-button");
            _surveyButton = root.Q<Button>("map-survey-button");
            _drillButton = root.Q<Button>("map-drill-button");
            _mineButton = root.Q<Button>("map-mine-button");
            EnsureControls(root);
            if (_overlayButton != null)
            {
                _overlayButton.clicked -= NextOverlay;
                _overlayButton.clicked += NextOverlay;
            }
            if (_zoomInButton != null)
            {
                _zoomInButton.clicked -= ZoomIn;
                _zoomInButton.clicked += ZoomIn;
            }
            if (_zoomOutButton != null)
            {
                _zoomOutButton.clicked -= ZoomOut;
                _zoomOutButton.clicked += ZoomOut;
            }
            if (!_modeButtonsBound)
            {
                BindModeButton(_cameraButton, MapClickMode.Camera);
                BindModeButton(_scoutButton, MapClickMode.Scout);
                BindModeButton(_roadButton, MapClickMode.Road);
                BindModeButton(_railButton, MapClickMode.Rail);
                BindModeButton(_bridgeButton, MapClickMode.Bridge);
                BindModeButton(_surveyButton, MapClickMode.Survey);
                BindModeButton(_drillButton, MapClickMode.Drill);
                BindModeButton(_mineButton, MapClickMode.Mine);
                _modeButtonsBound = _cameraButton != null || _scoutButton != null || _roadButton != null || _railButton != null || _bridgeButton != null;
            }
            if (_mapImage != null && !_callbacksBound)
            {
                _mapImage.RegisterCallback<PointerDownEvent>(OnMapPointerDown);
                _mapImage.RegisterCallback<PointerMoveEvent>(OnMapPointerMove);
                _mapImage.RegisterCallback<PointerUpEvent>(OnMapPointerUp);
                _mapImage.RegisterCallback<WheelEvent>(OnMapWheel);
                _callbacksBound = true;
            }
        }

        private void EnsureTexture()
        {
            if (_mapTexture != null) return;
            _mapTexture = new Texture2D(MapTextureSize, MapTextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private void RefreshMap()
        {
            EnsureTexture();
            var bounds = CurrentBounds();
            _knowledge.RenderMap(_mapTexture, bounds);
            DrawStrategicOverlays(bounds);
            if (_mapImage != null)
                _mapImage.style.backgroundImage = new StyleBackground(_mapTexture);
            if (_title != null)
                _title.text = $"{I18nSystem.Get("Strategic Map")} - {_knowledge.CurrentOverlay} / {_clickMode}";
            if (_details != null)
            {
                int controlled = 0;
                foreach (var region in _world.Regions)
                    if (region.OwnerFactionId == OpenWorldConstants.PlayerFactionId) controlled++;
                _details.text = $"{I18nSystem.Get("Explored")} {_knowledge.ExploredCells:n0}  {I18nSystem.Get("Visible")} {_knowledge.VisibleCells:n0}  {I18nSystem.Get("Zoom")} {_zoom:0.0}x  {I18nSystem.Get("Regions")} {controlled}/{_world.Regions.Count}  {I18nSystem.Get("Sites")} {RevealedSiteCount()}/{_world.StrategicSites.Count}  {I18nSystem.Get("Routes")} {_world.LogisticsRoutes.Count}";
            }
            if (_legend != null)
                _legend.text = I18nSystem.Get(LegendText());
        }

        private void OnMapPointerDown(PointerDownEvent evt)
        {
            if (_world == null || _mapImage == null) return;
            _dragging = true;
            _dragStart = ToVector2(evt.localPosition);
            _dragLast = _dragStart;
            _mapImage.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnMapPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging || _world == null || _mapImage == null) return;
            var local = ToVector2(evt.localPosition);
            var delta = local - _dragLast;
            _dragLast = local;
            if (delta.sqrMagnitude < 0.01f) return;

            var bounds = CurrentBounds();
            float width = Mathf.Max(1f, _mapImage.resolvedStyle.width);
            float height = Mathf.Max(1f, _mapImage.resolvedStyle.height);
            _viewCenter.x = Mathf.Clamp01(_viewCenter.x - delta.x / width * bounds.width / _world.MapSize);
            _viewCenter.y = Mathf.Clamp01(_viewCenter.y + delta.y / height * bounds.height / _world.MapSize);
            RefreshMap();
            evt.StopPropagation();
        }

        private void OnMapPointerUp(PointerUpEvent evt)
        {
            if (!_dragging || _world == null || _camera == null || _mapImage == null) return;
            _dragging = false;
            _mapImage.ReleasePointer(evt.pointerId);
            var local = ToVector2(evt.localPosition);
            if ((local - _dragStart).sqrMagnitude <= 16f)
                ExecuteMapClick(LocalToCell(local));
            evt.StopPropagation();
        }

        private void OnMapWheel(WheelEvent evt)
        {
            var before = LocalToCell(ToVector2(evt.localMousePosition));
            float previous = _zoom;
            SetZoom(_zoom * (evt.delta.y > 0f ? 0.86f : 1.16f), before, previous);
            RefreshMap();
            evt.StopPropagation();
        }

        private void NextOverlay()
        {
            _knowledge?.NextOverlay();
            RefreshMap();
        }

        private void ZoomIn()
        {
            SetZoom(_zoom * 1.25f, CenterCell(), _zoom);
            RefreshMap();
        }

        private void ZoomOut()
        {
            SetZoom(_zoom * 0.80f, CenterCell(), _zoom);
            RefreshMap();
        }

        private void SetMode(MapClickMode mode)
        {
            _clickMode = mode;
            RefreshMap();
        }

        private void SetZoom(float targetZoom, Vector2Int anchorCell, float previousZoom)
        {
            _zoom = Mathf.Clamp(targetZoom, 1f, 8f);
            if (Mathf.Approximately(previousZoom, _zoom)) return;
            var after = LocalToCell(CellToLocal(anchorCell));
            var drift = anchorCell - after;
            _viewCenter.x = Mathf.Clamp01(_viewCenter.x + drift.x / (float)_world.MapSize);
            _viewCenter.y = Mathf.Clamp01(_viewCenter.y + drift.y / (float)_world.MapSize);
        }

        private RectInt CurrentBounds()
        {
            if (_world == null) return new RectInt(0, 0, 1, 1);
            int span = Mathf.Clamp(Mathf.RoundToInt(_world.MapSize / _zoom), 32, _world.MapSize);
            int centerX = Mathf.RoundToInt(_viewCenter.x * (_world.MapSize - 1));
            int centerY = Mathf.RoundToInt(_viewCenter.y * (_world.MapSize - 1));
            int x = Mathf.Clamp(centerX - span / 2, 0, _world.MapSize - span);
            int y = Mathf.Clamp(centerY - span / 2, 0, _world.MapSize - span);
            return new RectInt(x, y, span, span);
        }

        private Vector2Int LocalToCell(Vector2 local)
        {
            var bounds = CurrentBounds();
            float width = Mathf.Max(1f, _mapImage.resolvedStyle.width);
            float height = Mathf.Max(1f, _mapImage.resolvedStyle.height);
            return new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(bounds.xMin + local.x / width * (bounds.width - 1)), 0, _world.MapSize - 1),
                Mathf.Clamp(Mathf.RoundToInt(bounds.yMin + (1f - local.y / height) * (bounds.height - 1)), 0, _world.MapSize - 1));
        }

        private Vector2 CellToLocal(Vector2Int cell)
        {
            var bounds = CurrentBounds();
            float width = Mathf.Max(1f, _mapImage?.resolvedStyle.width ?? 1f);
            float height = Mathf.Max(1f, _mapImage?.resolvedStyle.height ?? 1f);
            return new Vector2(
                (cell.x - bounds.xMin) / Mathf.Max(1f, bounds.width - 1f) * width,
                (1f - (cell.y - bounds.yMin) / Mathf.Max(1f, bounds.height - 1f)) * height);
        }

        private Vector2Int CenterCell()
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(_viewCenter.x * (_world.MapSize - 1)), 0, _world.MapSize - 1),
                Mathf.Clamp(Mathf.RoundToInt(_viewCenter.y * (_world.MapSize - 1)), 0, _world.MapSize - 1));
        }

        private void JumpCameraTo(Vector2Int cell)
        {
            var target = _world.CellToWorld(cell);
            var currentHeight = _camera.transform.position.y;
            var currentForward = _camera.transform.forward;

            if (Mathf.Abs(currentForward.y) > 0.01f)
            {
                var distance = (currentHeight - target.y) / -currentForward.y;
                _camera.transform.position = target - currentForward * distance;
            }
            else
            {
                _camera.transform.position = target + new Vector3(-35f, currentHeight, -45f);
            }
        }

        private void ExecuteMapClick(Vector2Int cell)
        {
            switch (_clickMode)
            {
                case MapClickMode.Scout:
                    _commands?.SubmitScout(cell);
                    JumpCameraTo(cell);
                    break;
                case MapClickMode.Road:
                    _commands?.SubmitTerrain(TerrainTool.Road, cell, 1, queue: true);
                    break;
                case MapClickMode.Rail:
                    _commands?.SubmitTerrain(TerrainTool.Rail, cell, 1, queue: true);
                    break;
                case MapClickMode.Bridge:
                    _commands?.SubmitTerrain(TerrainTool.Bridge, cell, 1, queue: true);
                    break;
                case MapClickMode.Survey:
                    _commands?.SubmitGeologicalSurvey(cell, 6);
                    JumpCameraTo(cell);
                    break;
                case MapClickMode.Drill:
                    _commands?.SubmitCoreDrill(cell);
                    JumpCameraTo(cell);
                    break;
                case MapClickMode.Mine:
                    _commands?.SubmitMiningZone(cell, 3, GroundMaterial.IronOre);
                    _commands?.SubmitTerrain(TerrainTool.Mine, cell, 3, queue: true);
                    break;
                default:
                    JumpCameraTo(cell);
                    break;
            }
            RefreshMap();
        }

        private void EnsureControls(VisualElement root)
        {
            if (_panel == null) return;
            VisualElement actionRow = root.Q<VisualElement>("strategic-map-generated-actions");
            if (actionRow == null && (_overlayButton == null || _cameraButton == null))
            {
                actionRow = new VisualElement { name = "strategic-map-generated-actions" };
                actionRow.AddToClassList("map-actions");
                int insertIndex = _mapImage != null ? _panel.IndexOf(_mapImage) : Mathf.Min(2, _panel.childCount);
                _panel.Insert(Mathf.Max(0, insertIndex), actionRow);
            }

            _overlayButton ??= CreateButton(actionRow, "Overlay", "map-overlay-button", "hud-button");
            _zoomInButton ??= CreateButton(actionRow, "+", "map-zoom-in-button", "icon-button");
            _zoomOutButton ??= CreateButton(actionRow, "-", "map-zoom-out-button", "icon-button");
            _cameraButton ??= CreateButton(actionRow, "Camera", "map-camera-button", "hud-button");
            _scoutButton ??= CreateButton(actionRow, "Scout", "map-scout-button", "hud-button");
            _roadButton ??= CreateButton(actionRow, "Road", "map-road-button", "hud-button");
            _railButton ??= CreateButton(actionRow, "Rail", "map-rail-button", "hud-button");
            _bridgeButton ??= CreateButton(actionRow, "Bridge", "map-bridge-button", "hud-button");
            _surveyButton ??= CreateButton(actionRow, "Survey", "map-survey-button", "hud-button");
            _drillButton ??= CreateButton(actionRow, "Drill", "map-drill-button", "hud-button");
            _mineButton ??= CreateButton(actionRow, "Mine", "map-mine-button", "hud-button");

            if (_legend == null)
            {
                _legend = new Label { name = "strategic-map-legend" };
                _legend.AddToClassList("map-help");
                int insertIndex = _mapImage != null ? _panel.IndexOf(_mapImage) + 1 : _panel.childCount;
                _panel.Insert(Mathf.Clamp(insertIndex, 0, _panel.childCount), _legend);
            }
        }

        private static Button CreateButton(VisualElement parent, string text, string name, string className)
        {
            if (parent == null) return null;
            var button = new Button { text = text, name = name };
            button.AddToClassList(className);
            parent.Add(button);
            return button;
        }

        private void BindModeButton(Button button, MapClickMode mode)
        {
            if (button == null) return;
            button.clicked += () => SetMode(mode);
        }

        private string LegendText()
        {
            return _knowledge.CurrentOverlay switch
            {
                StrategicOverlay.Territory => "Legend: blue player / red enemy / yellow neutral / green ally. Click mode sets camera, scout, or blueprint marks.",
                StrategicOverlay.Geology => "Legend: dim suspected / colored surveyed / bright drilled / gray exhausted. Survey, drill and mine modes create geology commands.",
                StrategicOverlay.Resources => "Legend: orange iron / dark coal-oil / green food-wood / gray stone. Icons show known sites and units.",
                StrategicOverlay.RoadsRails => "Legend: tan road / bright rail / brown bridge / yellow supply route. Vehicles use roads and rails faster.",
                StrategicOverlay.Supply => "Legend: route lines show active logistics; route status and bottlenecks are listed in HUD Logistics.",
                StrategicOverlay.Logistics => "Legend: route and vehicle icons identify no stock, no fuel, no idle vehicle, or no path bottlenecks.",
                StrategicOverlay.EnemyIntel => "Legend: red icons are visible or last-known enemy positions; fog hides unknown enemies.",
                StrategicOverlay.Blueprints => "Legend: cyan active blueprint / yellow paused / map click can place road, rail, or bridge marks.",
                StrategicOverlay.MoraleMedical => "Legend: green high morale / red pressure. HUD shows wounded, medicine, fatigue and morale.",
                _ => "Legend: black unknown / dim explored / bright visible. White frame is camera view; icons mark known units, buildings, sites, routes."
            };
        }

        private void DrawStrategicOverlays(RectInt bounds)
        {
            DrawRoutes(bounds);
            DrawGeology(bounds);
            DrawSites(bounds);
            DrawBuildings(bounds);
            DrawUnits(bounds);
            DrawIntel(bounds);
            DrawVehicles(bounds);
            DrawCameraFrame(bounds);
            _mapTexture.Apply(false);
        }

        private void DrawRoutes(RectInt bounds)
        {
            foreach (var route in _world.LogisticsRoutes)
                DrawLine(route.Source, route.Target, bounds, new Color(1f, 0.86f, 0.30f, 1f), 1);
        }

        private void DrawGeology(RectInt bounds)
        {
            if (_knowledge.CurrentOverlay != StrategicOverlay.Geology) return;
            foreach (var report in _world.DrillReports)
                DrawDiamond(report.Cell, bounds, new Color(1f, 1f, 1f, 1f), 3);
            foreach (var zone in _world.MiningZones)
                DrawBox(zone.Center, bounds, zone.Active ? new Color(0.20f, 0.90f, 0.90f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f), Mathf.Clamp(zone.Radius, 2, 5));
        }

        private void DrawSites(RectInt bounds)
        {
            foreach (var site in _world.StrategicSites)
            {
                if (!site.Revealed || !CanShow(site.Cell)) continue;
                var color = site.FactionId switch
                {
                    OpenWorldConstants.EnemyFactionId => new Color(1f, 0.16f, 0.12f, 1f),
                    OpenWorldConstants.PlayerFactionId => new Color(0.25f, 0.65f, 1f, 1f),
                    OpenWorldConstants.AllyFactionId => new Color(0.20f, 1f, 0.55f, 1f),
                    _ => new Color(1f, 0.84f, 0.24f, 1f)
                };
                DrawDiamond(site.Cell, bounds, color, 4);
            }
        }

        private void DrawBuildings(RectInt bounds)
        {
            foreach (var building in _world.Buildings.Values)
            {
                var cell = building.Origin + building.Size / 2;
                if (!CanShow(cell)) continue;
                var color = building.FactionId switch
                {
                    OpenWorldConstants.PlayerFactionId => new Color(0.18f, 0.58f, 1f, 1f),
                    OpenWorldConstants.EnemyFactionId => new Color(0.95f, 0.14f, 0.10f, 1f),
                    OpenWorldConstants.AllyFactionId => new Color(0.15f, 0.85f, 0.45f, 1f),
                    _ => new Color(0.90f, 0.75f, 0.22f, 1f)
                };
                DrawBox(cell, bounds, color, building.Kind == BuildableKind.ControlPoint ? 4 : 3);
            }
        }

        private void DrawUnits(RectInt bounds)
        {
            foreach (var unit in _world.Units.Values)
            {
                if (!CanShow(unit.Cell)) continue;
                if (unit.FactionId != OpenWorldConstants.PlayerFactionId && _knowledge.GetState(unit.Cell) != KnowledgeState.Visible) continue;
                var color = unit.FactionId == OpenWorldConstants.PlayerFactionId ? new Color(0.25f, 0.95f, 1f, 1f) : new Color(1f, 0.24f, 0.18f, 1f);
                DrawBox(unit.Cell, bounds, color, 2);
            }
        }

        private void DrawIntel(RectInt bounds)
        {
            if (_knowledge.CurrentOverlay != StrategicOverlay.EnemyIntel) return;
            foreach (var intel in _world.IntelSnapshots)
            {
                if (!CanShow(intel.Cell)) continue;
                float age = Mathf.Max(0f, Time.time - intel.SeenAt);
                float fade = Mathf.Clamp01(1f - age / 300f);
                DrawDiamond(intel.Cell, bounds, new Color(1f, 0.18f, 0.12f, Mathf.Lerp(0.35f, 1f, fade)), 3);
            }
        }

        private void DrawVehicles(RectInt bounds)
        {
            foreach (var vehicle in _world.Vehicles.Values)
            {
                if (!CanShow(vehicle.Cell)) continue;
                var color = vehicle.FactionId == OpenWorldConstants.PlayerFactionId ? new Color(1f, 0.58f, 0.18f, 1f) : new Color(0.88f, 0.18f, 0.14f, 1f);
                DrawDiamond(vehicle.Cell, bounds, color, 3);
            }
        }

        private void DrawCameraFrame(RectInt bounds)
        {
            if (_camera == null || _world == null) return;
            var center = _world.WorldToCell(_camera.transform.position + _camera.transform.forward * 45f);
            int half = Mathf.RoundToInt(24f / _zoom);
            var a = center + new Vector2Int(-half, -half);
            var b = center + new Vector2Int(half, -half);
            var c = center + new Vector2Int(half, half);
            var d = center + new Vector2Int(-half, half);
            var color = new Color(1f, 1f, 1f, 1f);
            DrawLine(a, b, bounds, color, 1);
            DrawLine(b, c, bounds, color, 1);
            DrawLine(c, d, bounds, color, 1);
            DrawLine(d, a, bounds, color, 1);
        }

        private bool CanShow(Vector2Int cell) => _knowledge != null && _knowledge.GetState(cell) != KnowledgeState.Unknown;

        private int RevealedSiteCount()
        {
            int count = 0;
            foreach (var site in _world.StrategicSites)
                if (site.Revealed) count++;
            return count;
        }

        private bool TryCellToPixel(Vector2Int cell, RectInt bounds, out Vector2Int pixel)
        {
            pixel = default;
            if (!bounds.Contains(cell)) return false;
            int x = Mathf.RoundToInt((cell.x - bounds.xMin) / Mathf.Max(1f, bounds.width - 1f) * (_mapTexture.width - 1));
            int y = Mathf.RoundToInt((cell.y - bounds.yMin) / Mathf.Max(1f, bounds.height - 1f) * (_mapTexture.height - 1));
            pixel = new Vector2Int(x, y);
            return true;
        }

        private void DrawBox(Vector2Int cell, RectInt bounds, Color color, int radius)
        {
            if (!TryCellToPixel(cell, bounds, out var center)) return;
            for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
                SetPixel(center.x + x, center.y + y, color);
        }

        private void DrawDiamond(Vector2Int cell, RectInt bounds, Color color, int radius)
        {
            if (!TryCellToPixel(cell, bounds, out var center)) return;
            for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
                if (Mathf.Abs(x) + Mathf.Abs(y) <= radius) SetPixel(center.x + x, center.y + y, color);
        }

        private void DrawLine(Vector2Int a, Vector2Int b, RectInt bounds, Color color, int radius)
        {
            if (!TryCellToPixel(a, bounds, out var pa) || !TryCellToPixel(b, bounds, out var pb)) return;
            int dx = Mathf.Abs(pb.x - pa.x);
            int dy = Mathf.Abs(pb.y - pa.y);
            int sx = pa.x < pb.x ? 1 : -1;
            int sy = pa.y < pb.y ? 1 : -1;
            int err = dx - dy;
            int x = pa.x;
            int y = pa.y;
            while (true)
            {
                for (int oy = -radius; oy <= radius; oy++)
                for (int ox = -radius; ox <= radius; ox++)
                    SetPixel(x + ox, y + oy, color);
                if (x == pb.x && y == pb.y) break;
                int e2 = err * 2;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
        }

        private void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= _mapTexture.width || y >= _mapTexture.height) return;
            _mapTexture.SetPixel(x, y, color);
        }

        private static Vector2 ToVector2(Vector3 value) => new(value.x, value.y);
    }
}
