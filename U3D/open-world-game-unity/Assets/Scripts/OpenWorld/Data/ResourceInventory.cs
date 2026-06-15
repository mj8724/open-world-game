using System;
using UnityEngine;

namespace OpenWorld
{
    [Serializable]
    public class ResourceInventory
    {
        public const int ResourceCount = 22;
        readonly int[] _values = new int[ResourceCount];

        public int Dirt { get => _values[0]; set => _values[0] = value; }
        public int Stone { get => _values[1]; set => _values[1] = value; }
        public int IronOre { get => _values[2]; set => _values[2] = value; }
        public int Coal { get => _values[3]; set => _values[3] = value; }
        public int Clay { get => _values[4]; set => _values[4] = value; }
        public int Wood { get => _values[5]; set => _values[5] = value; }
        public int Food { get => _values[6]; set => _values[6] = value; }
        public int Sulfur { get => _values[7]; set => _values[7] = value; }
        public int Nitrate { get => _values[8]; set => _values[8] = value; }
        public int Oil { get => _values[9]; set => _values[9] = value; }
        public int Lumber { get => _values[10]; set => _values[10] = value; }
        public int Brick { get => _values[11]; set => _values[11] = value; }
        public int IronIngot { get => _values[12]; set => _values[12] = value; }
        public int Steel { get => _values[13]; set => _values[13] = value; }
        public int MachineParts { get => _values[14]; set => _values[14] = value; }
        public int Medicine { get => _values[15]; set => _values[15] = value; }
        public int Ammo { get => _values[16]; set => _values[16] = value; }
        public int Gunpowder { get => _values[17]; set => _values[17] = value; }
        public int Fuel { get => _values[18]; set => _values[18] = value; }
        public int Power { get => _values[19]; set => _values[19] = value; }
        public int Weapons { get => _values[20]; set => _values[20] = value; }
        public int RailParts { get => _values[21]; set => _values[21] = value; }

        public int Total { get { int sum = 0; for (int i = 0; i < ResourceCount; i++) sum += _values[i]; return sum; } }

        public void Add(GroundMaterial material, int amount) { Add(MatToResource(material), amount); }

        public int Get(ResourceKind kind) => _values[IndexFor(kind)];

        public void Add(ResourceKind kind, int amount) => _values[IndexFor(kind)] += amount;

        public bool Spend(ResourceKind kind, int amount)
        {
            int idx = IndexFor(kind);
            if (_values[idx] < amount) return false;
            _values[idx] -= amount;
            return true;
        }

        public int AddLimited(ResourceKind kind, int amount, int capacity)
        {
            if (amount <= 0 || capacity <= Total) return 0;
            int accepted = Mathf.Min(amount, capacity - Total);
            Add(kind, accepted);
            return accepted;
        }

        public bool Spend(BuildCost cost)
        {
            if (Dirt < cost.Dirt || Stone < cost.Stone || IronOre < cost.IronOre || Wood < cost.Wood || Food < cost.Food)
                return false;
            Dirt -= cost.Dirt; Stone -= cost.Stone; IronOre -= cost.IronOre; Wood -= cost.Wood; Food -= cost.Food;
            return true;
        }

        public void CopyFrom(ResourceInventory other) { Array.Copy(other._values, _values, ResourceCount); }
        public void CopyTo(ResourceInventory other) { Array.Copy(_values, other._values, ResourceCount); }

        static int IndexFor(ResourceKind kind) => (int)kind;

        public static ResourceKind MatToResource(GroundMaterial mat) => mat switch
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
            _ => ResourceKind.Dirt
        };

        public static GroundMaterial MatToMaterial(ResourceKind r) => r switch
        {
            ResourceKind.Dirt => GroundMaterial.Dirt,
            ResourceKind.Stone => GroundMaterial.Stone,
            ResourceKind.IronOre => GroundMaterial.IronOre,
            ResourceKind.Coal => GroundMaterial.Coal,
            ResourceKind.Clay => GroundMaterial.Clay,
            ResourceKind.Wood => GroundMaterial.Wood,
            ResourceKind.Food => GroundMaterial.Food,
            ResourceKind.Sulfur => GroundMaterial.Sulfur,
            ResourceKind.Nitrate => GroundMaterial.Nitrate,
            ResourceKind.Oil => GroundMaterial.Oil,
            _ => GroundMaterial.Stone
        };
    }
}
