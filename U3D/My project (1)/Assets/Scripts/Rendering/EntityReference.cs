using UnityEngine;

/// <summary>
/// 实体引用组件 — 挂在 3D 场景节点/军队上供射线检测选中
/// </summary>
public class EntityReference : MonoBehaviour
{
    public string entityType = ""; // "node" | "army" | "wild"
    public string nodeId = "";
    public int entityId;
}
