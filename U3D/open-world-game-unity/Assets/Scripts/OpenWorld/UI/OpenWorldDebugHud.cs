using UnityEngine;

namespace OpenWorld
{
    public class OpenWorldDebugHud : MonoBehaviour
    {
        private OpenWorldState _world;
        private OpenWorldInputController _input;
        private UnitSystem _units;
        private float _fps;
        private float _accum;
        private int _frames;
        private float _timeLeft = 0.5f;

        public void Initialize(OpenWorldState world, OpenWorldInputController input, UnitSystem units)
        {
            _world = world;
            _input = input;
            _units = units;
        }

        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            _accum += Time.timeScale / Mathf.Max(Time.deltaTime, 0.0001f);
            _frames++;
            if (_timeLeft <= 0)
            {
                _fps = _accum / _frames;
                _timeLeft = 0.5f;
                _accum = 0;
                _frames = 0;
            }

            var keyboard = OpenWorldInput.Keyboard;
            if (OpenWorldInput.Pressed(keyboard?.f9Key) && _world != null)
                OpenWorldSaveService.Save(_world);
        }

        private void OnGUI()
        {
            if (_world == null || _input == null) return;
            GUILayout.BeginArea(new Rect(12, 12, 430, 330), GUI.skin.box);
            GUILayout.Label("<b>Open World Surface Slice</b>");
            GUILayout.Label($"FPS: {_fps:0}  Map: {_world.MapSize}x{_world.MapSize}  Chunk: {_world.ChunkSize}");
            GUILayout.Label($"Resources  Dirt:{_world.Inventory.Dirt}  Stone:{_world.Inventory.Stone}  Iron:{_world.Inventory.IronOre}  Wood:{_world.Inventory.Wood}  Food:{_world.Inventory.Food}");
            GUILayout.Label($"Buildings:{_world.Buildings.Count}  Units:{_world.Units.Count}  Jobs:{_world.Jobs.Count}  Selected:{_units?.SelectedUnits.Count ?? 0}");
            GUILayout.Space(6);
            GUILayout.Label($"Tool: {_input.CurrentTool}  Brush:[{_input.BrushRadius}]  Build:{_input.CurrentBuildable}");
            GUILayout.Label("1 Dig  2 Fill  3 Flatten  4 Ramp  5 Road  6 Trench  B Build");
            GUILayout.Label("F1 Wall  F2 Tower  F3 Warehouse  F4 Barracks  F5 Farm");
            GUILayout.Label("Left Click: apply/place  Shift+Left Dig: queue worker jobs  Drag: select  Right Click: move selected");
            GUILayout.Label("WASD move camera, wheel zoom, Q/E rotate, [/] brush, F9 save");
            GUILayout.EndArea();
        }
    }
}
