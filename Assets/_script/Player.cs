using System;
using Asmos.Bus;
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
    [FoldoutGroup("Interactions")][SerializeField] LayerMask interactionMask;
    [FoldoutGroup("Interactions")][SerializeField] float interactionDistance = 3f;
    [FoldoutGroup("Interactions")][SerializeField] AnimationClip drinkAnimation;
    [FoldoutGroup("Interactions")][SerializeField] GameObject can;
    [FoldoutGroup("Health and Speed", Expanded = true)][SerializeField] float recoveryTime = 1f;
    [FoldoutGroup("Health and Speed")][SerializeField] int baseHealthMax = 10;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseSpeed;
    [FoldoutGroup("Health and Speed")][SerializeField] float sprintMultiplier;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseStamina;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseAimCameraZoom;
    [FoldoutGroup("Grenades")][SerializeField] AnimationClip armAnimation;
    [FoldoutGroup("Grenades")][SerializeField] AnimationClip throwAnimation;
    [FoldoutGroup("Grenades")][SerializeField] GameObject grenadeMesh;
    [FoldoutGroup("Grenades")][SerializeField] GameObject grenadePrefab;
    [FoldoutGroup("Grenades")][SerializeField] int baseGrenades;
    [FoldoutGroup("Grenades")][SerializeField] int baseGrenadeDamages;
    [FoldoutGroup("Grenades")][SerializeField] float baseExplosionRadius;
    [FoldoutGroup("Grenades")][SerializeField] float baseExplosionTime;
    [FoldoutGroup("Grenades")][SerializeField] float maxThrowingTime;
    [FoldoutGroup("Grenades")][SerializeField] float minThrowingForce;
    [FoldoutGroup("Grenades")][SerializeField] float maxThrowingForce;
    [FoldoutGroup("Others")][SerializeField] int basePerkRefresh;
    [FoldoutGroup("Others")] public bool fireWhileRunning;
    #endregion
    #region hiddenParameters
    Interactable interactableInRange;
    ThirdPersonController tpsController;
    PlayerInput playerInput;
    InputAction fireAction, reloadAction, interactAction, sprintAction, aimAction, moveAction, swapSide, tab, grenadeAction;
    [HideInInspector] public Animator animator;
    Vector3 spawnPoint;
    CharacterController controller;
    CinemachineVirtualCamera tpsCamera;
    Cinemachine3rdPersonFollow tpsCameraComponent;
    float initialHeight, initialCameraDistance, _explosionTime, _explosionRadius;
    int _health, _healthMax, _perkRefresh, _grenades, _grenadeDamages;
    bool isThrowingGrenade;
    Timer recoveryTimer, staminaTimer, aimTimer, swapSideTimer, interactionTimer, drinkTimer, armGrenadeTimer, throwingGrenadeTimer, throwGrenadeDelayTimer;
    #endregion
    #region setters
    public int health
    {
        get => _health;
        set
        {
            _health = Math.Clamp(value, 0, healthMax);
            Bus.PushData("health", _health);
        }
    }
    public int healthMax
    {
        get => _healthMax;
        set
        {
            _healthMax = value;
            health = Math.Min(_healthMax, health);
            Bus.PushData("healthMax", _healthMax);
        }
    }
    public int perkRefresh
    {
        get => _perkRefresh;
        set
        {
            _perkRefresh = value;
            Bus.PushData("perkRefresh", value);
        }
    }
    public float speed
    {
        get => tpsController.MoveSpeed;
        set
        {
            tpsController.MoveSpeed = value;
            tpsController.SprintSpeed = value * sprintMultiplier;
        }
    }
    public float staminaMax
    {
        get => staminaTimer.endTime;
        set
        {
            staminaTimer.endTime = value;
            Bus.PushData("staminaMax", value);
            staminaTimer.Rewind();
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
    public int grenadeDamages
    {
        get => _grenadeDamages;
        set
        {
            _grenadeDamages = value;
            Bus.PushData("grenade damages", _grenadeDamages);
        }
    }
    public float explosionRadius
    {
        get => _explosionRadius;
        set
        {
            _explosionRadius = value;
            Bus.PushData("explosion radius", _explosionRadius);
        }
    }
    public float explosionTime
    {
        get => _explosionTime;
        set
        {
            _explosionTime = value;
            Bus.PushData("explosion time", _explosionTime);
        }
    }

    #endregion
    # region update
    void Update()
    {
        if (tab.IsPressed())
            print("tabbing");

        if (interactionTimer.IsStarted())
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

        if (grenadeAction.WasPressedThisFrame())
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
        Bus.PushData("health", health);

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

        aimTimer = new(.15f);
        interactionTimer = new(.15f);
        armGrenadeTimer = new(maxThrowingTime);
        armGrenadeTimer.OnTimerUpdate += () => Bus.PushData("arm", armGrenadeTimer.GetPercentage());
        throwingGrenadeTimer = new(throwAnimation.length, () => isThrowingGrenade = false);
        throwGrenadeDelayTimer = new(2f, ThrowGrenade);
        drinkTimer = new(drinkAnimation.length, () => can.SetActive(false));
        recoveryTimer = new(recoveryTime, EndRecovery);
        aimTimer.OnTimerUpdate += () => tpsCameraComponent.CameraDistance = Mathf.Lerp(initialCameraDistance, baseAimCameraZoom, aimTimer.GetPercentage());
        staminaTimer = new(baseStamina);
        staminaTimer.OnTimerUpdate += () => Bus.PushData("stamina", staminaTimer.GetTimeLeft());
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
        tab = InputSystem.actions.FindAction("Tab");
        grenadeAction = InputSystem.actions.FindAction("Grenade");
    }
    void Start()
    {
        healthMax = baseHealthMax;
        health = baseHealthMax;
        staminaMax = baseStamina;
        speed = baseSpeed;
        perkRefresh = basePerkRefresh;
        grenades = baseGrenades;
        grenadeDamages = baseGrenadeDamages;
        explosionRadius = baseExplosionRadius;
        explosionTime = baseExplosionTime;
        Bus.PushData("stamina", baseStamina);
        animator.SetLayerWeight(weapon.animLayer, 1);
        weapon.modelInHierarchy.SetActive(true);

    }
    public void RestartGame()
    {
        transform.position = spawnPoint;
        health = baseHealthMax;
        controller.height = initialHeight;
        Bus.PushData("health", health);
        animator.SetTrigger("Reset death");
        SetInputEnabled(true);
    }
    #endregion
    #region interactions
    void TryInteract()
    {
        if (interactableInRange)
            interactableInRange.Interact();
    }
    void FindInteractions()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out var hit, interactionDistance, interactionMask))
        {
            interactableInRange = hit.collider.gameObject.GetComponentInParent<Interactable>();
            interactableInRange.Highlight = true;
            return;
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
        grenade.InitializeExplosionTimer(explosionTime);

        armGrenadeTimer.Reset();
        throwGrenadeDelayTimer.Reset();
    }


    #endregion
    #region utils
    public bool IsDead() => health <= 0;
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
    }

    #endregion
    public void TakeExplosionDamages(float distancePercent)
    {
        Hit(Math.Min(1, (int)(grenadeDamages * distancePercent)));
    }
}
