using UnityEngine;
using RoR2;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    /// <summary>
    /// Applies and maintains Defiance gameplay benefits while the Defiance buff is active,
    /// then restores modified state when the buff ends.
    /// </summary>
    internal class DefianceBenefitsController : NetworkBehaviour
    {
        private CharacterBody characterBody;
        private SetStateOnHurt setStateOnHurt;

        private static readonly CharacterBody.BodyFlags DefianceBodyFlags = CharacterBody.BodyFlags.Unmovable | CharacterBody.BodyFlags.IgnoreKnockup;
        private int previousBuffCount = 0;
        private CharacterBody.BodyFlags appliedBodyFlags;
        private bool stateImmunitiesApplied;

        private const int MaxFervourStacks = 20;
        private float fervourAccumulator = 0f;
        [SyncVar (hook = nameof(OnFervourStacksChanged))]
        private int fervourStacks = 0;

        public bool IsDefianceActive => previousBuffCount > 0;
        public int FervourStacks => fervourStacks;
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
                Log.Debug("DefianceBenefitsController: Detected existing Defiance buff on Awake, applying benefits");
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
                TickFervour();
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
            Log.Warning("Applying Defiance benefits.");
            ApplyDefianceFlags();
            ApplyStateImmunities();

            if (NetworkServer.active)
            {
                RemoveDebuffs();
                fervourAccumulator = 0f;
                fervourStacks = 0;
                characterBody.MarkAllStatsDirty();
            }
        }

        private void RestoreDefianceBenefits()
        {
            RestoreStateImmunities();
            RestoreDefianceFlags();

            fervourAccumulator = 0f;
            fervourStacks = 0;
            characterBody.MarkAllStatsDirty();
        }

        private void TickFervour()
        {
            if (fervourStacks >= MaxFervourStacks)
            {
                return;
            }

            fervourAccumulator += Time.deltaTime;
            bool changed = false;
            while (fervourAccumulator >= 1f && fervourStacks < MaxFervourStacks)
            {
                fervourAccumulator -= 1f;
                fervourStacks++;
                changed = true;
            }

            if (changed)
            {
                characterBody.MarkAllStatsDirty();
            }
        }

        private void ApplyDefianceFlags()
        {
            appliedBodyFlags = DefianceBodyFlags & ~characterBody.bodyFlags;
            characterBody.bodyFlags |= DefianceBodyFlags;
            Log.Warning($"Applied Defiance body flags: {appliedBodyFlags}");
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
            Log.Warning("Applied Defiance state immunities.");
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

        private void OnFervourStacksChanged(int newStacks)
        {
            fervourStacks = newStacks;
            //characterBody.MarkAllStatsDirty();
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
