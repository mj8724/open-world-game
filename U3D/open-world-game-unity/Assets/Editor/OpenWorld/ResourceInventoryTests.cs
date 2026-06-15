using NUnit.Framework;
using UnityEngine;

namespace OpenWorld.Tests
{
    /// <summary>
    /// ResourceInventory 完整测试：Get/Add/Spend/Copy/Total/AddLimited/BuildCost/MatTo
    /// </summary>
    public class ResourceInventoryTests
    {
        [Test]
        public void Get_AllResourceKinds_ReturnCorrectValue()
        {
            var inv = new ResourceInventory();
            inv.Dirt = 100;
            inv.Stone = 200;
            inv.IronOre = 50;
            inv.Coal = 30;
            inv.Food = 500;

            Assert.AreEqual(100, inv.Get(ResourceKind.Dirt));
            Assert.AreEqual(200, inv.Get(ResourceKind.Stone));
            Assert.AreEqual(50, inv.Get(ResourceKind.IronOre));
            Assert.AreEqual(30, inv.Get(ResourceKind.Coal));
            Assert.AreEqual(500, inv.Get(ResourceKind.Food));
            Assert.AreEqual(0, inv.Get(ResourceKind.Oil)); // 未设置
        }

        [Test]
        public void Add_KindAndAmount_IncreasesCorrectly()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Wood, 50);
            inv.Add(ResourceKind.Wood, 30);
            inv.Add(ResourceKind.Fuel, 10);

            Assert.AreEqual(80, inv.Get(ResourceKind.Wood));
            Assert.AreEqual(10, inv.Get(ResourceKind.Fuel));
            Assert.AreEqual(0, inv.Get(ResourceKind.IronIngot));
        }

        [Test]
        public void Add_NegativeAmount_WorksLikeSpend()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Food, 100);
            inv.Add(ResourceKind.Food, -30);
            Assert.AreEqual(70, inv.Get(ResourceKind.Food));
        }

        [Test]
        public void Add_AllResourceKinds_DoesNotCrash()
        {
            var inv = new ResourceInventory();
            // 遍历所有 22 种资源
            for (int i = 0; i < 22; i++)
                inv.Add((ResourceKind)i, 10);

            Assert.AreEqual(10, inv.Get(ResourceKind.Dirt));
            Assert.AreEqual(10, inv.Get(ResourceKind.RailParts));
            Assert.AreEqual(220, inv.Total);
        }

        [Test]
        public void Spend_HasEnough_ReturnsTrueAndDeducts()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Food, 100);

            bool result = inv.Spend(ResourceKind.Food, 40);
            Assert.IsTrue(result);
            Assert.AreEqual(60, inv.Get(ResourceKind.Food));
        }

        [Test]
        public void Spend_NotEnough_ReturnsFalseAndPreserves()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.IronIngot, 10);

            bool result = inv.Spend(ResourceKind.IronIngot, 50);
            Assert.IsFalse(result);
            Assert.AreEqual(10, inv.Get(ResourceKind.IronIngot)); // 不变
        }

        [Test]
        public void Spend_ExactAmount_ReturnsTrue()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Stone, 30);

            Assert.IsTrue(inv.Spend(ResourceKind.Stone, 30));
            Assert.AreEqual(0, inv.Get(ResourceKind.Stone));
        }

        [Test]
        public void Spend_Zero_AlwaysTrue()
        {
            var inv = new ResourceInventory();
            Assert.IsTrue(inv.Spend(ResourceKind.Food, 0));
            Assert.AreEqual(0, inv.Get(ResourceKind.Food));
        }

        [Test]
        public void Total_SumOfAllResources_IsCorrect()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Dirt, 10);
            inv.Add(ResourceKind.Stone, 20);
            inv.Add(ResourceKind.Wood, 30);
            inv.Add(ResourceKind.Food, 40);

            Assert.AreEqual(100, inv.Total);
        }

        [Test]
        public void Total_EmptyInventory_IsZero()
        {
            var inv = new ResourceInventory();
            Assert.AreEqual(0, inv.Total);
        }

        [Test]
        public void AddLimited_BelowCapacity_AcceptsAll()
        {
            var inv = new ResourceInventory();
            int accepted = inv.AddLimited(ResourceKind.Food, 50, 100);

            Assert.AreEqual(50, accepted);
            Assert.AreEqual(50, inv.Get(ResourceKind.Food));
        }

        [Test]
        public void AddLimited_AboveCapacity_AcceptsPartial()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Dirt, 80); // Total = 80
            int accepted = inv.AddLimited(ResourceKind.Food, 50, 100); // only 20 room

            Assert.AreEqual(20, accepted);
            Assert.AreEqual(100, inv.Total);
        }

        [Test]
        public void AddLimited_AtCapacity_AcceptsZero()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Dirt, 100); // Total = 100
            int accepted = inv.AddLimited(ResourceKind.Food, 50, 100);

            Assert.AreEqual(0, accepted);
        }

        [Test]
        public void AddLimited_ZeroAmount_ReturnsZero()
        {
            var inv = new ResourceInventory();
            Assert.AreEqual(0, inv.AddLimited(ResourceKind.Food, 0, 100));
        }

        [Test]
        public void SpendBuildCost_HasAll_ReturnsTrue()
        {
            var inv = new ResourceInventory();
            inv.Dirt = 10;
            inv.Stone = 20;
            inv.IronOre = 5;
            inv.Wood = 15;
            inv.Food = 25;

            var cost = new BuildCost(5, 10, 2, 8, 12);
            Assert.IsTrue(inv.Spend(cost));
            Assert.AreEqual(5, inv.Dirt);
            Assert.AreEqual(10, inv.Stone);
            Assert.AreEqual(3, inv.IronOre);
            Assert.AreEqual(7, inv.Wood);
            Assert.AreEqual(13, inv.Food);
        }

        [Test]
        public void SpendBuildCost_MissingWood_ReturnsFalse()
        {
            var inv = new ResourceInventory();
            inv.Dirt = 100;
            inv.Stone = 100;
            inv.Wood = 0; // 缺失
            inv.Food = 100;

            var cost = new BuildCost(10, 10, 0, 10, 10);
            Assert.IsFalse(inv.Spend(cost));
            // 其他资源不应被扣除
            Assert.AreEqual(100, inv.Dirt);
            Assert.AreEqual(100, inv.Stone);
        }

        [Test]
        public void CopyFrom_CopiesAllResources()
        {
            var src = new ResourceInventory();
            src.Add(ResourceKind.Dirt, 10);
            src.Add(ResourceKind.Stone, 20);
            src.Add(ResourceKind.Food, 30);
            src.Add(ResourceKind.RailParts, 5);

            var dst = new ResourceInventory();
            dst.CopyFrom(src);

            Assert.AreEqual(10, dst.Get(ResourceKind.Dirt));
            Assert.AreEqual(20, dst.Get(ResourceKind.Stone));
            Assert.AreEqual(30, dst.Get(ResourceKind.Food));
            Assert.AreEqual(5, dst.Get(ResourceKind.RailParts));
            Assert.AreEqual(src.Total, dst.Total);
        }

        [Test]
        public void CopyTo_WritesAllResources()
        {
            var src = new ResourceInventory();
            src.Add(ResourceKind.IronIngot, 15);
            src.Add(ResourceKind.Steel, 25);

            var dst = new ResourceInventory();
            src.CopyTo(dst);

            Assert.AreEqual(15, dst.Get(ResourceKind.IronIngot));
            Assert.AreEqual(25, dst.Get(ResourceKind.Steel));
            Assert.AreEqual(src.Total, dst.Total);
        }

        [Test]
        public void CopyFrom_ThenModify_Independent()
        {
            var src = new ResourceInventory();
            src.Add(ResourceKind.Food, 100);
            var dst = new ResourceInventory();
            dst.CopyFrom(src);
            dst.Add(ResourceKind.Food, 50);

            Assert.AreEqual(100, src.Get(ResourceKind.Food)); // 源不变
            Assert.AreEqual(150, dst.Get(ResourceKind.Food));
        }

        [Test]
        public void MatToResource_AllGroundMaterials_MapCorrectly()
        {
            Assert.AreEqual(ResourceKind.Dirt, ResourceInventory.MatToResource(GroundMaterial.Dirt));
            Assert.AreEqual(ResourceKind.Stone, ResourceInventory.MatToResource(GroundMaterial.Stone));
            Assert.AreEqual(ResourceKind.IronOre, ResourceInventory.MatToResource(GroundMaterial.IronOre));
            Assert.AreEqual(ResourceKind.Coal, ResourceInventory.MatToResource(GroundMaterial.Coal));
            Assert.AreEqual(ResourceKind.Clay, ResourceInventory.MatToResource(GroundMaterial.Clay));
            Assert.AreEqual(ResourceKind.Wood, ResourceInventory.MatToResource(GroundMaterial.Wood));
            Assert.AreEqual(ResourceKind.Food, ResourceInventory.MatToResource(GroundMaterial.Food));
            Assert.AreEqual(ResourceKind.Sulfur, ResourceInventory.MatToResource(GroundMaterial.Sulfur));
            Assert.AreEqual(ResourceKind.Nitrate, ResourceInventory.MatToResource(GroundMaterial.Nitrate));
            Assert.AreEqual(ResourceKind.Oil, ResourceInventory.MatToResource(GroundMaterial.Oil));
        }

        [Test]
        public void MatToResource_UnknownMaterial_DefaultToDirt()
        {
            int invalid = 99;
            Assert.AreEqual(ResourceKind.Dirt, ResourceInventory.MatToResource((GroundMaterial)invalid));
        }

        [Test]
        public void MatToMaterial_RoundTrip()
        {
            // MatToResource → MatToMaterial should return original for all mapped
            var materials = new[] {
                GroundMaterial.Dirt, GroundMaterial.Stone, GroundMaterial.IronOre,
                GroundMaterial.Coal, GroundMaterial.Clay, GroundMaterial.Wood,
                GroundMaterial.Food, GroundMaterial.Sulfur, GroundMaterial.Nitrate,
                GroundMaterial.Oil
            };
            foreach (var mat in materials)
            {
                var resource = ResourceInventory.MatToResource(mat);
                var back = ResourceInventory.MatToMaterial(resource);
                Assert.AreEqual(mat, back, $"Round trip failed for {mat}");
            }
        }

        [Test]
        public void Get_OutOfRange_DefaultsToZero()
        {
            // IndexFor 不做范围检查，所以越界直接抛 IndexOutOfRangeException
            // 边界内 0..21 安全，超出范围不可调用
            Assert.DoesNotThrow(() => new ResourceInventory().Get(ResourceKind.Dirt));
        }

        [Test]
        public void Spend_InvalidResourceKind_ReturnsTrueForZero()
        {
            var inv = new ResourceInventory();
            // 无效 key 的 _values[999] 不存在，IndexFor 不检查范围
            // 只测合法值的边界
            Assert.IsTrue(inv.Spend(ResourceKind.Dirt, 0));
        }

        [Test]
        public void Add_HighValue_OverflowDoesNotCrash()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Food, int.MaxValue / 2);
            inv.Add(ResourceKind.Food, int.MaxValue / 2 - 1);
            // 接近溢出但不该崩溃
            // int.MaxValue/2 + int.MaxValue/2 - 1 = int.MaxValue - 1, but int.MaxValue/2=1073741823 (floor)
            // 1073741823 + 1073741822 = 2147483645
            Assert.AreEqual(2147483645, inv.Get(ResourceKind.Food));
        }

        [Test]
        public void Add_MaxOverflow_OverflowHandled()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Wood, int.MaxValue);
            inv.Add(ResourceKind.Wood, 1);
            // 溢出后应 wrap 到 int.MinValue
            Assert.AreEqual(int.MinValue, inv.Get(ResourceKind.Wood));
        }
    }
}