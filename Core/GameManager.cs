namespace AlongJourney.Core;

using Godot;
using AlongJourney.Entities.Player;
using AlongJourney.Components;

public partial class GameManager : Node
{
    [ExportGroup("Respawn Settings")]
    [Export] public float RespawnDelay = 3.0f;

    private const string SpawnPointGroup = "PlayerSpawn";
    private PackedScene _playerScene;

    private Player _currentPlayer;
    private Node _playerParent;
    private Vector2 _spawnPosition;
    private uint _playerCollisionLayer;
    private uint _playerCollisionMask;
    private bool _respawnInProgress;

    public override void _Ready()
    {
        GetTree().NodeAdded += OnNodeAdded;
        GetTree().SceneChanged += OnSceneChanged;
        CallDeferred(nameof(TryInitializeFromScene));
    }

    private void OnSceneChanged()
    {
        ClearPlayerSubscription();
        _respawnInProgress = false;
        CallDeferred(nameof(TryInitializeFromScene));
    }

    private void OnNodeAdded(Node node)
    {
        if (node is Player player)
        {
            SetupPlayer(player);
        }
    }

    private void TryInitializeFromScene()
    {
        var player = FindPlayer();
        if (player != null)
        {
            SetupPlayer(player);
        }
        else
        {
            GD.PushWarning("GameManager: No Player found in current scene.");
        }
    }

    private Player FindPlayer()
    {
        return GetTree().GetFirstNodeInGroup(GameConfig.GetPlayerGroupName()) as Player;
    }

    private void SetupPlayer(Player player)
    {
        if (_currentPlayer == player)
        {
            return;
        }

        ClearPlayerSubscription();
        CacheSpawnContext(player);
        RegisterPlayer(player);
    }

    private void CacheSpawnContext(Player player)
    {
        _currentPlayer = player;
        _playerParent = player.GetParent();
        _spawnPosition = ResolveSpawnPosition(player);
        _playerCollisionLayer = player.CollisionLayer;
        _playerCollisionMask = player.CollisionMask;

        if (_playerScene == null)
        {
            _playerScene = ResolvePlayerScene(player);
        }
    }

    private Vector2 ResolveSpawnPosition(Player player)
    {
        var spawnNode = GetTree().GetFirstNodeInGroup(SpawnPointGroup) as Node2D;
        if (spawnNode != null)
        {
            return spawnNode.GlobalPosition;
        }

        return player.GlobalPosition;
    }

    private PackedScene ResolvePlayerScene(Player player)
    {
        if (!string.IsNullOrEmpty(player.SceneFilePath))
        {
            return ResourceLoader.Load<PackedScene>(player.SceneFilePath);
        }

        return ResourceLoader.Load<PackedScene>("res://scenes/player.tscn");
    }

    private void RegisterPlayer(Player player)
    {
        _currentPlayer = player;
        _currentPlayer.PlayerDied += OnPlayerDied;
    }

    private void ClearPlayerSubscription()
    {
        if (_currentPlayer != null)
        {
            _currentPlayer.PlayerDied -= OnPlayerDied;
        }
        _currentPlayer = null;
    }

    private async void OnPlayerDied(Player player)
    {
        if (_respawnInProgress) return;
        _respawnInProgress = true;

        // 先清理信号订阅，避免在删除过程中触发信号
        ClearPlayerSubscription();

        float delay = GetRespawnDelay();
        if (delay > 0f)
        {
            await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        }

        if (!IsInsideTree())
        {
            _respawnInProgress = false;
            return;
        }

        if (IsInstanceValid(player))
        {
            player.QueueFree();
            if (player.IsInsideTree())
            {
                await ToSignal(player, Node.SignalName.TreeExited);
            }
        }

        if (!IsInsideTree())
        {
            _respawnInProgress = false;
            return;
        }

        SpawnPlayer();
        _respawnInProgress = false;
    }

    private float GetRespawnDelay()
    {
        return RespawnDelay;
    }

    private void SpawnPlayer()
    {
        if (_playerScene == null)
        {
            GD.PushError("GameManager: Player scene not set.");
            return;
        }

        Node playerNode = _playerScene.Instantiate();
        Node parent = _playerParent ?? GetTree().CurrentScene ?? this;
        parent.AddChild(playerNode);

        // 与 basic 等场景中实例名 "Player" 及 PhantomCamera follow_target 路径 "../ObjectLayer/Player" 保持一致
        playerNode.Name = "Player";

        if (playerNode is Node2D node2D)
        {
            node2D.GlobalPosition = _spawnPosition;
        }

        if (playerNode is Player player)
        {
            player.CollisionLayer = _playerCollisionLayer;
            player.CollisionMask = _playerCollisionMask;
            
            // 确保新玩家处于正确状态：启用碰撞和受击盒
            player.SetCollisionEnabled(true);
            player.SetHurtboxEnabled(true);
            
            RegisterPlayer(player);
            UpdatePhantomCameraFollowTarget(player);
        }
        else
        {
            GD.PushError("GameManager: Instanced player is not a Player.");
        }
    }

    /// <summary>
    /// 将场景中的 PhantomCamera2D 的 follow_target 设为新玩家，解决重生后跟丢的问题。
    /// </summary>
    private void UpdatePhantomCameraFollowTarget(Node2D newPlayer)
    {
        var root = GetTree().CurrentScene;
        if (root == null) return;

        var pcam = root.GetNodeOrNull<Node2D>("PhantomCamera2D");
        if (pcam == null) return;

        pcam.Call("set_follow_target", newPlayer);
    }
}
