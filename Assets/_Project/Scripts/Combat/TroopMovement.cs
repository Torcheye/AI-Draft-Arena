using UnityEngine;
using AdaptiveDraftArena.Core;

namespace AdaptiveDraftArena.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class TroopMovement : MonoBehaviour
    {
        public float MoveSpeed { get; private set; }
        public bool IsMoving { get; private set; }

        private Rigidbody rb;
        private Vector3 targetPosition;
        private float speedModifier = 1f;
        private float rotationSpeed = 10f;

        private const float MinDirectionThreshold = 0.001f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void Initialize(float speed)
        {
            MoveSpeed = speed;

            // Configure rigidbody for 3D ground-plane movement
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY |
                            RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
        }

        public void MoveToward(Vector3 target)
        {
            targetPosition = target;
            IsMoving = true;
        }

        public void Stop()
        {
            IsMoving = false;
            rb.linearVelocity = Vector3.zero;
        }

        public void SetSpeedModifier(float modifier)
        {
            speedModifier = Mathf.Max(0f, modifier);
        }

        private void FixedUpdate()
        {
            if (!IsMoving)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }

            // Calculate direction on XZ plane
            var direction = Extensions.DirectionXZ(transform.position, targetPosition);
            var finalSpeed = MoveSpeed * speedModifier;

            // Apply velocity on XZ plane (Y locked by constraints)
            rb.linearVelocity = direction * finalSpeed;

            // Rotate model to face movement direction
            if (direction.sqrMagnitude > MinDirectionThreshold)
            {
                var targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }
}
