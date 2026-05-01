using EntityStates;
using EntityStates.JunkCube;
using RoR2;
using SeamstressMod.Seamstress.Content;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    public class DefiantDashReactivate : BaseSkillState
    {
        private BleedingHeartComponent heart;
        private bool hasExecuted;

        public override void OnEnter()
        {
            base.OnEnter();

            heart = GetComponent<BleedingHeartComponent>();

            if (!isAuthority)
            {
                return;
            }

            ExecuteReactivation();
            hasExecuted = true;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!isAuthority || !hasExecuted)
            {
                return;
            }

            // Hold this follow-up state until button release to prevent immediate recast.
            if (inputBank == null || !inputBank.skill4.down)
            {
                outer.SetNextStateToMain();
            }
        }

        private void ExecuteReactivation()
        {
            if (!NetworkServer.active || heart == null || healthComponent == null || characterBody == null)
            {
                outer.SetNextStateToMain();
                return;
            }

            float storedHeart = heart.GetHeart();

            BlastAttack blastAttack = new BlastAttack();
            blastAttack.position = characterBody.corePosition;
            blastAttack.baseDamage = (400f * damageStat) + ((storedHeart/100) * damageStat);
            blastAttack.damageType = DamageType.Stun1s;
            Log.Fatal($"Calculated Defiant Dash Explosion Damage: {blastAttack.baseDamage} (Stored Heart: {storedHeart})");
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
            blastAttack.Fire();

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

            Util.PlaySound("Play_imp_overlord_teleport_end", gameObject);

            heart.ConsumeHeart(storedHeart);
            healthComponent.Heal(Mathf.Clamp(healthComponent.health + storedHeart, 1f, healthComponent.fullHealth), default(ProcChainMask));
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}