using System;
using Asmos.Bus;
using Sirenix.OdinInspector;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player player;
    #region exposedParameters
    [FoldoutGroup("GameObjects and Prefabs")][SerializeField] ParticleSystem bloodParticles;
    [FoldoutGroup("GameObjects and Prefabs")][SerializeField] PlayerInput playerInput;
    [FoldoutGroup("GameObjects and Prefabs")] public Transform playerTarget;
    [FoldoutGroup("GameObjects and Prefabs")][SerializeField] GameObject character;
    [FoldoutGroup("GameObjects and Prefabs")] public ThirdPersonController tpsController;
    [FoldoutGroup("Interactions")][SerializeField] LayerMask interactionMask;
    [FoldoutGroup("Interactions")][SerializeField] float interactionDistance = 3f;
    [FoldoutGroup("Health and Speed", Expanded = true)][SerializeField] float recoveryTime = 1f;
    [FoldoutGroup("Health and Speed")][SerializeField] int baseHealthMax = 10;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseSpeed;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseSprintSpeed;
    [FoldoutGroup("Health and Speed")][SerializeField] float baseStamina;
    #endregion
    #region hiddenParameters
    Interactable interactableInRange;
    InputAction fireAction, reloadAction, interactAction, sprintAction;
    [HideInInspector] public Weapon weapon;
    [HideInInspector] public Animator animator;
    Vector3 spawnPoint;
    CharacterController controller;
    float initialHeight;
    [HideInInspector] public bool canSprint = true;
    #endregion

    #region setters
    int _health, _healthMax;
    Timer recoveryTimer, staminaTimer;
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
    public float speed
    {
        get => tpsController.MoveSpeed;
        set
        {
            tpsController.MoveSpeed = value;
        }
    }
    public float speedSprint
    {
        get => tpsController.SprintSpeed;
        set
        {
            tpsController.SprintSpeed = value;
        }
    }
    public float staminaMax
    {
        get => staminaTimer.endTime;
        set
        {
            staminaTimer.endTime = value;
            Bus.PushData("staminaMax", value);
        }
    }

    #endregion
    void Start()
    {
        player = this;
        animator = GetComponentInChildren<Animator>();
        controller = GetComponentInChildren<CharacterController>();

        spawnPoint = character.transform.position;
        initialHeight = controller.height;

        recoveryTimer = new(recoveryTime, EndRecovery);
        staminaTimer = new(baseStamina, () => canSprint = false);
        staminaTimer.OnTimerUpdate += () => Bus.PushData("stamina", staminaTimer.GetTimeLeft());
        staminaTimer.useUpdateAsRewindAction = true;
        staminaTimer.rewindAutomatic = true;
        staminaTimer.OnTimerRewindEnd += () => canSprint = true;

        fireAction = InputSystem.actions.FindAction("Fire");
        interactAction = InputSystem.actions.FindAction("Interact");
        reloadAction = InputSystem.actions.FindAction("Reload");
        sprintAction = InputSystem.actions.FindAction("Sprint");

        Timer.OneShotTimer(.05f, () =>
        {
            healthMax = baseHealthMax;
            health = baseHealthMax;
            Bus.PushData("stamina", baseStamina);
            staminaMax = baseStamina;
            speed = baseSpeed;
            speedSprint = baseSprintSpeed;
        });
    }

    void Update()
    {
        if (!playerInput.inputIsActive)
            return;
        FindInteractions();
        if (fireAction.IsPressed() && weapon && weapon.CanFire())
            Fire();
        if (reloadAction.IsPressed() && weapon && weapon.CanReload())
            weapon.Reload();
        if (interactAction.IsPressed())
            TryInteract();

        if (sprintAction.WasPressedThisFrame())
        {
            canSprint = true;
            staminaTimer.Play();
        }
        else if (sprintAction.WasReleasedThisFrame())
        {
            staminaTimer.Rewind();
        }
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

    public void RestartGame()
    {
        character.transform.position = spawnPoint;
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

    public void PickupWeapon(Weapon newWeapon)
    {
        weapon = newWeapon;
        animator.SetLayerWeight(1, 0);
        animator.SetLayerWeight(newWeapon.animLayer, 1);
        GameObject template = GameObject.Find(weapon.nameInHierarchy);
        weapon.gameObject.transform.SetParent(template.transform.parent);
        weapon.gameObject.transform.SetPositionAndRotation(template.transform.position, template.transform.rotation);
        weapon.gameObject.transform.localScale = template.transform.localScale;
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
    public float DistanceWithPlayer(Vector3 otherPos)
    {
        Vector3 pos = new(playerTarget.position.x, 0, playerTarget.position.z);
        otherPos = new(otherPos.x, 0, otherPos.z);
        return (otherPos - pos).magnitude;
    }

    public bool IsDead() => health <= 0;
    #endregion
}
