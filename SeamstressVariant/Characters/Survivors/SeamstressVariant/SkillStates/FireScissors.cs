using EntityStates;
using RoR2;
using RoR2.Projectile;
using SeamstressMod.Seamstress.Content;
using SeamstressVariant.Survivors.SeamstressVariant;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using UnityEngine;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    /// <summary>
    /// Secondary skill — throws one scissor blade that homes toward a locked target.
    /// On cast a BullseyeSearch locks the nearest enemy within <see cref="trackingAngle"/> degrees
    /// and <see cref="trackingRange"/> metres of the aim ray; the projectile steers toward it.
    /// Falls back to straight-line fire if no valid target is found.
    /// Which scissor (L or R) is chosen based on current scissor buff state.
    /// Priority: both → L; only R → R; only L → L; neither → L.
    /// No health cost.
    /// </summary>
    public class FireScissors : BaseSkillState
    {
        public static float baseDuration = 0.5f;
        public static float damageCoefficient = SeamstressVariantStaticValues.scissorDamageCoefficient;
        public static float procCoefficient = 1f;
        public static float force = 0f;

        private float duration;
        private bool hasFired;
        private bool _firingLeft;
        private Ray aimRay;
        private string chosenAnim;
        private string muzzleString;
        private GameObject projectilePrefab;
        private HurtBox _lockedTarget;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            aimRay = GetAimRay();
            StartAimMode(aimRay);

            // Snapshot the current target from the persistent tracker (same cone the indicator shows).
            var tracker = characterBody.GetComponent<SeamstressTracker>();
            _lockedTarget = tracker != null ? tracker.GetTrackingTarget() : null;

            bool hasLeft  = characterBody.HasBuff(SeamstressVariantBuffs.scissorLeftBuff);
            bool hasRight = characterBody.HasBuff(SeamstressVariantBuffs.scissorRightBuff);

            if (hasRight && !hasLeft)
            {
                chosenAnim       = "FireScissorR";
                muzzleString     = "SwingLeftSmall";
                projectilePrefab = SeamstressVariantAssets.scissorRProjectile;
                _firingLeft      = false;
            }
            else
            {
                // Both, only-left, or neither → default to left.
                chosenAnim       = "FireScissorL";
                muzzleString     = "SwingRightSmall";
                projectilePrefab = SeamstressVariantAssets.scissorLProjectile;
                _firingLeft      = true;
            }

            Animator modelAnimator = GetModelAnimator();
            if (modelAnimator)
            {
                PlayCrossfade("Gesture, Override", chosenAnim, "Slash.playbackRate", duration, 0.05f);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!hasFired)
            {
                Fire();
                hasFired = true;
            }

            if (fixedAge >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        private void Fire()
        {
            Util.PlaySound("Play_item_lunar_specialReplace_explode", gameObject);
            Util.PlaySound("Play_imp_overlord_attack1_throw", gameObject);

            // Spawn the imp-dash muzzle flash at the scissor hand position.
            Transform muzzle = FindModelChild(muzzleString);
            if (muzzle != null && SeamstressAssets.impDashEffect != null)
            {
                EffectData effectData = new EffectData
                {
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                    origin   = muzzle.position,
                    scale    = 0.5f
                };
                EffectManager.SpawnEffect(SeamstressAssets.impDashEffect, effectData, true);
            }

            if (isAuthority)
            {
                // Aim toward the locked target when available; otherwise use the raw aim ray.
                Quaternion fireRotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                GameObject targetObject = null;
                if (_lockedTarget)
                {
                    fireRotation = Util.QuaternionSafeLookRotation(
                        _lockedTarget.transform.position - aimRay.origin);
                    targetObject = _lockedTarget.healthComponent.body.gameObject;
                }

                ProjectileManager.instance.FireProjectile(
                    projectilePrefab,
                    aimRay.origin,
                    fireRotation,
                    gameObject,
                    damageStat * damageCoefficient,
                    force,
                    Util.CheckRoll(critStat, characterBody.master),
                    DamageColorIndex.Default,
                    targetObject);
            }

            GetComponent<ScissorController>()?.OnScissorFired(_firingLeft);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
