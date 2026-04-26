using SeamstressVariant.Modules.BaseStates;
using SeamstressMod.Seamstress.Content;
using EntityStates;
using RoR2;
using UnityEngine;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    // Port of the original Seamstress Flurry: a stepped 1-2 claw slash combo.
    public class ClawCombo : BaseMeleeAttack
    {
        private GameObject _swingEffectInstance;
        public override void OnEnter()
        {
            hitboxGroupName = "Sword";

            damageType = DamageTypeCombo.GenericPrimary;
            damageCoefficient = SeamstressVariantStaticValues.swordDamageCoefficient;
            procCoefficient = 1f;
            pushForce = 300f;
            bonusForce = Vector3.zero;
            baseDuration = 1.1f;

            attackStartPercentTime = 0.2f;
            attackEndPercentTime = 0.4f;
            earlyExitPercentTime = 0.5f;

            hitStopDuration = 0.05f;
            attackRecoil = 2f;
            hitHopVelocity = 8f;

            swingSoundString = "sfx_seamstress_swing";
            hitSoundString = "";
            muzzleString = swingIndex % 2 == 0 ? "SwingLeftSmall" : "SwingRightSmall";
            playbackRateParam = "Slash.playbackRate";
            swingEffectPrefab = SeamstressAssets.clawSlashEffect;
            hitEffectPrefab = SeamstressAssets.scissorsHitImpactEffect;

            impactSound = SeamstressAssets.scissorsHitSoundEvent.index;

            base.OnEnter();
        }

        protected override void PlayAttackAnimation()
        {
            PlayCrossfade("Gesture, Override", swingIndex % 2 == 0 ? "Slash1" : "Slash2", playbackRateParam, duration, 0.1f * duration);
        }

        protected override void PlaySwingEffect()
        {
            // Orient the swing pivot toward the aim direction before the overlap fires
            Ray aimRay = GetAimRay();
            Transform pivot = FindModelChild("SwingPivot");
            if (pivot != null)
            {
                Vector3 dir = aimRay.direction;
                dir.y = Mathf.Max(dir.y, dir.y * 0.5f);
                pivot.rotation = Util.QuaternionSafeLookRotation(dir);
            }

            // clawSlashEffect has no EffectDef so SimpleMuzzleFlash would silently no-op.
            // Instantiate it directly as a child of the muzzle transform (matches OG behaviour).
            Transform muzzle = FindModelChild(muzzleString);
            if (muzzle != null && swingEffectPrefab != null)
            {
                if (_swingEffectInstance != null)
                    EntityState.Destroy(_swingEffectInstance);
                _swingEffectInstance = UnityEngine.Object.Instantiate(swingEffectPrefab, muzzle);
            }
        }

        protected override void OnHitEnemyAuthority()
        {
            base.OnHitEnemyAuthority();
        }

        public override void OnExit()
        {
            if (_swingEffectInstance != null)
                EntityState.Destroy(_swingEffectInstance);
            base.OnExit();
        }
    }
}
