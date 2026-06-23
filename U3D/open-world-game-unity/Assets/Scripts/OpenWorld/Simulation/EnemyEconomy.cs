using UnityEngine;
using System.Collections.Generic;

namespace OpenWorld
{
    public class EnemyEconomy
    {
        private readonly ResourceInventory _inventory = new ResourceInventory();
        private readonly HashSet<int> _minePosts = new HashSet<int>();
        private readonly HashSet<int> _smelters = new HashSet<int>();
        private readonly HashSet<int> _steelworks = new HashSet<int>();
        private readonly HashSet<int> _machineShops = new HashSet<int>();
        private readonly OpenWorldState _world;
        private int _lastBuildingCount = 0;

        public EnemyEconomy(OpenWorldState world)
        {
            _world = world;
            // start with some food and weapons so it doesn't freeze
            _inventory.Add(ResourceKind.Food, 50);
            _inventory.Add(ResourceKind.Weapons, 20);
            _inventory.Add(ResourceKind.IronOre, 50);
            _inventory.Add(ResourceKind.Coal, 50);
        }

        public void RegisterBuilding(BuildingEntity building)
        {
            if (building.Kind == BuildableKind.MinePost) _minePosts.Add(building.Id);
            else if (building.Kind == BuildableKind.Smelter) _smelters.Add(building.Id);
            else if (building.Kind == BuildableKind.Steelworks) _steelworks.Add(building.Id);
            else if (building.Kind == BuildableKind.MachineShop) _machineShops.Add(building.Id);
        }

        public void TickEconomy()
        {
            if (_world.Buildings.Count != _lastBuildingCount)
            {
                foreach (var b in _world.GetBuildingsListCached())
                {
                    if (b.FactionId == OpenWorldConstants.EnemyFactionId)
                        RegisterBuilding(b);
                }
                _lastBuildingCount = _world.Buildings.Count;
            }

            _inventory.Add(ResourceKind.Food, 2); // passive food income

            // a) Mine
            foreach (var id in _minePosts)
            {
                if (!_world.Buildings.TryGetValue(id, out var building)) continue;
                MiningZoneRecord zone = null;
                foreach (var z in _world.MiningZones)
                {
                    if (z.MineBuildingId == id) { zone = z; break; }
                }

                if (zone == null)
                {
                    var cell = _world.GetCell(building.Origin);
                    OpenWorldState.NormalizeLayers(ref cell);
                    var mat = cell.Layers[cell.CurrentLayer].Material;
                    if (mat == GroundMaterial.Dirt || mat == GroundMaterial.Stone) mat = GroundMaterial.IronOre;
                    zone = _world.AddMiningZone(building.Origin, 4, mat, id, 3);
                }

                if (zone != null)
                {
                    var cell = _world.GetCell(zone.Center);
                    OpenWorldState.NormalizeLayers(ref cell);
                    var layer = cell.Layers[cell.CurrentLayer];
                    if (layer.RemainingAmount > 0 && 
                        (layer.Material == GroundMaterial.IronOre || 
                         layer.Material == GroundMaterial.Coal || 
                         layer.Material == GroundMaterial.Sulfur || 
                         layer.Material == GroundMaterial.Nitrate))
                    {
                        int amount = Mathf.Min(1, layer.RemainingAmount);
                        layer.RemainingAmount -= amount;
                        cell.Layers[cell.CurrentLayer] = layer;
                        _world.SetCell(zone.Center, cell);
                        
                        if (layer.Material == GroundMaterial.IronOre) _inventory.Add(ResourceKind.IronOre, amount);
                        else if (layer.Material == GroundMaterial.Coal) _inventory.Add(ResourceKind.Coal, amount);
                        else if (layer.Material == GroundMaterial.Sulfur) _inventory.Add(ResourceKind.Sulfur, amount);
                        else if (layer.Material == GroundMaterial.Nitrate) _inventory.Add(ResourceKind.Nitrate, amount);
                    }
                }
            }

            // b) Smelt
            foreach (var id in _smelters)
            {
                if (_inventory.Get(ResourceKind.IronOre) >= 2 && _inventory.Get(ResourceKind.Coal) >= 1)
                {
                    _inventory.Add(ResourceKind.IronOre, -2);
                    _inventory.Add(ResourceKind.Coal, -1);
                    _inventory.Add(ResourceKind.IronIngot, 1);
                }
            }

            // c) Steel
            foreach (var id in _steelworks)
            {
                if (_inventory.Get(ResourceKind.IronIngot) >= 2 && _inventory.Get(ResourceKind.Coal) >= 1)
                {
                    _inventory.Add(ResourceKind.IronIngot, -2);
                    _inventory.Add(ResourceKind.Coal, -1);
                    _inventory.Add(ResourceKind.Steel, 1);
                }
            }

            // d) Machine Shop
            foreach (var id in _machineShops)
            {
                if (_inventory.Get(ResourceKind.Steel) >= 3 && _inventory.Get(ResourceKind.MachineParts) >= 1)
                {
                    _inventory.Add(ResourceKind.Steel, -3);
                    _inventory.Add(ResourceKind.MachineParts, -1);
                    _inventory.Add(ResourceKind.Weapons, 1);
                }
                else if (_inventory.Get(ResourceKind.Steel) >= 2 && _inventory.Get(ResourceKind.Coal) >= 1)
                {
                    _inventory.Add(ResourceKind.Steel, -2);
                    _inventory.Add(ResourceKind.Coal, -1);
                    _inventory.Add(ResourceKind.MachineParts, 1);
                }
            }
        }

        public bool CanAfford(ResourceAmount[] cost)
        {
            foreach (var c in cost)
            {
                if (_inventory.Get(c.Kind) < c.Amount) return false;
            }
            return true;
        }

        public void Deduct(ResourceAmount[] cost)
        {
            foreach (var c in cost)
            {
                _inventory.Add(c.Kind, -c.Amount);
            }
        }

        public bool HasResource(ResourceKind kind, int amount)
        {
            return _inventory.Get(kind) >= amount;
        }
        
        public int GetFood() => _inventory.Get(ResourceKind.Food);
        public int GetWeapons() => _inventory.Get(ResourceKind.Weapons);
    }
}
