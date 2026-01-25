using Godot;

/// <summary>
/// Actor 相关的工具方法
/// </summary>
public static class ActorHelper
{
    /// <summary>
    /// 从给定节点向上查找 Actor 父节点
    /// </summary>
    public static Actor FindActorOwner(Node node)
    {
        if (node == null)
        {
            return null;
        }

        Node current = node.GetParent();
        while (current != null && !(current is Actor))
        {
            current = current.GetParent();
        }

        return current as Actor;
    }
}
