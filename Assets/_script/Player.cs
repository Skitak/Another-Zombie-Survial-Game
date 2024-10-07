using System;
using Asmos.Bus;
using Cinemachine;
using Sirenix.OdinInspector;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player player;
    #region exposedParameters
    [FoldoutGroup("GameObjects and Prefabs")][SerializeField] ParticleSystem bloodParticles;
    [FoldoutGroup("GameObjects and Prefabs")][SerializeField] AnimationClip aimAnimation;
    [FoldoutGroup("GameObjects and Prefabs")] public Weapon weapon;
    [FoldoutGroup("Interactions")][SerializeField] LayerMask interactionMask;
    [FoldoutGroup("Interactions")][SerializeField] float interactionDistance = 3f;
    [FoldoutGroup("Health and Speed", Expanded = true)][SerializeField] float recoveryTime = 1f;
    [FoldoutGroup("Health and Speed")][SerializeField] int baseHealthMax = 10;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseSpeed;
    [FoldoutGroup("Health and Speed")][SerializeField] float sprintMultiplier;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseStamina;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseAimCameraZoom;
    [FoldoutGroup("Others")][SerializeField] int basePerkRefresh;
    [FoldoutGroup("Others")] public bool fireWhileRunning;
    #endregion
    #region hiddenParameters
    Interactable interactableInRange;
    ThirdPersonController tpsController;
    PlayerInput playerInput;
    InputAction fireAction, reloadAction, interactAction, sprintAction, aimAction, moveAction, swapSide, tab;
    [HideInInspector] public Animator animator;
    Vector3 spawnPoint;
    CharacterController controller;
    CinemachineVirtualCamera tpsCamera;
    Cinemachine3rdPersonFollow tpsCameraComponent;
    float initialHeight, initialCameraDistance;
    int _health, _healthMax, _perkRefresh;
    Timer recoveryTimer, staminaTimer, aimTimer, swapSideTimer;
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
    #endregion
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

    }

    void Start()
    {
        healthMax = baseHealthMax;
        health = baseHealthMax;
        staminaMax = baseStamina;
        speed = baseSpeed;
        perkRefresh = basePerkRefresh;
        Bus.PushData("stamina", baseStamina);
        animator.SetLayerWeight(weapon.animLayer, 1);
        weapon.modelInHierarchy.SetActive(true);

    }
    void Update()
    {
        if (sprintAction.WasReleasedThisFrame())
            CancelSprinting();
        if (aimAction.WasReleasedThisFrame())
            aimTimer.Rewind();

        if (!playerInput.inputIsActive)
            return;

        FindInteractions();
        if (aimAction.WasPressedThisFrame())
        {
            aimTimer.Play();
            CancelSprinting();
        }
        Vector2 move = moveAction.ReadValue<Vector2>();
        if (sprintAction.WasPressedThisFrame() && !aimAction.WasPressedThisFrame() && move.y > 0)
        {
            staminaTimer.Play();
            aimTimer.Rewind();
            weapon?.CancelReload();
        }

        if (move.y <= 0)
            CancelSprinting();

        if (reloadAction.WasPressedThisFrame() && weapon && weapon.CanReload())
            weapon.Reload();
        if (interactAction.IsPressed())
            TryInteract();

        if (weapon != null && (!staminaTimer.IsPlayingForward() || fireWhileRunning) && fireAction.IsPressed() && weapon.CanFire())
            Fire();

        if (swapSide.WasPressedThisFrame())
            if (swapSideTimer.IsFinished() || swapSideTimer.IsPlayingForward())
                swapSideTimer.Rewind();
            else
                swapSideTimer.Play();
        if (tab.IsPressed())
            print("tabbing");
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

    public void Hit()
    {
        if (recoveryTimer.IsStarted() || health <= 0)
            return;


        // TODO : Play sounds
        // TODO : Overlay with flashy elements
        // TODO : Camera shake

        --health;
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

    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
            playerInput.ActivateInput();
        else
            playerInput.DeactivateInput();
    }

    void EndRecovery()
    {
        // Indicate that the player can loose hp again
    }

    public void CancelSprinting()
    {
        staminaTimer.Rewind();
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
            interactableInRange = hit.collider.gameObject.GetComponent<Interactable>();
            interactableInRange.Highlight = true;
            return;
        }
        if (interactableInRange)
            interactableInRange.Highlight = false;
        interactableInRange = null;
    }

    #endregion

    #region publicUtils
    public bool IsDead() => health <= 0;
    #endregion
}
