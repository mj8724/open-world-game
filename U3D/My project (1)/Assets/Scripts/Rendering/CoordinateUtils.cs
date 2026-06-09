using UnityEngine;

/// <summary>
/// 坐标空间转换 — 后端 (x, y) ↔ 3D 世界 (x, y, z)
/// 移植自 client/src/map3d/world-space.js
/// </summary>
public static class CoordinateUtils
{
    /// <summary>后端坐标 → 3D 世界 XZ（不含高度）</summary>
    public static Vector2 ToWorldXZ(float backendX, float backendY)
    {
        return new Vector2(
            backendX * Constants.WorldScale,
            backendY * Constants.WorldScale
        );
    }

    /// <summary>3D 世界坐标 → 后端坐标</summary>
    public static Vector2 ToBackendXZ(float worldX, float worldZ)
    {
        return new Vector2(
            worldX / Constants.WorldScale,
            worldZ / Constants.WorldScale
        );
    }

    /// <summary>后端坐标 → 完整 3D 世界位置（含高度查询）</summary>
    public static Vector3 ToWorldPos(float backendX, float backendY, System.Func<float, float, float> getHeightAt)
    {
        var xz = ToWorldXZ(backendX, backendY);
        float y = getHeightAt?.Invoke(xz.x, xz.y) ?? 0f;
        return new Vector3(xz.x, y, xz.y);
    }

    /// <summary>后端距离 → 3D 世界距离</summary>
    public static float ToWorldDistance(float backendDist)
    {
        return backendDist * Constants.WorldScale;
    }

    /// <summary>3D 世界距离 → 后端距离</summary>
    public static float ToBackendDistance(float worldDist)
    {
        return worldDist / Constants.WorldScale;
    }
}
