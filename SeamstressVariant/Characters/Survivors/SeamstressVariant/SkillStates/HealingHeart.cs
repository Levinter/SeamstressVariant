using EntityStates;
using RoR2;
using RoR2.Skills;
using SeamstressMod.Seamstress.Content;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    public class HealingHeart : BaseSkillState
    {
        public float baseDuration = 0.5f;

        private BleedingHeartComponent heart;
        private float storedHeart;
        private bool transferApplied;
        private bool forcedTransitionToDefiantHeart;

        public override void OnEnter()
        {
            base.OnEnter();

            transferApplied = false;
            forcedTransitionToDefiantHeart = false;
            heart = GetComponent<BleedingHeartComponent>();

            DefianceSpecialController specialController = GetComponent<DefianceSpecialController>();
            if (specialController != null && specialController.ConsumeForcedDefianceActivation())
            {
                Log.Warning("HealingHeart: Forced Defiance activation detected on enter. Transitioning to DefiantHeart.");
                transferApplied = true;
                forcedTransitionToDefiantHeart = true;
                outer.SetNextState(new DefiantHeart());
                return;
            }

            if (heart == null || healthComponent == null || characterBody == null)
            {
                outer.SetNextStateToMain();
                return;
            }

            storedHeart = heart.GetHeart();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (fixedAge >= baseDuration / attackSpeedStat)
            {
                if (NetworkServer.active)
                {
                    ApplyHeartTransfer();
                }

                if (isAuthority)
                {
                    outer.SetNextStateToMain();
                }
            }
        }

        public override void OnExit()
        {
            Log.Warning("Exiting Healing Heart state.");

            if (NetworkServer.active)
            {
                
                if (!forcedTransitionToDefiantHeart)
                {
                    ApplyHeartTransfer();
                    RemoveDefiance();
                }
            }
            base.OnExit();
        }

        private void ApplyHeartTransfer()
        {
            if (transferApplied || heart == null || healthComponent == null || characterBody == null)
            {
                return;
            }

            float transferred = heart.ConsumeHeart(storedHeart);
            if (transferred > 0f)
            {
                float currentMaxHealth = healthComponent.fullHealth;
                healthComponent.Networkhealth = Mathf.Clamp(healthComponent.health + transferred, 1f, currentMaxHealth);
            }

            transferApplied = true;
        }

        private void RemoveDefiance()
        {
            int defianceCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);
            if (defianceCount > 0)
            {
                characterBody.RemoveBuff(SeamstressVariantBuffs.defianceBuff);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}