using Asmos.Bus;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player player;
    public Transform playerTarget;
    [SerializeField] LayerMask interactionMask;
    [SerializeField] float interactionDistance = 3f;
    [SerializeField] float recoveryTime = 1f;
    [SerializeField] int healthMax = 10;
    [SerializeField] ParticleSystem bloodParticles;
    [SerializeField] PlayerInput playerInput;
    int health = 1;
    Timer recoveryTimer;
    Interactable interactableInRange;
    [HideInInspector] public Weapon weapon;
    InputAction fireAction, reloadAction, interactAction;
    [HideInInspector] public Animator animator;
    [SerializeField] GameObject character;
    Vector3 spawnPoint;
    Quaternion spawnOrientation;
    CharacterController controller;
    float initialHeight;

    void Start()
    {
        player = this;
        animator = GetComponentInChildren<Animator>();
        controller = GetComponentInChildren<CharacterController>();
        recoveryTimer = new(recoveryTime, EndRecovery);
        health = healthMax;
        Timer.OneShotTimer(.1f, () => Bus.PushData("health", health));
        Timer.OneShotTimer(.1f, () => Bus.PushData("healthMax", healthMax));
        BindControls();
        spawnPoint = character.transform.position;
        spawnOrientation = character.transform.rotation;
        initialHeight = controller.height;
    }

    void Update()
    {

        FindInteractions();

        if (fireAction.IsPressed() && weapon && weapon.CanFire())
            Fire();
        if (reloadAction.IsPressed() && weapon && weapon.CanReload())
            Reload();
        if (interactAction.IsPressed())
            TryInteract();
    }

    void BindControls()
    {
        fireAction = InputSystem.actions.FindAction("Fire");
        interactAction = InputSystem.actions.FindAction("Interact");
        reloadAction = InputSystem.actions.FindAction("Reload");
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

    void Reload()
    {
        weapon.Reload();
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
        GetComponentInChildren<PlayerInput>().DeactivateInput();
        animator.SetTrigger("Death");
        GameManager.instance.EndGame();
        controller.height = 0;
    }

    void EndRecovery()
    {
        // Indicate that the player can loose hp again
    }

    public void RestartGame()
    {
        character.transform.position = spawnPoint;
        health = healthMax;
        controller.height = initialHeight;
        Bus.PushData("health", health);
        animator.SetTrigger("Reset death");
        GetComponentInChildren<PlayerInput>().ActivateInput();
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
    public float DistanceWithPlayer(Vector3 otherPos)
    {
        Vector3 pos = new(playerTarget.position.x, 0, playerTarget.position.z);
        otherPos = new(otherPos.x, 0, otherPos.z);
        return (otherPos - pos).magnitude;
    }

    public bool IsDead() => health <= 0;
}
