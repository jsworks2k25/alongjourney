namespace AlongJourney.Interfaces;

using Godot;
using AlongJourney.Entities.Items;

/// <summary>
/// 可交互对象的类型
/// </summary>
public enum InteractionType
{
    Object,  // 物体（树、敌人、NPC等）
    Tile,    // 瓦片（未来扩展）
    Area     // 区域（未来扩展）
}

/// <summary>
/// 可交互对象接口：用于解耦选中和交互逻辑
/// 支持未来扩展：物体选中、瓦片选中、区域选中
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 获取交互类型
    /// </summary>
    InteractionType GetInteractionType();

    /// <summary>
    /// 获取交互位置（用于计算距离和方向）
    /// </summary>
    Vector2 GetInteractionPosition();

    /// <summary>
    /// 检查是否可以用特定武器进行交互
    /// </summary>
    /// <param name="weapon">武器实例</param>
    /// <returns>是否可以交互</returns>
    bool CanInteractWith(Weapon weapon);

    /// <summary>
    /// 悬停进入时调用
    /// </summary>
    void OnHoverEnter();

    /// <summary>
    /// 悬停退出时调用
    /// </summary>
    void OnHoverExit();
}
