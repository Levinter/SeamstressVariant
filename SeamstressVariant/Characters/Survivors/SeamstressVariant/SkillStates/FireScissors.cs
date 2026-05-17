using EntityStates;
using RoR2;
using RoR2.Projectile;
using SeamstressMod.Seamstress.Content;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using UnityEngine;
using OGSeamstressController = SeamstressMod.Seamstress.Components.SeamstressController;

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
        public static float procCoefficient = 1f;
        public static float force = 0f;

        private float duration;
        private bool hasFired;
        private bool _firingLeft;
        private Ray aimRay;
        private string chosenAnim;
        private string muzzleString;
        private GameObject projectilePrefab;
        private GameObject scissorFiringPrefab = SeamstressAssets.impDashEffect;
        private HurtBox _lockedTarget;
        private ScissorController _scissors;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            aimRay = GetAimRay();
            StartAimMode(aimRay);

            // Match OG Seamstress: use alternate firing VFX while the blue variant is active.
            OGSeamstressController seamstressController = GetComponent<OGSeamstressController>();
            if (seamstressController != null && seamstressController.blue && SeamstressAssets.impDashEffect2 != null)
            {
                scissorFiringPrefab = SeamstressAssets.impDashEffect2;
            }

            // Snapshot the current target from the persistent tracker (same cone the indicator shows).
            var tracker = characterBody.GetComponent<SeamstressTracker>();
            _lockedTarget = tracker != null ? tracker.GetTrackingTarget() : null;

            _scissors = characterBody != null ? characterBody.GetComponent<ScissorController>() : null;
            bool hasLeft = _scissors != null && _scissors.HasLeftScissor;
            bool hasRight = _scissors != null && _scissors.HasRightScissor;

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

            Log.Info($"FireScissors: enter side={(_firingLeft ? "L" : "R")} hasLeft={hasLeft} hasRight={hasRight} target={(_lockedTarget != null)}");

            Animator modelAnimator = GetModelAnimator();
            if (modelAnimator)
            {
                PlayCrossfade("Gesture, Override", chosenAnim, "Slash.playbackRate", duration, 0.05f);
            }
        }

        public override void OnExit()
        {
            Log.Debug($"FireScissors: exit age={fixedAge:F2} fired={hasFired}");
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
            if (projectilePrefab == null)
            {
                Log.Error($"FireScissors: missing projectile prefab for side={(_firingLeft ? "L" : "R")}");
                return;
            }

            Util.PlaySound("Play_item_lunar_specialReplace_explode", gameObject);
            Util.PlaySound("Play_imp_overlord_attack1_throw", gameObject);

            // Spawn the imp-dash muzzle flash at the scissor hand position.
            Transform muzzle = FindModelChild(muzzleString);
            if (muzzle != null && scissorFiringPrefab != null)
            {
                EffectData effectData = new EffectData
                {
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                    origin   = muzzle.position,
                    scale    = 0.5f
                };
                EffectManager.SpawnEffect(scissorFiringPrefab, effectData, true);
            }

            if (isAuthority)
            {
                // Launch along the aim ray. Homing is handled by projectile steering components.
                Quaternion fireRotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                GameObject targetObject = null;
                if (_lockedTarget && _lockedTarget.healthComponent != null && _lockedTarget.healthComponent.body != null)
                {
                    targetObject = _lockedTarget.healthComponent.body.gameObject;
                }

                ProjectileManager.instance.FireProjectile(
                    projectilePrefab,
                    aimRay.origin,
                    fireRotation,
                    gameObject,
                    damageStat,
                    force,
                    Util.CheckRoll(critStat, characterBody.master),
                    DamageColorIndex.Default,
                    targetObject);

                if (_scissors != null)
                {
                    _scissors.NotifyScissorFired(_firingLeft);
                }
                else
                {
                    Log.Warning($"FireScissors: missing ScissorController while firing side={(_firingLeft ? "L" : "R")}");
                }

                Log.Info($"FireScissors: fired side={(_firingLeft ? "L" : "R")} target={targetObject != null} critRollFromStat={critStat:F2}");
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
