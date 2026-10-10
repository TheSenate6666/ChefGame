using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace ChefGame.Input
{
    /// <summary>
    /// Local co-op movement for a player hand using a keyboard layout or gamepad.
    /// Movement stays within the Main Camera's viewport and a configurable
    /// depth range around the hand's position when play begins.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalCoopPlayerController : MonoBehaviour
    {
        private static readonly List<LocalCoopPlayerController> GamepadControllers = new();

        public enum InputScheme
        {
            WASD,
            Arrows,
            Gamepad
        }

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField, FormerlySerializedAs("keyboardLayout")] private InputScheme inputScheme = InputScheme.WASD;
        [SerializeField] private bool autoAssignGamepad = true;
        [SerializeField, Min(0)] private int gamepadSlot;
        [SerializeField] private Camera movementCamera;
        [SerializeField, Min(0f)] private float movementAcceleration = 40f;
        [SerializeField, Min(0f)] private float movementZAcceleration = 20f;
        [SerializeField, Min(0f)] private float maximumSpeed = 4.5f;
        [SerializeField, Min(0f)] private float drag = 7f;
        [SerializeField, Min(0f)] private float depthMovementLimit = 2f;
        [SerializeField, Range(0f, 0.25f)] private float screenEdgePadding = 0.03f;

        private InputActionAsset runtimeActions;
        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction backwardAction;
        private InputAction forwardAction;
        private InputAction interactAction;
        private Gamepad assignedGamepad;
        private Vector3 movementCenter;
        private Vector3 velocity;
        private Renderer[] visualRenderers;

        public InputAction InteractAction => interactAction;

        private string BindingGroup => inputScheme switch
        {
            InputScheme.WASD => "WASD",
            InputScheme.Arrows => "Arrows",
            InputScheme.Gamepad => "Gamepad",
            _ => "WASD"
        };

        private void Awake()
        {
            movementCenter = transform.position;
            visualRenderers = GetComponentsInChildren<Renderer>();

            if (movementCamera == null)
                movementCamera = Camera.main;

            if (movementCamera == null)
                Debug.LogWarning($"{nameof(LocalCoopPlayerController)} on '{name}' could not find a Main Camera. Screen-edge clamping is disabled.", this);

            if (inputActions == null)
            {
                Debug.LogError($"{nameof(LocalCoopPlayerController)} on '{name}' has no Input Action Asset assigned.", this);
                enabled = false;
                return;
            }

            // Each player gets a private copy so its scheme mask and device
            // filter cannot affect the other player.
            runtimeActions = Instantiate(inputActions);

            gameplayMap = runtimeActions.FindActionMap("Gameplay", true);
            gameplayMap.bindingMask = InputBinding.MaskByGroup(BindingGroup);
            moveAction = gameplayMap.FindAction("Move", true);
            backwardAction = gameplayMap.FindAction("Backward", true);
            forwardAction = gameplayMap.FindAction("Forward", true);
            interactAction = gameplayMap.FindAction("Interact", true);

        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += HandleDeviceChange;

            if (inputScheme == InputScheme.Gamepad)
            {
                if (!GamepadControllers.Contains(this))
                    GamepadControllers.Add(this);
                ReassignGamepads();
            }

            if (gameplayMap != null)
            {
                // Refresh the scheme mask before enabling in case it was
                // changed in the Inspector while the component was disabled.
                gameplayMap.bindingMask = InputBinding.MaskByGroup(BindingGroup);
                gameplayMap.Enable();
            }
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;

            if (gameplayMap != null)
                gameplayMap.Disable();

            if (GamepadControllers.Remove(this))
                ReassignGamepads();
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (inputScheme == InputScheme.Gamepad && (device is Gamepad || device == assignedGamepad))
                ReassignGamepads();
        }

        private static void ReassignGamepads()
        {
            GamepadControllers.RemoveAll(controller => controller == null || !controller.isActiveAndEnabled || controller.inputScheme != InputScheme.Gamepad);
            GamepadControllers.Sort((left, right) => string.CompareOrdinal(
                GetHierarchySortKey(left.transform),
                GetHierarchySortKey(right.transform)));

            var claimedSlots = new HashSet<int>();

            // Reserve manual selections before assigning the remaining pads.
            foreach (LocalCoopPlayerController controller in GamepadControllers)
            {
                if (controller.autoAssignGamepad)
                    continue;

                if (controller.gamepadSlot >= 0 && controller.gamepadSlot < Gamepad.all.Count && claimedSlots.Add(controller.gamepadSlot))
                    controller.AssignGamepad(Gamepad.all[controller.gamepadSlot]);
                else
                    controller.AssignGamepad(null);
            }

            foreach (LocalCoopPlayerController controller in GamepadControllers)
            {
                if (!controller.autoAssignGamepad)
                    continue;

                int availableSlot = 0;
                while (availableSlot < Gamepad.all.Count && claimedSlots.Contains(availableSlot))
                    availableSlot++;

                if (availableSlot < Gamepad.all.Count)
                {
                    claimedSlots.Add(availableSlot);
                    controller.AssignGamepad(Gamepad.all[availableSlot]);
                }
                else
                {
                    controller.AssignGamepad(null);
                }
            }
        }

        private void AssignGamepad(Gamepad gamepad)
        {
            if (assignedGamepad == gamepad || runtimeActions == null)
                return;

            assignedGamepad = gamepad;
            // An empty device list is important when a pad is unavailable:
            // a null list would let this player's actions listen to every pad.
            runtimeActions.devices = assignedGamepad != null
                ? new InputDevice[] { assignedGamepad }
                : new InputDevice[0];
        }

        private static string GetHierarchySortKey(Transform target)
        {
            var siblingIndices = new List<int>();
            for (Transform current = target; current != null; current = current.parent)
                siblingIndices.Add(current.GetSiblingIndex());

            string key = target.gameObject.scene.path;
            for (int i = siblingIndices.Count - 1; i >= 0; i--)
                key += "/" + siblingIndices[i].ToString("D4");
            return key;
        }

        private void Update()
        {
            if (moveAction == null)
                return;

            Vector2 planarInput = moveAction.ReadValue<Vector2>();
            float depthInput = (forwardAction.IsPressed() ? 1f : 0f) -
                               (backwardAction.IsPressed() ? 1f : 0f);
            float deltaTime = Time.deltaTime;
            if (planarInput.sqrMagnitude > 0.0001f || Mathf.Abs(depthInput) > 0.0001f)
            {
                // Keep planar and depth acceleration independently tunable.
                // The Input System already normalizes the 2D keyboard/gamepad
                // Move value; do not normalize it together with depth input.
                velocity.x += planarInput.x * movementAcceleration * deltaTime;
                velocity.y += planarInput.y * movementAcceleration * deltaTime;
                velocity.z += depthInput * movementZAcceleration * deltaTime;
            }
            else
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, drag * deltaTime);

            velocity = Vector3.ClampMagnitude(velocity, maximumSpeed);

            Vector3 proposedPosition = transform.position + velocity * deltaTime;
            float minimumZ = movementCenter.z - depthMovementLimit;
            float maximumZ = movementCenter.z + depthMovementLimit;
            float clampedZ = Mathf.Clamp(proposedPosition.z, minimumZ, maximumZ);

            if (!Mathf.Approximately(clampedZ, proposedPosition.z))
            {
                bool movingOutward = proposedPosition.z > maximumZ ? velocity.z > 0f : velocity.z < 0f;
                if (movingOutward)
                    velocity.z = 0f;
                proposedPosition.z = clampedZ;
            }

            transform.position = ClampToScreen(proposedPosition);
        }

        private Vector3 ClampToScreen(Vector3 position)
        {
            if (movementCamera == null)
                return position;

            Vector3 viewportPosition = movementCamera.WorldToViewportPoint(position);
            if (viewportPosition.z <= movementCamera.nearClipPlane)
                return position;

            Vector3 rendererOffset = position - transform.position;
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer == null || !visualRenderer.enabled)
                    continue;

                Bounds bounds = visualRenderer.bounds;
                Vector3 center = bounds.center + rendererOffset;
                Vector3 extents = bounds.extents;

                for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
                {
                    Vector3 corner = center + new Vector3(
                        (cornerIndex & 1) == 0 ? -extents.x : extents.x,
                        (cornerIndex & 2) == 0 ? -extents.y : extents.y,
                        (cornerIndex & 4) == 0 ? -extents.z : extents.z);
                    Vector3 viewportCorner = movementCamera.WorldToViewportPoint(corner);
                    if (viewportCorner.z <= movementCamera.nearClipPlane)
                        continue;

                    minX = Mathf.Min(minX, viewportCorner.x);
                    maxX = Mathf.Max(maxX, viewportCorner.x);
                    minY = Mathf.Min(minY, viewportCorner.y);
                    maxY = Mathf.Max(maxY, viewportCorner.y);
                }
            }

            // If there is no visible renderer, constrain the hand pivot itself.
            if (float.IsPositiveInfinity(minX))
            {
                minX = maxX = viewportPosition.x;
                minY = maxY = viewportPosition.y;
            }

            float horizontalCorrection = minX < screenEdgePadding
                ? screenEdgePadding - minX
                : maxX > 1f - screenEdgePadding ? 1f - screenEdgePadding - maxX : 0f;
            float verticalCorrection = minY < screenEdgePadding
                ? screenEdgePadding - minY
                : maxY > 1f - screenEdgePadding ? 1f - screenEdgePadding - maxY : 0f;

            if (horizontalCorrection == 0f && verticalCorrection == 0f)
                return position;

            Vector3 adjustedViewportPosition = new Vector3(
                viewportPosition.x + horizontalCorrection,
                viewportPosition.y + verticalCorrection,
                viewportPosition.z);
            Vector3 screenClampedPosition = movementCamera.ViewportToWorldPoint(adjustedViewportPosition);

            // The current Main Camera looks straight along world Z, so screen
            // correction should not alter the hand's clamped depth.
            screenClampedPosition.z = position.z;
            return screenClampedPosition;
        }

        private void OnDestroy()
        {
            if (runtimeActions != null)
                Destroy(runtimeActions);
        }

    }
}
