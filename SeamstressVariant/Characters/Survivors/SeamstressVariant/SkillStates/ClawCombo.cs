using SeamstressVariant.Modules.BaseStates;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
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
        private bool hasLeftScissor = false;
        private bool hasRightScissor = false;
        private bool isScissorActive = false;

        // Scissor second-hit state
        private bool hasFiredScissor = false;
        private OverlapAttack scissorOverlapAttack;
        private GameObject _scissorSwingEffectInstance;

        public override void OnEnter()
        {
            // Read scissor state from the dedicated passive controller.
            var scissors = characterBody != null ? characterBody.GetComponent<ScissorController>() : null;
            if (scissors != null)
            {
                hasLeftScissor = scissors.HasLeftScissor;
                hasRightScissor = scissors.HasRightScissor;
            }

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

            // Apply scissor mechanics (may adjust baseDuration and timing percentages)
            ApplyScissorMechanics();

            base.OnEnter();

            // Set up the scissor second-hit after base.OnEnter() so damageStat and attack.isCrit are ready
            if (isScissorActive)
                SetupScissorAttack();
        }

        private void SetupScissorAttack()
        {
            // Cross-over: left swing hits with the right scissor hitbox and vice versa (matches OG)
            bool isLeftSwing = swingIndex % 2 == 0;
            string scissorHitboxGroup = isLeftSwing ? "Right" : "Left";

            scissorOverlapAttack = new OverlapAttack();
            scissorOverlapAttack.attacker = gameObject;
            scissorOverlapAttack.inflictor = gameObject;
            scissorOverlapAttack.teamIndex = GetTeam();
            scissorOverlapAttack.damage = (damageCoefficient * 2) * damageStat;
            scissorOverlapAttack.procCoefficient = procCoefficient;
            scissorOverlapAttack.hitEffectPrefab = hitEffectPrefab;
            scissorOverlapAttack.forceVector = bonusForce;
            scissorOverlapAttack.pushAwayForce = 450f;
            scissorOverlapAttack.hitBoxGroup = FindHitBoxGroup(scissorHitboxGroup);
            scissorOverlapAttack.isCrit = attack.isCrit;
            scissorOverlapAttack.impactSound = impactSound;
            scissorOverlapAttack.damageType = damageType;
        }

        private void ApplyScissorMechanics()
        {
            // Determine if this swing should use a scissor
            bool isLeftSwing = swingIndex % 2 == 0;
            isScissorActive = (isLeftSwing && hasLeftScissor) || (!isLeftSwing && hasRightScissor);

            if (isScissorActive)
            {
                // Scissors slow the state (matches OG Flurry baseScissorDuration = 2f).
                // Scale attackStart/EndPercentTime to preserve the claw's absolute fire times:
                //   original: 1.1f * 0.20 = 0.22s start, 1.1f * 0.40 = 0.44s end
                //   scissor:  2.0f * 0.11 = 0.22s start, 2.0f * 0.22 = 0.44s end
                // Scissor second-hit fires at 2.0f * 0.20 = 0.40s .. 2.0f * 0.40 = 0.80s
                baseDuration = 2f;
                attackStartPercentTime = 0.11f;
                attackEndPercentTime = 0.22f;
                // swingEffectPrefab stays as clawSlashEffect; scissor effect plays separately
            }

            // Toggle scissor model layer visibility based on active scissors
            Animator modelAnimator = GetModelAnimator();
            if (modelAnimator != null)
            {
                // Set layer weights for scissor visibility
                int scissorLayerIndex = modelAnimator.GetLayerIndex("Scissor, Override");
                if (scissorLayerIndex >= 0)
                {
                    float layerWeight = (hasLeftScissor || hasRightScissor) ? 1f : 0f;
                    modelAnimator.SetLayerWeight(scissorLayerIndex, layerWeight);
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!isScissorActive || scissorOverlapAttack == null) return;

            // Scissor second-hit window: 20%-40% of the (slowed) duration = 0.40s..0.80s at base speed
            float scissorStart = duration * 0.2f;
            float scissorEnd   = duration * 0.4f;

            bool scissorFireStarted = stopwatch >= scissorStart;
            bool scissorFireEnded   = stopwatch >= scissorEnd;

            if ((scissorFireStarted && !scissorFireEnded) || (scissorFireStarted && scissorFireEnded && !hasFiredScissor))
            {
                if (!hasFiredScissor)
                {
                    PlayScissorSwingEffect();
                    hasFiredScissor = true;
                }
                if (isAuthority)
                    scissorOverlapAttack.Fire();
            }
        }

        private void PlayScissorSwingEffect()
        {
            // Swing SFX — pitched by attack speed to match OG EnterAttack() behaviour
            Util.PlayAttackSpeedSound("sfx_seamstress_swing_scissor", gameObject, attackSpeedStat);

            bool isLeftSwing = swingIndex % 2 == 0;
            // Use the wider muzzle to match the scissor hitbox position
            string scissorMuzzle = isLeftSwing ? "SwingLeft" : "SwingRight";
            Transform muzzle = FindModelChild(scissorMuzzle);
            if (muzzle != null && SeamstressAssets.scissorsSlashEffect != null)
            {
                if (_scissorSwingEffectInstance != null)
                    EntityState.Destroy(_scissorSwingEffectInstance);
                _scissorSwingEffectInstance = UnityEngine.Object.Instantiate(SeamstressAssets.scissorsSlashEffect, muzzle);
            }
        }

        protected override void PlayAttackAnimation()
        {
            // When scissors are active, `duration` is the extended scissor state duration (2s).
            // The animation must play at claw speed (1.1s) so it stays in sync with the claw hit —
            // matching OG where `duration` (claw) and `scissorDuration` (state length) are separate.
            float animDuration = isScissorActive ? (1.1f / attackSpeedStat) : duration;
            PlayCrossfade("Gesture, Override", swingIndex % 2 == 0 ? "Slash1" : "Slash2", playbackRateParam, animDuration, 0.1f * animDuration);
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
            if (_scissorSwingEffectInstance != null)
                EntityState.Destroy(_scissorSwingEffectInstance);
            base.OnExit();
        }
    }
}
