using UnityEngine;

namespace AdaptiveDraftArena.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class TroopMovement : MonoBehaviour
    {
        public float MoveSpeed { get; private set; }
        public bool IsMoving { get; private set; }

        private Rigidbody2D rb;
        private Vector2 targetPosition;
        private float speedModifier = 1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void Initialize(float speed)
        {
            MoveSpeed = speed;

            // Configure rigidbody for top-down movement
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public void MoveToward(Vector2 target)
        {
            targetPosition = target;
            IsMoving = true;
        }

        public void Stop()
        {
            IsMoving = false;
            rb.linearVelocity = Vector2.zero;
        }

        public void SetSpeedModifier(float modifier)
        {
            speedModifier = Mathf.Max(0f, modifier);
        }

        private void FixedUpdate()
        {
            if (!IsMoving)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            var direction = (targetPosition - (Vector2)transform.position).normalized;
            var finalSpeed = MoveSpeed * speedModifier;
            rb.linearVelocity = direction * finalSpeed;

            // Optional: Flip sprite based on movement direction
            if (direction.x != 0)
            {
                var scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
                transform.localScale = scale;
            }
        }
    }
}
