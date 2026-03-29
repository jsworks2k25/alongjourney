namespace AlongJourney.Core;

using System;
using Godot;

public enum GamePhase
{
    MainMenu = 0,
    Playing = 1,
    Paused = 2,
}

/// <summary>
/// 全局流程：主菜单 / 游戏中 / 暂停。与 Actor 的 StateMachine 无关。
/// </summary>
public partial class GameFlow : Node
{
    public const string MainMenuScenePath = "res://UI/main_menu.tscn";

    [Signal]
    public delegate void PhaseChangedEventHandler(int phase);

    public GamePhase Current { get; private set; } = GamePhase.MainMenu;

    private CanvasLayer _pauseLayer;
    private Control _pauseRoot;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GetTree().SceneChanged += OnSceneChanged;
        CallDeferred(nameof(BuildPauseOverlay));
        CallDeferred(nameof(SyncPhaseFromCurrentScene));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsEcho())
        {
            return;
        }

        if (!Input.IsActionJustPressed("pause"))
        {
            return;
        }

        if (Current != GamePhase.Playing && Current != GamePhase.Paused)
        {
            return;
        }

        TogglePause();
        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 在 Playing 与 Paused 之间切换；其它阶段忽略。
    /// </summary>
    public void TogglePause()
    {
        if (Current == GamePhase.Playing)
        {
            SetPhase(GamePhase.Paused);
        }
        else if (Current == GamePhase.Paused)
        {
            SetPhase(GamePhase.Playing);
        }
    }

    public void SetPhase(GamePhase phase)
    {
        if (Current == phase)
        {
            ApplyTreePauseForPhase(phase);
            UpdatePauseOverlayVisibility(phase);
            return;
        }

        Current = phase;
        ApplyTreePauseForPhase(phase);
        UpdatePauseOverlayVisibility(phase);
        EmitSignal(SignalName.PhaseChanged, (int)phase);
    }

    public void EnterMainMenuPhase()
    {
        SetPhase(GamePhase.MainMenu);
    }

    public void EnterPlayingPhase()
    {
        SetPhase(GamePhase.Playing);
    }

    private void OnSceneChanged()
    {
        SyncPhaseFromCurrentScene();
    }

    private void SyncPhaseFromCurrentScene()
    {
        var scene = GetTree().CurrentScene;
        string path = scene?.SceneFilePath ?? string.Empty;
        bool isMainMenu = path.EndsWith("main_menu.tscn", StringComparison.OrdinalIgnoreCase);

        if (isMainMenu)
        {
            SetPhase(GamePhase.MainMenu);
        }
        else
        {
            SetPhase(GamePhase.Playing);
        }
    }

    private void ApplyTreePauseForPhase(GamePhase phase)
    {
        GetTree().Paused = phase == GamePhase.Paused;
    }

    private void UpdatePauseOverlayVisibility(GamePhase phase)
    {
        if (_pauseRoot == null)
        {
            return;
        }

        _pauseRoot.Visible = phase == GamePhase.Paused;
    }

    private void BuildPauseOverlay()
    {
        _pauseLayer = new CanvasLayer
        {
            Layer = 128,
            ProcessMode = ProcessModeEnum.Always,
        };
        AddChild(_pauseLayer);

        _pauseRoot = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.55f),
            ProcessMode = ProcessModeEnum.Always,
            Visible = false,
        };
        _pauseRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _pauseRoot.GrowHorizontal = Control.GrowDirection.Both;
        _pauseRoot.GrowVertical = Control.GrowDirection.Both;
        _pauseLayer.AddChild(_pauseRoot);

        var center = new CenterContainer
        {
            ProcessMode = ProcessModeEnum.Always,
        };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _pauseRoot.AddChild(center);

        var panel = new PanelContainer
        {
            ProcessMode = ProcessModeEnum.Always,
        };
        center.AddChild(panel);

        var vbox = new VBoxContainer
        {
            ProcessMode = ProcessModeEnum.Always,
        };
        panel.AddChild(vbox);

        var title = new Label
        {
            Text = "Paused",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        vbox.AddChild(title);

        var resume = new Button { Text = "Resume" };
        resume.ProcessMode = ProcessModeEnum.Always;
        resume.Pressed += OnPauseResumePressed;
        vbox.AddChild(resume);

        var toMenu = new Button { Text = "Main menu" };
        toMenu.ProcessMode = ProcessModeEnum.Always;
        toMenu.Pressed += OnPauseMainMenuPressed;
        vbox.AddChild(toMenu);
    }

    private void OnPauseResumePressed()
    {
        if (Current == GamePhase.Paused)
        {
            SetPhase(GamePhase.Playing);
        }
    }

    private void OnPauseMainMenuPressed()
    {
        GetTree().Paused = false;
        if (_pauseRoot != null)
        {
            _pauseRoot.Visible = false;
        }

        GetTree().ChangeSceneToFile(MainMenuScenePath);
    }
}
