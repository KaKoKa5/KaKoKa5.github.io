using CharacterController.SaverioScript.PlayerControllers.CharacterController.MoveState;
using CharacterController.Script;
using CharacterController.Script.PlayerControllers;

namespace CharacterController
{
	public class NormalWalk : IMoveState
	{
		public float MoveSpeed { get; set; }

		public void EnterState(CharacterSetup characterSetup)
		{
			MoveSpeed = characterSetup.CharacterData.playerVelocity;
		}

		public void UpdateState(CharacterSetup characterSetup)
		{
				var input = characterSetup.ReadInput;

				if (input.Interact && characterSetup.Grounded && !characterSetup.Sliding && characterSetup.Grabber.CanGrab()) 
				{ 
					characterSetup.GoTo(new GrabState()); 
					return;
				}
				if (characterSetup.Sliding)                     
				{ 
					characterSetup.GoTo(new SlideDownState()); 
					return;
				}
				if (input.isJumping && characterSetup.Climb)
				{ 
					characterSetup.GoTo(new ClimbState());     
					return;
				}
				if (input.IsFighting)
				{
					characterSetup.GoTo(new FightState());
					return;
				}
				if (input.IsRunning)
				{
					characterSetup.GoTo(new RunState());
					return;
				}
				if (input.IsCrunching)
				{ 
					characterSetup.GoTo(new CrounchState());
					return; 
				}
		}
		public void ExitState(CharacterSetup characterSetup)
		{
		}
	}
}
