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

        public override void OnEnter()
        {
            base.OnEnter();

            transferApplied = false;
            heart = GetComponent<BleedingHeartComponent>();

            DefianceSpecialController specialController = GetComponent<DefianceSpecialController>();
            if (specialController != null && specialController.ConsumeForcedDefianceActivation())
            {
                transferApplied = true;
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
            if (NetworkServer.active)
            {
                ApplyHeartTransfer();
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

            int defianceCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);
            if (defianceCount > 0)
            {
                characterBody.SetBuffCount(SeamstressVariantBuffs.defianceBuff.buffIndex, 0);
            }

            transferApplied = true;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}