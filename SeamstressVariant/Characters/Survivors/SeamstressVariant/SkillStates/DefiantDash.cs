using EntityStates;
using RoR2;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using SeamstressMod.Seamstress.Content;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    public class DefiantDash : BaseSkillState
    {
        // Dash phase parameters
        public float baseDuration = 0.5f;
        public float dashPower = 5f;

        // Sustained phase parameters (from DefiantHeart)
        public static float heartDrainPerTick = 5f;
        public static float heartDrainInterval = 0.25f;

        // OG VFX assets — null-safe, fall back to no-op if unavailable.
        private GameObject impDashEffect;
        private GameObject smallBlinkEffect;
        private GameObject bloodExplosionEffect;
        private GameObject bloodSplatterEffect;
        private Material destealthMaterial;
        private Color mainColor;

        // Defiance visual state
        private GameObject trailEffectR;
        private GameObject trailEffectL;
        private bool sustainedVisualActive;

        // Dash phase state
        private Vector3 dashVector;
        private bool hasDelayed;
        private bool dashCompleted;

        // Sustained phase state
        private BleedingHeartComponent heart;
        private SetStateOnHurt setStateOnHurt;
        private float nextDrainAt;
        private bool canReactivate;
        private bool originalCanBeHitStunned;
        private bool originalCanBeStunned;
        private bool originalCanBeFrozen;
        private bool originalCanBeTaunted;

        public override void OnEnter()
        {
            base.OnEnter();

            // Fetch OG assets lazily at runtime so load-order doesn't matter.
            impDashEffect = SeamstressAssets.impDashEffect;
            smallBlinkEffect = SeamstressAssets.smallBlinkEffect;
            bloodExplosionEffect = SeamstressAssets.bloodExplosionEffect;
            bloodSplatterEffect = SeamstressAssets.bloodSplatterEffect;
            destealthMaterial = SeamstressAssets.destealthMaterial;
            mainColor = SeamstressAssets.coolRed;

            // Dash phase initialization
            hasDelayed = false;
            dashCompleted = false;

            // Sustained phase initialization
            heart = GetComponent<BleedingHeartComponent>();
            setStateOnHurt = GetComponent<SetStateOnHurt>();
            nextDrainAt = heartDrainInterval;
            canReactivate = false;

            if (characterMotor)
            {
                characterMotor.disableAirControlUntilCollision = false;
            }

            // Destealth overlay — same as OG.
            Transform modelTransform = GetModelTransform();
            if (modelTransform)
            {
                Animator anim = modelTransform.GetComponent<Animator>();
                if (destealthMaterial && anim)
                {
                    TemporaryOverlayInstance overlay = TemporaryOverlayManager.AddOverlay(gameObject);
                    overlay.duration = 1.2f;
                    overlay.destroyComponentOnEnd = true;
                    overlay.originalMaterial = destealthMaterial;
                    overlay.inspectorCharacterModel = anim.gameObject.GetComponent<CharacterModel>();
                    overlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    overlay.animateShaderAlpha = true;
                }
            }

            if (characterMotor)
            {
                characterMotor.velocity = Vector3.zero;
            }

            PlayCrossfade("FullBody, Override", "RipHeart", "Dash.playbackRate",
                baseDuration / attackSpeedStat * 1.8f,
                baseDuration / attackSpeedStat * 0.05f);

            Util.PlayAttackSpeedSound("Play_imp_overlord_attack2_tell", gameObject, attackSpeedStat);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            // Dash phase active
            if (!dashCompleted)
            {
                UpdateDashPhase();
                return;
            }

            // Sustained Defiance phase active
            if (isAuthority)
            {
                UpdateSustainedPhase();
            }
        }

        // ============ Dash Phase ============

        private void UpdateDashPhase()
        {
            // Phase 1: windup — wait until halfway through before launching.
            if (!hasDelayed)
            {
                if (characterMotor)
                {
                    characterMotor.velocity.y = 0f;
                }

                if (fixedAge > baseDuration / 2f / attackSpeedStat)
                {
                    // Apply Defiance as the dash begins so the buff is immediately visible/active.
                    if (NetworkServer.active && characterBody)
                    {
                        characterBody.AddBuff(SeamstressVariantBuffs.defianceBuff);
                    }

                    dashVector = inputBank ? inputBank.aimDirection : characterDirection.forward;

                    Util.PlaySound("sfx_seamstress_dash", gameObject);
                    Util.PlaySound("sfx_seamstress_chains", gameObject);

                    // Spawn departure VFX.
                    if (bloodExplosionEffect)
                    {
                        EffectManager.SpawnEffect(bloodExplosionEffect, new EffectData
                        {
                            origin = transform.position,
                            rotation = Quaternion.identity,
                            scale = 0.5f
                        }, false);
                    }

                    EffectData dashEffectData = new EffectData
                    {
                        origin = characterBody.corePosition,
                        rotation = Util.QuaternionSafeLookRotation(dashVector),
                        scale = 3f
                    };

                    if (impDashEffect)
                    {
                        EffectManager.SpawnEffect(impDashEffect, dashEffectData, false);
                    }

                    if (smallBlinkEffect)
                    {
                        EffectManager.SpawnEffect(smallBlinkEffect, dashEffectData, false);
                    }

                    // Blood splatter on nearby world geometry.
                    if (bloodSplatterEffect)
                    {
                        Vector3 splatterOrigin = transform.localPosition;
                        RaycastHit hit;
                        if (Physics.Raycast(splatterOrigin, Vector3.one, out hit, 10f, LayerMask.GetMask("World")))
                        {
                            splatterOrigin = hit.point;
                        }
                        EffectManager.SpawnEffect(bloodSplatterEffect, new EffectData
                        {
                            origin = splatterOrigin,
                            rotation = Quaternion.identity,
                            color = (Color32)mainColor
                        }, false);
                    }

                    // Launch.
                    if (characterMotor)
                    {
                        characterMotor.velocity.y = 0f;
                        characterMotor.velocity += dashVector * (dashPower * (moveSpeedStat + 1f));
                    }

                    hasDelayed = true;
                }

                return;
            }

            // Phase 2: dashing — steer and wait for duration.
            if (characterDirection)
            {
                characterDirection.forward = dashVector;
            }

            if (characterBody)
            {
                characterBody.isSprinting = true;
            }

            if (fixedAge >= baseDuration / attackSpeedStat)
            {
                // Spawn arrival VFX.
                if (impDashEffect)
                {
                    EffectManager.SpawnEffect(impDashEffect, new EffectData
                    {
                        origin = characterBody.corePosition,
                        rotation = Util.QuaternionSafeLookRotation(dashVector),
                        scale = 3f
                    }, false);
                }

                // Transition to sustained Defiance phase.
                dashCompleted = true;

                // Initialize sustained phase.
                EnterSustainedPhase();
            }
        }

        // ============ Sustained Defiance Phase (formerly DefiantHeart) ============

        private void EnterSustainedPhase()
        {
            if (heart == null || !heart.CanSustainDefiantHeart())
            {
                outer.SetNextStateToMain();
                return;
            }

            if (NetworkServer.active && characterBody)
            {
                ApplyStateImmunities();
                RemoveDebuffs();
            }

            if (GetModelAnimator())
            {
                PlayAnimation("Gesture, Override", "BufferEmpty");
                ApplyDefianceVisuals();
            }
        }

        private void UpdateSustainedPhase()
        {
            if (heart == null || !heart.CanSustainDefiantHeart())
            {
                outer.SetNextStateToMain();
                return;
            }

            if (NetworkServer.active)
            {
                RemoveDebuffs();
            }

            if (!canReactivate)
            {
                canReactivate = inputBank == null || !inputBank.skill4.down;
            }

            if (canReactivate && inputBank != null && inputBank.skill4.down)
            {
                ReactivateSustainedPhase();
                return;
            }

            if (fixedAge >= nextDrainAt)
            {
                nextDrainAt += heartDrainInterval;

                if (NetworkServer.active)
                {
                    heart.ConsumeHeart(heartDrainPerTick);
                }

                if (!heart.CanSustainDefiantHeart())
                {
                    outer.SetNextStateToMain();
                }
            }
        }

        private void ReactivateSustainedPhase()
        {

            if (!NetworkServer.active || heart == null || healthComponent == null)
            {
                outer.SetNextStateToMain();
                return;
            }

            float storedHeart = heart.GetHeart();

            heart.ConsumeHeart(storedHeart);
            healthComponent.health = Mathf.Clamp(healthComponent.health + storedHeart, 1f, healthComponent.fullHealth);

            outer.SetNextStateToMain();
        }

        private void RemoveDebuffs()
        {
            if (characterBody == null)
            {
                return;
            }

            foreach (BuffIndex buffIndex in BuffCatalog.debuffBuffIndices)
            {
                if (characterBody.HasBuff(buffIndex))
                {
                    characterBody.SetBuffCount(buffIndex, 0);
                }
            }
        }

        private void ApplyStateImmunities()
        {
            if (setStateOnHurt == null)
            {
                return;
            }

            originalCanBeHitStunned = setStateOnHurt.canBeHitStunned;
            originalCanBeStunned = setStateOnHurt.canBeStunned;
            originalCanBeFrozen = setStateOnHurt.canBeFrozen;
            originalCanBeTaunted = setStateOnHurt.canBeTaunted;

            setStateOnHurt.canBeHitStunned = false;
            setStateOnHurt.canBeStunned = false;
            setStateOnHurt.canBeFrozen = false;
            setStateOnHurt.canBeTaunted = false;
            setStateOnHurt.Cleanse();
        }

        private void RestoreStateImmunities()
        {
            if (setStateOnHurt == null)
            {
                return;
            }

            setStateOnHurt.canBeHitStunned = originalCanBeHitStunned;
            setStateOnHurt.canBeStunned = originalCanBeStunned;
            setStateOnHurt.canBeFrozen = originalCanBeFrozen;
            setStateOnHurt.canBeTaunted = originalCanBeTaunted;
        }

        private void ApplyDefianceVisuals()
        {
            sustainedVisualActive = true;

            // Activate "Body, Butchered" animation layer
            Animator anim = GetModelAnimator();
            if (anim)
            {
                int layerIdx = anim.GetLayerIndex("Body, Butchered");
                if (layerIdx >= 0)
                {
                    anim.SetLayerWeight(layerIdx, 1f);
                }
            }

            // Hide heart child
            Transform heartModel = FindModelChild("HeartModel");
            if (heartModel)
            {
                heartModel.gameObject.SetActive(false);
            }

            // Entry flash overlay
            Transform modelTransform = GetModelTransform();
            if (modelTransform && destealthMaterial)
            {
                TemporaryOverlayInstance entryOverlay = TemporaryOverlayManager.AddOverlay(gameObject);
                entryOverlay.duration = 1f;
                entryOverlay.destroyComponentOnEnd = true;
                entryOverlay.originalMaterial = destealthMaterial;
                entryOverlay.inspectorCharacterModel = modelTransform.GetComponent<CharacterModel>();
                entryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                entryOverlay.animateShaderAlpha = true;
            }

            // Spawn hand trail effects
            if (SeamstressAssets.trailEffectHands)
            {
                Transform handR = FindModelChild("HandR");
                Transform handL = FindModelChild("HandL");
                if (handR)
                {
                    trailEffectR = Object.Instantiate(SeamstressAssets.trailEffectHands, handR);
                }
                if (handL)
                {
                    trailEffectL = Object.Instantiate(SeamstressAssets.trailEffectHands, handL);
                }
            }
        }

        private void RemoveDefianceVisuals()
        {
            if (!sustainedVisualActive)
            {
                return;
            }
            sustainedVisualActive = false;

            // Deactivate "Body, Butchered" animation layer
            Animator anim = GetModelAnimator();
            if (anim)
            {
                int layerIdx = anim.GetLayerIndex("Body, Butchered");
                if (layerIdx >= 0)
                {
                    anim.SetLayerWeight(layerIdx, 0f);
                }
            }

            // Restore heart child
            Transform heartModel = FindModelChild("HeartModel");
            if (heartModel)
            {
                heartModel.gameObject.SetActive(true);
            }

            // Destroy hand trails
            if (trailEffectR)
            {
                Object.Destroy(trailEffectR);
                trailEffectR = null;
            }
            if (trailEffectL)
            {
                Object.Destroy(trailEffectL);
                trailEffectL = null;
            }

            // Exit overlay
            Transform modelTransform = GetModelTransform();
            if (modelTransform && destealthMaterial)
            {
                TemporaryOverlayInstance exitOverlay = TemporaryOverlayManager.AddOverlay(gameObject);
                exitOverlay.duration = 1f;
                exitOverlay.destroyComponentOnEnd = true;
                exitOverlay.originalMaterial = destealthMaterial;
                exitOverlay.inspectorCharacterModel = modelTransform.GetComponent<CharacterModel>();
                exitOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                exitOverlay.animateShaderAlpha = true;
            }

            // End sound
            Util.PlaySound("Play_voidman_transform_return", gameObject);
        }

        public override void OnExit()
        {
            if (NetworkServer.active)
            {
                RestoreStateImmunities();

                if (characterBody)
                {
                    int defianceCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);
                    if (defianceCount > 0)
                    {
                        Log.Fatal("DEFIANT HEART ONEXIT CALL");
                        characterBody.SetBuffCount(SeamstressVariantBuffs.defianceBuff.buffIndex, 0);
                        RemoveDefianceVisuals();
                    }
                }
            }

            // Bleed off velocity if dash did not complete (e.g. interrupted).
            if (!hasDelayed && characterMotor)
            {
                characterMotor.velocity *= 0.2f;
            }

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}
