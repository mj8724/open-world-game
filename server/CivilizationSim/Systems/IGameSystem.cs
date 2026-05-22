using CivilizationSim.Dict;
using CivilizationSim.Ecs;
using CivilizationSim.Utils;

namespace CivilizationSim.Systems;

/// <summary>游戏系统接口 — 所有 System 必须实现</summary>
public interface IGameSystem
{
    /// <summary>执行优先级（数字越小越先执行）</summary>
    int Order { get; }

    /// <summary>每 Tick 执行一次</summary>
    void Execute(World world, DictRegistry dict, GameLogger logger);
}
