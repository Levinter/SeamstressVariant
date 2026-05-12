using UnityEngine;
using RoR2;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    /// <summary>
    /// Applies and maintains Defiance gameplay benefits while the Defiance buff is active,
    /// then restores modified state when the buff ends.
    /// </summary>
    internal class DefianceBenefitsController : MonoBehaviour
    {
        private CharacterBody characterBody;
        private SetStateOnHurt setStateOnHurt;

        private static readonly CharacterBody.BodyFlags DefianceBodyFlags = CharacterBody.BodyFlags.Unmovable | CharacterBody.BodyFlags.IgnoreKnockup;
        private int previousBuffCount = 0;
        private CharacterBody.BodyFlags appliedBodyFlags;
        private bool stateImmunitiesApplied;
        private bool originalCanBeHitStunned;
        private bool originalCanBeStunned;
        private bool originalCanBeFrozen;
        private bool originalCanBeTaunted;

        private void Awake()
        {
            characterBody = GetComponent<CharacterBody>();
            setStateOnHurt = GetComponent<SetStateOnHurt>();

            if (characterBody == null)
            {
                Log.Error("DefianceBenefitsController: CharacterBody not found on this GameObject");
                enabled = false;
                return;
            }

            // Initialize from current replicated buff state in case this component starts while Defiance is already active.
            previousBuffCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);
            if (previousBuffCount > 0)
            {
                ApplyDefianceBenefits();
            }
        }

        private void Update()
        {
            if (characterBody == null)
            {
                return;
            }

            // Defiance does not stack, but count checks are safer than HasBuff in replication edge cases.
            int currentBuffCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);

            if (currentBuffCount != previousBuffCount)
            {
                if (currentBuffCount > 0)
                {
                    ApplyDefianceBenefits();
                }
                else if (previousBuffCount > 0)
                {
                    RestoreDefianceBenefits();
                }

                previousBuffCount = currentBuffCount;
            }

            if (currentBuffCount > 0 && NetworkServer.active)
            {
                RemoveDebuffs();
            }
        }

        private void OnDisable()
        {
            if (characterBody != null)
            {
                RestoreDefianceBenefits();
            }

            previousBuffCount = 0;
        }

        private void ApplyDefianceBenefits()
        {
            ApplyDefianceFlags();
            ApplyStateImmunities();

            if (NetworkServer.active)
            {
                RemoveDebuffs();
            }
        }

        private void RestoreDefianceBenefits()
        {
            RestoreStateImmunities();
            RestoreDefianceFlags();
        }

        private void ApplyDefianceFlags()
        {
            appliedBodyFlags = DefianceBodyFlags & ~characterBody.bodyFlags;
            characterBody.bodyFlags |= DefianceBodyFlags;
        }

        private void RestoreDefianceFlags()
        {
            if (appliedBodyFlags == 0)
            {
                return;
            }

            characterBody.bodyFlags &= ~appliedBodyFlags;
            appliedBodyFlags = 0;
        }

        private void ApplyStateImmunities()
        {
            if (setStateOnHurt == null || stateImmunitiesApplied)
            {
                return;
            }

            originalCanBeHitStunned = setStateOnHurt.canBeHitStunned;
            originalCanBeStunned = setStateOnHurt.canBeStunned;
            originalCanBeFrozen = setStateOnHurt.canBeFrozen;
            originalCanBeTaunted = setStateOnHurt.canBeTaunted;

            setStateOnHurt.canBeHitStunned = false;
            setStateOnHurt.canBeStunned = false;
            setStateOnHurt.canBeFrozen = false;
            setStateOnHurt.canBeTaunted = false;
            setStateOnHurt.Cleanse();
            stateImmunitiesApplied = true;
        }

        private void RestoreStateImmunities()
        {
            if (setStateOnHurt == null || !stateImmunitiesApplied)
            {
                return;
            }

            setStateOnHurt.canBeHitStunned = originalCanBeHitStunned;
            setStateOnHurt.canBeStunned = originalCanBeStunned;
            setStateOnHurt.canBeFrozen = originalCanBeFrozen;
            setStateOnHurt.canBeTaunted = originalCanBeTaunted;
            stateImmunitiesApplied = false;
        }

        private void RemoveDebuffs()
        {
            if (characterBody == null)
            {
                return;
            }

            foreach (BuffIndex buffIndex in BuffCatalog.debuffBuffIndices)
            {
                if (characterBody.HasBuff(buffIndex) && buffIndex != (BuffIndex)236)
                {
                    characterBody.SetBuffCount(buffIndex, 0);
                }
            }
        }

        private void OnDestroy()
        {
            if (characterBody != null)
            {
                RestoreDefianceBenefits();
            }
        }
    }
}
