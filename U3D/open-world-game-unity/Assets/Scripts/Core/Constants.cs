/// <summary>
/// 3D 地图常量配置 — 移植自 client/src/map3d/constants.js
/// 所有魔法数字集中于此，便于调整
/// </summary>
public static class Constants
{
    // ─── 坐标缩放 ───
    /// <summary>后端坐标 → 3D 世界坐标的缩放因子</summary>
    public const float WorldScale = 0.05f;
    /// <summary>高度缩放因子</summary>
    public const float ElevationScale = 1.0f;

    // ─── 地形配置 ───
    public static readonly TerrainConfigEntry[] TerrainConfig = {
        new("PLAINS",   0,    0x4a7c3f, "#4a7c3f", 0.8f),
        new("MOUNTAIN", 8,    0x8b8b8b, "#8b8b8b", 1.0f),
        new("HILLS",    4,    0x6b8e4e, "#6b8e4e", 0.9f),
        new("COASTAL",  -0.5f,0xc2b280, "#c2b280", 0.6f),
        new("FOREST",   1,    0x2d5a1e, "#2d5a1e", 1.0f),
        new("DESERT",   0.5f, 0xd4a855, "#d4a855", 0.7f),
        new("RUINS",    1.5f, 0x6b5b4f, "#6b5b4f", 0.85f),
        new("SWAMP",    -0.3f,0x3a5c3a, "#3a5c3a", 0.95f),
    };

    // ─── 势力颜色 ───
    public static readonly FactionColorEntry[] FactionColors = {
        new("PLAYER",  0x2563EB, "#2563EB"),
        new("AI",      0xDC2626, "#DC2626"),
        new("NEUTRAL", 0x9CA3AF, "#9CA3AF"),
    };

    // ─── 建筑模型尺寸 ───
    public static readonly BuildingModelConfig[] BuildingModels = {
        new("FARM",          1.2f, 1.0f, 0.4f,  0.1f,  0xd4a017, 0x8B4513),
        new("MINE",          1.0f, 1.0f, 0.6f,  0.15f, 0x696969, 0x4a4a4a),
        new("ARSENAL",       1.4f, 1.0f, 0.5f,  0.2f,  0x8B0000, 0x5c0000),
        new("ORACLE_BEACON", 0.6f, 0.6f, 1.2f,  0.3f,  0xDAA520, 0xFFD700),
        new("HALL",          1.6f, 1.4f, 0.8f,  0f,    0xF5DEB3, 0x8B4513),
    };

    // ─── 城墙参数 ───
    public static class WallConfig
    {
        public const float Thickness = 0.15f;
        public const float HeightBase = 0.8f;
        public const float HeightPerLevel = 0.4f;
        public const float BattlementWidth = 0.1f;
        public const float BattlementHeight = 0.15f;
        public const float BattlementSpacing = 0.3f;
        public const float CornerTowerSize = 0.4f;
        public const float CornerTowerHeight = 1.2f;
    }

    // ─── 城市参数 ───
    public static class CityConfig
    {
        /// <summary>城市影响半径（3D 世界单位）</summary>
        public const float InfluenceRadius = 5.0f;
        public const float PlatformHeight = 0.1f;
        public const float FlagPoleHeight = 1.5f;
        public const float FlagSize = 0.4f;
        public const float SelectionRingRadius = 3.0f;
        public const float SelectionRingHeight = 0.05f;
    }

    // ─── 道路参数 ───
    public static readonly RoadConfigEntry[] RoadConfigs = {
        new("ROAD",    0.3f,  0x8B7355, 20),
        new("TRAIL",   0.15f, 0xA0937D, 20),
        new("RAILWAY", 0.25f, 0x4a4a4a, 20),
    };

    // ─── 军队参数 ───
    public static class ArmyConfig
    {
        public const float SoldierHeight = 0.25f;
        public const float SoldierRadius = 0.08f;
        public const float FormationSpacing = 0.2f;
        public const int MaxVisibleSoldiers = 12;
        public const float HealthBarWidth = 0.5f;
        public const float HealthBarHeight = 0.05f;
    }

    // ─── 相机默认值 ───
    public static class CameraDefaults
    {
        public const float Fov = 60f;
        public const float Near = 0.1f;
        public const float Far = 500f;
        public const float PolarAngle = 45f;        // 45° 俯视（度）
        public const float AzimuthalAngle = -45f;
        public const float Distance = 30f;
        public const float MinDistance = 5f;
        public const float MaxDistance = 80f;
        public const float MinPolarAngle = 8.5f;     // 接近正上方（度）
        public const float MaxPolarAngle = 82f;      // 不太水平（度）
        public const float DampingFactor = 0.1f;
        public const float FocusLerpSpeed = 0.05f;
    }

    // ─── 小地图 ───
    public static class MinimapConfig
    {
        public const int Width = 200;
        public const int Height = 150;
        public const int BorderWidth = 2;
        public const string BorderColor = "#4a5568";
        public const uint BackgroundColor = 0x1a1a2e;
        public const uint ViewportColor = 0xffffff;
        public const float ViewportOpacity = 0.3f;
    }

    // ─── 天空/环境 ───
    public static class EnvConfig
    {
        public const uint SkyColor = 0x87CEEB;
        public const uint GroundColor = 0x4a7c3f;
        public const float FogNear = 40f;
        public const float FogFar = 80f;
        public const float AmbientLightIntensity = 0.5f;
        public const float DirectionalLightIntensity = 0.8f;
        public const uint HemisphereSkyColor = 0x87CEEB;
        public const uint HemisphereGroundColor = 0x4a7c3f;
        public const float HemisphereIntensity = 0.3f;
        public const int ShadowMapSize = 2048;
        public const float ShadowCameraSize = 50f;
    }

    // ─── 地形生成 ───
    public static class TerrainGen
    {
        /// <summary>地形网格细分数</summary>
        public const int GridSize = 200;
        /// <summary>地形世界空间大小</summary>
        public const float WorldSize = 60f;
        /// <summary>IDW 插值幂次</summary>
        public const float HeightBlendPower = 2f;
        /// <summary>节点影响最大距离</summary>
        public const float MaxInfluenceDistance = 30f;
        /// <summary>水面高度</summary>
        public const float WaterLevel = -0.2f;
        public const uint WaterColor = 0x1e90ff;
        public const float WaterOpacity = 0.6f;
    }

    // ─── 野外实体 ───
    public static class WildEntityConfig
    {
        public const float FlagPoleHeight = 1.0f;
        public const float CaptureRadius = 2.0f;
    }
}

// ─── 辅助数据结构 ───

public readonly struct TerrainConfigEntry
{
    public readonly string Name;
    public readonly float Elevation;
    public readonly int Color;
    public readonly string CssColor;
    public readonly float Roughness;

    public TerrainConfigEntry(string name, float elevation, int color, string cssColor, float roughness)
    {
        Name = name;
        Elevation = elevation;
        Color = color;
        CssColor = cssColor;
        Roughness = roughness;
    }
}

public readonly struct FactionColorEntry
{
    public readonly string Name;
    public readonly int Color3D;
    public readonly string CssColor;
    public FactionColorEntry(string name, int color3D, string cssColor)
    {
        Name = name; Color3D = color3D; CssColor = cssColor;
    }
}

public readonly struct BuildingModelConfig
{
    public readonly string Id;
    public readonly float Width;
    public readonly float Depth;
    public readonly float HeightBase;
    public readonly float HeightPerLevel;
    public readonly int Color;
    public readonly int RoofColor;
    public BuildingModelConfig(string id, float w, float d, float hBase, float hPerLevel, int color, int roofColor)
    {
        Id = id; Width = w; Depth = d; HeightBase = hBase; HeightPerLevel = hPerLevel; Color = color; RoofColor = roofColor;
    }
}

public readonly struct RoadConfigEntry
{
    public readonly string Type;
    public readonly float Width;
    public readonly int Color;
    public readonly int Segments;
    public RoadConfigEntry(string type, float width, int color, int segments)
    {
        Type = type; Width = width; Color = color; Segments = segments;
    }
}
