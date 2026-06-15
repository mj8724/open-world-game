using NUnit.Framework;

namespace OpenWorld.Tests
{
    /// <summary>
    /// OpenWorldDataCatalog 核心查询测试
    /// </summary>
    public class OpenWorldDataCatalogTests
    {
        [Test]
        public void CanSpend_EnoughResources_ReturnsTrue()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Food, 100);
            inv.Add(ResourceKind.Wood, 50);

            var amounts = new[] { new ResourceAmount(ResourceKind.Food, 30), new ResourceAmount(ResourceKind.Wood, 20) };
            bool can = OpenWorldDataCatalog.CanSpend(inv, amounts, out string missing);
            Assert.IsTrue(can);
            Assert.AreEqual("", missing);
        }

        [Test]
        public void CanSpend_NotEnough_ReturnsFalseAndMissing()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Food, 10);

            var amounts = new[] { new ResourceAmount(ResourceKind.Food, 50) };
            bool can = OpenWorldDataCatalog.CanSpend(inv, amounts, out string missing);
            Assert.IsFalse(can);
            Assert.IsNotEmpty(missing);
            Assert.IsTrue(missing.Contains("Food"));
        }

        [Test]
        public void CanSpend_EmptyList_ReturnsTrue()
        {
            var inv = new ResourceInventory();
            var amounts = new ResourceAmount[0];
            bool can = OpenWorldDataCatalog.CanSpend(inv, amounts, out string missing);
            Assert.IsTrue(can);
            Assert.AreEqual("", missing);
        }

        [Test]
        public void Spend_DeductsAllAmounts()
        {
            var inv = new ResourceInventory();
            inv.Add(ResourceKind.Food, 100);
            inv.Add(ResourceKind.Wood, 100);
            inv.Add(ResourceKind.Stone, 100);

            var amounts = new[] {
                new ResourceAmount(ResourceKind.Food, 20),
                new ResourceAmount(ResourceKind.Wood, 30),
                new ResourceAmount(ResourceKind.Stone, 40)
            };
            OpenWorldDataCatalog.Spend(inv, amounts);

            Assert.AreEqual(80, inv.Get(ResourceKind.Food));
            Assert.AreEqual(70, inv.Get(ResourceKind.Wood));
            Assert.AreEqual(60, inv.Get(ResourceKind.Stone));
        }

        [Test]
        public void Add_AddsAllAmounts()
        {
            var inv = new ResourceInventory();
            var amounts = new[] {
                new ResourceAmount(ResourceKind.IronIngot, 10),
                new ResourceAmount(ResourceKind.Steel, 20)
            };
            OpenWorldDataCatalog.Add(inv, amounts);

            Assert.AreEqual(10, inv.Get(ResourceKind.IronIngot));
            Assert.AreEqual(20, inv.Get(ResourceKind.Steel));
        }

        [Test]
        public void EraUnlocked_SameEra_ReturnsTrue()
        {
            Assert.IsTrue(OpenWorldDataCatalog.EraUnlocked(TechEra.Iron, TechEra.Iron));
            Assert.IsTrue(OpenWorldDataCatalog.EraUnlocked(TechEra.Aviation, TechEra.Aviation));
        }

        [Test]
        public void EraUnlocked_HigherThanRequired_ReturnsTrue()
        {
            Assert.IsTrue(OpenWorldDataCatalog.EraUnlocked(TechEra.Industrial, TechEra.Iron));
            Assert.IsTrue(OpenWorldDataCatalog.EraUnlocked(TechEra.Aviation, TechEra.WoodStone));
        }

        [Test]
        public void EraUnlocked_LowerThanRequired_ReturnsFalse()
        {
            Assert.IsFalse(OpenWorldDataCatalog.EraUnlocked(TechEra.WoodStone, TechEra.Iron));
            Assert.IsFalse(OpenWorldDataCatalog.EraUnlocked(TechEra.Iron, TechEra.Aviation));
        }

        [Test]
        public void GetRecipe_ValidId_ReturnsRecipe()
        {
            // 验证几个关键配方存在
            var foodRecipe = OpenWorldDataCatalog.GetRecipe("lumber-wood");
            Assert.NotNull(foodRecipe);

            var ironRecipe = OpenWorldDataCatalog.GetRecipe("smelt-iron");
            Assert.NotNull(ironRecipe);
        }

        [Test]
        public void GetRecipe_InvalidId_ReturnsNull()
        {
            Assert.IsNull(OpenWorldDataCatalog.GetRecipe("nonexistent-recipe-id"));
            Assert.IsNull(OpenWorldDataCatalog.GetRecipe(""));
        }

        [Test]
        public void GetUnit_AllKinds_ReturnsNonNull()
        {
            var kinds = new[] {
                UnitKind.Worker, UnitKind.Hauler, UnitKind.Militia, UnitKind.Melee,
                UnitKind.Spearman, UnitKind.Ranged, UnitKind.Musketeer, UnitKind.Rifleman,
                UnitKind.MachineGunner, UnitKind.Artillery, UnitKind.Engineer,
                UnitKind.Scout, UnitKind.Medic
            };
            foreach (var kind in kinds)
            {
                var def = OpenWorldDataCatalog.GetUnit(kind);
                Assert.NotNull(def, $"Unit def should exist for {kind}");
                Assert.AreEqual(kind, def.Kind);
            }
        }

        [Test]
        public void GetUnit_InvalidKind_ReturnsNull()
        {
            Assert.IsNull(OpenWorldDataCatalog.GetUnit((UnitKind)999));
        }

        [Test]
        public void GetVehicle_AllKinds_ReturnsNonNull()
        {
            var kinds = new[] {
                VehicleKind.HandCart, VehicleKind.Wagon, VehicleKind.Truck,
                VehicleKind.ArmoredCar, VehicleKind.Locomotive, VehicleKind.CargoWagon
            };
            foreach (var kind in kinds)
            {
                var def = OpenWorldDataCatalog.GetVehicle(kind);
                Assert.NotNull(def, $"Vehicle def should exist for {kind}");
                Assert.AreEqual(kind, def.Kind);
            }
        }

        [Test]
        public void StorageCapacityFor_KnownBuildings_ReturnsPositive()
        {
            Assert.Greater(OpenWorldDataCatalog.StorageCapacityFor(BuildableKind.Warehouse), 0);
            Assert.Greater(OpenWorldDataCatalog.StorageCapacityFor(BuildableKind.TownCenter), 0);
        }

        [Test]
        public void StorageCapacityFor_NonStorage_ReturnsZero()
        {
            Assert.AreEqual(0, OpenWorldDataCatalog.StorageCapacityFor(BuildableKind.Wall));
            Assert.AreEqual(0, OpenWorldDataCatalog.StorageCapacityFor(BuildableKind.Tower));
        }

        [Test]
        public void IsStorageNode_KnownBuildings_ReturnsTrue()
        {
            Assert.IsTrue(OpenWorldDataCatalog.IsStorageNode(BuildableKind.Warehouse));
            Assert.IsTrue(OpenWorldDataCatalog.IsStorageNode(BuildableKind.TownCenter));
        }

        [Test]
        public void RequiredEraFor_AllBuildableKinds_DoesNotThrow()
        {
            // 验证所有建筑类型都有合法科技要求
            for (int i = 0; i <= (int)BuildableKind.Refinery; i++)
            {
                var era = OpenWorldDataCatalog.RequiredEraFor((BuildableKind)i);
                Assert.GreaterOrEqual((int)era, 0);
            }
        }

        [Test]
        public void GetTech_ValidId_ReturnsTech()
        {
            var tech = OpenWorldDataCatalog.GetTech("Iron Working");
            Assert.NotNull(tech);
            Assert.AreEqual("Iron Working", tech.Id);
        }

        [Test]
        public void GetTech_InvalidId_ReturnsNull()
        {
            Assert.IsNull(OpenWorldDataCatalog.GetTech("nonexistent-tech"));
        }

        [Test]
        public void UnitKinds_List_ContainsAllKinds()
        {
            var kinds = OpenWorldDataCatalog.UnitKinds;
            Assert.IsNotNull(kinds);
            Assert.Greater(kinds.Count, 0);
            // 至少包含 Worker
            Assert.IsNotNull(System.Linq.Enumerable.FirstOrDefault(kinds, u => u.Kind == UnitKind.Worker));
        }

        [Test]
        public void Vehicles_List_ContainsTruckAndWagon()
        {
            var vehicles = OpenWorldDataCatalog.Vehicles;
            Assert.IsNotNull(vehicles);
            Assert.Greater(vehicles.Count, 2);
        }

        [Test]
        public void ProductionRecipes_List_HasRecipes()
        {
            var recipes = OpenWorldDataCatalog.ProductionRecipes;
            Assert.IsNotNull(recipes);
            Assert.Greater(recipes.Count, 5); // 至少有几个配方
        }

        [Test]
        public void Techs_List_HasTechs()
        {
            var techs = OpenWorldDataCatalog.Techs;
            Assert.IsNotNull(techs);
            Assert.Greater(techs.Count, 3);
        }
    }
}