using UnityEngine;
using UnityEngine.InputSystem;

namespace ChefGame.Items
{
    /// <summary>
    /// Picks up the nearest nearby object tagged Item and attaches it to this
    /// player's item holder. Pressing Interact again drops the held object.
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
        private Collider[] heldColliders;
        private bool[] heldColliderStates;
        private bool missingInteractActionLogged;

        public bool IsHoldingItem => isHoldingItem;
        public Transform HeldItem => heldItem;

        private void Awake()
        {
            playerController = GetComponent<ChefGame.Input.LocalCoopPlayerController>();
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

            interactAction.performed += HandleInteract;
            return true;
        }

        private void OnDisable()
        {
            if (interactAction != null)
                interactAction.performed -= HandleInteract;

            if (isHoldingItem)
                DropItem();
        }

        private void HandleInteract(InputAction.CallbackContext context)
        {
            if (isHoldingItem)
                DropItem();
            else
                TryPickUpItem();
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

            heldRigidbody = heldItem.GetComponent<Rigidbody>();
            if (heldRigidbody != null)
            {
                heldRigidbodyWasKinematic = heldRigidbody.isKinematic;
                heldRigidbodyUsedGravity = heldRigidbody.useGravity;
                heldRigidbody.isKinematic = true;
                heldRigidbody.useGravity = false;
            }

            heldColliders = heldItem.GetComponentsInChildren<Collider>(true);
            heldColliderStates = new bool[heldColliders.Length];
            for (int i = 0; i < heldColliders.Length; i++)
            {
                heldColliderStates[i] = heldColliders[i].enabled;
                heldColliders[i].enabled = false;
            }

            isHoldingItem = true;
        }

        private void DropItem()
        {
            if (heldItem != null)
                heldItem.SetParent(null, true);

            if (heldRigidbody != null)
            {
                heldRigidbody.useGravity = heldRigidbodyUsedGravity;
                heldRigidbody.isKinematic = heldRigidbodyWasKinematic;
            }

            if (heldColliders != null)
            {
                for (int i = 0; i < heldColliders.Length; i++)
                {
                    if (heldColliders[i] != null)
                        heldColliders[i].enabled = heldColliderStates[i];
                }
            }

            heldItem = null;
            heldRigidbody = null;
            heldColliders = null;
            heldColliderStates = null;
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
