using UnityEngine;
using UnityEngine.Networking;
using RoR2;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    /// <summary>
    /// Applies knockback-related body flags during Defiance buff duration and restores them when the buff ends.
    /// </summary>
    internal class DefianceKnockbackController : MonoBehaviour
    {
        private CharacterBody characterBody;
        private static readonly CharacterBody.BodyFlags DefianceBodyFlags = CharacterBody.BodyFlags.Unmovable | CharacterBody.BodyFlags.IgnoreKnockup;
        private int previousBuffCount = 0;
        private CharacterBody.BodyFlags appliedBodyFlags;

        private void Awake()
        {
            characterBody = GetComponent<CharacterBody>();
            if (characterBody == null)
            {
                Log.Error("DefianceKnockbackController: CharacterBody not found on this GameObject");
                enabled = false;
                return;
            }

            previousBuffCount = 0;
        }

        private void Update()
        {
            // Only apply body flag changes on the server.
            if (!NetworkServer.active || characterBody == null)
            {
                return;
            }

            // Get current Defiance buff count (0 or 1, since canStack=false)
            int currentBuffCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);

            // Check if buff state changed
            if (currentBuffCount != previousBuffCount)
            {
                if (currentBuffCount > 0)
                {
                    ApplyDefianceFlags();
                }
                else if (previousBuffCount > 0)
                {
                    RestoreDefianceFlags();
                }

                previousBuffCount = currentBuffCount;
            }
        }

        private void ApplyDefianceFlags()
        {
            appliedBodyFlags = DefianceBodyFlags & ~characterBody.bodyFlags;
            characterBody.bodyFlags |= DefianceBodyFlags;
            //Log.Debug($"DefianceKnockbackController: Applied Defiance flags {appliedBodyFlags}");
        }

        private void RestoreDefianceFlags()
        {
            characterBody.bodyFlags &= ~appliedBodyFlags;
            appliedBodyFlags = 0;
            //Log.Debug("DefianceKnockbackController: Restored Defiance flags");
        }

        private void OnDestroy()
        {
            // Ensure flags are restored if component is destroyed while Defiance is active.
            if (characterBody != null && appliedBodyFlags != 0)
            {
                characterBody.bodyFlags &= ~appliedBodyFlags;
                appliedBodyFlags = 0;
                //Log.Debug("DefianceKnockbackController: Restored Defiance flags on component destruction");
            }
        }
    }
}
