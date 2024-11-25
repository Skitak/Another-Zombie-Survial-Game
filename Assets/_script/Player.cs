using System;
using Asmos.Bus;
using Asmos.Timers;
using Cinemachine;
using Sirenix.OdinInspector;
using StarterAssets;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, ITakeExplosionDamages
{
    public static Player player;
    #region exposedParameters
    [FoldoutGroup("GameObjects and Prefabs")][SerializeField] ParticleSystem bloodParticles;
    [FoldoutGroup("GameObjects and Prefabs")][SerializeField] AnimationClip aimAnimation;
    [FoldoutGroup("GameObjects and Prefabs")] public Weapon weapon;
    [FoldoutGroup("GameObjects and Prefabs")] public StatKit statKit;
    [FoldoutGroup("Interactions")][SerializeField] LayerMask interactionMask;
    [FoldoutGroup("Interactions")][SerializeField] float interactionDistance = 3f;
    [FoldoutGroup("Interactions")][SerializeField] AnimationClip drinkAnimation;
    [FoldoutGroup("Interactions")][SerializeField] GameObject can;
    [FoldoutGroup("Health and Speed", Expanded = true)][SerializeField] float recoveryTime = 1f;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseAimCameraZoom;
    [FoldoutGroup("Health and Speed")] public float sprintSpeed;
    [FoldoutGroup("Grenades")][SerializeField] AnimationClip armAnimation;
    [FoldoutGroup("Grenades")][SerializeField] AnimationClip throwAnimation;
    [FoldoutGroup("Grenades")][SerializeField] GameObject grenadeMesh;
    [FoldoutGroup("Grenades")][SerializeField] GameObject grenadePrefab;
    [FoldoutGroup("Grenades")][SerializeField] int baseGrenades;
    [FoldoutGroup("Grenades")][SerializeField] float maxThrowingTime;
    [FoldoutGroup("Grenades")][SerializeField] float minThrowingForce;
    [FoldoutGroup("Grenades")][SerializeField] float maxThrowingForce;
    [FoldoutGroup("Others")][SerializeField] int basePerkRefresh;
    [FoldoutGroup("Others")] public bool fireWhileRunning;
    #endregion
    #region hiddenParameters
    Interactable interactableInRange;
    [HideInInspector] public ThirdPersonController tpsController;
    PlayerInput playerInput;
    InputAction fireAction, reloadAction, interactAction, sprintAction, aimAction, moveAction, swapSide, tab, grenadeAction, pauseAction;
    [HideInInspector] public Animator animator;
    Vector3 spawnPoint;
    CharacterController controller;
    CinemachineVirtualCamera tpsCamera;
    Cinemachine3rdPersonFollow tpsCameraComponent;
    float initialHeight, initialCameraDistance, _explosionTime, _explosionRadius;
    int _health, _healthMax, _perkRefresh, _grenades, _grenadeDamages;
    bool isThrowingGrenade, isInteracting;
    Timer recoveryTimer, staminaTimer, aimTimer, swapSideTimer, drinkTimer, armGrenadeTimer, throwingGrenadeTimer, throwGrenadeDelayTimer;
    #endregion
    #region setters
    public int health
    {
        get => _health;
        set
        {
            _health = Math.Clamp(value, 0, healthMax);
            Bus.PushData("HEALTH", _health);
        }
    }
    public int healthMax { get => (int)StatManager.Get(StatType.HEALTH_MAX); }
    public int perkRefresh
    {
        get => _perkRefresh;
        set
        {
            _perkRefresh = value;
            Bus.PushData("perkRefresh", value);
        }
    }
    public float aimValue { get => aimTimer.GetPercentage(); }
    public bool isSprinting { get => staminaTimer.IsPlayingForward(); }
    public int grenades
    {
        get => _grenades;
        set
        {
            _grenades = value;
            Bus.PushData("grenades", _grenades);
        }
    }
    public int grenadeDamages { get => (int)StatManager.Get(StatType.EXPLOSION_DAMAGES); }
    public float explosionRadius { get => StatManager.Get(StatType.EXPLOSION_RADIUS); }

    #endregion
    # region update
    void Update()
    {
        if (isInteracting && interactAction.WasReleasedThisFrame())
            CancelInteracting();

        if (isInteracting)
            return;

        if (sprintAction.WasReleasedThisFrame())
            CancelSprinting();
        if (aimAction.WasReleasedThisFrame())
            CancelAiming();

        FindInteractions();

        if (!playerInput.inputIsActive)
            return;

        if (aimAction.WasPressedThisFrame())
        {
            aimTimer.Play();
            CancelSprinting();
        }

        if (swapSide.WasPressedThisFrame())
            if (swapSideTimer.IsFinished() || swapSideTimer.IsPlayingForward())
                swapSideTimer.Rewind();
            else
                swapSideTimer.Play();

        Vector2 move = moveAction.ReadValue<Vector2>();
        if (sprintAction.WasPressedThisFrame() && !aimAction.WasPressedThisFrame() && move.y > 0)
        {
            staminaTimer.Play();
            CancelAiming();
            weapon.CancelReload();
        }

        if (move.y <= 0.1f)
            CancelSprinting();

        if ((!staminaTimer.IsPlayingForward() || fireWhileRunning) && fireAction.IsPressed() && weapon.CanFire())
            Fire();

        if (grenadeAction.WasReleasedThisFrame() && (armGrenadeTimer.IsStarted() || armGrenadeTimer.IsFinished())
        )
        {
            if (armGrenadeTimer.Time < armAnimation.length)
                DelayGrenadeThrow();
            else
                ThrowGrenade();
        }

        if (drinkTimer.IsStarted() || isThrowingGrenade)
            return;

        if (grenadeAction.WasPressedThisFrame() && grenades > 0)
            ArmGrenade();

        if (reloadAction.WasPressedThisFrame() && weapon && weapon.CanReload())
            weapon.Reload();
        if (interactAction.WasPressedThisFrame())
            TryInteract();
    }
    void Fire()
    {
        if (fireAction.WasReleasedThisFrame())
            weapon.TriggerRelease();
        else if (fireAction.WasPerformedThisFrame())
            weapon.TriggerEnter();
        else
            weapon.TriggerPress();
    }
    public void Hit(int damages)
    {
        if (recoveryTimer.IsStarted() || health <= 0)
            return;

        // TODO : Play sounds
        // TODO : Overlay with flashy elements
        // TODO : Camera shake

        health -= damages;

        if (health <= 0)
        {
            Die();
        }

        animator.SetTrigger("Hit");
        bloodParticles.Play();
        recoveryTimer.ResetPlay();
    }
    void Die()
    {
        SetInputEnabled(false);
        animator.SetTrigger("Death");
        GameManager.instance.EndGame();
        controller.height = 0;
    }
    void EndRecovery()
    {
        // Indicate that the player can loose hp again
    }
    #endregion
    #region game cycle
    void Awake()
    {
        player = this;
        tpsCamera = transform.parent.GetComponentInChildren<CinemachineVirtualCamera>();
        tpsController = GetComponent<ThirdPersonController>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        tpsCameraComponent = tpsCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        spawnPoint = transform.parent.transform.position;
        initialHeight = controller.height;
        initialCameraDistance = tpsCameraComponent.CameraDistance;
        tpsController.player = this;

        aimTimer = new(.1f);
        armGrenadeTimer = new(maxThrowingTime);
        armGrenadeTimer.OnTimerUpdate += () => Bus.PushData("arm", armGrenadeTimer.GetPercentage());
        throwingGrenadeTimer = new(throwAnimation.length, () => isThrowingGrenade = false);
        throwGrenadeDelayTimer = new(2f, ThrowGrenade);
        drinkTimer = new(drinkAnimation.length, () => can.SetActive(false));
        recoveryTimer = new(recoveryTime, EndRecovery);
        aimTimer.OnTimerUpdate += () => tpsCameraComponent.CameraDistance = Mathf.Lerp(initialCameraDistance, baseAimCameraZoom, aimTimer.GetPercentage());
        staminaTimer = new(1f);
        staminaTimer.OnTimerUpdate += () => Bus.PushData("stamina", staminaTimer.GetPercentageLeft());
        staminaTimer.useUpdateAsRewindAction = true;
        staminaTimer.rewindAutomatic = true;
        swapSideTimer = new(.2f);
        swapSideTimer.OnTimerUpdate += () =>
        {
            tpsCameraComponent.CameraSide = swapSideTimer.GetPercentage();
            animator.SetFloat("swap side", swapSideTimer.GetPercentage());
        };
        swapSideTimer.useUpdateAsRewindAction = true;

        fireAction = InputSystem.actions.FindAction("Fire");
        interactAction = InputSystem.actions.FindAction("Interact");
        reloadAction = InputSystem.actions.FindAction("Reload");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        moveAction = InputSystem.actions.FindAction("Move");
        aimAction = InputSystem.actions.FindAction("Aim");
        swapSide = InputSystem.actions.FindAction("SwapSide");
        grenadeAction = InputSystem.actions.FindAction("Grenade");

        StatManager.Subscribe(StatType.STAMINA_MAX, (value) =>
        {
            staminaTimer.endTime = value;
            staminaTimer.Rewind();
        });
    }
    void Start()
    {
        StatManager.instance.AddStatkit(statKit);
        health = healthMax;
        staminaTimer.Reset();
        perkRefresh = basePerkRefresh;
        grenades = baseGrenades;
        animator.SetLayerWeight(weapon.animLayer, 1);
        weapon.modelInHierarchy.SetActive(true);
        staminaTimer.endTime = StatManager.Get(StatType.STAMINA_MAX);
    }
    public void RestartGame()
    {
        transform.position = spawnPoint;
        health = (int)StatManager.Get(StatType.HEALTH_MAX);
        controller.height = initialHeight;
        animator.SetTrigger("Reset death");
        SetInputEnabled(true);
    }
    #endregion
    #region interactions
    void TryInteract()
    {
        if (!interactableInRange || !interactableInRange.canInteract) return;
        if (!interactableInRange.isInteractionTimed)
        {
            interactableInRange.Interact();
            return;
        }
        CancelEverything();
        SetMovementEnabled(false);
        interactableInRange.StartInteracting();
        isInteracting = true;
    }
    public void CancelInteracting()
    {
        if (!isInteracting)
            return;
        interactableInRange.CancelInteracting();
        isInteracting = false;
        SetMovementEnabled(true);
    }
    void FindInteractions()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out var hit, interactionDistance, interactionMask))
        {
            if (hit.collider.tag == Interactable.tag)
            {
                interactableInRange = hit.collider.gameObject.GetComponentInParent<Interactable>();
                interactableInRange.Highlight = true;
                return;
            }
        }
        if (interactableInRange)
            interactableInRange.Highlight = false;
        interactableInRange = null;
    }
    public void Drink()
    {
        animator.SetTrigger("drink");
        weapon.CancelReload();
        drinkTimer.ResetPlay();
        can.SetActive(true);
    }
    public void ArmGrenade()
    {
        grenades--;
        isThrowingGrenade = true;
        grenadeMesh.SetActive(true);
        animator.SetTrigger("arm");
        weapon.CancelReload();
        armGrenadeTimer.ResetPlay();
        grenadeMesh.SetActive(true);
    }
    void DelayGrenadeThrow()
    {
        throwGrenadeDelayTimer.endTime = armAnimation.length - armGrenadeTimer.Time;
        throwGrenadeDelayTimer.ResetPlay();
        armGrenadeTimer.Pause();
    }
    public void ThrowGrenade()
    {

        animator.SetTrigger("throw");
        throwingGrenadeTimer.ResetPlay();
        grenadeMesh.SetActive(false);
        GameObject grenadeObj = Instantiate(grenadePrefab, grenadeMesh.transform.position, quaternion.identity);
        Grenade grenade = grenadeObj.GetComponent<Grenade>();
        float throwingForce = Mathf.Lerp(minThrowingForce, maxThrowingForce, armGrenadeTimer.GetPercentage());
        grenade.rigidbody.AddForce(Camera.main.transform.forward * throwingForce, ForceMode.Impulse);

        armGrenadeTimer.Reset();
        throwGrenadeDelayTimer.Reset();
    }
    #endregion
    #region utils
    public bool IsDead() => health <= 0;
    public void SetMovementEnabled(bool enabled)
    {
        tpsController.enabled = enabled;
    }
    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
        {
            playerInput.ActivateInput();
            return;
        }
        playerInput.DeactivateInput();
        CancelEverything();
    }
    void CancelAiming()
    {
        aimTimer.Rewind();
    }
    public void CancelSprinting()
    {
        staminaTimer.Rewind();
    }
    void CancelEverything()
    {
        weapon.CancelReload();
        CancelAiming();
        CancelSprinting();
        CancelInteracting();
    }
    #endregion
    public void TakeExplosionDamages(float distancePercent)
    {
        Hit(Math.Min(1, (int)(grenadeDamages * distancePercent)));
    }
}
