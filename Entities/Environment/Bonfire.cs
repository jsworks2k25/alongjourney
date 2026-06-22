namespace AlongJourney.Entities.Environment;

using Godot;
using AlongJourney.Core;
using AlongJourney.Interfaces;

/// <summary>
/// 营火：玩家的核心保护目标，可被 AI 选为追击对象。
/// </summary>
public partial class Bonfire : StaticBody2D, ITargetable
{
    public bool IsAlive => true;

    public override void _Ready()
    {
        AddToGroup(GameConstants.BonfireGroupName);
    }
}
