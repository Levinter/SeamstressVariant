using EntityStates;
using RoR2;
using SeamstressMod.Seamstress.Content;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    public class DefiantDashReactivate : BaseSkillState
    {
        public float baseDuration = 0.5f;

        private BleedingHeartComponent heart;
        private float storedHeart;

        public override void OnEnter()
        {
            base.OnEnter();

            heart = GetComponent<BleedingHeartComponent>();

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

            if (!isAuthority)
            {
                return;
            }

            if (fixedAge >= baseDuration / attackSpeedStat)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            if (NetworkServer.active && heart != null && healthComponent != null)
            {
                heart.ConsumeHeart(storedHeart);
                float currentMaxHealth = healthComponent.fullHealth;
                healthComponent.health = Mathf.Clamp(healthComponent.health + storedHeart, 1f, currentMaxHealth);

                if (characterBody != null)
                {
                    int defianceCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);
                    if (defianceCount > 0)
                    {
                        characterBody.SetBuffCount(SeamstressVariantBuffs.defianceBuff.buffIndex, 0);
                    }
                }
            }

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}