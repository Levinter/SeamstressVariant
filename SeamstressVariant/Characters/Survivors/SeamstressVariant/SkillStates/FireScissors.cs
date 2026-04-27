using EntityStates;
using RoR2;
using RoR2.Projectile;
using SeamstressVariant.Survivors.SeamstressVariant;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using UnityEngine;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    /// <summary>
    /// Secondary skill — throws one scissor blade in a straight line.
    /// Which scissor (L or R) is chosen based on current scissor buff state.
    /// Priority: both → L; only R → R; only L → L; neither → L.
    /// No health cost. No homing.
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
        private GameObject projectilePrefab;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            aimRay = GetAimRay();
            StartAimMode(aimRay);

            bool hasLeft  = characterBody.HasBuff(SeamstressVariantBuffs.scissorLeftBuff);
            bool hasRight = characterBody.HasBuff(SeamstressVariantBuffs.scissorRightBuff);

            if (hasRight && !hasLeft)
            {
                chosenAnim       = "FireScissorR";
                projectilePrefab = SeamstressVariantAssets.scissorRProjectile;
                _firingLeft      = false;
            }
            else
            {
                // Both, only-left, or neither → default to left.
                chosenAnim       = "FireScissorL";
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

            if (isAuthority)
            {
                ProjectileManager.instance.FireProjectile(
                    projectilePrefab,
                    aimRay.origin,
                    Util.QuaternionSafeLookRotation(aimRay.direction),
                    gameObject,
                    damageStat * damageCoefficient,
                    force,
                    Util.CheckRoll(critStat, characterBody.master));
            }

            GetComponent<ScissorController>()?.OnScissorFired(_firingLeft);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
