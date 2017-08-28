using InControl;
using UnityEngine;


public class PlayerActions : PlayerActionSet
{
	public PlayerAction Crouch;
	public PlayerAction Jump;
	public PlayerAction Punch;
	public PlayerAction Kick;
	public PlayerAction Left;
	public PlayerAction Right;
	public PlayerAction Forward;
	public PlayerAction Back;
	public PlayerTwoAxisAction Move;


	public PlayerActions()
	{
		Crouch = CreatePlayerAction ("Crouch");
		Jump = CreatePlayerAction( "Jump" );
		Left = CreatePlayerAction( "Move Left" );
		Right = CreatePlayerAction( "Move Right" );
		Forward = CreatePlayerAction( "Move Forward" );
		Back = CreatePlayerAction( "Move Back" );
		Punch = CreatePlayerAction ("Punch");
		Kick = CreatePlayerAction ("Kick");
		Move = CreateTwoAxisPlayerAction( Left, Right, Back, Forward );
	}


	public static PlayerActions CreateWithDefaultBindings()
	{
		Debug.Log ("Created Binding Set");
		PlayerActions playerActions = new PlayerActions();
		//playerActions.Device = device;
		//Debug.Log ("Created " + playerActions.Device + " " + playerActions.Device.Name);

	
		// How to set up mutually exclusive keyboard bindings with a modifier key.
		// playerActions.Back.AddDefaultBinding( Key.Shift, Key.Tab );
		// playerActions.Next.AddDefaultBinding( KeyCombo.With( Key.Tab ).AndNot( Key.Shift ) );


		//playerActions.Device = InputManager.
		playerActions.Jump.AddDefaultBinding (Key.Space);
		playerActions.Crouch.AddDefaultBinding (Key.LeftShift);
		playerActions.Left.AddDefaultBinding (Key.A);
		playerActions.Right.AddDefaultBinding (Key.D);
		playerActions.Forward.AddDefaultBinding (Key.W);
		playerActions.Back.AddDefaultBinding (Key.S);
		playerActions.Punch.AddDefaultBinding (Mouse.LeftButton);
		playerActions.Kick.AddDefaultBinding (Mouse.RightButton);

		playerActions.ListenOptions.IncludeUnknownControllers = true;
		playerActions.ListenOptions.MaxAllowedBindings = 4;
		//playerActions.ListenOptions.MaxAllowedBindingsPerType = 1;
		//playerActions.ListenOptions.AllowDuplicateBindingsPerSet = true;
		playerActions.ListenOptions.UnsetDuplicateBindingsOnSet = true;
		//playerActions.ListenOptions.IncludeMouseButtons = true;
		//playerActions.ListenOptions.IncludeModifiersAsFirstClassKeys = true;
		//playerActions.ListenOptions.IncludeMouseButtons = true;
		//playerActions.ListenOptions.IncludeMouseScrollWheel = true;

		playerActions.ListenOptions.OnBindingFound = ( action, binding ) => {
			if (binding == new KeyBindingSource( Key.Escape ))
			{
				action.StopListeningForBinding();
				return false;
			}
			return true;
		};

		playerActions.ListenOptions.OnBindingAdded += ( action, binding ) => {
			Debug.Log( "Binding added... " + binding.DeviceName + ": " + binding.Name );
		};

		playerActions.ListenOptions.OnBindingRejected += ( action, binding, reason ) => {
			Debug.Log( "Binding rejected... " + reason );
		};

		return playerActions;
	}
}

