namespace AlongJourney.Core;

using Godot;
using System.Collections.Generic;
using AlongJourney.Interfaces;

/// <summary>
/// 选择管理器：处理鼠标悬停检测和目标选择
/// 支持物体、瓦片、区域等多种选择类型（未来扩展）
/// </summary>
public partial class SelectionManager : Node
{
    [Signal]
    public delegate void HoverEnteredEventHandler(Node interactableNode);

    [Signal]
    public delegate void HoverExitedEventHandler(Node interactableNode);

    [ExportGroup("Selection Settings")]
    [Export] public uint SelectionCollisionLayer = 64; // 默认检测第6层（Hurtbox层）
    [Export] public float MaxSelectionDistance = 1000f; // 最大选择距离

    private IInteractable _currentHoveredTarget;
    private PhysicsDirectSpaceState2D _spaceState;

    public override void _Process(double delta)
    {
        UpdateHoverTarget();
    }

    /// <summary>
    /// 更新当前悬停的目标
    /// </summary>
    private void UpdateHoverTarget()
    {
        // 获取当前场景的空间状态
        var viewport = GetViewport();
        if (viewport == null)
        {
            return;
        }

        var camera = viewport.GetCamera2D();
        if (camera == null)
        {
            return;
        }

        Vector2 worldPos = camera.GetGlobalMousePosition();

        // 使用 PhysicsPointQueryParameters2D 进行点查询
        var query = new PhysicsPointQueryParameters2D
        {
            Position = worldPos,
            CollisionMask = SelectionCollisionLayer,
            CollideWithAreas = true,
            CollideWithBodies = false // 只检测 Area2D
        };

        _spaceState = GetViewport().GetWorld2D().DirectSpaceState;
        var results = _spaceState.IntersectPoint(query);

        IInteractable newTarget = null;

        // 查找最近的 IInteractable 对象
        foreach (var result in results)
        {
            var area = result["collider"].AsGodotObject() as Area2D;
            if (area == null)
            {
                continue;
            }

            // 向上查找 IInteractable 接口
            Node currentNode = area;
            while (currentNode != null)
            {
                if (currentNode is IInteractable interactable)
                {
                    newTarget = interactable;
                    break;
                }
                currentNode = currentNode.GetParent();
            }

            if (newTarget != null)
            {
                break;
            }
        }
        
        // 检查当前目标是否仍然有效（防止目标被销毁后引用失效）
        if (_currentHoveredTarget != null && _currentHoveredTarget is Node node)
        {
            if (!IsInstanceValid(node) || !node.IsInsideTree())
            {
                // 目标已被销毁，清除引用
                _currentHoveredTarget = null;
            }
        }

        // 如果目标发生变化，更新悬停状态
        if (newTarget != _currentHoveredTarget)
        {
            if (_currentHoveredTarget != null)
            {
                try
                {
                    _currentHoveredTarget.OnHoverExit();
                    // 发送信号时传递 Node 对象（因为信号不支持接口类型）
                    if (_currentHoveredTarget is Node currentNode)
                    {
                        EmitSignal(SignalName.HoverExited, currentNode);
                    }
                }
                catch (System.Exception)
                {
                    // 如果目标已被销毁，忽略错误
                }
            }

            _currentHoveredTarget = newTarget;

            if (_currentHoveredTarget != null)
            {
                try
                {
                    _currentHoveredTarget.OnHoverEnter();
                    // 发送信号时传递 Node 对象（因为信号不支持接口类型）
                    if (_currentHoveredTarget is Node newNode)
                    {
                        EmitSignal(SignalName.HoverEntered, newNode);
                    }
                }
                catch (System.Exception)
                {
                    // 如果目标已被销毁，忽略错误
                }
            }
        }
    }

    /// <summary>
    /// 获取当前悬停的目标
    /// </summary>
    public IInteractable GetCurrentHoveredTarget()
    {
        // 检查目标是否仍然有效
        if (_currentHoveredTarget != null && _currentHoveredTarget is Node node)
        {
            if (!IsInstanceValid(node) || !node.IsInsideTree())
            {
                // 目标已被销毁，清除引用
                _currentHoveredTarget = null;
            }
        }
        return _currentHoveredTarget;
    }

    /// <summary>
    /// 清除当前悬停的目标（手动清除，例如当目标被销毁时）
    /// </summary>
    public void ClearHoveredTarget()
    {
        if (_currentHoveredTarget != null)
        {
            _currentHoveredTarget.OnHoverExit();
            // 发送信号时传递 Node 对象（因为信号不支持接口类型）
            if (_currentHoveredTarget is Node node)
            {
                EmitSignal(SignalName.HoverExited, node);
            }
            _currentHoveredTarget = null;
        }
    }
}
