using UnityEngine;
using UnityEngine.InputSystem;

namespace ChefGame.Items
{
    /// <summary>
    /// Picks up the nearest nearby object tagged Item and attaches it to this
    /// player's item holder while Interact is held, then drops it on release.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ChefGame.Input.LocalCoopPlayerController))]
    public sealed class LocalCoopItemInteractor : MonoBehaviour
    {
        [SerializeField] private Transform itemHolder;
        [SerializeField] private Transform interactionCenter;
        [SerializeField, Min(0f)] private float interactionRadius = 1f;
        [SerializeField] private LayerMask itemLayers = ~0;
        [SerializeField] private bool isHoldingItem;

        private ChefGame.Input.LocalCoopPlayerController playerController;
        private InputAction interactAction;
        private Transform heldItem;
        private Rigidbody heldRigidbody;
        private bool heldRigidbodyWasKinematic;
        private bool heldRigidbodyUsedGravity;
        private Collider[] playerColliders;
        private Collider[] heldColliders;
        private bool missingInteractActionLogged;

        public bool IsHoldingItem => isHoldingItem;
        public Transform HeldItem => heldItem;

        private void Awake()
        {
            playerController = GetComponent<ChefGame.Input.LocalCoopPlayerController>();
            playerColliders = GetComponentsInChildren<Collider>(true);
            if (interactionCenter == null)
                interactionCenter = transform;
        }

        private void OnEnable()
        {
            if (playerController == null)
                playerController = GetComponent<ChefGame.Input.LocalCoopPlayerController>();

            // OnEnable can run before the controller's Awake has created its
            // private action asset. Try now, then retry in Start after all
            // Awake calls have completed.
            TrySubscribeToInteractAction();
        }

        private void Start()
        {
            if (!TrySubscribeToInteractAction() && !missingInteractActionLogged)
            {
                Debug.LogError($"{nameof(LocalCoopItemInteractor)} on '{name}' could not find the Interact action. Check that the player controller has a LocalChef asset assigned and that it contains a Gameplay/Interact action.", this);
                missingInteractActionLogged = true;
            }
        }

        private bool TrySubscribeToInteractAction()
        {
            if (interactAction != null)
                return true;

            if (playerController == null)
                playerController = GetComponent<ChefGame.Input.LocalCoopPlayerController>();

            interactAction = playerController != null ? playerController.InteractAction : null;
            if (interactAction == null)
                return false;

            interactAction.performed += HandleInteractPressed;
            interactAction.canceled += HandleInteractReleased;
            return true;
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.performed -= HandleInteractPressed;
                interactAction.canceled -= HandleInteractReleased;
            }

            if (isHoldingItem)
                DropItem();
        }

        private void HandleInteractPressed(InputAction.CallbackContext context)
        {
            if (!isHoldingItem)
                TryPickUpItem();
        }

        private void HandleInteractReleased(InputAction.CallbackContext context)
        {
            if (isHoldingItem)
                DropItem();
        }

        private void TryPickUpItem()
        {
            if (itemHolder == null)
            {
                Debug.LogWarning($"{nameof(LocalCoopItemInteractor)} on '{name}' has no Item Holder assigned.", this);
                return;
            }

            Vector3 center = interactionCenter != null ? interactionCenter.position : transform.position;
            Collider[] nearbyColliders = Physics.OverlapSphere(
                center,
                interactionRadius,
                itemLayers,
                QueryTriggerInteraction.Collide);

            Transform nearestItem = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            foreach (Collider nearbyCollider in nearbyColliders)
            {
                if (nearbyCollider == null)
                    continue;

                Transform taggedItem = FindTaggedItemRoot(nearbyCollider.transform);
                if (taggedItem == null)
                    continue;

                float distanceSquared = (taggedItem.position - center).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestItem = taggedItem;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            if (nearestItem != null)
                PickUpItem(nearestItem);
        }

        private static Transform FindTaggedItemRoot(Transform candidate)
        {
            for (Transform current = candidate; current != null; current = current.parent)
            {
                if (current.CompareTag("Item"))
                    return current;
            }

            return null;
        }

        private void PickUpItem(Transform item)
        {
            heldItem = item;
            heldItem.SetParent(itemHolder, true);
            heldItem.position = itemHolder.position;

            // Keep held colliders enabled for contacts with the ground and
            // world, while ignoring only this player's own hand colliders.
            heldColliders = heldItem.GetComponentsInChildren<Collider>(true);
            foreach (Collider itemCollider in heldColliders)
            {
                if (itemCollider == null)
                    continue;

                foreach (Collider playerCollider in playerColliders)
                {
                    if (playerCollider != null)
                        Physics.IgnoreCollision(itemCollider, playerCollider, true);
                }
            }

            heldRigidbody = heldItem.GetComponent<Rigidbody>();
            if (heldRigidbody != null)
            {
                heldRigidbodyWasKinematic = heldRigidbody.isKinematic;
                heldRigidbodyUsedGravity = heldRigidbody.useGravity;
                heldRigidbody.isKinematic = true;
                heldRigidbody.useGravity = false;
            }

            isHoldingItem = true;
        }

        private void DropItem()
        {
            if (heldItem != null)
                heldItem.SetParent(null, true);

            if (heldColliders != null)
            {
                foreach (Collider itemCollider in heldColliders)
                {
                    if (itemCollider == null)
                        continue;

                    foreach (Collider playerCollider in playerColliders)
                    {
                        if (playerCollider != null)
                            Physics.IgnoreCollision(itemCollider, playerCollider, false);
                    }
                }
            }

            if (heldRigidbody != null)
            {
                heldRigidbody.useGravity = heldRigidbodyUsedGravity;
                heldRigidbody.isKinematic = heldRigidbodyWasKinematic;
            }

            heldItem = null;
            heldRigidbody = null;
            heldColliders = null;
            isHoldingItem = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isHoldingItem ? Color.green : Color.yellow;
            Vector3 center = interactionCenter != null ? interactionCenter.position : transform.position;
            Gizmos.DrawWireSphere(center, interactionRadius);
        }
    }
}
