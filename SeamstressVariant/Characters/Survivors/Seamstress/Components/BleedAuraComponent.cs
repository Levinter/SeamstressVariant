using UnityEngine;
using RoR2;
using SeamstressVariant.Survivors.Seamstress.Components;

namespace SeamstressVariant.Survivors.Seamstress.Components
{
    /// <summary>
    /// Detects and counts bleeding enemies within a radius around the player.
    /// Provides the foundation for aura-based effects based on bleed proc density.
    /// </summary>
    public class BleedAuraComponent : MonoBehaviour
    {
        private CharacterBody characterBody;
        private TeamComponent teamComponent;

        // Detection settings
        public float detectionRadius = 25f;
        public float updateInterval = 1.0f; // Check every 1 seconds for performance

        // Results
        public int bleedingEnemiesCount { get; private set; }
        private float lastUpdateTime;

        // Buff management
        private int currentAuraBuffCount = 0;

        // Debug settings
        public bool enableDebugLogging = false;
        private float lastDebugLogTime;
        private const float DEBUG_LOG_INTERVAL = 2f; // Log every 2 seconds

        private void Start()
        {
            characterBody = GetComponent<CharacterBody>();
            teamComponent = GetComponent<TeamComponent>();
            lastUpdateTime = Time.time;
            lastDebugLogTime = Time.time;
            currentAuraBuffCount = 0; // Initialize buff count
        }

        private void FixedUpdate()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateBleedCount();
                UpdateAuraBuff();
                lastUpdateTime = Time.time;
            }

            // Debug logging
            if (enableDebugLogging && Time.time - lastDebugLogTime >= DEBUG_LOG_INTERVAL)
            {
                Debug.Log($"Bleeding Heart Aura: {bleedingEnemiesCount} bleeding enemies detected within {detectionRadius} units");
                lastDebugLogTime = Time.time;
            }
        }

        /// <summary>
        /// Updates the count of bleeding enemies within detection radius.
        /// </summary>
        private void UpdateBleedCount()
        {
            bleedingEnemiesCount = 0;

            // Find all colliders within detection radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, LayerIndex.entityPrecise.mask);

            foreach (Collider collider in colliders)
            {
                CharacterBody potentialEnemy = collider.GetComponent<CharacterBody>();
                if (potentialEnemy != null && IsValidEnemy(potentialEnemy))
                {
                    if (HasActiveBleed(potentialEnemy))
                    {
                        bleedingEnemiesCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the target is a valid enemy (not on our team and alive).
        /// </summary>
        private bool IsValidEnemy(CharacterBody targetBody)
        {
            if (targetBody == characterBody) return false; // Don't count self
            if (!targetBody.healthComponent.alive) return false; // Only count living enemies

            // Check team relationship
            if (teamComponent && targetBody.teamComponent)
            {
                return !teamComponent.teamIndex.Equals(targetBody.teamComponent.teamIndex);
            }

            return false;
        }

        /// <summary>
        /// Checks if the target has an active bleed DoT.
        /// </summary>
        private bool HasActiveBleed(CharacterBody targetBody)
        {
            // Method 1: Check DotController component for active bleed
            DotController dotController = targetBody.GetComponent<DotController>();
            if (dotController != null)
            {
                // Try to access the HasDotActive method if it exists
                return HasDotActive(dotController, DotController.DotIndex.Bleed);
            }

            // Method 2: Fallback - check for bleed buff if DoT system uses buffs
            // This is a common pattern in RoR2 where DoTs might be represented as buffs
            return targetBody.HasBuff(RoR2Content.Buffs.Bleeding);
        }

        /// <summary>
        /// Attempts to check if a specific DoT is active on the DotController.
        /// Uses reflection as a fallback if the method isn't directly accessible.
        /// </summary>
        private bool HasDotActive(DotController dotController, DotController.DotIndex dotIndex)
        {
            // Try direct method call first (if it exists in this version of RoR2)
            var method = typeof(DotController).GetMethod("HasDotActive");
            if (method != null)
            {
                try
                {
                    return (bool)method.Invoke(dotController, new object[] { dotIndex });
                }
                catch
                {
                    // Method exists but failed to call
                }
            }

            // Fallback: Check if the dotController has any active dots and assume bleed is present
            // This is less accurate but better than nothing
            var activeDotsField = typeof(DotController).GetField("activeDots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (activeDotsField != null)
            {
                try
                {
                    var activeDots = activeDotsField.GetValue(dotController) as System.Collections.Generic.List<DotController.DotStack>;
                    if (activeDots != null)
                    {
                        foreach (var dot in activeDots)
                        {
                            if (dot.dotIndex == dotIndex)
                            {
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    // Reflection failed
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the bleed aura visualization buff to show current bleeding enemy count.
        /// </summary>
        private void UpdateAuraBuff()
        {
            if (characterBody == null)
                return;

            // Only update if the count changed
            if (bleedingEnemiesCount != currentAuraBuffCount)
            {
                int delta = bleedingEnemiesCount - currentAuraBuffCount;

                if (delta > 0)
                {
                    for (int i = 0; i < delta; i++)
                    {
                        characterBody.AddBuff(SeamstressBuffs.bleedAuraBuff);
                    }
                }
                else if (delta < 0)
                {
                    for (int i = 0; i < -delta; i++)
                    {
                        characterBody.RemoveBuff(SeamstressBuffs.bleedAuraBuff);
                    }
                }

                currentAuraBuffCount = bleedingEnemiesCount;

                // Debug log buff changes
                if (enableDebugLogging)
                {
                    Debug.Log($"Bleed Aura Buff updated: {currentAuraBuffCount} stacks (delta: {delta})");
                }
            }
        }

        /// <summary>
        /// Debug method to get current bleed count (can be called from other components).
        /// </summary>
        public int GetBleedingEnemiesCount()
        {
            return bleedingEnemiesCount;
        }

        /// <summary>
        /// Enables or disables debug logging of bleed detection.
        /// </summary>
        public void SetDebugLogging(bool enabled)
        {
            enableDebugLogging = enabled;
            if (enabled)
            {
                Debug.Log("Bleeding Heart Aura: Debug logging enabled");
            }
            else
            {
                Debug.Log("Bleeding Heart Aura: Debug logging disabled");
            }
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            if (characterBody != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, detectionRadius);
            }
        }
    }
}