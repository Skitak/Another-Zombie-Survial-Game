using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [FoldoutGroup("Interactions")][SerializeField] float interactionDistance = 3f;
    [FoldoutGroup("Interactions")][SerializeField] LayerMask interactionMask;
    PlayerInput playerInput;
    InputAction fireAction, reloadAction, interactAction, sprintAction, aimAction, moveAction, swapSide, grenadeAction, flashAction;
    Player player;
    Interactable interactableInRange;
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        player = GetComponent<Player>();

        fireAction = InputSystem.actions.FindAction("Fire");
        interactAction = InputSystem.actions.FindAction("Interact");
        reloadAction = InputSystem.actions.FindAction("Reload");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        moveAction = InputSystem.actions.FindAction("Move");
        aimAction = InputSystem.actions.FindAction("Aim");
        swapSide = InputSystem.actions.FindAction("SwapSide");
        grenadeAction = InputSystem.actions.FindAction("Grenade");
        flashAction = InputSystem.actions.FindAction("Flash");
    }
    void Update()
    {
        if (interactAction.WasReleasedThisFrame())
            player.CancelInteracting();

        if (flashAction.WasPressedThisFrame())
            player.ToggleFlashLight();

        if (player.IsInteracting())
            return;

        if (sprintAction.WasReleasedThisFrame())
            player.CancelSprinting();
        if (aimAction.WasReleasedThisFrame())
            player.CancelAiming();

        FindInteractions();

        if (!playerInput.inputIsActive)
            return;

        if (aimAction.WasPressedThisFrame())
            player.Aim();

        if (swapSide.WasPressedThisFrame())
            player.SwapSide();

        Vector2 move = moveAction.ReadValue<Vector2>();
        if (sprintAction.WasPressedThisFrame() && !aimAction.WasPressedThisFrame() && move.y > 0)
            player.Sprint();

        // TODO : Check if that needs to be moved to the player
        if (move.y <= 0.1f)
            player.CancelSprinting();

        if (fireAction.IsPressed())
            player.Fire(fireAction.WasPerformedThisFrame(), fireAction.WasReleasedThisFrame());

        if (grenadeAction.WasReleasedThisFrame())
            player.ThrowGrenade();

        if (player.IsDrinking() || player.isThrowingGrenade)
            return;

        if (grenadeAction.WasPressedThisFrame())
            player.ArmGrenade();

        if (reloadAction.WasPressedThisFrame())
            player.Reload();
        if (interactAction.WasPressedThisFrame())
            TryInteract();
    }
    void TryInteract()
    {
        if (!interactableInRange || !interactableInRange.canInteract) return;
        if (!interactableInRange.isInteractionTimed)
            interactableInRange.Interact();
        else
            player.Interact(interactableInRange);
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
}
