namespace AlongJourney.Entities.Characters.AICharacters;

using Godot;

public partial class Robot0 : AICharacter
{
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (AnimationController != null)
		{
			AnimationController.UpdateAnimation();
		}
	}
}
