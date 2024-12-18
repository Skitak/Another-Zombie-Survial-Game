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
    [FoldoutGroup("GameObjects and Prefabs")] public GameObject flashLight;
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
    [HideInInspector] public ThirdPersonController tpsController;
    [HideInInspector] public Animator animator;
    Vector3 spawnPoint;
    CharacterController controller;
    CinemachineVirtualCamera tpsCamera;
    Cinemachine3rdPersonFollow tpsCameraComponent;
    float initialHeight, initialCameraDistance;
    int _health, _perkRefresh, _grenades;
    [HideInInspector] public bool isThrowingGrenade;
    Interactable interactable;

    Timer recoveryTimer, staminaTimer, flashlightTimer, aimTimer, swapSideTimer, drinkTimer, armGrenadeTimer, throwingGrenadeTimer, throwGrenadeDelayTimer;
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
        EnableInput(false);
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
        flashlightTimer = new(100f, () => flashLight.SetActive(false));

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
        EnableInput(true);
    }
    #endregion

    #region grenade
    public void ArmGrenade()
    {
        if (grenades <= 0 || IsInteracting() || isThrowingGrenade)
            return;
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
        throwGrenadeDelayTimer.endTime = armAnimation.length - armGrenadeTimer.Time + 0.01f;
        throwGrenadeDelayTimer.ResetPlay();
        armGrenadeTimer.Pause();
    }
    public void ThrowGrenade()
    {
        if (!IsArmingGrenade())
            return;
        if (armGrenadeTimer.Time < armAnimation.length)
        {
            DelayGrenadeThrow();
            return;
        }
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

    public bool IsArmingGrenade() => armGrenadeTimer.IsStarted() || armGrenadeTimer.IsFinished();

    #endregion

    #region actions
    public void Fire(bool firePressed, bool fireReleased)
    {
        if (IsInteracting() || isThrowingGrenade)
            return;

        if (firePressed)
            weapon.TriggerEnter();
        else if (fireReleased)
            weapon.TriggerRelease();
        else
            weapon.TriggerPress();
    }
    public void Drink()
    {
        animator.SetTrigger("drink");
        weapon.CancelReload();
        drinkTimer.ResetPlay();
        can.SetActive(true);
    }
    public void ToggleFlashLight()
    {
        if (flashlightTimer.IsStarted())
        {
            flashlightTimer.Pause();
            flashLight.SetActive(false);
        }
        else if (!flashlightTimer.IsFinished())
        {
            flashlightTimer.Play();
            flashLight.SetActive(true);
        }
    }
    public void Aim()
    {
        aimTimer.Play();
        CancelSprinting();
    }
    public void SwapSide()
    {
        if (swapSideTimer.IsFinished() || swapSideTimer.IsPlayingForward())
            swapSideTimer.Rewind();
        else
            swapSideTimer.Play();
    }
    public void Sprint()
    {
        if (IsInteracting() || isThrowingGrenade)
            return;
        CancelAiming();
        CancelReload();
        staminaTimer.Play();
    }
    public void Reload()
    {
        if (!weapon || !weapon.CanReload() || IsInteracting() || isThrowingGrenade)
            return;

        CancelSprinting();
        weapon.Reload();
        // TODO : Handle animations better
    }
    public void Interact(Interactable interactableInRange)
    {
        CancelEverything();
        EnableMovement(false);
        interactableInRange.StartInteracting();
        interactable = interactableInRange;
    }
    #endregion
    #region utils
    public bool IsDead() => health <= 0;
    public bool IsInteracting() => interactable != null;
    public bool IsDrinking() => drinkTimer.IsStarted();
    public void EnableMovement(bool enabled)
    {
        tpsController.enabled = enabled;
    }
    public void EnableInput(bool enabled)
    {
        // TODO : Move that into playerController
        // if (enabled)
        //     playerInput.ActivateInput();
        // else
        // {
        // playerInput.DeactivateInput();
        // CancelEverything();
        // }
    }
    public void CancelAiming()
    {
        aimTimer.Rewind();
    }
    public void CancelReload()
    {
        if (!weapon.IsReloading())
            return;
        weapon.CancelReload();
        animator.SetTrigger("cancelReload");
    }
    public void CancelInteracting()
    {
        if (!IsInteracting())
            return;
        interactable = null;
        interactable.CancelInteracting();
        EnableMovement(true);
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
