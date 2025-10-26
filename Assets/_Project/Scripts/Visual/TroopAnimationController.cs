using UnityEngine;
using DG.Tweening;
using AdaptiveDraftArena.Combat;

namespace AdaptiveDraftArena.Visual
{
    // Handles visual animations for troops: walking wobble and attack weapon rotation
    public class TroopAnimationController : MonoBehaviour
    {
        [Header("Walking Animation")]
        [SerializeField] private float walkWobbleAngle = 15f;
        [SerializeField] private float walkWobbleDuration = 0.3f;

        [Header("Attack Animation")]
        [SerializeField] private float attackRotationDuration = 0.4f;

        private Transform bodyModel;
        private Transform weaponModel;
        private TroopMovement movement;

        private Tweener walkTween;
        private Tweener walkResetTween;
        private Tweener attackTween;
        private bool wasMoving;

        public void Initialize(Transform body, Transform weapon, TroopMovement troopMovement)
        {
            bodyModel = body;
            weaponModel = weapon;
            movement = troopMovement;

            if (bodyModel == null || movement == null)
            {
                Debug.LogWarning("TroopAnimationController initialized with null references. Disabling component.");
                enabled = false;
                return;
            }

            wasMoving = false;
        }

        private void OnValidate()
        {
            if (walkWobbleAngle < 0) walkWobbleAngle = 0;
            if (walkWobbleDuration <= 0) walkWobbleDuration = 0.1f;
            if (attackRotationDuration <= 0) attackRotationDuration = 0.1f;
        }

        private void Update()
        {
            // Track movement state changes
            var isMoving = movement.IsMoving;

            if (isMoving && !wasMoving)
            {
                StartWalkAnimation();
            }
            else if (!isMoving && wasMoving)
            {
                StopWalkAnimation();
            }

            wasMoving = isMoving;
        }

        private void StartWalkAnimation()
        {
            // Kill any existing walk tween
            walkTween?.Kill();

            // Create continuous wobble animation (left-right rotation on Z axis)
            walkTween = bodyModel.DOLocalRotate(
                new Vector3(0f, 0f, walkWobbleAngle),
                walkWobbleDuration
            )
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        }

        private void StopWalkAnimation()
        {
            // Kill walk tween and reset rotation
            walkTween?.Kill();
            walkResetTween?.Kill();
            walkResetTween = bodyModel.DOLocalRotate(Vector3.zero, walkWobbleDuration * 0.5f);
        }

        public void PlayAttackAnimation()
        {
            if (weaponModel == null) return;

            // Kill any existing attack tween
            attackTween?.Kill();

            // Store original rotation
            var originalRotation = weaponModel.localRotation;

            // Full 360° forward rotation on X axis
            attackTween = weaponModel.DOLocalRotate(
                new Vector3(360f, 0f, 0f),
                attackRotationDuration,
                RotateMode.FastBeyond360
            )
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Reset to original rotation after animation
                weaponModel.localRotation = originalRotation;
            });
        }

        private void OnDisable()
        {
            CleanupTweens();
        }

        private void OnDestroy()
        {
            CleanupTweens();
        }

        private void CleanupTweens()
        {
            walkTween?.Kill();
            walkResetTween?.Kill();
            attackTween?.Kill();
        }
    }
}
