using System;
using System.Collections.Generic;
using CharacterController.SaverioScript.PlayerControllers.CharacterController;
using CharacterController.Script;
using CharacterController.Script.PlayerControllers;
using CharacterController.Script.PlayerControllers.Utilities;
using CharacterController.Utilities;
using UnityEngine;

namespace CharacterController
{
    public class CharacterSetup : MonoBehaviour
    {
        [Header("References")]
        [field: SerializeField] public Camera Cam { get; private set; }
        [field: SerializeField] public CharacterData CharacterData { get; private set; }
        [field: SerializeField] public ReadInput ReadInput { get; private set; }
        [field: SerializeField] public MovementBase MovementBase { get; private set; }
        [field: SerializeField] public Animator Animator { get; private set; }
        [field: SerializeField] public CrateGrabber Grabber { get; private set; }
        
        [Header("Debug Booleans")]
        [field: SerializeField] public bool Grounded { get; private set; }
        [field: SerializeField] public bool Climb { get; private set; }
        public bool Sliding => GroundInfo.Touching && !GroundInfo.Walkable;
        public bool CanJump => (Grounded || Sliding) && MovementBase.VerticalVelocity <= 0.1f;
        
        [Header("Settings")]
        [SerializeField] private float coyoteTime = 1f;
        [SerializeField] private float enterFallstate;
        [field: SerializeField] public float airSpeedCap = 8;
        
        public GroundInfo GroundInfo => groundChecker.Info;
        private float coyoteTimeCounter;
        private float currentFallstate = 0f;
        public IMoveState MoveState { get; private set; }
        public event Action<IMoveState> OnStateChanged;
        public IReadOnlyList<IAptitude> Aptitudes => aptitudes;
        private readonly List<IAptitude> aptitudes = new();
        private GroundChecker groundChecker;
        
        private void Reset()
        {
            Animator = GetComponentInChildren<Animator>();
            Cam = Camera.main;
        }
        private void Awake()
        {
            if (Cam == null) Cam = Camera.main;
            GetComponentsInChildren(true, aptitudes);
            foreach (var aptitude in aptitudes)
                aptitude.Initialize(this);
        }
        private void Start()
        {
            groundChecker = GetAptitude<GroundChecker>();
            Grabber =  GetAptitude<CrateGrabber>();
            GoTo(new NormalWalk());
        }
        private void OnEnable()
        {
            ReadInput.OnAttackPressed += HandleAttackPressed;
        }
        private void OnDisable()
        {
            ReadInput.OnAttackPressed -= HandleAttackPressed;
        }
        private void Update()
        {
            var cc = MovementBase.Controller;
            var origin = cc.transform.position + Vector3.up * 0.1f;
            groundChecker.Check();
            
            Climb = RaycastChecker.Check(origin, cc.transform.forward, 0.8f, GameLayers.Climb, out _, new Color(1, 0, 0));

            if(groundChecker.Info.Walkable)
                coyoteTimeCounter = coyoteTime;
            else
                coyoteTimeCounter -= Time.deltaTime;
            Grounded = coyoteTimeCounter >0;
            
            MoveState.UpdateState(this);
    
            if (MoveState is VaultState or ClimbState or LedgeClimbState) 
                return;

            if (!Grounded)
                MovementBase.ApplyGravity(this);
            
            MovementBase.Jump(this);
            MovementBase.HandleMovement(this, Cam);
            MovementBase.HandleRotation(CharacterData.rotationSpeed, Cam);
            
            if (Grounded || Sliding)
                currentFallstate = 0f;
            else if (MoveState is not FallState)
            {
                currentFallstate += Time.deltaTime;
                if (currentFallstate > enterFallstate)
                {
                    GoTo(new FallState());
                    currentFallstate = 0f;
                }
            }
            
            if (MovementBase.CurrentMoveDirection.magnitude > CharacterData.vaultAutoSpeedThreshold)
                Vaulting.TryVault(this, MovementBase.CurrentMoveDirection.magnitude);
        }
        public T GetAptitude<T>() where T : class, IAptitude
        {
            foreach (var aptitude in aptitudes)
                if (aptitude is T found)
                    return found;
            return null;
        }
        private void HandleAttackPressed()
        {
            if (MovementBase.Controller.isGrounded)
                GoTo(new AttackState(CharacterData.firstAttack));
        }
        public void GoTo(IMoveState state)
        {
            MoveState?.ExitState(this);
            MoveState = state;
            MoveState?.EnterState(this);
            OnStateChanged?.Invoke(state);
        }
    }
}
