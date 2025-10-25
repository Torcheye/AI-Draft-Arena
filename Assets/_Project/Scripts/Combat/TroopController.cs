using UnityEngine;
using AdaptiveDraftArena.Abilities;
using AdaptiveDraftArena.Modules;
using AdaptiveDraftArena.Visual;

namespace AdaptiveDraftArena.Combat
{
    public class TroopController : MonoBehaviour
    {
        // Module composition
        public TroopCombination Combination { get; private set; }
        public Team Team { get; private set; }

        // Components
        public HealthComponent Health { get; private set; }
        public TroopMovement Movement { get; private set; }
        public TroopCombat Combat { get; private set; }
        public TargetingSystem Targeting { get; private set; }
        public TroopVisuals Visuals { get; private set; }
        public AbilityExecutor AbilityExecutor { get; private set; }

        // State
        public bool IsAlive => Health != null && Health.IsAlive;

        private bool isInitialized;

        private void Awake()
        {
            // Get or add required components
            Health = GetComponent<HealthComponent>();
            if (Health == null) Health = gameObject.AddComponent<HealthComponent>();

            Movement = GetComponent<TroopMovement>();
            if (Movement == null) Movement = gameObject.AddComponent<TroopMovement>();

            Combat = GetComponent<TroopCombat>();
            if (Combat == null) Combat = gameObject.AddComponent<TroopCombat>();

            Targeting = GetComponent<TargetingSystem>();
            if (Targeting == null) Targeting = gameObject.AddComponent<TargetingSystem>();

            Visuals = GetComponent<TroopVisuals>();
            if (Visuals == null) Visuals = gameObject.AddComponent<TroopVisuals>();

            AbilityExecutor = GetComponent<AbilityExecutor>();
            if (AbilityExecutor == null) AbilityExecutor = gameObject.AddComponent<AbilityExecutor>();
        }

        public void Initialize(TroopCombination combination, Team team, Vector2 spawnPosition)
        {
            if (isInitialized)
            {
                Debug.LogWarning($"TroopController {name} is already initialized!");
                return;
            }

            Combination = combination;
            Team = team;
            transform.position = spawnPosition;

            // Initialize components
            var maxHP = combination.GetFinalHP();
            var moveSpeed = combination.GetFinalSpeed();

            Health.Initialize(maxHP);
            Movement.Initialize(moveSpeed);
            Combat.Initialize(this);
            Targeting.Initialize(team);
            Visuals.Compose(combination);
            AbilityExecutor.Initialize(combination.ability, this);

            // Subscribe to death event
            Health.OnDeath += HandleDeath;

            // Register with targeting system
            TargetingSystem.RegisterTroop(this);

            // Set name for debugging
            gameObject.name = $"{combination.DisplayName}_{team}";

            isInitialized = true;

            Debug.Log($"Initialized {combination.DisplayName} on team {team} with {maxHP} HP");
        }

        private void HandleDeath()
        {
            Debug.Log($"{name} has died!");

            // Notify ability system
            AbilityExecutor.OnOwnerDeath();

            // Unregister from targeting system
            TargetingSystem.UnregisterTroop(this);

            // Stop all movement
            Movement.Stop();

            // TODO: Play death VFX
            // TODO: Play death sound

            // Destroy after delay
            Destroy(gameObject, 0.5f);
        }

        private void OnDestroy()
        {
            // Cleanup
            if (Health != null)
            {
                Health.OnDeath -= HandleDeath;
            }

            // Ensure unregistration
            if (isInitialized)
            {
                TargetingSystem.UnregisterTroop(this);
            }
        }
    }
}
