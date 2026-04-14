using UnityEngine;
using UnityEngine.Networking;
using RoR2;

namespace SeamstressVariant.Survivors.Seamstress.Components
{
    /// <summary>
    /// Redirects incoming healing into Heart that can be used as a resource for skills.
    /// The Heart builds up from healing received and can be consumed by abilities.
    /// </summary>
    internal class BleedingHeartComponent : MonoBehaviour
    {
        private HealthComponent healthComponent;
        private CharacterBody body;

        // Heart settings
        private float MaxHeart = 110f;
        public float currentHeart = 0f;
        private int activeBleedStacks = 0;
        private int nearbyEnemyCount = 0;
        private const float NearbyEnemyRadius = 20f;
        private float scanTimer = 0f;
        private const float ScanInterval = 1f;
        private const float HealPerBleedStack = 5f;

        private bool isInitialized = false;

        private void Awake()
        {
            healthComponent = GetComponent<HealthComponent>();
            body = GetComponent<CharacterBody>();

            if (healthComponent != null)
            {
                MaxHeart = healthComponent.fullHealth;
                isInitialized = true;
            }
        }

        private void Update()
        {
            if (!NetworkServer.active || body == null)
            {
                return;
            }

            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = ScanInterval;
                ScanNearbyEnemies();
                Log.Debug("NEARBY ENEMIES = " + nearbyEnemyCount + " | BLEED STACKS = " + activeBleedStacks);
                UpdateBleedStackBuff();
                ApplyPassiveHeal();
            }
        }

        private void OnEnable()
        {
            if (body != null)
            {
                body.onRecalculateStats += OnBodyRecalculateStates;
            }
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.onRecalculateStats -= OnBodyRecalculateStates;
            }
        }

        // Update maxHeart when maxHealth increased
        private void OnBodyRecalculateStates(CharacterBody body)
        {
            MaxHeart = body.maxHealth;
            Log.Debug("MaxHeart = " + MaxHeart);
        }

        // Add Health to heart from healing
        public void AddToHeart(float amount)
        {
            if (isInitialized)
            {
                currentHeart = Mathf.Min(currentHeart + amount, MaxHeart);
                Log.Debug("AMOUNT IN HEART = " + currentHeart);
                body.MarkAllStatsDirty();
            }
        }

        // return the current amount of health on heart
        public float GetHeart()
        {
            return currentHeart;
        }

        public float GetMaxHeart()
        {
            return MaxHeart;
        }

        public bool IsHeartFull()
        {
            return currentHeart >= MaxHeart;
        }

        public int GetActiveBleedStacks()
        {
            return activeBleedStacks;
        }

        public int GetNearbyEnemyCount()
        {
            return nearbyEnemyCount;
        }

        private void ScanNearbyEnemies()
        {
            if (body == null)
            {
                return;
            }

            TeamIndex myTeam = TeamComponent.GetObjectTeam(body.gameObject);
            Vector3 center = body.footPosition;
            float radiusSqr = NearbyEnemyRadius * NearbyEnemyRadius;
            int enemyCount = 0;
            int bleedCount = 0;

            foreach (CharacterBody otherBody in CharacterBody.readOnlyInstancesList)
            {
                if (!CountsAsNearbyEnemy(otherBody, myTeam))
                {
                    continue;
                }

                Vector3 delta = otherBody.footPosition - center;
                if (delta.sqrMagnitude <= radiusSqr)
                {
                    enemyCount++;
                    bleedCount += otherBody.GetBuffCount(RoR2Content.Buffs.Bleeding);
                }
            }

            nearbyEnemyCount = enemyCount;
            activeBleedStacks = bleedCount;
        }

        private void ApplyPassiveHeal()
        {
            if (activeBleedStacks <= 0 || healthComponent == null)
            {
                return;
            }

            float healAmount = HealPerBleedStack * activeBleedStacks;
            healthComponent.Heal(healAmount, default);
            Log.Debug("Passive heal: " + healAmount + " (" + activeBleedStacks + " stacks)");
        }

        private bool CountsAsNearbyEnemy(CharacterBody otherBody, TeamIndex myTeam)
        {
            if (otherBody == null || otherBody == body || otherBody.healthComponent == null || !otherBody.healthComponent.alive)
            {
                return false;
            }

            TeamIndex otherTeam = TeamComponent.GetObjectTeam(otherBody.gameObject);
            return otherTeam != TeamIndex.None && otherTeam != myTeam;
        }

        private void UpdateBleedStackBuff()
        {
            if (!NetworkServer.active || body == null)
            {
                return;
            }

            // Get the buff count before updating
            int currentBuffCount = body.GetBuffCount(SeamstressBuffs.bleedStackCounterBuff);
            
            // Set the buff count to match the active bleed stacks on enemies
            if (activeBleedStacks > 0)
            {
                // Set exact buff count to match bleed stacks
                body.SetBuffCount(SeamstressBuffs.bleedStackCounterBuff.buffIndex, activeBleedStacks);
                Log.Debug("Updated bleed stack visualization buff to " + activeBleedStacks);
            }
            else if (currentBuffCount > 0)
            {
                // Clear the buff if no bleeds are active
                body.SetBuffCount(SeamstressBuffs.bleedStackCounterBuff.buffIndex, 0);
                Log.Debug("Cleared bleed stack visualization buff");
            }
        }
    }
}
