using UnityEngine;
using UnityEngine.InputSystem;

namespace ChefGame.Input
{
    /// <summary>
    /// Keyboard-only local co-op movement for a player hand.
    /// Movement is constrained to the XY plane and to a radius around the
    /// hand's position when play begins.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalCoopPlayerController : MonoBehaviour
    {
        public enum KeyboardLayout
        {
            WASD,
            Arrows
        }

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private KeyboardLayout keyboardLayout = KeyboardLayout.WASD;
        [SerializeField, Min(0f)] private float movementAcceleration = 20f;
        [SerializeField, Min(0f)] private float maximumSpeed = 4.5f;
        [SerializeField, Min(0f)] private float drag = 7f;
        [SerializeField, Min(0f)] private float movementRadius = 3f;

        private InputActionAsset runtimeActions;
        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private Vector3 movementCenter;
        private Vector2 velocity;

        private string BindingGroup => keyboardLayout == KeyboardLayout.WASD ? "WASD" : "Arrows";

        private void Awake()
        {
            movementCenter = transform.position;

            if (inputActions == null)
            {
                Debug.LogError($"{nameof(LocalCoopPlayerController)} on '{name}' has no Input Action Asset assigned.", this);
                enabled = false;
                return;
            }

            // Each player gets a private copy so the binding mask for one
            // keyboard layout cannot affect the other player.
            runtimeActions = Instantiate(inputActions);

            gameplayMap = runtimeActions.FindActionMap("Gameplay", true);
            gameplayMap.bindingMask = InputBinding.MaskByGroup(BindingGroup);
            moveAction = gameplayMap.FindAction("Move", true);
        }

        private void OnEnable()
        {
            if (gameplayMap != null)
            {
                // Refresh the layout mask before enabling in case it was
                // changed in the Inspector while the component was disabled.
                gameplayMap.bindingMask = InputBinding.MaskByGroup(BindingGroup);
                gameplayMap.Enable();
            }
        }

        private void OnDisable()
        {
            if (gameplayMap != null)
                gameplayMap.Disable();
        }

        private void Update()
        {
            if (moveAction == null)
                return;

            Vector2 input = moveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            float deltaTime = Time.deltaTime;
            if (input.sqrMagnitude > 0.0001f)
                velocity += input * (movementAcceleration * deltaTime);
            else
                velocity = Vector2.MoveTowards(velocity, Vector2.zero, drag * deltaTime);

            velocity = Vector2.ClampMagnitude(velocity, maximumSpeed);

            Vector2 proposedOffset = new Vector2(
                transform.position.x - movementCenter.x,
                transform.position.y - movementCenter.y) + velocity * deltaTime;
            Vector2 clampedOffset = Vector2.ClampMagnitude(proposedOffset, movementRadius);

            // Remove velocity pushing out through the circular boundary, while
            // keeping motion tangent to it available.
            if ((clampedOffset - proposedOffset).sqrMagnitude > 0.000001f && proposedOffset.sqrMagnitude > 0f)
            {
                Vector2 outwardNormal = proposedOffset.normalized;
                float outwardSpeed = Vector2.Dot(velocity, outwardNormal);
                if (outwardSpeed > 0f)
                    velocity -= outwardNormal * outwardSpeed;
            }

            transform.position = new Vector3(
                movementCenter.x + clampedOffset.x,
                movementCenter.y + clampedOffset.y,
                transform.position.z);
        }

        private void OnDestroy()
        {
            if (runtimeActions != null)
                Destroy(runtimeActions);
        }

        private void OnDrawGizmos()
        {
            Vector3 center = Application.isPlaying ? movementCenter : transform.position;
            float radius = Mathf.Max(0f, movementRadius);
            const int segments = 64;

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
            Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * (Mathf.PI * 2f / segments);
                Vector3 nextPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);

                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
    }
}
