using NUnit.Framework;
using OpenWorld;
using UnityEngine;

namespace OpenWorld.Tests
{
    [TestFixture]
    public class CombatSystemTests
    {
        private OpenWorldState _world;
        private OpenWorldCombatSystem _combat;

        [SetUp]
        public void SetUp()
        {
            _world = new OpenWorldState(128, 64, 1234);
            _combat = new OpenWorldCombatSystem(_world);
        }

        [Test]
        public void FindHostilesInGrid_WithVisionRangeSquared_FindsTargetAtMaxRange()
        {
            // 安排：放置两个敌对单位，距离正好等于视野半径
            var scout = _world.AddUnit(UnitKind.Scout, new Vector2Int(64, 64), OpenWorldConstants.PlayerFactionId);
            scout.VisionRange = 26; // Scout 标准视野

            var enemy = _world.AddUnit(UnitKind.Melee, new Vector2Int(64 + 26, 64), OpenWorldConstants.EnemyFactionId);
            enemy.Hp = 50;

            // 确保外交关系为敌对
            _world.Diplomacy.Add(new DiplomacyRecord
            {
                FactionA = OpenWorldConstants.PlayerFactionId,
                FactionB = OpenWorldConstants.EnemyFactionId,
                Stance = DiplomacyStance.Hostile
            });

            // 重建网格
            _combat.RebuildCombatGrid();

            // 执行：使用平方视野半径查找
            float radiusSqr = scout.VisionRange * scout.VisionRange; // 676
            var hostiles = _combat.FindHostilesInGrid(scout.Cell, radiusSqr, scout.FactionId, 10);

            // 断言：应该找到敌人（距离 26 格，视野半径 26）
            Assert.AreEqual(1, hostiles.Count, "应该在视野半径内找到敌人");
            Assert.AreEqual(enemy.Id, hostiles[0].Id);
        }

        [Test]
        public void FindHostilesInGrid_BeyondVisionRange_FindsNothing()
        {
            // 安排：敌人在视野外
            var scout = _world.AddUnit(UnitKind.Scout, new Vector2Int(64, 64), OpenWorldConstants.PlayerFactionId);
            scout.VisionRange = 26;

            var enemy = _world.AddUnit(UnitKind.Melee, new Vector2Int(64 + 27, 64), OpenWorldConstants.EnemyFactionId);
            enemy.Hp = 50;

            _world.Diplomacy.Add(new DiplomacyRecord
            {
                FactionA = OpenWorldConstants.PlayerFactionId,
                FactionB = OpenWorldConstants.EnemyFactionId,
                Stance = DiplomacyStance.Hostile
            });

            _combat.RebuildCombatGrid();

            // 执行
            float radiusSqr = scout.VisionRange * scout.VisionRange;
            var hostiles = _combat.FindHostilesInGrid(scout.Cell, radiusSqr, scout.FactionId, 10);

            // 断言：超出视野，应该找不到
            Assert.AreEqual(0, hostiles.Count, "超出视野半径不应找到敌人");
        }

        [Test]
        public void FindHostilesInGrid_MultipleTargets_ReturnsClosestFirst()
        {
            // 安排：多个敌人，不同距离
            var unit = _world.AddUnit(UnitKind.Scout, new Vector2Int(64, 64), OpenWorldConstants.PlayerFactionId);
            unit.VisionRange = 20;

            var nearEnemy = _world.AddUnit(UnitKind.Melee, new Vector2Int(64 + 5, 64), OpenWorldConstants.EnemyFactionId);
            nearEnemy.Hp = 50;

            var farEnemy = _world.AddUnit(UnitKind.Melee, new Vector2Int(64 + 15, 64), OpenWorldConstants.EnemyFactionId);
            farEnemy.Hp = 50;

            _world.Diplomacy.Add(new DiplomacyRecord
            {
                FactionA = OpenWorldConstants.PlayerFactionId,
                FactionB = OpenWorldConstants.EnemyFactionId,
                Stance = DiplomacyStance.Hostile
            });

            _combat.RebuildCombatGrid();

            // 执行
            float radiusSqr = unit.VisionRange * unit.VisionRange;
            var hostiles = _combat.FindHostilesInGrid(unit.Cell, radiusSqr, unit.FactionId, 10);

            // 断言：应该两个都找到，近的在前
            Assert.AreEqual(2, hostiles.Count);
            Assert.AreEqual(nearEnemy.Id, hostiles[0].Id, "最近的敌人应该排第一");
            Assert.AreEqual(farEnemy.Id, hostiles[1].Id);
        }

        [Test]
        public void AreHostile_PlayerVsEnemy_ReturnsTrue()
        {
            Assert.IsTrue(_combat.AreHostile(OpenWorldConstants.PlayerFactionId, OpenWorldConstants.EnemyFactionId));
            Assert.IsTrue(_combat.AreHostile(OpenWorldConstants.EnemyFactionId, OpenWorldConstants.PlayerFactionId));
        }

        [Test]
        public void AreHostile_PlayerVsNeutral_ReturnsFalse()
        {
            // 中性阵营默认不敌对
            Assert.IsFalse(_combat.AreHostile(OpenWorldConstants.PlayerFactionId, OpenWorldConstants.NeutralFactionId));
            Assert.IsFalse(_combat.AreHostile(OpenWorldConstants.NeutralFactionId, OpenWorldConstants.EnemyFactionId));
        }

        [Test]
        public void AreHostile_WithDiplomacyRecord_UsesRecordStance()
        {
            // 安排：玩家与中性阵营外交记录设为敌对
            _world.Diplomacy.Add(new DiplomacyRecord
            {
                FactionA = OpenWorldConstants.PlayerFactionId,
                FactionB = OpenWorldConstants.NeutralFactionId,
                Stance = DiplomacyStance.Hostile
            });

            // 执行/断言：外交记录覆盖默认规则
            Assert.IsTrue(_combat.AreHostile(OpenWorldConstants.PlayerFactionId, OpenWorldConstants.NeutralFactionId));
        }

        [Test]
        public void AreHostile_SameFaction_ReturnsFalse()
        {
            Assert.IsFalse(_combat.AreHostile(OpenWorldConstants.PlayerFactionId, OpenWorldConstants.PlayerFactionId));
        }

        [Test]
        public void CombatGridKey_ConsistentForSameCell()
        {
            var key1 = _combat.CombatGridKey(new Vector2Int(10, 20));
            var key2 = _combat.CombatGridKey(new Vector2Int(10, 20));
            Assert.AreEqual(key1, key2);
        }

        [Test]
        public void CombatGridKey_SameForNearbyCells()
        {
            // CombatGridCellSize = 12, 所以 (10,20) 和 (11,21) 应该在同一格
            var key1 = _combat.CombatGridKey(new Vector2Int(10, 20));
            var key2 = _combat.CombatGridKey(new Vector2Int(11, 21));
            Assert.AreEqual(key1, key2, "相邻单元格应映射到同一战斗网格");
        }
    }
}
