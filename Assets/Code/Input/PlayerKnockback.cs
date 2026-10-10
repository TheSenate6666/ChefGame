using UnityEngine;

namespace ChefGame.Input
{
    /// <summary>
    /// Applies an outward impulse when this player's hand collides with
    /// another player's hand. Put this on the Player-tagged Rigidbody root.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerKnockback : MonoBehaviour
    {
        [Header("Collision filtering")]
         private string playerTag = "Player";
         private string handTag = "Hand";

        [Header("Knockback tuning")]
        [SerializeField, Min(0f)] private float knockbackImpulse = 150f;
        [SerializeField, Min(0f)] private float otherPlayerImpulseMultiplier = 1f;
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 0.25f;
        [SerializeField, Min(0f)] private float knockbackCooldown = 0.2f;
        [SerializeField] private float upwardBias;

        private Rigidbody playerRigidbody;
        private float nextKnockbackTime;

        private void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.collider == null || collision.rigidbody == null)
                return;

            if (!collision.collider.CompareTag(handTag))
                return;

            Rigidbody otherPlayerRigidbody = collision.rigidbody;
            if (!otherPlayerRigidbody.gameObject.CompareTag(playerTag) || otherPlayerRigidbody == playerRigidbody)
                return;

            // Both player roots receive this collision callback. Let only the
            // lower instance ID process the pair to avoid applying impulses twice.
            if (playerRigidbody.GetInstanceID() > otherPlayerRigidbody.GetInstanceID())
                return;

            if (Time.time < nextKnockbackTime || collision.relativeVelocity.magnitude < minimumImpactSpeed)
                return;

            Vector3 contactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : collision.rigidbody.worldCenterOfMass;
            Vector3 knockbackDirection = playerRigidbody.worldCenterOfMass - contactPoint;

            if (knockbackDirection.sqrMagnitude < 0.0001f)
                knockbackDirection = playerRigidbody.worldCenterOfMass - collision.rigidbody.worldCenterOfMass;

            // Knockback is planar: never transfer collision force along depth.
            knockbackDirection.z = 0f;
            knockbackDirection.y += upwardBias;
            if (knockbackDirection.sqrMagnitude < 0.0001f)
                return;

            playerRigidbody.AddForce(knockbackDirection.normalized * knockbackImpulse, ForceMode.Impulse);
            otherPlayerRigidbody.AddForce(-knockbackDirection.normalized * knockbackImpulse * otherPlayerImpulseMultiplier, ForceMode.Impulse);
            nextKnockbackTime = Time.time + knockbackCooldown;

            PlayerKnockback otherKnockback = otherPlayerRigidbody.GetComponent<PlayerKnockback>();
            if (otherKnockback != null)
                otherKnockback.nextKnockbackTime = nextKnockbackTime;
        }
    }
}
