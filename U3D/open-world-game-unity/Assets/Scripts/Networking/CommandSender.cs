using System.Collections.Generic;

/// <summary>
/// CommandSender — 封装发送指令的便捷 API
/// 移植自 client/src/bridge/command-sender.js
/// </summary>
namespace Networking
{
    public class CommandSender
    {
        private readonly WebSocketClient _ws;

        public CommandSender(WebSocketClient ws)
        {
            _ws = ws;
        }

        // ─── 建造/升级 ───
        public void SendBuild(string nodeId, string buildingType) =>
            _ws.SendCommand("BUILD", new { nodeId, buildingType });

        public void SendPlaceBuilding(string nodeId, string buildingType, float localX, float localZ, float rotation = 0) =>
            _ws.SendCommand("PLACE_BUILDING", new { nodeId, buildingType, localX, localZ, rotation });

        public void SendBuildWall(string nodeId, float fromX, float fromZ, float toX, float toZ) =>
            _ws.SendCommand("BUILD_WALL", new { nodeId, fromX, fromZ, toX, toZ });

        public void SendDemolishBuilding(string nodeId, int buildingIndex) =>
            _ws.SendCommand("DEMOLISH_BUILDING", new { nodeId, buildingIndex });

        public void SendDemolishWall(string nodeId, int wallIndex) =>
            _ws.SendCommand("DEMOLISH_WALL", new { nodeId, wallIndex });

        // ─── 科技研发 ───
        public void SendResearch(string techId) =>
            _ws.SendCommand("RESEARCH", new { techId });

        // ─── 军事 ───
        public void SendAttack(string fromNodeId, string targetNodeId, int troopCount) =>
            _ws.SendCommand("ATTACK", new { fromNodeId, targetNodeId, troopCount });

        public void SendRetreat(string armyId) =>
            _ws.SendCommand("RETREAT", new { armyId });

        public void SendCreateCompany(string nodeId, string unitDefId = "MILITIA") =>
            _ws.SendCommand("CREATE_COMPANY", new { nodeId, unitDefId });

        public void SendMoveUnit(int entityId, string targetNodeId) =>
            _ws.SendCommand("MOVE_UNIT", new { entityId, targetNodeId });

        public void SendAttackNode(int entityId, string targetNodeId) =>
            _ws.SendCommand("ATTACK_NODE", new { entityId, targetNodeId });

        public void SendRetreatUnit(int entityId) =>
            _ws.SendCommand("RETREAT_UNIT", new { entityId });

        public void SendCreateFormation(string name, string formationType, int[] entityIds) =>
            _ws.SendCommand("CREATE_FORMATION", new { name, formationType, entityIds });

        // ─── 游戏控制 ───
        public void SendSetSpeed(int speed) =>
            _ws.SendCommand("SET_SPEED", new { speed });

        // ─── 交通 ───
        public void SendUpgradeEdge(string edgeId) =>
            _ws.SendCommand("UPGRADE_EDGE", new { edgeId });

        // ─── 物流 ───
        public void SendCreateRoute(string fromNodeId, string targetNodeId, string cargoType = "FOOD",
            string transportType = "PORTER", int transportCount = 1, int priority = 50, string routeMode = "MANUAL") =>
            _ws.SendCommand("CREATE_ROUTE", new { fromNodeId, targetNodeId, cargoType, transportType, transportCount, priority, routeMode });

        public void SendCancelRoute(int routeId) =>
            _ws.SendCommand("CANCEL_ROUTE", new { routeId });

        public void SendUpdateRoute(int routeId, IDictionary<string, object>? patch = null)
        {
            var payload = new Dictionary<string, object>
            {
                ["routeId"] = routeId,
                ["troopCount"] = routeId
            };

            if (patch != null)
            {
                foreach (var (key, value) in patch)
                    payload[key] = value;
            }

            _ws.SendCommand("UPDATE_ROUTE", payload);
        }

        // ─── 集结点 ───
        public void SendSetRallyPoint(string nodeId, object policies) =>
            _ws.SendCommand("SET_RALLY_POINT", new { nodeId, policies });

        public void SendClearRallyPoint(string nodeId) =>
            _ws.SendCommand("CLEAR_RALLY_POINT", new { nodeId });

        // ─── 运输工具生产 ───
        public void SendProduceTransport(string nodeId, string transportType, int quantity = 1) =>
            _ws.SendCommand("PRODUCE_TRANSPORT", new { nodeId, transportType, quantity });
    }
}
