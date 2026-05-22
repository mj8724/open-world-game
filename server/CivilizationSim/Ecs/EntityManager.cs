namespace CivilizationSim.Ecs;

/// <summary>
/// 实体ID分配器，提供全局唯一的整数ID
/// </summary>
public class EntityManager
{
    private int _nextId = 1;

    /// <summary>分配一个新的实体ID</summary>
    public int CreateEntityId() => Interlocked.Increment(ref _nextId);

    /// <summary>当前已分配的最大ID</summary>
    public int CurrentMaxId => _nextId;
}
