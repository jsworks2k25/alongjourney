namespace AlongJourney.UI;

using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("Center/VBox/Start").Pressed += OnStartPressed;
        GetNode<Button>("Center/VBox/Quit").Pressed += OnQuitPressed;
    }

    private void OnStartPressed()
    {
        GetTree().ChangeSceneToFile("res://basic.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
