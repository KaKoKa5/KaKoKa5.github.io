using CharacterController.SaverioScript.PlayerControllers.CharacterController;
using CharacterController.Script.PlayerControllers;
using CharacterController.Script.PlayerControllers.Utilities;
using CharacterController.Utilities;
using UnityEngine;

namespace CharacterController.Script
{
	public class MovementBase : MonoBehaviour,IAptitude
	{
		[field:SerializeField] public UnityEngine.CharacterController Controller { get;private set; }
		[field:SerializeField] public Vector3 CurrentMoveDirection { get; private set; }
		public float VerticalVelocity => verticalVelocity;
		
		private Vector3 slideVelocity;
		private float verticalVelocity;
		private static readonly int XHash = Animator.StringToHash("X");
		private static readonly int YHash = Animator.StringToHash("Y");
		
		public void Initialize(CharacterSetup setup)
		{
		}
		public void HandleRotation(float rotationSpeed,Camera cam)
		{
			var camForward = cam.transform.forward;
			camForward.y = 0f;
			if (camForward.sqrMagnitude < 0.001f) return;

			var targetRotation = Quaternion.LookRotation(camForward.normalized);
			Controller.transform.rotation = Quaternion.Slerp(Controller.transform.rotation, targetRotation,
				rotationSpeed * Time.deltaTime);
		}
		public void HandleMovement(CharacterSetup characterSetup, Camera cam)
		{
			var data = characterSetup.CharacterData;
			var direction = characterSetup.ReadInput.Direction;

			var camForward = cam.transform.forward; camForward.y = 0f; camForward.Normalize();
			var camRight   = cam.transform.right;   camRight.y   = 0f; camRight.Normalize();
			var moveDir = (camForward * direction.y + camRight * direction.x).normalized;
			var hasInput = direction.magnitude > 0.1f;

			if (characterSetup.Grounded)
			{
				var target = moveDir * characterSetup.MoveState.MoveSpeed;
				var rate = hasInput ? data.playerAcceleration : data.playerDeceleration;
				CurrentMoveDirection = Vector3.MoveTowards(CurrentMoveDirection, target, rate * Time.deltaTime);
				characterSetup.airSpeedCap = CurrentMoveDirection.magnitude;
			}
			else if (characterSetup.Sliding)
			{
				var target = hasInput ? moveDir * data.playerVelocity : Vector3.zero;
				CurrentMoveDirection = Vector3.MoveTowards(CurrentMoveDirection, target,
					data.slideControl * Time.deltaTime);
				characterSetup.airSpeedCap = CurrentMoveDirection.magnitude;
			}
			else if (hasInput)
			{
				var cap = Mathf.Max(characterSetup.airSpeedCap, data.playerVelocity);
				CurrentMoveDirection = Vector3.MoveTowards(CurrentMoveDirection, moveDir * cap,
					8 * Time.deltaTime);
			}
			ApplySlide(characterSetup);
			Controller.Move((CurrentMoveDirection + slideVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
		}
		public void ApplyGravity(CharacterSetup characterSetup)
		{
			var data = characterSetup.CharacterData;
			if (characterSetup.Grounded && verticalVelocity < 0f)
			{
				verticalVelocity = -2f;
				return;
			}
			var g = data.Gravity * (verticalVelocity < 0f ? data.fallMultiplier : 1f);
			verticalVelocity = Mathf.Max(verticalVelocity + g * Time.deltaTime, -data.maxFallSpeed);
		}
		public void Jump(CharacterSetup characterSetup)
		{
			var input = characterSetup.ReadInput;
			if (!input.isJumping) return;

			input.isJumping = false;

			Vaulting.TryVault(characterSetup, CurrentMoveDirection.magnitude);
			if (characterSetup.MoveState is VaultState) return;

			if (characterSetup.CanJump)
				verticalVelocity = characterSetup.CharacterData.JumpForce;
		}
		public void Climb(CharacterSetup characterSetup, float speed,RaycastHit hit)
		{
			var data = characterSetup.CharacterData;
			var input = characterSetup.ReadInput.Direction;
			var t = Controller.transform;

			var origin = t.position + Vector3.up * (Controller.height * 0.6f);
			if (!RaycastChecker.Check(origin, t.forward, 0.5f, GameLayers.Climb, out hit, new Color(1, 0, 0)))
				return;

			var wallNormal = hit.normal;

			var flat = new Vector3(-wallNormal.x, 0f, -wallNormal.z);
			if (flat.sqrMagnitude > 0.001f)
				t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(flat.normalized),
					data.rotationSpeed * Time.deltaTime);

			var right = Vector3.Cross(Vector3.up, -wallNormal);
			var move = (right * input.x + Vector3.up * input.y) * speed;

			var wantedDistance = Controller.radius + 0.05f;
			move += -wallNormal * ((hit.distance - wantedDistance) * 10f);

			Controller.Move(move * Time.deltaTime);

			characterSetup.Animator.SetFloat(XHash, input.x, 0.1f, Time.deltaTime);
			characterSetup.Animator.SetFloat(YHash, input.y, 0.1f, Time.deltaTime);
		}
		private void ApplySlide(CharacterSetup characterSetup)
		{
			CharacterData data = characterSetup.CharacterData;
			GroundInfo ground = characterSetup.GroundInfo;

			if (ground.Touching && !ground.Walkable)
			{
				var slideDir = Vector3.ProjectOnPlane(Vector3.down, ground.Normal).normalized;
				slideVelocity = Vector3.MoveTowards(slideVelocity, slideDir * data.slideSpeed,
					data.slideAcceleration * Time.deltaTime);

				var uphill = new Vector3(ground.Normal.x, 0f, ground.Normal.z).normalized;
				var into = Vector3.Dot(CurrentMoveDirection, -uphill);
				if (into > 0f) CurrentMoveDirection += uphill * into;

				verticalVelocity = Mathf.Max(verticalVelocity, -2f);
			}
			else
			{
				slideVelocity = Vector3.MoveTowards(slideVelocity, Vector3.zero,
					data.slideAcceleration * Time.deltaTime);
			}
		}
		public void ResetMotion()
		{
			CurrentMoveDirection = Vector3.zero;
			verticalVelocity = 0f;
		}
		
	}
}
