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
        public float damageCoefficient = SeamstressVariantStaticValues.dashDamageCoefficient;
        private BlastAttack blastAttack;
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
            blastAttack = CreateBlastAttack(storedHeart);

            PlayCrossfade("FullBody, Override", "RipHeart", "Dash.playbackRate",
                baseDuration / attackSpeedStat * 1.8f,
                baseDuration / attackSpeedStat * 0.05f);

            Util.PlayAttackSpeedSound("Play_imp_overlord_attack2_tell", gameObject, attackSpeedStat);
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

        private BlastAttack CreateBlastAttack(float heartValue)
        {
            BlastAttack blastAttack = new BlastAttack();
            blastAttack.position = characterBody.corePosition;
            float baseDamage = damageCoefficient * damageStat;
            float additionalDamage = (heartValue * 0.01f) * damageStat;
            blastAttack.baseDamage = baseDamage + additionalDamage;
            blastAttack.damageType = DamageType.Stun1s;
            blastAttack.baseForce = 800f;
            blastAttack.bonusForce = Vector3.zero;
            blastAttack.radius = SeamstressVariantStaticValues.explodeRadius;
            blastAttack.attacker = gameObject;
            blastAttack.inflictor = gameObject;
            blastAttack.teamIndex = GetTeam();
            blastAttack.crit = RollCrit();
            blastAttack.procChainMask = default(ProcChainMask);
            blastAttack.procCoefficient = 1f;
            blastAttack.falloffModel = BlastAttack.FalloffModel.Linear;
            blastAttack.damageColorIndex = DamageColorIndex.Default;

            return blastAttack;
        }

        public override void OnExit()
        {
            Util.PlaySound("Play_imp_overlord_teleport_end", gameObject);

            if (isAuthority && blastAttack != null)
            {
                blastAttack.Fire();
            }

            if (SeamstressAssets.genericImpactExplosionEffect)
            {
                EffectManager.SpawnEffect(SeamstressAssets.genericImpactExplosionEffect, new EffectData
                {
                    origin = characterBody.corePosition,
                    rotation = Quaternion.identity,
                    color = (Color32)SeamstressAssets.coolRed
                }, true);
            }

            if (SeamstressAssets.slamEffect)
            {
                EffectManager.SpawnEffect(SeamstressAssets.slamEffect, new EffectData
                {
                    origin = characterBody.corePosition,
                    rotation = Quaternion.identity
                }, true);
            }

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