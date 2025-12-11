using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class PlayerMovement3D : NetworkBehaviour
{
    [Header("Options General")] 
    public int playerID = 0;

    public int currentMaxLife = 15;
    public float recoveryTime = 2f;
    public float currentForce = 10f;
    public float respawnCooldown = 10f;
    public float gravityScale;
    public float damageCooldown = 2.5f;
    public float FlipDimensionCoolDown = 1f;
 
    public RigidbodyConstraints defaultConstraints;
    public Vector2 moveInput;
    public Vector2 aimInput;
    public Vector2 dpadInput;
    public Vector3 capsuleSize;
    public Vector3 capsuleCenter;
    public Vector3 originalScale;
    public Transform visualRoot;
    public Vector3 visualRootDefaultLocalPos;
    public Vector3 visualRootDefaultLocalScale;
    public bool deactivateOnOffScreen;
    public bool alignToGroundSlope = true;
    public bool use3DMovement = true;
    public bool rotateChildOnDash = true;
    
    public float maxAngleWithFriction = 30f;
    public bool canWallJump = true;
    public bool canDash = true;
    public bool canAttack = true;
    public bool canGlide = true;

    [Header("UI")]
    public GameObject playerCanvas;              
    public TextMeshProUGUI respawnTextTMP;        
    public Text respawnTextUI;     
    public bool DisplayScore = true;                    
    public TextMeshProUGUI scoreVersusTMP;             
    public Text scoreVersusUI;
    private Coroutine scoreVersusAnimationCoroutine;
    private Coroutine respawnCoroutine;
    private bool canPressRespawn;    
    public Image lifeFillImage;             
    public TextMeshProUGUI currentLifeTMP;  
    public TextMeshProUGUI maxLifeTMP;    
    public Text lifeTextUI;                  
    public Image iconImage;                  
    public PlayerIcon playerIconData;       
    private Coroutine iconAnimationCoroutine;
    private PlayerIconState currentIconState = PlayerIconState.Idle;
    
    public enum PlayerIconState
    {
        Idle,
        Attacking,
        Damage,
        Dead
    }

    
    [Space(5)]
    [Header("Dynamique Collider")]
    public float raycastLimit = 100f;
    public float collider2DMaxSizeZ = 100f;     
    public float collider2DResizeDelay = 1f;   

    private bool dynamicColliderInitialized;
    private bool lastIs3DState;

    private float capsuleHeight3D;
    private float capsuleRadius3D;
    private Vector3 capsuleCenter3D;

    private Vector3 topBodySize3D;
    private Vector3 topBodyCenter3D;

    private Vector3 feetSize3D;
    private Vector3 feetCenter3D;

    private Vector3 dashSize3D;
    private Vector3 dashCenter3D;

    private Vector3 groundPoundSize3D;
    private Vector3 groundPoundCenter3D;

    private Vector3 weaponSize3D;
    private Vector3 weaponCenter3D;

    private float groundCheckSizeZ3D;
    private float wallCheckSizeZ3D;

    private float groundCheckOffsetZ3D;
    private float frontWallCheckOffsetZ3D;
    private float backWallCheckOffsetZ3D;
    
    private float currentCollider2DCenterZ;
    private float currentCollider2DSizeZ;
    private float currentPosExtentZ2D;
    private float currentNegExtentZ2D;
    
    private float targetCollider2DSizeZ;
    private float targetCollider2DCenterZ;
    private float targetPosExtentZ2D;
    private float targetNegExtentZ2D;

    private float collider2DNextResizeTime;
    private bool collider2DResizePending;

    private Collider lastPosZHit;
    private Collider lastNegZHit;
    private bool collider2DAtMaxExtent;

   
    


    
    [SerializeField] private bool debugCollider2DRays = true;
    [SerializeField] private Color debugRayPosColor = Color.green;
    [SerializeField] private Color debugRayNegColor = Color.magenta;
    [SerializeField] private Color debugRayHitColor = Color.red;
    [SerializeField] private Color debugRayOriginColor = Color.cyan;

    
    
    [Space(5)]
    [Header("Online")]
    public NetworkVariable<FixedString64Bytes> netAnimationState = new NetworkVariable<FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> currentLife = new NetworkVariable<int>(15, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> is3DNow = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isCrushedNetwork = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> scoreVersus = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasSuperCollectible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Space(5)]
    [Header("References")]
    public PlayerData data;
    public PlayerInput playerControls;
    public GameObject baseModelPrefab;
    public Animator playerAnimator;
    public GameObject playerModel;
    public CapsuleCollider colliderBottomBody;
    public BoxCollider collideTopBody;
    public BoxCollider colliderFeet;
    public BoxCollider colliderDash;
    public BoxCollider colliderGroundPound;
    public BoxCollider colliderWeapon;
    public GameObject colliderObject;
    public Rigidbody rb;
    public RagdollController ragdollController;
    public GameObject parent;
    public PhysicsMaterial frictionMaterial;
    public PhysicsMaterial noFrictionMaterial;
    public Camera cam;

    [Space(2)]
    public GameObject armOriginal;
    public GameObject armAim;
    public Transform armPivot;
    public bool flipAimArm;
    public float pivotCorrection = 180f;

    [Space(5)]
    [Header("Checks")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector3 groundCheckSize = new Vector3(0.49f, 0.03f, 0f);
    [SerializeField] public Transform frontWallCheckPoint;
    [SerializeField] public Transform backWallCheckPoint;
    [SerializeField] private Vector3 wallCheckSize = new Vector3(0.5f, 1f, 0f);
    [SerializeField] private Transform airAttackGroundCheckPoint;
    [SerializeField] private float airAttackGroundRadius = 0.5f;
    [SerializeField] private float airAttackGroundHeight = 1.0f;
    [SerializeField] private bool glideRequested;  


    [Space(5)]
    [Header("Layers & Tags")]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    public LayerMask enemyProjectileLayer;

    [Space(5)]
    [Header("Parametres des états")]
    public bool cannotMove;

    public bool IsRagdoll 
    {
        get { return ragdollController != null && ragdollController.IsRagdollActive; }
    }
    
    public bool areControllsRemoved;
    public bool wasGroundedLastFrame = false;
    public bool dashRefilling { get; private set; }
    public bool isFacingRight { get; private set; }
    public bool isJumping { get; private set; }
    
    public bool isGliding { get; private set; }
    
    public bool isFalling { get; private set; }
    public bool isWallJumping { get; private set; }
    public bool isDashing { get; private set; }
    public bool groundPoundCancelledExternally { get; private set; }
    public bool isSliding { get; private set; }
    public bool isGroundPounding { get; private set; }
    public bool isStayAirAttacking { get; private set; }
    public float attackStateEndTime { get; private set; }
    public bool isAirAttcking { get; private set; }
    public bool isMovingAttcking { get; private set; }
    public bool isIdleAttcking { get; private set; }
    public bool isIdleAttackStopping { get; private set; }  
    public float idleAttackStartTime { get; private set; }  
    public float idleAttackStopStartTime { get; private set; } 
    private bool idleAttackReleaseQueued; 
    public bool isJumpCut { get; private set; }
    public bool isJumpFalling { get; private set; }
    public bool isGroundSliding { get; private set; }
    public bool isDashRefilling { get; private set; }
    
    public bool dashCancelledExternally { get; private set; }
    public bool isDashAttacking { get; private set; }
    public bool isStunned { get; private set; }
    public bool isCrushed { get; private set; }
    public int crushedTapActionLeft { get; private set; }
    public bool crushTapInProgress { get; private set; }
    public bool isRecovery {get; private set;}
    public bool visualRootDefaultsInitialized {get; private set;}
    public bool fixedLastOnGroundTime { get; private set; }
    public bool isGrappling { get; private set; }
    public bool isGroundedNow { get; private set; }
    public int lastWallJumpDir { get; private set; }
    public int dashesLeft { get; private set; }
    public int currentAttackInstanceId { get; private set; }
    public float targetSpeed { get; private set; }
    public float wallJumpStartTime { get; private set; }
    public float lastOnGroundTime { get; private set; }
    public float lastOnWallTime { get; private set; }
    public float lastOnWallRightTime { get; private set; }
    public float lastOnWallLeftTime { get; private set; }
    public float lastPressedJumpTime { get; private set; }
    public float lastPressedDashTime { get; private set; }
    public Vector3 lastDashDir { get; private set; }

    public Vector3 stayAirAttackVelocity { get; private set; }
    public float stayAirCurrentHeight { get; private set; }
    public float lastJumpMaxY { get; private set; }
    public float lastGroundY { get; private set; }
    public bool trackJumpHeight { get; private set; }
    public float lastJumpButtonTime { get; private set; }

    [Header("Mode versus")]
    [SerializeField] private GameObject superCollectibleTimerPrefab;      
    [SerializeField] private float superCollectibleTimerLifetime = 6f;    
    [SerializeField] private float superCollectibleTimerPickupLockDuration = 2f; 
    [SerializeField] private float superCollectibleDropHeight = 1.0f;     

    private Coroutine superCollectibleCoroutine;
    private int superCollectibleScorePerTick = 10;
    private float superCollectibleInterval = 2f;
    
    [Header("Nom des Actions")]
    public string actionMapName = "Gameplay";
    public string actionMoveName = "Move";
    public string actionDpadName = "Dpad";
    public string actionAimName = "Aim";
    public string actionJumpName = "Jump";
    public string actionDashName = "Dash";
    public string actionUseName = "Use";
    public string actionAttackName = "Attack";
    public string actionGrapName = "Grap";
    public string actionStartName = "Start";
    public string actionPauseName = "Pause";
    public string actionSelectRName = "SelectR";
    public string actionSelectLName = "SelectL";
    public string actionFlipDimensionName = "FlipDimension";

    private InputAction moveAction;
    private InputAction dpadAction;
    private InputAction aimAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction useAction;
    private InputAction attackAction;
    private InputAction grapAction;
    private InputAction startAction;
    private InputAction pauseAction;
    private InputAction selectRAction;
    private InputAction selectLAction;
    private InputAction flipAction;

    [Header("liste des SXF")]
    public List<string> clipsRandomImpact = new List<string> { "impact1", "impact2", "impact3", "impact4" };
    public List<string> clipsRandomDeath = new List<string> { "deathBell1" };
    public List<string> clipsRandomSlap = new List<string> { "slap1" };
    public List<string> clipsRandomjump = new List<string> { "jump1" };
    public List<string> clipsRandomWalljump = new List<string> { "wall jump" };
    public List<string> clipsRandomDash = new List<string> { "dash1" };

    private void Awake()
    {
        playerID = GameManager.instance.AssignePlayerID();
        gameObject.name = "Player " + playerID;
        if (gameObject.transform.parent != null)
        {
            parent = gameObject.transform.parent.gameObject;
        }
        
        rb = GetComponent<Rigidbody>();
        colliderBottomBody = GetComponent<CapsuleCollider>();
        playerControls = GetComponent<PlayerInput>();
        playerControls.actions.Disable();   
        playerControls.enabled = false;
        defaultConstraints = rb.constraints;

        currentLife.Value = currentMaxLife;
        
        if (currentLifeTMP != null)
            currentLifeTMP.text = currentLife.Value.ToString();

        if (maxLifeTMP != null)
            maxLifeTMP.text = currentMaxLife.ToString();
    }

    void Start()
    {
        if (rb != null) rb.useGravity = false;
        gameObject.layer = LayerMask.NameToLayer("Player");
        capsuleSize = colliderBottomBody.bounds.size;
        capsuleCenter = colliderBottomBody.bounds.center;
        
        InitializeDynamicColliders();

        if (data != null)
            SetGravityScale(data.gravityScale);

        isFacingRight = true;
        cam = Camera.main;
        originalScale = transform.localScale;

        if (visualRoot == null)
        {
            Transform child0 = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (child0 != null && child0.childCount > 0)
                visualRoot = child0.GetChild(0);
        }
        if (visualRoot != null)
        {
            visualRootDefaultLocalPos   = visualRoot.localPosition;
            visualRootDefaultLocalScale = visualRoot.localScale;
        }

        if (data != null)
        {
            dashesLeft = data.dashAmount;
        }
        else
        {
            dashesLeft = 1;
        }
    }


    void OnEnable()
    {
        #region ENABLED INPUT ACTIONS

        if (!IsSpawned || !IsOwner) return;
        
        var actions = playerControls.actions;
        if (!string.IsNullOrEmpty(actionMapName))
            actions.FindActionMap(actionMapName, throwIfNotFound: true);

        moveAction = actions[actionMoveName];
        dpadAction = actions[actionDpadName];
        aimAction = actions[actionAimName];

        jumpAction = actions[actionJumpName];
        dashAction = actions[actionDashName];
        useAction = actions[actionUseName];
        attackAction = actions[actionAttackName];
        grapAction = actions[actionGrapName];
        startAction = actions[actionStartName];
        pauseAction = actions[actionPauseName];
        selectRAction = actions[actionSelectRName];
        selectLAction = actions[actionSelectLName];
        flipAction = actions[actionFlipDimensionName];

        jumpAction.performed += OnJumpPressed;
        jumpAction.canceled += OnJumpReleased;

        dashAction.performed += OnDashPressed;
        dashAction.canceled += OnDashReleased;

        useAction.performed += OnUsePressed;
        useAction.canceled += OnUseReleased;

        attackAction.performed += OnAttackPressed;
        attackAction.canceled += OnAttackReleased;

        grapAction.performed += OnGrapPressed;
        grapAction.canceled += OnGrapReleased;

        startAction.performed += OnStartPressed;
        pauseAction.performed += OnPausePressed;

        selectRAction.performed += OnSelectRPressed;
        selectLAction.performed += OnSelectRPressed;

        flipAction.performed += OnFlipPressed;

        playerControls.actions.Enable();


        #endregion
    }

    private void OnDisable()
    {
        #region DISABLE INPUT ACTIONS

        if (!IsOwner) return;
        
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPressed;
            jumpAction.canceled -= OnJumpReleased;
        }

        if (dashAction != null)
        {
            dashAction.performed -= OnDashPressed;
            dashAction.canceled -= OnDashReleased;
        }

        if (useAction != null)
        {
            useAction.performed -= OnUsePressed;
            useAction.canceled -= OnUseReleased;
        }

        if (attackAction != null)
        {
            attackAction.performed -= OnAttackPressed;
            attackAction.canceled -= OnAttackReleased;
        }

        if (grapAction != null)
        {
            grapAction.performed -= OnGrapPressed;
            grapAction.canceled -= OnGrapReleased;
        }

        if (startAction != null)
        {
            startAction.performed -= OnStartPressed;
        }

        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePressed;
        }

        if (selectRAction != null)
        {
            selectRAction.performed -= OnSelectRPressed;
        }

        if (selectLAction != null)
        {
            selectLAction.performed -= OnSelectRPressed;
        }

        if (flipAction != null)
        {
            flipAction.performed -= OnFlipPressed;
        }

        #endregion
        
        if (iconAnimationCoroutine != null)
        {
            StopCoroutine(iconAnimationCoroutine);
            iconAnimationCoroutine = null;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        netAnimationState.OnValueChanged += OnAnimationChanged;
        scoreVersus.OnValueChanged += OnScoreVersusChanged;
    
        UpdateScoreVersusUI(scoreVersus.Value);


        if (playerCanvas != null)
            playerCanvas.SetActive(IsOwner);

        if (IsOwner)
        {
            playerControls.enabled = true;
            playerControls.actions.Enable();
            rb.interpolation = RigidbodyInterpolation.None;
        }
        else
        {
            playerControls.enabled = false;
            playerControls.actions.Disable();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }


    
    public override void OnNetworkDespawn()
    {
        netAnimationState.OnValueChanged -= OnAnimationChanged;
        scoreVersus.OnValueChanged -= OnScoreVersusChanged;
        currentLife.OnValueChanged -= OnLifeChanged;
    }





    private void Update()
    {
        if (!IsOwner) return;
        
        if (isDead.Value || IsRagdoll)
            return;

        if (currentLifeTMP != null)
            currentLifeTMP.text = currentLife.Value.ToString();

        if (maxLifeTMP != null)
            maxLifeTMP.text = currentMaxLife.ToString();
        
        is3DNow.Value = GameManager.instance.is3d;
        isCrushedNetwork.Value = isCrushed;
        UpdateDynamicColliders();
        
        if (moveAction == null) return;
        if (GameManager.instance.isPaused) return;

        
        
        moveInput = moveAction.ReadValue<Vector2>();
        aimInput = aimAction.ReadValue<Vector2>();
        dpadInput = dpadAction.ReadValue<Vector2>();
        targetSpeed = moveInput.x * (data != null ? data.runMaxSpeed : 0f);

        lastOnGroundTime -= Time.deltaTime;
        lastOnWallTime -= Time.deltaTime;
        lastOnWallLeftTime -= Time.deltaTime;
        lastOnWallRightTime -= Time.deltaTime;
        lastPressedJumpTime -= Time.deltaTime;
        
        if (trackJumpHeight && !wasGroundedLastFrame)
        {
            lastJumpMaxY = Mathf.Max(lastJumpMaxY, transform.position.y);
        }

        GroundCheck3D();
        WallCheck3D();

        HandleFacing();

        HandleJumpState();
        HandleJumpBuffer();
        HandleGlideState();
        HandleDashState();
        HandleAttackState();
    }


    void FixedUpdate()
    {
        if (!IsOwner)
        {
            rb.isKinematic = true;
            return;
        }
        
        if (isDead.Value || IsRagdoll)
            return;

        if (GameManager.instance != null && GameManager.instance.isPaused) return;

        if (GameManager.instance != null)
        {
            GameManager.instance.FindPlayer(name, transform, this);
            GameManager.instance.CharacterCheck(name, data.playerName);
        }


        if (isDashing || isDashAttacking)
            return;
        
        if (isStayAirAttacking)
        {
            SwitchAnimation("isStayAttack");
            HandleStayAirAttackMovement();
            ApplyCustomGravity();
            return;
        }
        
        bool pushingIntoWall =
            (lastOnWallLeftTime > 0f  && moveInput.x < -0.01f) ||
            (lastOnWallRightTime > 0f && moveInput.x >  0.01f);

        if (CanSlide() && pushingIntoWall)
        {
            isSliding = true;
            SwitchAnimation("isSliding");
            rb.constraints = defaultConstraints | RigidbodyConstraints.FreezePositionZ;
        }
        else
        {
            isSliding = false;
            rb.constraints = defaultConstraints;
        }

        if (isSliding)
        {
            Slide3D();
            return;
        }
        
        ApplyCustomGravity();

        if (isDead.Value) return;

        if (GameManager.instance.is3d)
            HandleMovement3D();
        else
            HandleMovement2D();
    }

    #region INPUT ACTION BUTTONS

    private void OnFlipPressed(InputAction.CallbackContext obj)
    {
        if (!IsOwner || cannotMove || isStunned || isCrushed) return;
        SwitchAnimation("isFlip");
        GameManager.instance.ChangeDimension();
        StartCoroutine(FlipDimensionCoolDownCoroutine(FlipDimensionCoolDown));

    }

    private void OnSelectRPressed(InputAction.CallbackContext obj)
    {
        if (!IsOwner)return;
        Debug.Log("OnSelectRPressed");
    }

    private void OnPausePressed(InputAction.CallbackContext obj)
    {
        if (!IsOwner)return;
        Debug.Log("OnPausePressed");
    }

    private void OnStartPressed(InputAction.CallbackContext obj)
    {
        
        if (!IsOwner) return;
        Debug.Log("OnStartPressed");

        // On ne peut respawn que si :
        // - on est mort
        // - le cooldown est terminé
        if (isDead.Value && canPressRespawn)
        {
            RequestRespawnServerRpc();
        }
    }

    private void OnGrapReleased(InputAction.CallbackContext obj)
    {
        if (!IsOwner)return;
        Debug.Log("OnGrapReleased");
    }

    private void OnGrapPressed(InputAction.CallbackContext obj)
    {
        if (!IsOwner)return;
        Debug.Log("OnGrapPressed");
    }

    private void OnAttackReleased(InputAction.CallbackContext obj)
    {
        if (!IsOwner || cannotMove) return;

        if (isStayAirAttacking)
        {
            isStayAirAttacking = false;
            SwitchAnimation("");
        }
        
        if (isIdleAttcking)
        {
            idleAttackReleaseQueued = true;
        }
    }


    private void OnAttackPressed(InputAction.CallbackContext obj)
    {
        if (!IsOwner || cannotMove) return;
        
        if (!canAttack) return;
        if (isDashing || isDashAttacking) return;
        if (isGroundPounding) return;

        
        bool grounded = lastOnGroundTime > 0f && Mathf.Abs(rb.linearVelocity.y) < 0.01f;
        
        if (!wasGroundedLastFrame)
        {
            isIdleAttcking = false;
            isMovingAttcking = false;
            attackStateEndTime = 0f;

            if (!isAirAttcking && !isStayAirAttacking)
            {
                StartAirAttack();
            }

            return;
        }


        if (Time.time < attackStateEndTime && (isIdleAttcking || isMovingAttcking))
            return;

        bool moving2D = Mathf.Abs(moveInput.x) > 0.01f;
        bool moving3D = moveInput.sqrMagnitude > 0.01f;

        bool moving = GameManager.instance != null && GameManager.instance.is3d ? moving3D : moving2D;

        if (moving)
            StartMovingAttack();
        else
            StartIdleAttack();
    }

    private void OnUseReleased(InputAction.CallbackContext obj)
    {
        if (!IsOwner)return;
        Debug.Log("OnUseReleased");
    }

    private void OnUsePressed(InputAction.CallbackContext obj)
    {
        if (!IsOwner)return;
        Debug.Log("OnUsePressed");
    }

    private void OnDashPressed(InputAction.CallbackContext obj)
    {
        if (!IsOwner || cannotMove) return;
        if (!canDash) return;
        lastPressedDashTime = data.dashInputBufferTime;
    }

    private void OnDashReleased(InputAction.CallbackContext obj)
    {
        if (!IsOwner || cannotMove) return;
        if (!canDash) return;
        lastPressedDashTime = 0;
    }

    private void OnJumpReleased(InputAction.CallbackContext obj)
    {
        if (!IsOwner || cannotMove) return;
        if (isGliding)
        {
            CancelGlide();
        }

        if (CanJumpCut())
            isJumpCut = true;
    }


    private void OnJumpPressed(InputAction.CallbackContext obj)
    {
        if (!IsOwner || cannotMove) return;
        
        if (isCrushed)
        {
            DisabledCrushedState();
            return;
        }
        
        lastJumpButtonTime = Time.time;

        if (isStayAirAttacking)
            return;

        
        bool grounded = lastOnGroundTime > 0f;
        bool inAir = !grounded;

        if (canGlide && inAir && !isGliding)
        {
            glideRequested = true; 
        }
        
        lastPressedJumpTime = data.jumpInputBufferTime;
    }


    #endregion

    #region GRAVITY

    public void SetGravityScale(float scale)
    {
        rb.useGravity = false;
        gravityScale = scale;
    }

    private void ApplyCustomGravity()
    {
        if (data == null || isSliding || isDashing || isDashAttacking || isGroundPounding)
            return;

        float baseGravity = data.gravityScale;
        
        if (isGliding)
        {
            SetGravityScale(baseGravity * data.glideGravityMult);
            
            Vector3 vel = rb.linearVelocity;
            if (vel.y < -data.glideMaxFallSpeed)
            {
                vel.y = -data.glideMaxFallSpeed;
                rb.linearVelocity = vel;
            }

            rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
            return;
        }
        
        if (isJumpCut && rb.linearVelocity.y > 0f)
        {
            SetGravityScale(baseGravity * data.jumpCutGravityMult);
        }
        else if ((isJumping || isJumpFalling) &&
                 Mathf.Abs(rb.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            SetGravityScale(baseGravity * data.jumpHangGravityMult);
        }
        else if (rb.linearVelocity.y < 0 && lastOnGroundTime <= 0)
        {
            SetGravityScale(baseGravity * data.fallGravityMult);

            Vector3 vel2 = rb.linearVelocity;
            vel2.y = Mathf.Max(vel2.y, -data.maxFallSpeed);
            rb.linearVelocity = vel2;
        }
        else
        {
            SetGravityScale(baseGravity);
        }

        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
    }

    #endregion

    #region CHECKS

    private void GroundCheck3D()
    {
        bool grounded = false;


        if (isStayAirAttacking && airAttackGroundCheckPoint != null)
        {
            float halfX = airAttackGroundRadius;
            float halfY = airAttackGroundHeight * 0.5f;

            float halfZ;

            bool is2DMode = GameManager.instance != null && !GameManager.instance.is3d;

            if (is2DMode && currentCollider2DSizeZ > 0f)
            {

                halfZ = currentCollider2DSizeZ * 0.5f;
            }
            else
            {
                halfZ = airAttackGroundRadius;
            }

            Vector3 halfExtents = new Vector3(halfX, halfY, halfZ);

            grounded = Physics.CheckBox(
                airAttackGroundCheckPoint.position,
                halfExtents,
                Quaternion.identity,
                groundLayer
            );
        }
        else
        {
            if (groundCheckPoint != null)
            {
                grounded = Physics.CheckBox(
                    groundCheckPoint.position,
                    groundCheckSize * 0.5f,
                    Quaternion.identity,
                    groundLayer
                );
            }
        }

        if (grounded)
            lastOnGroundTime = data != null ? data.coyoteTime : 0.1f;

        if (grounded && !wasGroundedLastFrame)
        {
            TouchGround();
        }

        if (!grounded && wasGroundedLastFrame)
        {
            LeaveGround();
        }

        wasGroundedLastFrame = grounded;
    }



    private void WallCheck3D()
    {
        if (frontWallCheckPoint == null || backWallCheckPoint == null) return;

        bool frontHit = Physics.CheckBox(
            frontWallCheckPoint.position,
            wallCheckSize * 0.5f,
            Quaternion.identity,
            groundLayer);

        bool backHit = Physics.CheckBox(
            backWallCheckPoint.position,
            wallCheckSize * 0.5f,
            Quaternion.identity,
            groundLayer);

        if (((frontHit && isFacingRight) || (backHit && !isFacingRight)) && !isWallJumping)
        {
            lastOnWallRightTime = data != null ? data.coyoteTime : 0.1f;
        }

        if (((frontHit && !isFacingRight) || (backHit && isFacingRight)) && !isWallJumping)
        {
            lastOnWallLeftTime = data != null ? data.coyoteTime : 0.1f;
        }

        lastOnWallTime = Mathf.Max(lastOnWallLeftTime, lastOnWallRightTime);
    }

    private void HandleFacing()
    {
        if (isIdleAttcking || isIdleAttackStopping || isGroundPounding) return;
        if (!GameManager.instance.is3d && isMovingAttcking)return;
        if (moveInput.x > 0.01f)
            CheckDirectionToFace(true);
        else if (moveInput.x < -0.01f)
            CheckDirectionToFace(false);
    }

    private void CheckDirectionToFace(bool moveRight)
    {
        if (moveRight != isFacingRight)
        {
            if (!isDashing)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;

                isFacingRight = !isFacingRight;
            }
        }
    }

    private void HandleJumpState()
    {
        if (isStunned) return;
        if (isJumping && rb.linearVelocity.y < 0f)
        {
            isJumping = false;
            isJumpFalling = true;

            if (!isAirAttcking && !isStayAirAttacking)
            {
                SwitchAnimation("isFalling");
                isFalling = true;
            }
        }

        if (isWallJumping && data != null && Time.time - wallJumpStartTime > data.wallJumpTime)
        {
            isWallJumping = false;
        }

        if (lastOnGroundTime > 0f)
        {
            isJumping = false;
            isWallJumping = false;
            isJumpCut = false;
            isJumpFalling = false;
        }
    }


    private void HandleJumpBuffer()
    {
        if (data == null) return;

        if (CanJump() && lastPressedJumpTime > 0f)
        {
            isJumping = true;
            isWallJumping = false;
            isJumpCut = false;
            isJumpFalling = false;

            Jump();

            lastPressedJumpTime = 0f;
            return;
        }

        if (CanWallJump() && lastPressedJumpTime > 0f)
        {
            isWallJumping = true;
            isJumping = false;
            isJumpCut = false;
            isJumpFalling = false;

            wallJumpStartTime = Time.time;
            lastWallJumpDir = (lastOnWallRightTime > 0f) ? -1 : 1;

            WallJump(lastWallJumpDir);
            lastPressedJumpTime = 0f;
        }
    }
    
    private void HandleGlideState()
    {
        if (!canGlide || data == null || rb == null) return;

        bool grounded = lastOnGroundTime > 0f;
        
        if (grounded)
        {
            if (isGliding)
                CancelGlide();

            glideRequested = false;
            return;
        }


        if (isDashing || isDashAttacking || isGroundPounding || isStayAirAttacking || cannotMove)
        {
            if (isGliding)
                CancelGlide();
            glideRequested = false;
            return;
        }

        if (isGliding)
        {
            if (jumpAction == null || !jumpAction.IsPressed())
            {
                CancelGlide();
            }

            return;
        }
        
        if (!glideRequested)
            return;
        
        if (rb.linearVelocity.y >= 0f)
            return;

        if (jumpAction == null || !jumpAction.IsPressed())
        {
            glideRequested = false;
            return;
        }

        StartGlide();
    }

    private void StartGlide()
    {
        if (!canGlide || data == null) return;

        isGliding = true;
        glideRequested = false;
        isJumpCut = false;
        isJumpFalling = false;
        
        Vector3 vel = rb.linearVelocity;
        if (data.glideStartVerticalSpeed > 0f && vel.y < -data.glideStartVerticalSpeed)
        {
            vel.y = -data.glideStartVerticalSpeed;
            rb.linearVelocity = vel;
        }

        SwitchAnimation("isGliding");
    }

    private void CancelGlide()
    {
        if (!isGliding) return;

        isGliding = false;
        glideRequested = false;
        
        if (lastOnGroundTime <= 0f &&
            !isJumping && !isJumpFalling &&
            !isAirAttcking && !isStayAirAttacking)
        {
            SwitchAnimation("isFalling");
            isFalling = true;
        }
    }


    void HandleDashState()
    {
        if (isGroundPounding) 
            return;

        if (!CanDash() || lastPressedDashTime <= 0f)
            return;
        if (moveInput == Vector2.zero)
        {
            SwitchAnimation("isCAC");
            return;
        }

        if (isStayAirAttacking)
            isStayAirAttacking = false;

        Vector2 dashInput = moveInput;
        
        if (GameManager.instance != null && GameManager.instance.is3d)
        {
            dashInput = new Vector2(moveInput.y, moveInput.x);
        }

        Vector3 inputDir;

        if (GameManager.instance != null && GameManager.instance.is3d)
        {
            inputDir = new Vector3(dashInput.x, 0f, -dashInput.y);
        }
        else
        {
            inputDir = new Vector3(dashInput.x, dashInput.y, 0f);
        }


        if (inputDir.sqrMagnitude < 0.0001f)
            return;
        
        bool inAir = !wasGroundedLastFrame;


        bool is2DMode = GameManager.instance == null || !GameManager.instance.is3d;

        if (is2DMode && inAir && moveInput.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;

            if (Mathf.Abs(Mathf.DeltaAngle(angle, -90f)) <= data.groundPoundAngleTolerance)
            {
                lastPressedDashTime = 0f;
                StartCoroutine(StartGroundPound());
                return;
            }
        }

        
        lastDashDir = inputDir.normalized;

        isDashing = true;
        isJumping = false;
        isWallJumping = false;
        isJumpCut = false;
        dashCancelledExternally = false;

        StartCoroutine(StartDash(lastDashDir));

        lastPressedDashTime = 0f;
    }
    
    private IEnumerator GroundPoundLanding()
    {
        cannotMove = true;
        SwitchAnimation("isLanded");
        
        yield return new WaitForSeconds(data.groundPoundFreezeTime);

        cannotMove = false;

    }


    

    private void HandleAttackState()
    {
        if (isStunned) return;
        
        HandleIdleAttackHoldState();
        
        if (isMovingAttcking && Time.time >= attackStateEndTime)
        {
            isMovingAttcking = false;

            if (!isIdleAttcking && !isIdleAttackStopping &&
                !isAirAttcking && !isStayAirAttacking)
            {
                SwitchAnimation("");
            }
        }

        if (isAirAttcking && Time.time >= attackStateEndTime)
        {
            isAirAttcking = false;

            if (attackAction != null && attackAction.IsPressed() && canAttack)
            {
                StartStayAirAttack();
            }
            else
            {
                if (!isIdleAttcking && !isIdleAttackStopping &&
                    !isMovingAttcking && !isStayAirAttacking)
                {
                    SwitchAnimation("");
                }
            }
        }
    }


    private void HandleStayAirAttackMovement()
    {
        if (data == null) return;

        Vector3 inputDir;
        if (GameManager.instance != null && GameManager.instance.is3d)
        {
            inputDir = new Vector3(moveInput.y, 0f, -moveInput.x);
        }
        else
        {
            inputDir = new Vector3(moveInput.x, 0f, 0f);
        }

        Vector3 horizontalVel = new Vector3(stayAirAttackVelocity.x, 0f, stayAirAttackVelocity.z);

        if (inputDir.sqrMagnitude > 0.0001f)
        {
            Vector3 inputNorm = inputDir.normalized;

            if (horizontalVel.sqrMagnitude < data.stayAirAttackMinSpeed * data.stayAirAttackMinSpeed)
            {
                horizontalVel += inputNorm * data.stayAirAttackAccel * Time.fixedDeltaTime;
            }
            else
            {
                Vector3 velNorm = horizontalVel.normalized;
                float dot = Vector3.Dot(inputNorm, velNorm);

                if (dot >= 0f)
                {
                    horizontalVel += inputNorm * data.stayAirAttackAccel * Time.fixedDeltaTime;
                }
                else
                {
                    horizontalVel += inputNorm * data.stayAirAttackOppositeDecel * Time.fixedDeltaTime;
                }
            }
        }
        else
        {
            float speed = horizontalVel.magnitude;
            float newSpeed = Mathf.MoveTowards(speed, 0f, data.stayAirAttackOppositeDecel * 0.5f * Time.fixedDeltaTime);
            horizontalVel = (speed > 0f) ? horizontalVel.normalized * newSpeed : Vector3.zero;
        }

        if (horizontalVel.magnitude > data.stayAirAttackMaxSpeed)
            horizontalVel = horizontalVel.normalized * data.stayAirAttackMaxSpeed;

        stayAirAttackVelocity = new Vector3(horizontalVel.x, stayAirAttackVelocity.y, horizontalVel.z);

        Vector3 rbVel = rb.linearVelocity;
        rbVel.x = horizontalVel.x;
        rbVel.z = horizontalVel.z;
        rb.linearVelocity = rbVel;

        if (attackAction != null && !attackAction.IsPressed())
        {
            isStayAirAttacking = false;
            SwitchAnimation("");
        }
    }


    private void HandleIdleAttackHoldState()
    {
        if (!isIdleAttcking && !isIdleAttackStopping)
            return;
        
        if (isIdleAttcking)
        {
            bool minHoldElapsed = Time.time >= idleAttackStartTime + data.idleAttackMinHoldTime;
            
            bool attackReleasedNow = idleAttackReleaseQueued || (attackAction != null && !attackAction.IsPressed());
            
            if (minHoldElapsed && attackReleasedNow)
            {
                isIdleAttcking = false;
                idleAttackReleaseQueued = false;
                StartIdleAttackStop();
            }
            
            return;
        }
        
        if (isIdleAttackStopping)
        {
            if (Time.time >= idleAttackStopStartTime + data.idleAttackStopDuration)
            {
                isIdleAttackStopping = false;

                if (!isStunned && !isAirAttcking && !isStayAirAttacking && !isMovingAttcking)
                {
                    SwitchAnimation("");
                }
            }
        }
    }

    
    public bool CanDash()
    {
        if (!canDash || data == null) return false;

        if (!isDashing && dashesLeft < data.dashAmount &&
            (lastOnGroundTime > 0f || lastOnWallTime > 0f) && !dashRefilling)
        {
            StartCoroutine(RefillDash(1));
        }

        return dashesLeft > 0;
    }

    public IEnumerator RefillDash(int amount)
    {
        dashRefilling = true;
        isDashRefilling = true;

        yield return new WaitForSeconds(data.dashRefillTime);

        dashRefilling = false;
        isDashRefilling = false;

        dashesLeft = Mathf.Min(data.dashAmount, dashesLeft + amount);
    }


    

    private bool CanJump()
    {
        if (isStayAirAttacking)
            return false;

        return lastOnGroundTime > 0f;
    }

    private bool CanJumpCut()
    {
        return isJumping && rb.linearVelocity.y > 0f;
    }

    private bool CanWallJump()
    {
        if (!canWallJump) return false;
        if (IsWallSlippery()) return false;
        if (isStayAirAttacking) return false;

        return (lastPressedJumpTime > 0 &&
                lastOnWallTime > 0 &&
                lastOnGroundTime <= 0 &&
                (!isWallJumping ||
                 (lastOnWallRightTime > 0 && lastWallJumpDir == 1) ||
                 (lastOnWallLeftTime > 0 && lastWallJumpDir == -1)));
    }

    private bool IsWallSlippery()
    {
        if (frontWallCheckPoint == null || backWallCheckPoint == null) return false;

        Collider[] frontHits = Physics.OverlapBox(
            frontWallCheckPoint.position,
            wallCheckSize * 0.5f,
            Quaternion.identity,
            groundLayer);

        foreach (var hit in frontHits)
        {
            if (hit.CompareTag("Slippery"))
                return true;
        }

        Collider[] backHits = Physics.OverlapBox(
            backWallCheckPoint.position,
            wallCheckSize * 0.5f,
            Quaternion.identity,
            groundLayer);

        foreach (var hit in backHits)
        {
            if (hit.CompareTag("Slippery"))
                return true;
        }

        return false;
    }

    public bool CanSlide()
    {
        if (IsWallSlippery())
            return false;
        
        if (isStayAirAttacking)
            return false;

        return lastOnWallTime > 0f
               && lastOnGroundTime <= 0f
               && !isJumping
               && !isWallJumping
               && !isDashing
               && rb.linearVelocity.y <= 0.01f;
    }

    public void EnableCollider(bool value)
    {
      colliderBottomBody.enabled = value;
      collideTopBody.enabled = value;
      colliderFeet.enabled = value;
      colliderDash.enabled = value;
      colliderGroundPound.enabled = value;
      colliderWeapon.enabled = value;
    }
    
    private Vector3 GetRespawnPosition()
    {
        GameObject levelObj = GameObject.FindGameObjectWithTag("Level");
        if (levelObj == null)
        {
            Debug.LogWarning("PlayerMovement3D: aucun objet avec le tag 'Level' trouvé, respawn sur place.");
            return transform.position;
        }
        
        var list = levelObj.GetComponent<RespawnPointList>();
        if (list == null)
        {
            Debug.LogWarning("PlayerMovement3D: script 'respawnPointList' introuvable sur l'objet Level, respawn sur place.");
            return transform.position;
        }

        if (list.respawnPoint == null || list.respawnPoint.Count == 0)
        {
            Debug.LogWarning("PlayerMovement3D: aucune respawnPoint dans 'respawnPointList', respawn sur place.");
            return transform.position;
        }
        
        int index = Mathf.Clamp(playerID, 0, list.respawnPoint.Count - 1);
        Transform point = list.respawnPoint[index];

        return point != null ? point.position : transform.position;
    }


    #endregion
    
    #region DAMAGE

    public void GetHit(Vector3 direction, float force, int amount, float duration, bool facing)
    {
        if (!isRecovery)
        {
            Damage(amount, facing);
            Recovery(!(amount <=0));
        
            if (isCrushedNetwork.Value) return;
        
            Stunned(duration);
        }

        Debug.Log("player " + playerID + " get hit, is crushed :" + isCrushed);
        Knockback(direction, force);
        
        
    }

    public void GetHitByCrushing(int amount, float durationMin, int tapActionMin, int tapActionMax, bool facing)
    {
        if (isRecovery || isCrushed) return;
        Debug.Log("Player " + playerID + "just get hit by crushing");
        Damage(amount, facing);
        Recovery(!(amount <=0));
        Crushed(durationMin, tapActionMin, tapActionMax, "isCrushed");
    }

    private void Damage(int amount, bool facing)
    {
        if (isRecovery) return;
        if (amount <= 0) return;
        Debug.Log("Player " + playerID + "just receive damage : " + amount);
        
        ApplyDamageServerRpc(amount, facing);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ApplyDamageServerRpc(int amount, bool facing)
    {
        if (isRecovery) return;
        if (amount <= 0) return;

        CancelDash();
        CancelGlide();

        currentLife.Value -= amount;

        if (hasSuperCollectible.Value)
        {
            Debug.Log($"[SuperBonus] Player {playerID} perd le super bonus suite à un hit.");
            LoseSuperCollectibleAndSpawnTimer();
        }
        
        if (currentLife.Value <= 0)
        {
            Death(facing);
        }

        Debug.Log($"Player {playerID} just Apply damage : amount={amount}, life={currentLife.Value}");
    }






    private void Stunned(float duration)
    {
        if (duration <= 0) return;

        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        if (duration == 0) yield break;
    
        cannotMove = true; 
        isStunned = true; 
        
        SwitchAnimationByNetwork();

        yield return new WaitForSeconds(duration);
    
        cannotMove = false; 
        isStunned = false; 

        if (!isIdleAttcking && !isMovingAttcking && !isAirAttcking && !isStayAirAttacking)
            SwitchAnimation("");
    }
    
    private void Crushed(float duration, int tapActionMin, int tapActionMax, string animationName = "isCrushed")
    {
        if (duration <= 0) return;
        Debug.Log("Player " + playerID + "just been crushed with duration : " + duration + " ; tapActionMin : " + tapActionMin + " ; tapActionMax : " + tapActionMax + " ; AnimationName :" + animationName );
        RequestCrushedServerRpc(duration, tapActionMin, tapActionMax, animationName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCrushedServerRpc(float duration, int tapActionMin, int tapActionMax, string animationName = "isCrushed")
    {
        CrushedOwnerClientRpc(duration, tapActionMin, tapActionMax, animationName);
    }

    [ClientRpc]
    private void CrushedOwnerClientRpc(float duration, int tapActionMin, int tapActionMax, string animationName = "isCrushed")
    {
        if (!IsOwner) return;
        
        if (isCrushedNetwork.Value) return;
        
        StartCoroutine(CrushRoutine(duration, tapActionMin, tapActionMax, animationName));
    }
    



    private IEnumerator CrushRoutine(float duration, int tapActionMin, int tapActionMax, string animationName = "isCrushed")
    {
        if (duration == 0) yield break;

        // Réinit propre
        isCrushed = true;
        crushedTapActionLeft = 0;
        crushTapInProgress = false;

        cannotMove = true;
        SwitchAnimationByNetwork(animationName);

        Debug.Log("Player " + playerID + " just Start crushedCoroutine : duration=" + duration +
                  " tapActionMin=" + tapActionMin + " tapActionMax=" + tapActionMax +
                  " animationName=" + animationName);

        yield return new WaitForSeconds(duration);

        int rng = Random.Range(tapActionMin, tapActionMax);
        crushedTapActionLeft = rng;

        Debug.Log("Player " + playerID + " has finished his coroutine, and choose crushedTapActionLeft = " + rng);

        cannotMove = false;
    }


    private void DisabledCrushedState()
    {
        if (!isCrushed || crushedTapActionLeft <= 0) return;

        Debug.Log("Player " + playerID + "just DisabledCrushedState and crushedTapActionLeft = " +
                  crushedTapActionLeft);

        StartCoroutine(DisabledCrushedStateCoroutine());
    }

    private IEnumerator DisabledCrushedStateCoroutine()
    {
        TriggerLandBounceFX();
            
        Debug.Log("Player " + playerID + "just StartCoroutine and crushedTapActionLeft = " +
                  crushedTapActionLeft);
        
        yield return new WaitForSeconds(0.5f);
        
        --crushedTapActionLeft;

        if (crushedTapActionLeft <= 1)
        {
            isCrushed = false;
            crushedTapActionLeft = 0;
            
            Debug.Log("Player " + playerID + "just FinishedCoroutine and crushedTapActionLeft = " +
                      crushedTapActionLeft + " try to jump ");
            
            yield break;
        }
        
        Debug.Log("Player " + playerID + "just Finished Coroutine and crushedTapActionLeft = " +
                  crushedTapActionLeft);
    }



    private void Recovery(bool isHurt)
    {

        if (!isHurt) return;
        if (isRecovery) return; 

        RequestRecoveryServerRpc();
    }

    private IEnumerator RecoveryRoutine()
    {
        if (isRecovery)
            yield break;

        isRecovery = true;

        float blinkTime = 0.1f;
        float end = Time.time + recoveryTime;

        while (Time.time < end)
        {
            if (playerModel != null)
                playerModel.SetActive(false);
            yield return new WaitForSeconds(blinkTime);

            if (playerModel != null)
                playerModel.SetActive(true);
            yield return new WaitForSeconds(blinkTime);
        }

        if (playerModel != null)
            playerModel.SetActive(true);

        isRecovery = false;
    }

    
    [ServerRpc(RequireOwnership = false)]
    private void RequestRecoveryServerRpc()
    {
        StartRecoveryClientRpc();
    }

    [ClientRpc]
    private void StartRecoveryClientRpc()
    {
        if (isRecovery) return;
        StartCoroutine(RecoveryRoutine());
    }



    public void Death(bool facing)
    {
        
        if (!IsServer)
            return;

        if (isDead.Value) return;

        if (hasSuperCollectible.Value)
        {
            LoseSuperCollectibleAndSpawnTimer();
        }
        
        isDead.Value = true;
        cannotMove   = true;

        Debug.Log("Death");
        
        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }


        OnDeathClientRpc(facing);
    }

    [ClientRpc]
    private void OnDeathClientRpc(bool facing)
    {
        if (ragdollController != null)
        {
            ragdollController.EnableRagdoll(true);
            ragdollController.PlayDeathImpulse(facing);
        }
        
        SetIconState(PlayerIconState.Dead, true);
        UpdateLifeUI(currentLife.Value, currentMaxLife);
        
        if (IsOwner)
        {
            if (respawnCoroutine != null)
                StopCoroutine(respawnCoroutine);

            respawnCoroutine = StartCoroutine(RespawnCountdownRoutine());
        }
    }

    private IEnumerator RespawnCountdownRoutine()
    {
        canPressRespawn = false;

        if (respawnTextTMP != null)
            respawnTextTMP.gameObject.SetActive(true);

        float timer = respawnCooldown;

        while (timer > 0f)
        {
            int seconds = Mathf.CeilToInt(timer);
            SetRespawnMessage($"Vous êtes mort, respawn dans : {seconds} s");

            timer -= Time.deltaTime;
            yield return null;
        }
        
        canPressRespawn = true;
        SetRespawnMessage("Appuyez sur Start pour respawn");
    }

    private void SetRespawnMessage(string message)
    {
        if (!IsOwner) return; 

        if (respawnTextTMP != null)
            respawnTextTMP.text = message;

        if (respawnTextUI != null)
            respawnTextUI.text = message;
    }
    
    public void Respawn()
    {
        if (!IsServer)
            return;
        
        Vector3 spawnPos = GetRespawnPosition();
        
        transform.position = spawnPos;

        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        isDead.Value = false;
        cannotMove   = false;
        
        currentLife.Value = currentMaxLife;
        
        RespawnClientRpc(spawnPos);
        
        StartRecoveryClientRpc();
    }
    
    [ServerRpc(RequireOwnership = true)]
    private void RequestRespawnServerRpc()
    {
        if (!isDead.Value) return;

        Respawn();
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 spawnPos)
    {
        transform.position = spawnPos;
        
        if (ragdollController != null)
            ragdollController.EnableRagdoll(false);
        
        SetIconState(PlayerIconState.Idle, true);
        UpdateLifeUI(currentLife.Value, currentMaxLife);
        
        if (IsOwner)
        {
            if (respawnTextTMP.gameObject != null)
                respawnTextTMP.gameObject.SetActive(false);

            canPressRespawn = false;

            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }
        }
    }

    
    
    #endregion

    #region MOVEMENT

    private void HandleMovement2D()
    {
        if (isStunned) return;
        if (isCrushed) return;
        if (isIdleAttcking || isIdleAttackStopping) return;


        float currentVelX = rb.linearVelocity.x;
        float desiredSpeed = targetSpeed;
        float accelRate;
        if (lastOnGroundTime > 0)
            accelRate = (Mathf.Abs(desiredSpeed) > 0.01f) ? data.runAccelAmount : data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(desiredSpeed) > 0.01f)
                ? data.runAccelAmount * data.accelInAir
                : data.runDeccelAmount * data.deccelInAir;

        if ((isJumping || isWallJumping || isJumpFalling) &&
            Mathf.Abs(rb.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            accelRate *= data.jumpHangAccelerationMult;
            desiredSpeed *= data.jumpHangMaxSpeedMult;
        }

        if (data.doConserveMomentum &&
            Mathf.Abs(currentVelX) > Mathf.Abs(desiredSpeed) &&
            Mathf.Sign(currentVelX) == Mathf.Sign(desiredSpeed) &&
            Mathf.Abs(desiredSpeed) > 0.01f &&
            lastOnGroundTime < 0)
        {
            accelRate = 0;
        }

        float speedDif = desiredSpeed - currentVelX;
        float movement = speedDif * accelRate;

        rb.AddForce(Vector3.right * movement, ForceMode.Force);

        //animation

        if (isJumping || isJumpFalling || isIdleAttcking || isMovingAttcking ||
            isAirAttcking || isStayAirAttacking || isGliding) return;


        if (Mathf.Abs(moveInput.x) == 0)
        {
            SwitchAnimation("");
        }
        else if (Mathf.Abs(moveInput.x) >= 0.5f)
        {
            SwitchAnimation("isRunning");
        }
        else
        {
            SwitchAnimation("isWalking");
        }
    }

    private void HandleMovement3D()
    {

        if (isStunned) return;
        if (isCrushed) return;
        if (isIdleAttcking || isIdleAttackStopping) return;


        Vector3 inputDir = new Vector3(moveInput.y, 0f, -moveInput.x);
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float inputMag = Mathf.Clamp01(inputDir.magnitude);
        float targetSpeedMagnitude = data.runMaxSpeed * inputMag;
        Vector3 desiredVel = (inputMag > 0.01f) ? inputDir.normalized * targetSpeedMagnitude : Vector3.zero;

        float accelRate;
        if (lastOnGroundTime > 0)
            accelRate = (targetSpeedMagnitude > 0.01f) ? data.runAccelAmount : data.runDeccelAmount;
        else
            accelRate = (targetSpeedMagnitude > 0.01f)
                ? data.runAccelAmount * data.accelInAir
                : data.runDeccelAmount * data.deccelInAir;

        if ((isJumping || isWallJumping || isJumpFalling) &&
            Mathf.Abs(rb.linearVelocity.y) < data.jumpHangTimeThreshold)
        {
            accelRate *= data.jumpHangAccelerationMult;
            desiredVel *= data.jumpHangMaxSpeedMult;
        }

        Vector3 speedDif = desiredVel - horizontalVel;
        Vector3 movement = speedDif * accelRate;

        rb.AddForce(movement, ForceMode.Force);

        //animation

        if (isJumping || isJumpFalling || isIdleAttcking || isMovingAttcking ||
            isAirAttcking || isStayAirAttacking || isGliding) return;


        if (Mathf.Abs(moveInput.x) == 0 && Mathf.Abs(moveInput.y) == 0)
        {
            SwitchAnimation("");
        }
        else if (Mathf.Abs(moveInput.x) >= 0.5f || Mathf.Abs(moveInput.y) >= 0.5f)
        {
            SwitchAnimation("isRunning");
        }
        else
        {
            SwitchAnimation("isWalking");
        }
    }

    public void Jump()
    {
        if (isIdleAttcking || isIdleAttackStopping) return;
        
        lastPressedJumpTime = 0;
        lastOnGroundTime = 0;

        float force = data.jumpForce;

        if (rb.linearVelocity.y < 0)
        {
            force -= rb.linearVelocity.y;
        }

        SwitchAnimation("isJumping");

        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }
    
    public void Bump(float multiplier = 0.1f)
    {
        if (isCrushed) return;
        if (multiplier <= 0f)
            multiplier = 1f;
        
        RequestBumpServerRpc(multiplier);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBumpServerRpc(float multiplier)
    {
        BumpOwnerClientRpc(multiplier);
    }

    [ClientRpc]
    private void BumpOwnerClientRpc(float multiplier)
    {
        if (!IsOwner) return;

        ApplyBump(multiplier);
    }

    private void ApplyBump(float multiplier)
    {
        if (data == null || rb == null) return;
        
        float force = data.jumpForce * multiplier;
        
        rb.linearVelocity = Vector3.zero;
        
        lastPressedJumpTime = 0f;
        lastOnGroundTime   = 0f;
        
        SwitchAnimation("isFalling");
        
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        
        Debug.Log("Player " + playerID + " has Bump " + force);
    }


    private void WallJump(int dir)
    {
        if (!canWallJump || isStunned|| isCrushed || data == null) return;

        lastPressedJumpTime = 0;
        lastOnGroundTime = 0;
        lastOnWallRightTime = 0;
        lastOnWallLeftTime = 0;

        // SoundManager.Instance.PlayRandomSFX(clipsRandomWalljump, 0.9f, 1.1f);

        Vector3 force = new Vector3(data.wallJumpForce.x * dir, data.wallJumpForce.y, 0f);

        if (Mathf.Sign(rb.linearVelocity.x) != Mathf.Sign(force.x))
            force.x -= rb.linearVelocity.x;

        if (rb.linearVelocity.y < 0)
            force.y -= rb.linearVelocity.y;

        rb.AddForce(force, ForceMode.Impulse);
    }

    private void Slide3D()
    {
        if (isStunned ||isCrushed || data == null) return;

        if (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }

        float targetY = data.slideSpeed;
        float speedDif = targetY - rb.linearVelocity.y;
        float movement = speedDif * data.slideAccel;

        float maxForce = Mathf.Abs(speedDif) / Time.fixedDeltaTime;
        movement = Mathf.Clamp(movement, -maxForce, maxForce);

        rb.AddForce(Vector3.up * movement, ForceMode.Force);

        Vector3 vel = rb.linearVelocity;
        vel.z = 0f;
        rb.linearVelocity = vel;
    }
    
    IEnumerator StartDash(Vector3 dir)
    {
        if (!canDash || isCrushed || isStunned || data == null)
        {
            isDashing = false;
            yield break;
        }

        StartNewAttackInstance();
        
        if (isGliding)
            CancelGlide();

        SwitchAnimation("isDashing");

        float dashCompressFactor = 0.6f;
        float dashStretchFactor = 1.9f;

        Transform dashChild = null;
        Vector3 childOriginalScale = Vector3.one;
        Quaternion childOriginalRotation = Quaternion.identity;

        if (transform.childCount > 0)
        {
            dashChild = transform.GetChild(0);
            childOriginalScale = dashChild.localScale;
            childOriginalRotation = dashChild.localRotation;
        }

        Tween stretchTween = null;
        if (dashChild != null)
        {
            float dashDuration = data.dashAttackTime;
            Vector3 startScale = dashChild.localScale;
            Vector3 targetScale = new Vector3(
                startScale.x * dashStretchFactor,
                startScale.y * dashCompressFactor,
                startScale.z
            );


            Vector2 animInput = moveInput;
            
            if (GameManager.instance != null && GameManager.instance.is3d)
            {
                animInput = new Vector2(-moveInput.y, moveInput.x);
            }

            if (!isFacingRight)
            {
                animInput.x *= -1f;
            }


            if (animInput.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(animInput.y, animInput.x) * Mathf.Rad2Deg;

                Vector3 baseEuler = childOriginalRotation.eulerAngles;
                dashChild.localRotation = Quaternion.Euler(baseEuler.x, baseEuler.y, angle);
            }
            else
            {
                dashChild.localRotation = childOriginalRotation;
            }

            stretchTween = dashChild
                .DOScale(targetScale, dashDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    dashChild.DOScale(startScale, dashDuration * 0.5f)
                             .SetEase(Ease.OutQuad);
                });
        }

        lastOnGroundTime = 0f;
        lastPressedDashTime = 0f;
        lastPressedJumpTime = 0f;

        float startTime = Time.time;

        dashesLeft--;
        isDashAttacking = true;

        SetGravityScale(0f);
        Vector3 dashDir;
        if (GameManager.instance != null && GameManager.instance.is3d)
        {
            dashDir = new Vector3(dir.x, 0f, dir.z).normalized;
        }
        else
        {
            dashDir = new Vector3(dir.x, dir.y, 0f).normalized;
        }

        float currentAlongDash = Vector3.Dot(rb.linearVelocity, dashDir);
        float targetDashSpeed = currentAlongDash + data.dashSpeed;
        targetDashSpeed = Mathf.Clamp(targetDashSpeed, data.dashSpeed, data.dashMaxSpeed);
        
        while (!dashCancelledExternally && Time.time - startTime <= data.dashAttackTime)
        {
            if (jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                if (TryDashJump(dashDir, dashChild, childOriginalScale, childOriginalRotation, stretchTween))
                    yield break;
            }

            Vector3 vel = rb.linearVelocity;

            float currentAlongNow = Vector3.Dot(vel, dashDir);
            Vector3 velPerp = vel - dashDir * currentAlongNow;

            Vector3 dashVel = dashDir * targetDashSpeed;

            rb.linearVelocity = velPerp + dashVel;

            yield return null;
        }

        if (stretchTween != null && stretchTween.IsActive())
            stretchTween.Kill();
        if (dashChild != null)
        {
            dashChild.localScale = childOriginalScale;
            dashChild.localRotation = childOriginalRotation;
        }

        isDashAttacking = false;
        
        if (!dashCancelledExternally && rb.linearVelocity.y > 0f)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y *= 0.42f;
            rb.linearVelocity = vel;
        }

        SetGravityScale(data.gravityScale);

        isDashing = false;
        dashCancelledExternally = false; 
    }

    
    
    public void CancelDash()
    {
        if (!isDashing && !isDashAttacking) return;
        
        dashCancelledExternally = true;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    

    private IEnumerator StartGroundPound()
    {
        if (isCrushed || isStunned) yield break;
        
        if (GameManager.instance != null && GameManager.instance.is3d) yield break;

        StartNewAttackInstance();

        if (isGliding)
            CancelGlide();

        if (data == null)
            yield break;

        isGroundPounding = true;
        groundPoundCancelledExternally = false;

        isDashAttacking = false;
        isDashing = false;
        isJumping = false;
        isWallJumping = false;
        isJumpCut = false;
        isJumpFalling = false;
        isStayAirAttacking = false;

        Transform child = null;
        if (transform.childCount > 0)
        {
            child = transform.GetChild(0);
            Vector3 euler = child.localRotation.eulerAngles;
            child.localRotation = Quaternion.Euler(euler.x, euler.y, 0f);
        }

        SwitchAnimation("isGroundPound");

        SetGravityScale(0f);
        
        while (!wasGroundedLastFrame && !groundPoundCancelledExternally)
        {
            Vector3 vel = rb.linearVelocity;
            vel.x = 0f;
            vel.z = 0f;
            vel.y = -data.groundPoundSpeed;
            rb.linearVelocity = vel;

            yield return null;
        }

        isGroundPounding = false;
        
        rb.linearVelocity = Vector3.zero;
        
        SetGravityScale(data.gravityScale);
        
        if (groundPoundCancelledExternally && !wasGroundedLastFrame)
        {
            groundPoundCancelledExternally = false;
            
            if (!isJumping && !isAirAttcking && !isStayAirAttacking && !isGliding)
            {
                SwitchAnimation("isFalling");
                isFalling = true;
            }

            yield break;
        }


        groundPoundCancelledExternally = false;
        StartCoroutine(GroundPoundLanding());
    }


    public void CancelGroundPound()
    {
        if (!isGroundPounding) return;
        
        groundPoundCancelledExternally = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        Debug.Log("CancelGroundPound() appelé sur le player " + playerID);
    }

    




    public void StartNewAttackInstance()
    {
        if (currentAttackInstanceId == int.MaxValue)
            currentAttackInstanceId = 1;
        else
            currentAttackInstanceId++;
    }
    
    private void StartIdleAttack()
    {
        if (isStunned || isCrushed) return;
        
        if (data == null || rb == null) return;

        StartNewAttackInstance();
        
        isIdleAttcking = true;
        isIdleAttackStopping = false;
        idleAttackReleaseQueued = false;
        idleAttackStartTime = Time.time;

        isMovingAttcking = false;
        isAirAttcking = false;
        isStayAirAttacking = false;
        
        Vector3 vel = rb.linearVelocity;
        vel.x = 0f;
        vel.z = 0f;
        rb.linearVelocity = vel;

        SwitchAnimation("isIdleAttack");
    }
    
    private void StartIdleAttackStop()
    {
        if (isStunned || isCrushed)return;
        
        isIdleAttackStopping = true;
        idleAttackStopStartTime = Time.time;
        
        SwitchAnimation("isIdleAttackStop");
    }


    
    private bool TryDashJump(Vector3 dashDir, Transform dashChild, Vector3 childOriginalScale, Quaternion childOriginalRotation, Tween stretchTween)
    {
        
        
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Vector3 effectiveNormal = Vector3.up;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, data.dashJumpMaxDistance, groundLayer))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            if (slopeAngle <= data.dashJumpMaxSlopeAngle)
            {
                effectiveNormal = hit.normal;
            }
        }
        
        if (stretchTween != null && stretchTween.IsActive())
            stretchTween.Kill();

        if (dashChild != null)
        {
            dashChild.localScale = childOriginalScale;
            dashChild.localRotation = childOriginalRotation;
        }
        
        isDashAttacking = false;
        isDashing = false;

        SetGravityScale(data.gravityScale);

        isJumping = true;
        isWallJumping = false;
        isJumpCut = false;
        isJumpFalling = false;

        lastPressedJumpTime = 0f;
        lastOnGroundTime = 0f;


        Vector3 vel = rb.linearVelocity;
        
        Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);
        
        Vector3 upDir = Vector3.Lerp(Vector3.up, effectiveNormal, 0.4f).normalized;
        
        float upSpeed = data.dashJumpUpSpeed;

        float currentAlongUp = Vector3.Dot(vel, upDir);
        Vector3 velPerpToUp = vel - upDir * currentAlongUp;

        Vector3 finalVel = velPerpToUp + upDir * upSpeed;
        
        if (finalVel.y > data.dashJumpMaxUpSpeed)
        {
            finalVel.y = data.dashJumpMaxUpSpeed;
        }

        rb.linearVelocity = finalVel;
        
        SwitchAnimation("isJumping");

        return true;
    }




    private void StartMovingAttack()
    {
        if (isStunned || isCrushed) return;

        StartNewAttackInstance();
        
        isIdleAttcking = false;
        isMovingAttcking = true;
        isAirAttcking = false;
        isStayAirAttacking = false;

        attackStateEndTime = Time.time + data.movingAttackTime;

        if (GameManager.instance != null && GameManager.instance.is3d)
        {
            SwitchAnimation("is3DAttack");
        }
        else
        {
            SwitchAnimation("is2DAttack");
        }
        
    }

    private void StartAirAttack()
    {
        if (isStunned || isCrushed) return;

        StartNewAttackInstance();
        
        if (isGliding)
            CancelGlide();
        
        isIdleAttcking = false;
        isMovingAttcking = false;
        isAirAttcking = true;
        isStayAirAttacking = false;

        attackStateEndTime = Time.time + data.airAttackTime;

        SwitchAnimation("isAirAttack");
    }

    private void StartStayAirAttack()
    {
        if (isStunned || isCrushed) return;

        StartNewAttackInstance();
        
        if (isGliding)
            CancelGlide();
        
        isIdleAttcking = false;
        isMovingAttcking = false;
        isAirAttcking = false;
        isStayAirAttacking = true;
    
        float rawHeight = Mathf.Max(0f, lastJumpMaxY - lastGroundY);
        if (rawHeight <= 0.01f)
            rawHeight = data.minHeightBounce;
    
        rawHeight = Mathf.Clamp(rawHeight, data.minHeightBounce, data.maxHeightBounce);
        
        stayAirCurrentHeight = rawHeight;

        Vector3 vel = rb.linearVelocity;
        stayAirAttackVelocity = new Vector3(vel.x, 0f, vel.z);

        if (vel.y > 0f)
        {
            vel.y = 0f;
            rb.linearVelocity = vel;
        }

        SwitchAnimation("isStayAttack");
    }
    

    private void Knockback(Vector3 dir, float force)
    {
        if (isCrushedNetwork.Value) return;
        
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        if (GameManager.instance != null && !GameManager.instance.is3d)
        {
            dir = new Vector3(dir.x, dir.y, 0);
        }
        
        dir = dir.normalized;

        RequestKnockbackServerRpc(dir, force);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestKnockbackServerRpc(Vector3 dir, float force)
    {
        KnockbackOwnerClientRpc(dir, force);
    }

    [ClientRpc]
    private void KnockbackOwnerClientRpc(Vector3 dir, float force)
    {
        if (!IsOwner) return;

        ApplyKnockback(dir, force);
    }

    private void ApplyKnockback(Vector3 dir, float force)
    {
        if (rb == null) return;
        if (isDead.Value || IsRagdoll) return;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dir * force, ForceMode.Impulse);
    }





    
 



    #endregion

    #region COLLISION & FRICTION
    

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead.Value || IsRagdoll) return;  
        
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0) return;

        if (collision.contactCount == 0) return;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (isDead.Value || IsRagdoll) return;  
        
        if (isStunned) return;
        
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0) return;

        if (collision.contactCount == 0) return;
        
        AdaptRotationToTerrain2D(collision);

        
    }

    private void OnCollisionExit(Collision collision)
    {
        if (isDead.Value || IsRagdoll) return;  
        
        if (isStunned) return;
        
        if ((groundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        Vector3 euler = transform.eulerAngles;
        euler.z = 0f;
        transform.rotation = Quaternion.Euler(euler);

        if (colliderBottomBody != null && noFrictionMaterial != null)
            colliderBottomBody.sharedMaterial = noFrictionMaterial;
    }

    #endregion
    
    #region DYNAMIC COLLIDERS 2D / 3D

    private void InitializeDynamicColliders()
    {
        if (dynamicColliderInitialized)
            return;

        if (colliderBottomBody != null)
        {
            capsuleHeight3D = colliderBottomBody.height;
            capsuleRadius3D = colliderBottomBody.radius;
            capsuleCenter3D = colliderBottomBody.center;
        }

        if (collideTopBody != null)
        {
            topBodySize3D   = collideTopBody.size;
            topBodyCenter3D = collideTopBody.center;
        }

        if (colliderFeet != null)
        {
            feetSize3D   = colliderFeet.size;
            feetCenter3D = colliderFeet.center;
        }

        if (colliderDash != null)
        {
            dashSize3D   = colliderDash.size;
            dashCenter3D = colliderDash.center;
        }

        if (colliderGroundPound != null)
        {
            groundPoundSize3D   = colliderGroundPound.size;
            groundPoundCenter3D = colliderGroundPound.center;
        }

        if (colliderWeapon != null)
        {
            weaponSize3D   = colliderWeapon.size;
            weaponCenter3D = colliderWeapon.center;
        }

        groundCheckSizeZ3D = groundCheckSize.z;
        wallCheckSizeZ3D   = wallCheckSize.z;

        if (groundCheckPoint != null)
            groundCheckOffsetZ3D = groundCheckPoint.localPosition.z;

        if (frontWallCheckPoint != null)
            frontWallCheckOffsetZ3D = frontWallCheckPoint.localPosition.z;

        if (backWallCheckPoint != null)
            backWallCheckOffsetZ3D = backWallCheckPoint.localPosition.z;

        if (GameManager.instance != null)
            lastIs3DState = GameManager.instance.is3d;
        else
            lastIs3DState = true;

        lastPosZHit = null;
        lastNegZHit = null;
        collider2DAtMaxExtent = false;

        currentCollider2DSizeZ   = 0f;
        currentCollider2DCenterZ = 0f;
        currentPosExtentZ2D      = 0f;
        currentNegExtentZ2D      = 0f;
        targetCollider2DSizeZ    = 0f;
        targetCollider2DCenterZ  = 0f;
        targetPosExtentZ2D       = 0f;
        targetNegExtentZ2D       = 0f;
        collider2DResizePending  = false;
        collider2DNextResizeTime = 0f;

        dynamicColliderInitialized = true;
    }

    private void UpdateDynamicColliders()
    {
        if (GameManager.instance == null)
            return;

        if (!dynamicColliderInitialized)
            InitializeDynamicColliders();


        
        if (is3DNow.Value != lastIs3DState)
        {
            lastIs3DState = is3DNow.Value;

            lastPosZHit = null;
            lastNegZHit = null;
            collider2DAtMaxExtent = false;

            currentCollider2DSizeZ   = 0f;
            currentCollider2DCenterZ = 0f;
            currentPosExtentZ2D      = 0f;
            currentNegExtentZ2D      = 0f;
            targetCollider2DSizeZ    = 0f;
            targetCollider2DCenterZ  = 0f;
            targetPosExtentZ2D       = 0f;
            targetNegExtentZ2D       = 0f;
            collider2DResizePending  = false;

            if (is3DNow.Value)
            {
                SwitchToCollider3D();
            }
            else
            {
      
                SwitchToCollider2D(true);
            }

            return;
        }
        
        if (!is3DNow.Value)
        {
            if (collider2DAtMaxExtent)
                return;

            SwitchToCollider2D(false);
        }
    }

    private void ApplyCollider2DSize(float newSizeZ, float centerOffsetZ)
    {

        if (colliderBottomBody != null)
        {
            Vector3 c = colliderBottomBody.center;
            c.z = centerOffsetZ;
            colliderBottomBody.center = c;
            
            colliderBottomBody.height = newSizeZ;

            float halfThickness = newSizeZ * 0.5f;
            float targetRadius  = Mathf.Min(capsuleRadius3D, halfThickness);
            colliderBottomBody.radius = targetRadius;
        }


        if (collideTopBody != null)
        {
            Vector3 size = collideTopBody.size;
            size.z = newSizeZ;
            collideTopBody.size = size;

            Vector3 c = collideTopBody.center;
            c.z = centerOffsetZ;
            collideTopBody.center = c;
        }

        if (colliderFeet != null)
        {
            Vector3 size = colliderFeet.size;
            size.z = newSizeZ;
            colliderFeet.size = size;

            Vector3 c = colliderFeet.center;
            c.z = centerOffsetZ;
            colliderFeet.center = c;
        }
        
        if (colliderDash != null)
        {
            Vector3 size = colliderDash.size;
            size.z = newSizeZ;
            colliderDash.size = size;

            Vector3 c = colliderDash.center;
            c.z = centerOffsetZ;
            colliderDash.center = c;
        }
        
        if (colliderGroundPound != null)
        {
            Vector3 size = colliderGroundPound.size;
            size.z = newSizeZ;
            colliderGroundPound.size = size;

            Vector3 c = colliderGroundPound.center;
            c.z = centerOffsetZ;
            colliderGroundPound.center = c;
        }
        
        if (colliderWeapon != null)
        {
            Vector3 size = colliderWeapon.size;
            size.z = newSizeZ;
            colliderWeapon.size = size;

            Vector3 c = colliderWeapon.center;
            c.z = centerOffsetZ;
            colliderWeapon.center = c;
        }
        
        Vector3 gSize = groundCheckSize;
        gSize.z = newSizeZ;
        groundCheckSize = gSize;

        Vector3 wSize2 = wallCheckSize;
        wSize2.z = newSizeZ;
        wallCheckSize = wSize2;

        if (groundCheckPoint != null)
        {
            Vector3 p = groundCheckPoint.localPosition;
            p.z = centerOffsetZ;
            groundCheckPoint.localPosition = p;
        }

        if (frontWallCheckPoint != null)
        {
            Vector3 p = frontWallCheckPoint.localPosition;
            p.z = centerOffsetZ;
            frontWallCheckPoint.localPosition = p;
        }

        if (backWallCheckPoint != null)
        {
            Vector3 p = backWallCheckPoint.localPosition;
            p.z = centerOffsetZ;
            backWallCheckPoint.localPosition = p;
        }
    }

    private void SwitchToCollider3D()
    {
        if (!dynamicColliderInitialized) return;

        if (colliderBottomBody != null)
        {
            colliderBottomBody.height = capsuleHeight3D;
            colliderBottomBody.radius = capsuleRadius3D;
            colliderBottomBody.center = capsuleCenter3D;
        }

        if (collideTopBody != null)
        {
            collideTopBody.size   = topBodySize3D;
            collideTopBody.center = topBodyCenter3D;
        }

        if (colliderFeet != null)
        {
            colliderFeet.size   = feetSize3D;
            colliderFeet.center = feetCenter3D;
        }

        if (colliderDash != null)
        {
            colliderDash.size   = dashSize3D;
            colliderDash.center = dashCenter3D;
        }

        if (colliderGroundPound != null)
        {
            colliderGroundPound.size   = groundPoundSize3D;
            colliderGroundPound.center = groundPoundCenter3D;
        }

        if (colliderWeapon != null)
        {
            colliderWeapon.size   = weaponSize3D;
            colliderWeapon.center = weaponCenter3D;
        }

        Vector3 gSize = groundCheckSize;
        gSize.z = groundCheckSizeZ3D;
        groundCheckSize = gSize;

        Vector3 wSize = wallCheckSize;
        wSize.z = wallCheckSizeZ3D;
        wallCheckSize = wSize;

        if (groundCheckPoint != null)
        {
            Vector3 p = groundCheckPoint.localPosition;
            p.z = groundCheckOffsetZ3D;
            groundCheckPoint.localPosition = p;
        }

        if (frontWallCheckPoint != null)
        {
            Vector3 p = frontWallCheckPoint.localPosition;
            p.z = frontWallCheckOffsetZ3D;
            frontWallCheckPoint.localPosition = p;
        }

        if (backWallCheckPoint != null)
        {
            Vector3 p = backWallCheckPoint.localPosition;
            p.z = backWallCheckOffsetZ3D;
            backWallCheckPoint.localPosition = p;
        }
    }

    private void SwitchToCollider2D(bool forceRecalculation)
    {
        if (!dynamicColliderInitialized) return;
        
        Vector3 origin = transform.position;

        float maxExtent = Mathf.Max(0.01f, collider2DMaxSizeZ);             
        float rayMax    = (raycastLimit > 0f) ? raycastLimit : maxExtent * 2;
        float signedPos =  maxExtent; 
        float signedNeg = -maxExtent;

        RaycastHit hit;

        if (Physics.Raycast(origin, Vector3.forward, out hit, rayMax, groundLayer))
        {
            float d = Mathf.Clamp(hit.distance, 0f, maxExtent);
            signedPos = d;
            lastPosZHit = hit.collider;
        }

        if (Physics.Raycast(origin, Vector3.back, out hit, rayMax, groundLayer))
        {
            float d = Mathf.Clamp(hit.distance, 0f, maxExtent);
            signedNeg = -d;
            lastNegZHit = hit.collider;
        }

        float candidatePosExtent = Mathf.Max(0.01f, signedPos);  
        float candidateNegExtent = Mathf.Max(0.01f, -signedNeg); 
        float candidateSpan    = Mathf.Clamp(candidatePosExtent + candidateNegExtent, 0.02f, maxExtent * 2f);
        float candidateCenterZ = (signedPos + signedNeg) * 0.5f;

        const float epsilon = 0.001f;

        if (forceRecalculation || currentCollider2DSizeZ <= epsilon)
        {
            currentPosExtentZ2D      = candidatePosExtent;
            currentNegExtentZ2D      = candidateNegExtent;
            currentCollider2DSizeZ   = candidateSpan;
            currentCollider2DCenterZ = candidateCenterZ;

            collider2DResizePending = false;
            collider2DAtMaxExtent   =
                currentPosExtentZ2D >= maxExtent - 0.001f &&
                currentNegExtentZ2D >= maxExtent - 0.001f;

            ApplyCollider2DSize(currentCollider2DSizeZ, currentCollider2DCenterZ);
            return;
        }

        float prevPosExtent = currentPosExtentZ2D;
        float prevNegExtent = currentNegExtentZ2D;
        float desiredPosExtent = Mathf.Max(prevPosExtent, candidatePosExtent);
        float desiredNegExtent = Mathf.Max(prevNegExtent, candidateNegExtent);

        bool wantGrow =
            desiredPosExtent > prevPosExtent + epsilon ||
            desiredNegExtent > prevNegExtent + epsilon;

        if (!wantGrow)
        {
            return;
        }

        float desiredSpan    = Mathf.Clamp(desiredPosExtent + desiredNegExtent, 0.02f, maxExtent * 2f);
        float desiredCenterZ = (desiredPosExtent - desiredNegExtent) * 0.5f;
        
        if (!collider2DResizePending)
        {
            targetPosExtentZ2D      = desiredPosExtent;
            targetNegExtentZ2D      = desiredNegExtent;
            targetCollider2DSizeZ   = desiredSpan;
            targetCollider2DCenterZ = desiredCenterZ;

            collider2DNextResizeTime = Time.time + collider2DResizeDelay;
            collider2DResizePending  = true;
        }
        else
        {
            if (desiredPosExtent > targetPosExtentZ2D + epsilon ||
                desiredNegExtent > targetNegExtentZ2D + epsilon)
            {
                targetPosExtentZ2D = Mathf.Max(targetPosExtentZ2D, desiredPosExtent);
                targetNegExtentZ2D = Mathf.Max(targetNegExtentZ2D, desiredNegExtent);

                targetCollider2DSizeZ   = Mathf.Clamp(targetPosExtentZ2D + targetNegExtentZ2D, 0.02f, maxExtent * 2f);
                targetCollider2DCenterZ = (targetPosExtentZ2D - targetNegExtentZ2D) * 0.5f;

                collider2DNextResizeTime = Time.time + collider2DResizeDelay;
            }
        }
        
        if (collider2DResizePending && Time.time >= collider2DNextResizeTime)
        {
            currentPosExtentZ2D      = targetPosExtentZ2D;
            currentNegExtentZ2D      = targetNegExtentZ2D;
            currentCollider2DSizeZ   = targetCollider2DSizeZ;
            currentCollider2DCenterZ = targetCollider2DCenterZ;

            collider2DResizePending = false;
            collider2DAtMaxExtent   =
                currentPosExtentZ2D >= maxExtent - 0.001f &&
                currentNegExtentZ2D >= maxExtent - 0.001f;

            ApplyCollider2DSize(currentCollider2DSizeZ, currentCollider2DCenterZ);
        }
    }

    #endregion
    
    #region SUPER COLLECTIBLE VERSUS
    
    public void GainSuperCollectible(int scorePerTick, float intervalSeconds)
    {
        if (!IsServer) return;

        superCollectibleScorePerTick = scorePerTick;
        superCollectibleInterval = intervalSeconds;

        hasSuperCollectible.Value = true;

        if (superCollectibleCoroutine != null)
            StopCoroutine(superCollectibleCoroutine);

        superCollectibleCoroutine = StartCoroutine(SuperCollectibleRoutine());
    }
    
    private IEnumerator SuperCollectibleRoutine()
    {
        while (hasSuperCollectible.Value && !isDead.Value)
        {
            yield return new WaitForSeconds(superCollectibleInterval);

            if (!hasSuperCollectible.Value || isDead.Value)
                break;

            AddScoreVersus(superCollectibleScorePerTick);
        }

        superCollectibleCoroutine = null;
    }

    private void LoseSuperCollectibleAndSpawnTimer()
    {
        if (!IsServer) return;
        if (!hasSuperCollectible.Value) return;

        hasSuperCollectible.Value = false;

        if (superCollectibleCoroutine != null)
        {
            StopCoroutine(superCollectibleCoroutine);
            superCollectibleCoroutine = null;
        }

        if (superCollectibleTimerPrefab == null)
            return;

        Vector3 spawnPos = transform.position + Vector3.up * superCollectibleDropHeight;
        Quaternion spawnRot = Quaternion.identity;

        GameObject timerObj = Instantiate(superCollectibleTimerPrefab, spawnPos, spawnRot);
        NetworkObject netObj = timerObj.GetComponent<NetworkObject>();

        SuperCollectibleVersusTimer timer = timerObj.GetComponent<SuperCollectibleVersusTimer>();
        if (timer != null)
        {
            timer.Initialize(
                this,
                superCollectibleScorePerTick,
                superCollectibleInterval,
                superCollectibleTimerLifetime,
                superCollectibleTimerPickupLockDuration
            );
        }

        if (netObj != null && !netObj.IsSpawned)
        {
            netObj.Spawn(true);
        }
    }

    #endregion

    
    #region UI
    
    public void AddScoreVersus(int amount)
    {
        if (amount == 0) return;

        if (!IsServer)
        {
            RequestAddScoreVersusServerRpc(amount);
            return;
        }

        int newValue = Mathf.Max(0, scoreVersus.Value + amount);
        scoreVersus.Value = newValue;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAddScoreVersusServerRpc(int amount)
    {
        AddScoreVersus(amount);
    }

    private void OnScoreVersusChanged(int previousValue, int newValue)
    {
        if (!DisplayScore)
        {
            UpdateScoreVersusUI(newValue);
            return;
        }
        
        if (scoreVersusAnimationCoroutine != null)
            StopCoroutine(scoreVersusAnimationCoroutine);
        
        scoreVersusAnimationCoroutine = StartCoroutine(
            AnimateScoreVersus(previousValue, newValue)
        );
    }

    private IEnumerator AnimateScoreVersus(int from, int to)
    {
        if (from == to)
        {
            UpdateScoreVersusUI(to);
            scoreVersusAnimationCoroutine = null;
            yield break;
        }

        const float duration = 1f;      
        int direction = (to > from) ? 1 : -1;

        int diff = Mathf.Abs(to - from);
        
        int stepMagnitude = diff < 2 ? 1 : 2;
        int step = stepMagnitude * direction;

        int steps = Mathf.CeilToInt((float)diff / stepMagnitude);
        float stepDuration = duration / steps;

        float elapsed = 0f;
        float nextStepTime = 0f;
        int current = from;
        
        UpdateScoreVersusUI(current);

        while ((direction > 0 && current < to) ||
               (direction < 0 && current > to))
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextStepTime)
            {
                int remaining = to - current;
                
                if (direction > 0 && current + step > to ||
                    direction < 0 && current + step < to)
                {
                    current = to;
                }
                else
                {
                    current += step;
                }

                UpdateScoreVersusUI(current);
                nextStepTime += stepDuration;
            }

            yield return null;
        }
        
        UpdateScoreVersusUI(to);
        scoreVersusAnimationCoroutine = null;
    }


    private void UpdateScoreVersusUI(int value)
    {
        if (!DisplayScore) return;
        
        string text = $"Score : {value.ToString("D7")}";

        if (scoreVersusTMP != null)
            scoreVersusTMP.text = text;

        if (scoreVersusUI != null)
            scoreVersusUI.text = text;
    }

    private void OnLifeChanged(int previousValue, int newValue)
    {
        UpdateLifeUI(newValue, currentMaxLife);
    }

    private void UpdateLifeUI(int current, int max)
    {
        
        current = Mathf.Clamp(current, 0, max);
        
        if (currentLifeTMP != null)
            currentLifeTMP.text = current.ToString();

        if (maxLifeTMP != null)
            maxLifeTMP.text = max.ToString();
        
        if (lifeFillImage != null)
        {
            float amount = (max > 0) ? (float)current / max : 0f;
            lifeFillImage.fillAmount = amount;
        }
    }




    private void SetIconState(PlayerIconState newState, bool force = false)
    {
        if (!force && newState == currentIconState)
            return;

        currentIconState = newState;

        if (iconAnimationCoroutine != null)
            StopCoroutine(iconAnimationCoroutine);

        iconAnimationCoroutine = StartCoroutine(PlayIconAnimationCoroutine(newState));
    }

    private PlayerIcon.IconCategory GetIconCategory(PlayerIconState state)
    {
        if (playerIconData == null)
            return null;

        switch (state)
        {
            case PlayerIconState.Idle:      return playerIconData.idle;
            case PlayerIconState.Attacking: return playerIconData.attacking;
            case PlayerIconState.Damage:    return playerIconData.damage;
            case PlayerIconState.Dead:      return playerIconData.dead;
        }

        return null;
    }

    private IEnumerator PlayIconAnimationCoroutine(PlayerIconState state)
    {
        if (iconImage == null || playerIconData == null)
            yield break;

        var category = GetIconCategory(state);
        if (category == null || category.frames == null || category.frames.Length == 0)
            yield break;

        float frameTime = Mathf.Max(0.01f, category.frameTime);
        int index = 0;

        while (currentIconState == state)
        {
            iconImage.sprite = category.frames[index];
            iconImage.enabled = (iconImage.sprite != null);

            index++;
            if (index >= category.frames.Length)
                index = 0;

            yield return new WaitForSeconds(frameTime);
        }
    }
    
    private void UpdateIconFromAnimation(string animationName)
    {
        if (playerIconData == null || iconImage == null)
            return;

        // Si le joueur est mort, on force Dead
        if (isDead.Value)
        {
            SetIconState(PlayerIconState.Dead);
            return;
        }

        PlayerIconState targetState = PlayerIconState.Idle;

        if (animationName == "isDamaged")
        {
            targetState = PlayerIconState.Damage;
        }
        else if (animationName == "is2DAttack" ||
                 animationName == "is3DAttack" ||
                 animationName == "isAirAttack" ||
                 animationName == "isStayAttack" ||
                 animationName == "isIdleAttack" ||
                 animationName == "isIdleAttackStop" ||
                 animationName == "isGroundPound" ||
                 animationName == "isCAC" ||
                 animationName == "isDashing" ||
                 animationName == "isPuching")
        {
            targetState = PlayerIconState.Attacking;
        }
        else
        {
            targetState = PlayerIconState.Idle;
        }

        SetIconState(targetState);
    }



    #endregion
    
    #region MISC

    public void AdaptRotationToTerrain2D(Collision collision)
    {
        if (isDead.Value || IsRagdoll) return;
        
        ContactPoint bestContact = collision.GetContact(0);
        for (int i = 1; i < collision.contactCount; i++)
        {
            var c = collision.GetContact(i);
            if (c.normal.y > bestContact.normal.y)
                bestContact = c;
        }

        Vector2 n2D = new Vector2(bestContact.normal.x, bestContact.normal.y).normalized;
        if (n2D.sqrMagnitude < 0.0001f)
            return;

        float normalAngle = Mathf.Atan2(n2D.y, n2D.x) * Mathf.Rad2Deg;
        float surfaceAngle = normalAngle - 90f;

        if (surfaceAngle >= -45f && surfaceAngle <= 45f && !GameManager.instance.is3d)
        {
            Vector3 euler = transform.eulerAngles;
            euler.z = surfaceAngle;
            transform.rotation = Quaternion.Euler(euler);
        }

        bool noMoveInput = moveInput.sqrMagnitude < 0.001f;

        bool withinFrictionAngle =
            surfaceAngle >= -maxAngleWithFriction &&
            surfaceAngle <= maxAngleWithFriction;

        bool onGround = lastOnGroundTime > 0f;

        if (withinFrictionAngle && noMoveInput && onGround)
        {
            if (colliderBottomBody != null && frictionMaterial != null)
                colliderBottomBody.sharedMaterial = frictionMaterial;
        }
        else
        {
            if (colliderBottomBody != null && noFrictionMaterial != null)
                colliderBottomBody.sharedMaterial = noFrictionMaterial;
        }
    }

    public IEnumerator FlipDimensionCoolDownCoroutine(float time)
    {
        rb.isKinematic = true;
        cannotMove = true;
        yield return new WaitForSeconds(time);
        rb.isKinematic = false;
        cannotMove = false;
    }
    
    #endregion
    
    #region ANIMATION

    public void DisableAllAnimations()
    {
        playerAnimator.SetBool("isWalking", false);
        playerAnimator.SetBool("isRunning", false);
        playerAnimator.SetBool("isJumping", false);
        playerAnimator.SetBool("isFalling", false);
        playerAnimator.SetBool("isSliding", false);
        playerAnimator.SetBool("isDamaged", false);
        playerAnimator.SetBool("isDashing", false);
        playerAnimator.SetBool("isCAC", false);
        playerAnimator.SetBool("isPuching", false);
        playerAnimator.SetBool("isSlidingDown", false);
        playerAnimator.SetBool("isAirAttack", false);
        playerAnimator.SetBool("is2DAttack", false);
        playerAnimator.SetBool("is3DAttack", false);
        playerAnimator.SetBool("isIdleAttack", false);
        playerAnimator.SetBool("isIdleAttackStop", false);
        playerAnimator.SetBool("isStayAttack", false);
        playerAnimator.SetBool("isGroundPound", false);
        playerAnimator.SetBool("isLanded", false);
        playerAnimator.SetBool("isGliding", false);
        playerAnimator.SetBool("isCrushed", false);
        playerAnimator.SetBool("isFlip", false);
    }

    public void SwitchAnimation(string animationName)
    {
        if (isStunned && animationName != "isDamaged")
        {
            animationName = "isDamaged";
        }

        DisableAllAnimations();

        if (!string.IsNullOrEmpty(animationName))
            playerAnimator.SetBool(animationName, true);

        if (IsServer)
        {
            netAnimationState.Value = animationName;
        }
        else
        {
            UpdateAnimationServerRpc(animationName);
        }
        
        UpdateIconFromAnimation(animationName);
    }

    
    [ServerRpc(RequireOwnership = false)]
    private void UpdateAnimationServerRpc(string animationName)
    {
        netAnimationState.Value = animationName;
    }

    private void OnAnimationChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        SwitchAnimationRemote(newValue.ToString());
    }

    private void SwitchAnimationRemote(string animationName)
    {
        if (isStunned && animationName != "isDamaged")
        {
            animationName = "isDamaged";
        }
        
        if (isCrushed && animationName != "isCrushed")
        {
            Debug.Log("Player " + playerID + " SwitchAnimationRemote : " + animationName);
            animationName = "isCrushed";
        }

        DisableAllAnimations();

        if (!string.IsNullOrEmpty(animationName))
            playerAnimator.SetBool(animationName, true);
        
        UpdateIconFromAnimation(animationName);
    }
    
    private void SwitchAnimationByNetwork(string animationName = "isDamaged")
    {
        Debug.Log("Player " + playerID + " SwitchAnimationByNetwork : " + animationName);
        
        if (IsServer)
        {
            AnimationClientRpc(animationName);
        }
        else
        {
            RequestAnimationServerRpc(animationName);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAnimationServerRpc(string animationName)
    {
        Debug.Log("Player " + playerID + " RequestAnimationServerRpc : " + animationName);
        
        AnimationClientRpc(animationName);
    }

    [ClientRpc]
    private void AnimationClientRpc(string animationName)
    {
        Debug.Log("Player " + playerID + " AnimationClientRpc : " + animationName);
        PlayAnimationLocal(animationName);
    }

    private void PlayAnimationLocal(string animationName)
    {
        Debug.Log("Player " + playerID + " PlayAnimationLocal : " + animationName);
        
        DisableAllAnimations();
        if (playerAnimator != null)
            playerAnimator.SetBool(animationName, true);
        UpdateIconFromAnimation(animationName);
    }

    



    #endregion
    
    #region GROUND CALLBACKS

    public void TouchGround()
    {
        if (isGliding)
        {
            isGliding = false;
            glideRequested = false;
        }
        
        if (isStayAirAttacking && data != null)
        {
            float previousHeight;

            if (stayAirCurrentHeight <= 0f)
            {
                float rawHeight = Mathf.Max(0f, lastJumpMaxY - lastGroundY);
                if (rawHeight <= 0.01f)
                    rawHeight = data.minHeightBounce;

                rawHeight = Mathf.Clamp(rawHeight, data.minHeightBounce, data.maxHeightBounce);
                previousHeight = rawHeight;
            }
            else
            {
                previousHeight = stayAirCurrentHeight;
            }

            float nextHeight = previousHeight / data.nextBounceDivision;
            
            nextHeight = Mathf.Clamp(nextHeight, data.minHeightBounce, data.maxHeightBounce);

            if (Time.time - lastJumpButtonTime <= data.bonusBounceMarge)
            {
                nextHeight *= data.bonusBounceMult;
                nextHeight = Mathf.Clamp(nextHeight, data.minHeightBounce, data.maxHeightBounce);
            }
            
            stayAirCurrentHeight = nextHeight;
            
            float effectiveGravity = Mathf.Abs(data.gravityStrength);
            if (effectiveGravity < 0.0001f)
            {

                effectiveGravity = Mathf.Abs(Physics.gravity.y);
            }

            float bounceVelocity = Mathf.Sqrt(2f * effectiveGravity * nextHeight);

            Vector3 vel = rb.linearVelocity;
            vel.y = bounceVelocity;
            rb.linearVelocity = vel;
            
            Debug.Log($"[StayAir] prev={previousHeight:F3} next={nextHeight:F3} g={effectiveGravity:F3} v={bounceVelocity:F3}");
        }
        
        trackJumpHeight = false;

        isSliding = false;
        isJumping = false;
        isWallJumping = false;
        isJumpCut = false;
        isJumpFalling = false;
        isFalling = false;

        TriggerLandBounceFX();
    }


    private void LeaveGround()
    {
        isIdleAttcking = false;
        isIdleAttackStopping = false; 
        idleAttackReleaseQueued = false;
        isMovingAttcking = false;
        attackStateEndTime = 0f;

        lastGroundY = transform.position.y;
        lastJumpMaxY = transform.position.y;
        trackJumpHeight = true;

        glideRequested = false;

        TriggerStretchFX(new Vector3(0.9f, 1.1f, 1f), 0.2f);
    }


    #endregion

    #region GIZMOS
    private void OnDrawGizmos()
    {
  
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }

        if (frontWallCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(frontWallCheckPoint.position, wallCheckSize);
        }

        if (backWallCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(backWallCheckPoint.position, wallCheckSize);
        }
        if (airAttackGroundCheckPoint != null)
        {
            Gizmos.color = Color.blue;

            float halfX = airAttackGroundRadius;
            float halfY = airAttackGroundHeight * 0.5f;

            float halfZ;

            bool is2DMode = Application.isPlaying &&
                            GameManager.instance != null &&
                            !GameManager.instance.is3d &&
                            currentCollider2DSizeZ > 0f;

            if (is2DMode)
            {

                halfZ = currentCollider2DSizeZ * 0.5f;
            }
            else
            {
                
                halfZ = airAttackGroundRadius;
            }

            Vector3 size = new Vector3(halfX * 2f, halfY * 2f, halfZ * 2f);
            Gizmos.DrawWireCube(airAttackGroundCheckPoint.position, size);
        }



        if (!debugCollider2DRays)
            return;

        Vector3 origin;
        if (colliderBottomBody != null)
            origin = colliderBottomBody.bounds.center;
        else
            origin = transform.position;

        Gizmos.color = debugRayOriginColor;
        Gizmos.DrawSphere(origin, 0.05f);

        float maxRayLen = raycastLimit <= 0f ? 100f : raycastLimit;

        
        if (Application.isPlaying)
        {
            RaycastHit hit;


            Gizmos.color = debugRayPosColor;
            Vector3 dirPos = Vector3.forward;
            float lenPos = maxRayLen;
            if (Physics.Raycast(origin, dirPos, out hit, maxRayLen, groundLayer))
            {
                lenPos = hit.distance;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.color = debugRayHitColor;
                Gizmos.DrawSphere(hit.point, 0.07f);
            }
            else
            {
                Gizmos.DrawLine(origin, origin + dirPos * lenPos);
            }
            
            Gizmos.color = debugRayNegColor;
            Vector3 dirNeg = Vector3.back;
            float lenNeg = maxRayLen;
            if (Physics.Raycast(origin, dirNeg, out hit, maxRayLen, groundLayer))
            {
                lenNeg = hit.distance;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.color = debugRayHitColor;
                Gizmos.DrawSphere(hit.point, 0.07f);
            }
            else
            {
                Gizmos.DrawLine(origin, origin + dirNeg * lenNeg);
            }
        }
        else
        {
            
            Gizmos.color = debugRayPosColor;
            Gizmos.DrawLine(origin, origin + Vector3.forward * maxRayLen);

            Gizmos.color = debugRayNegColor;
            Gizmos.DrawLine(origin, origin + Vector3.back * maxRayLen);
        }
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0f), 0f, Mathf.Sin(0f)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint =
                center +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    #endregion

    #region DOTWEEN

    private Transform GetModelRoot()
    {

        if (transform.childCount == 0)
            return transform;
        
        Transform firstChild = transform.GetChild(0);
        
        if (colliderObject != null && firstChild == colliderObject.transform)
        {
            if (transform.childCount > 1)
                return transform.GetChild(1);
            return null;
        }
        
        if (colliderObject != null)
        {
            for (int i = 0; i < firstChild.childCount; i++)
            {
                Transform c = firstChild.GetChild(i);
                if (c != colliderObject.transform)
                    return c;
            }
        }

       
        if (firstChild.childCount > 0)
            return firstChild.GetChild(0);

        return firstChild;
    }
    

    public Tween TweenBounce(float squishDuration = 0.1f, float recoverDuration = 0.4f)
    {
        if (visualRoot == null) return null;

        EnsureVisualRootDefaults();
        
        visualRoot.DOKill(true);

        Sequence seq = DOTween.Sequence();
        
        Vector3 baseScale = visualRootDefaultLocalScale;
        Vector3 basePos   = visualRootDefaultLocalPos;

        visualRoot.localScale    = baseScale;
        visualRoot.localPosition = basePos;

        Vector3 squishScale = new Vector3(
            baseScale.x * 1.7f,
            baseScale.y * 0.5f,
            baseScale.z
        );

        float offsetY = (baseScale.y - squishScale.y) * 0.5f;
        Vector3 squishPos = basePos - new Vector3(0f, offsetY, 0f);

        seq.Append(
            visualRoot.DOScale(squishScale, squishDuration).SetEase(Ease.OutQuad)
        );
        seq.Join(
            visualRoot.DOLocalMove(squishPos, squishDuration).SetEase(Ease.OutQuad)
        );

        seq.Append(
            visualRoot.DOScale(baseScale, recoverDuration).SetEase(Ease.OutElastic, 2f)
        );
        seq.Join(
            visualRoot.DOLocalMove(basePos, recoverDuration).SetEase(Ease.OutElastic, 2f)
        );

        seq.OnComplete(() =>
        {
            visualRoot.localScale    = baseScale;
            visualRoot.localPosition = basePos;
        });

        return seq.Play();
    }


    
    private void TriggerLandBounceFX()
    {

        TweenBounce();
        
        if (IsOwner)
        {
            TriggerLandBounceFXServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void TriggerLandBounceFXServerRpc()
    {
        TriggerLandBounceFXClientRpc();
    }

    [ClientRpc]
    private void TriggerLandBounceFXClientRpc()
    {
        if (IsOwner) return;

        TweenBounce();
    }



    public Tween TweenStretch(Vector3 stretchFactors, float duration = 0.2f, Ease ease = Ease.Linear)
    {
        if (visualRoot == null) return null;

        EnsureVisualRootDefaults();

        visualRoot.DOKill(true);

        Sequence seq = DOTween.Sequence();

        Vector3 baseScale = visualRootDefaultLocalScale;
        Vector3 basePos   = visualRootDefaultLocalPos;

        visualRoot.localScale    = baseScale;
        visualRoot.localPosition = basePos;

        Vector3 targetScale = new Vector3(
            baseScale.x * stretchFactors.x,
            baseScale.y * stretchFactors.y,
            baseScale.z * stretchFactors.z
        );

        seq.Append(
            visualRoot.DOScale(targetScale, duration * 0.5f).SetEase(ease)
        );

        seq.Append(
            visualRoot.DOScale(baseScale, duration * 0.5f).SetEase(ease)
        );

        seq.OnComplete(() =>
        {
            visualRoot.localScale    = baseScale;
            visualRoot.localPosition = basePos;
        });

        return seq.Play();
    }



    public void TriggerStretchFX(Vector3 stretchFactors, float duration = 0.2f, Ease ease = Ease.Linear)
    {
        TweenStretch(stretchFactors, duration, ease);
        
        if (IsOwner)
        {
            TriggerStretchFXServerRpc(stretchFactors, duration, ease);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void TriggerStretchFXServerRpc(Vector3 stretchFactors, float duration, Ease ease)
    {
        TriggerStretchFXClientRpc(stretchFactors, duration, ease);
    }

    [ClientRpc]
    public void TriggerStretchFXClientRpc(Vector3 stretchFactors, float duration, Ease ease)
    {
        if (IsOwner) return;

        TweenStretch(stretchFactors, duration, ease);
    }

    private void EnsureVisualRootDefaults()
    {
        if (visualRoot == null) return;
        if (visualRootDefaultsInitialized) return;

        visualRootDefaultLocalPos   = visualRoot.localPosition;
        visualRootDefaultLocalScale = visualRoot.localScale;
        visualRootDefaultsInitialized = true;
    }




    #endregion
}
