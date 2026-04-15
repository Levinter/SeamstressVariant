using EntityStates;
using RoR2;
using SeamstressVariant.Survivors.Seamstress.Components;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.Seamstress.SkillStates
{
    public class DefiantHeart : BaseSkillState
    {
        public static float heartDrainPerTick = 10f;
        public static float heartDrainInterval = 1f;

        private BleedingHeartComponent heart;
        private SetStateOnHurt setStateOnHurt;
        private float nextDrainAt;
        private bool canReactivate;
        private bool originalCanBeHitStunned;
        private bool originalCanBeStunned;
        private bool originalCanBeFrozen;
        private bool originalCanBeTaunted;

        public override void OnEnter()
        {
            base.OnEnter();

            heart = GetComponent<BleedingHeartComponent>();
            setStateOnHurt = GetComponent<SetStateOnHurt>();
            nextDrainAt = heartDrainInterval;
            canReactivate = false;

            if (heart == null || !heart.CanSustainDefiantHeart())
            {
                outer.SetNextStateToMain();
                return;
            }

            if (NetworkServer.active && characterBody)
            {
                characterBody.AddBuff(SeamstressBuffs.defianceBuff);
                ApplyStateImmunities();
                RemoveDebuffs();
            }

            if (GetModelAnimator())
            {
                PlayAnimation("Gesture, Override", "BufferEmpty");
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!isAuthority)
            {
                return;
            }

            if (heart == null || !heart.CanSustainDefiantHeart())
            {
                outer.SetNextStateToMain();
                return;
            }

            if (NetworkServer.active)
            {
                RemoveDebuffs();
            }

            if (!canReactivate)
            {
                canReactivate = inputBank == null || !inputBank.skill4.down;
            }

            if (canReactivate && inputBank != null && inputBank.skill4.down)
            {
                ReactivateDefiantHeart();
                return;
            }

            if (fixedAge >= nextDrainAt)
            {
                nextDrainAt += heartDrainInterval;

                if (NetworkServer.active)
                {
                    heart.ConsumeHeart(heartDrainPerTick);
                }

                if (!heart.CanSustainDefiantHeart())
                {
                    outer.SetNextStateToMain();
                }
            }
        }

        private void ReactivateDefiantHeart()
        {
            if (!NetworkServer.active || heart == null || healthComponent == null)
            {
                outer.SetNextStateToMain();
                return;
            }

            float storedHeart = heart.GetHeart();

            heart.ConsumeHeart(storedHeart);
            healthComponent.health = Mathf.Clamp(storedHeart, 1f, healthComponent.fullHealth);

            if (characterBody)
            {
                characterBody.RemoveBuff(SeamstressBuffs.defianceBuff);
            }

            outer.SetNextStateToMain();
        }

        private void RemoveDebuffs()
        {
            if (characterBody == null)
            {
                return;
            }

            foreach (BuffIndex buffIndex in BuffCatalog.debuffBuffIndices)
            {
                if (characterBody.HasBuff(buffIndex))
                {
                    characterBody.SetBuffCount(buffIndex, 0);
                }
            }
        }

        private void ApplyStateImmunities()
        {
            if (setStateOnHurt == null)
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
        }

        private void RestoreStateImmunities()
        {
            if (setStateOnHurt == null)
            {
                return;
            }

            setStateOnHurt.canBeHitStunned = originalCanBeHitStunned;
            setStateOnHurt.canBeStunned = originalCanBeStunned;
            setStateOnHurt.canBeFrozen = originalCanBeFrozen;
            setStateOnHurt.canBeTaunted = originalCanBeTaunted;
        }

        public override void OnExit()
        {
            if (NetworkServer.active)
            {
                RestoreStateImmunities();

                if (characterBody)
                {
                    characterBody.RemoveBuff(SeamstressBuffs.defianceBuff);
                }
            }

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}