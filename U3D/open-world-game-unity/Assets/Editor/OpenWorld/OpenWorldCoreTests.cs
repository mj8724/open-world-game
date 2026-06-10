using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OpenWorld.Tests
{
    public class OpenWorldCoreTests
    {
        [Test]
        public void StrataGeneration_IsDeterministicForSeed()
        {
            var first = new OpenWorldState(128, 32, 8724);
            var second = new OpenWorldState(128, 32, 8724);
            var cell = new Vector2Int(73, 49);

            var a = first.GetCell(cell);
            var b = second.GetCell(cell);

            Assert.AreEqual(a.Height, b.Height);
            Assert.AreEqual(a.Layers.Length, b.Layers.Length);
            for (int i = 0; i < a.Layers.Length; i++)
            {
                Assert.AreEqual(a.Layers[i].Material, b.Layers[i].Material);
                Assert.AreEqual(a.Layers[i].Grade, b.Layers[i].Grade);
                Assert.AreEqual(a.Layers[i].RemainingAmount, b.Layers[i].RemainingAmount);
            }
        }

        [Test]
        public void LegacySaveMigration_InitializesNewCollectionsAndLayers()
        {
            var data = new OpenWorldSaveData
            {
                Version = 1,
                MapSize = 64,
                ChunkSize = 16,
                Seed = 42,
                Surveys = null,
                RailSchedules = null,
                ModifiedCells = new()
                {
                    new SavedCell
                    {
                        X = 4,
                        Z = 5,
                        Cell = new SurfaceCell { Layers = new[] { new MaterialLayer(GroundMaterial.IronOre, 4) } }
                    }
                }
            };

            OpenWorldSaveService.Migrate(data);

            Assert.AreEqual(2, data.Version);
            Assert.NotNull(data.Surveys);
            Assert.NotNull(data.RailSchedules);
            Assert.Greater(data.ModifiedCells[0].Cell.Layers[0].Grade, 0f);
            Assert.Greater(data.ModifiedCells[0].Cell.Layers[0].RemainingAmount, 0);
        }

        [Test]
        public void MiningRecipes_DoNotGenerateOreFromNothing()
        {
            Assert.IsFalse(OpenWorldDataCatalog.ProductionRecipes.Any(recipe =>
                recipe.Building == BuildableKind.MinePost &&
                recipe.Outputs.Any(output => output.Kind is ResourceKind.IronOre or ResourceKind.Coal)));
        }

        [Test]
        public void OilRequiresDerrickAndRefineryStages()
        {
            var fuelRecipe = OpenWorldDataCatalog.GetRecipe("oil-fuel");
            Assert.NotNull(fuelRecipe);
            Assert.AreEqual(BuildableKind.Refinery, fuelRecipe.Building);
            Assert.AreEqual(ResourceKind.Oil, fuelRecipe.Inputs[0].Kind);
            Assert.AreEqual(ResourceKind.Fuel, fuelRecipe.Outputs[0].Kind);
            Assert.AreEqual(TechEra.Industrial, OpenWorldDataCatalog.RequiredEraFor(BuildableKind.OilDerrick));
        }

        [Test]
        public void SurveyAndMiningState_RestoreWithIndexes()
        {
            var data = new OpenWorldSaveData { MapSize = 64, ChunkSize = 16, Seed = 7 };
            data.Surveys.Add(new SurveyRecord
            {
                Cell = new Vector2Int(12, 13),
                State = SurveyState.Drilled,
                Confidence = 1f,
                EstimatedMaterial = GroundMaterial.IronOre,
                MinReserve = 80,
                MaxReserve = 80
            });
            data.MiningZones.Add(new MiningZoneRecord { Id = 1, Center = new Vector2Int(12, 13), TargetMaterial = GroundMaterial.IronOre });

            var restored = new OpenWorldState(64, 16, 7);
            restored.RestoreFrom(data);

            Assert.AreEqual(SurveyState.Drilled, restored.GetSurvey(new Vector2Int(12, 13)).State);
            Assert.AreEqual(1, restored.MiningZones.Count);
        }
    }
}
