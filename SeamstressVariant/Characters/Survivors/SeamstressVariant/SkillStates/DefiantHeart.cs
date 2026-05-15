using EntityStates;
using RoR2;
using RoR2.Skills;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using SeamstressMod.Seamstress.Content;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    public class DefiantHeart : BaseSkillState
    {
        public static float heartDrainPerTick = 1f;
        public static float heartDrainInterval = 0.25f;
        public static float startupFreezeDuration = 1.0f;
        public static float animDuration = 1.5f;
        private Material destealthMaterial;
        private GameObject trailEffectR;
        private GameObject trailEffectL;
        private bool sustainedVisualActive;
        private TemporaryOverlayInstance persistentDefianceOverlay;
        private EffectManagerHelper defianceBleedEffect;

        private BleedingHeartComponent heart;
        private HeartOverlayController heartOverlayController;
        private float nextDrainAt;
        private float startupFreezeEndTime;
        private float currentDrainPerTick;
        private bool startupFreezeActive;
        private bool canReactivate;
        private bool transitioningToReactivate;
        private bool fired;
        private CameraTargetParams.CameraParamsOverrideHandle transformCameraParamsHandle;
        private bool startupMoveLockApplied;
        private bool cachedDisableAirControlUntilCollision;
        private bool cachedDisableAirControlUntilCollisionValid;
        private bool startupAntiGravityApplied;
        private bool startupFlightApplied;

        public override void OnEnter()
        {
            base.OnEnter();

            destealthMaterial = SeamstressAssets.destealthMaterial;
            heart = GetComponent<BleedingHeartComponent>();
            heartOverlayController = GetComponent<HeartOverlayController>();
            nextDrainAt = heartDrainInterval;
            currentDrainPerTick = heartDrainPerTick;
            startupFreezeEndTime = startupFreezeDuration;
            startupFreezeActive = startupFreezeDuration > 0f;
            canReactivate = false;
            transitioningToReactivate = false;
            startupMoveLockApplied = false;
            cachedDisableAirControlUntilCollisionValid = false;
            startupAntiGravityApplied = false;
            startupFlightApplied = false;

            if (startupFreezeActive)
            {
                ApplyStartupGravityLock();
                ApplyStartupMovementLock();
            }

            if (NetworkServer.active && characterBody)
            {
                characterBody.AddBuff(SeamstressVariantBuffs.defianceBuff);
            }

            

            PlayCrossfade("FullBody, Override", "RipHeart", "Dash.playbackRate", animDuration, 0.05f);

            Util.PlaySound("Play_imp_overlord_attack2_tell", gameObject);

            ApplyTransformEnterEffect();
            //StartTransformCameraOverride();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (startupFreezeActive)
            {
                ApplyStartupMovementLock();

                if (fixedAge >= startupFreezeEndTime)
                {
                    EndStartupFreeze();
                }
            }

            if (!fired && fixedAge >= animDuration * 0.5f)
            {
                fired = true;
                
                EnterSustainedPhase();
            }

            if (fired && (isAuthority || NetworkServer.active))
            {
                UpdateSustainedPhase();
            }
        }

        private void EndStartupFreeze()
        {
            if (!startupFreezeActive)
            {
                return;
            }

            startupFreezeActive = false;
            ReleaseStartupMovementLock();
            ReleaseStartupGravityLock();
        }

        private void ApplyStartupGravityLock()
        {
            if (!characterMotor)
            {
                return;
            }

            if (!startupAntiGravityApplied)
            {
                CharacterGravityParameters gravityParameters = characterMotor.gravityParameters;
                gravityParameters.channeledAntiGravityGranterCount++;
                characterMotor.gravityParameters = gravityParameters;
                startupAntiGravityApplied = true;
            }

            if (!startupFlightApplied)
            {
                CharacterFlightParameters flightParameters = characterMotor.flightParameters;
                flightParameters.channeledFlightGranterCount++;
                characterMotor.flightParameters = flightParameters;
                startupFlightApplied = true;
            }
        }

        private void ReleaseStartupGravityLock()
        {
            if (!characterMotor)
            {
                startupAntiGravityApplied = false;
                startupFlightApplied = false;
                return;
            }

            if (startupFlightApplied)
            {
                CharacterFlightParameters flightParameters = characterMotor.flightParameters;
                flightParameters.channeledFlightGranterCount--;
                characterMotor.flightParameters = flightParameters;
                startupFlightApplied = false;
            }

            if (startupAntiGravityApplied)
            {
                CharacterGravityParameters gravityParameters = characterMotor.gravityParameters;
                gravityParameters.channeledAntiGravityGranterCount--;
                characterMotor.gravityParameters = gravityParameters;
                startupAntiGravityApplied = false;
            }
        }

        private void ApplyStartupMovementLock()
        {
            if (characterMotor)
            {
                if (!startupMoveLockApplied)
                {
                    cachedDisableAirControlUntilCollision = characterMotor.disableAirControlUntilCollision;
                    cachedDisableAirControlUntilCollisionValid = true;
                    characterMotor.disableAirControlUntilCollision = true;
                    startupMoveLockApplied = true;
                }

                characterMotor.velocity = Vector3.zero;
            }

            if (characterDirection)
            {
                characterDirection.moveVector = Vector3.zero;
            }

            if (inputBank)
            {
                inputBank.moveVector = Vector3.zero;
            }
        }

        private void ReleaseStartupMovementLock()
        {
            if (characterMotor && startupMoveLockApplied && cachedDisableAirControlUntilCollisionValid)
            {
                characterMotor.disableAirControlUntilCollision = cachedDisableAirControlUntilCollision;
            }

            startupMoveLockApplied = false;
            cachedDisableAirControlUntilCollisionValid = false;
        }

        private void ApplyDefianceBleedEffect()
        {
            if (defianceBleedEffect)
            {
                return;
            }

            GameObject bleedEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/BleedEffect");
            if (!bleedEffectPrefab)
            {
                return;
            }

            defianceBleedEffect = EffectManager.GetAndActivatePooledEffect(bleedEffectPrefab, transform, true);
        }

        private void RemoveDefianceBleedEffect()
        {
            if (!defianceBleedEffect)
            {
                return;
            }

            if (defianceBleedEffect.OwningPool != null)
            {
                defianceBleedEffect.transform.SetParent(null);
                defianceBleedEffect.ReturnToPool();
            }
            else
            {
                Object.Destroy(defianceBleedEffect.gameObject);
            }

            defianceBleedEffect = null;
        }

        private void EnterSustainedPhase()
        {
            if (heart == null)
            {
                if (isAuthority)
                {
                    outer.SetNextStateToMain();
                }
                return;
            }

            if (!heart.CanSustainDefiantHeart())
            {
                if (isAuthority)
                {
                    outer.SetNextStateToMain();
                }
                return;
            }

            if (GetModelAnimator())
            {
                PlayAnimation("Gesture, Override", "BufferEmpty");
                ApplyDefianceVisuals();
            }

            if (heartOverlayController != null)
            {
                heartOverlayController.SetHeartDrainActive(true);
            }
        }

        private void UpdateSustainedPhase()
        {
            if (heart == null)
            {
                if (isAuthority)
                {
                    outer.SetNextStateToMain();
                }
                return;
            }

            if (!heart.CanSustainDefiantHeart())
            {
                if (isAuthority)
                {
                    outer.SetNextStateToMain();
                }
                return;
            }

            if (!canReactivate)
            {
                canReactivate = inputBank == null || !inputBank.skill4.down;
            }

            if (canReactivate && inputBank != null && inputBank.skill4.down)
            {
                transitioningToReactivate = true;

                if (isAuthority)
                {
                    outer.SetNextState(new HealingHeart());
                }
                return;
            }

            if (fixedAge >= nextDrainAt)
            {
                nextDrainAt += heartDrainInterval;

                if (NetworkServer.active)
                {
                    heart.ConsumeHeart(currentDrainPerTick);
                }

                currentDrainPerTick += 1f;

                if (!heart.CanSustainDefiantHeart())
                {
                    if (isAuthority)
                    {
                        outer.SetNextStateToMain();
                    }
                }
            }
        }

        private void ApplyDefianceVisuals()
        {
            if (sustainedVisualActive)
            {
                return;
            }

            sustainedVisualActive = true;

            ApplyDefianceBleedEffect();

            Animator anim = GetModelAnimator();
            if (anim)
            {
                if (destealthMaterial && persistentDefianceOverlay == null)
                {
                    persistentDefianceOverlay = TemporaryOverlayManager.AddOverlay(gameObject);
                    persistentDefianceOverlay.duration = 9999f;
                    persistentDefianceOverlay.destroyComponentOnEnd = true;
                    persistentDefianceOverlay.originalMaterial = destealthMaterial;
                    persistentDefianceOverlay.inspectorCharacterModel = anim.gameObject.GetComponent<CharacterModel>();
                    persistentDefianceOverlay.alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
                    persistentDefianceOverlay.animateShaderAlpha = true;
                }
            }

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
            RemoveDefianceBleedEffect();

            if (persistentDefianceOverlay != null)
            {
                persistentDefianceOverlay.Destroy();
                persistentDefianceOverlay = null;
            }

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

            /*if (SeamstressVariantAssets.defianceEndEffect && characterBody)
            {
                EffectManager.SpawnEffect(SeamstressVariantAssets.defianceEndEffect, new EffectData
                {
                    origin = characterBody.corePosition,
                    rotation = Quaternion.identity,
                    scale = 1f
                }, true);
            }*/

            //ApplyTransformExitEffect();

            Util.PlaySound("Play_voidman_transform_return", gameObject);
        }

        private void ApplyTransformEnterEffect()
        {
            Log.Debug("Attempting to apply transform enter effect.");
            if (!SeamstressVariantAssets.defiantTransformEnterEffect || !characterBody)
            {
                return;
            }

            float envelopeScale = 2.5f;
            Vector3 origin = characterBody.corePosition + Vector3.up * (characterBody.radius * 0.75f);

            EffectManager.SpawnEffect(SeamstressVariantAssets.defiantTransformEnterEffect, new EffectData
            {
                origin = origin,
                rotation = Quaternion.identity,
                scale = envelopeScale
            }, true);
        }

        private void ApplyTransformExitEffect()
        {
            if (!SeamstressVariantAssets.defiantTransformExitEffect || !characterBody)
            {
                return;
            }

            EffectManager.SpawnEffect(SeamstressVariantAssets.defiantTransformExitEffect, new EffectData
            {
                origin = characterBody.corePosition,
                rotation = Quaternion.identity,
                scale = 1f
            }, true);
        }

        public override void OnExit()
        {
            EndStartupFreeze();
            ReleaseStartupMovementLock();
            ReleaseStartupGravityLock();

            if (heartOverlayController != null)
            {
                heartOverlayController.SetHeartDrainActive(false);
            }

            if (NetworkServer.active && characterBody)
            {
                int defianceCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);
                if (defianceCount > 0 && !transitioningToReactivate)
                {
                    characterBody.SetBuffCount(SeamstressVariantBuffs.defianceBuff.buffIndex, 0);
                }

                DefianceSpecialController specialController = GetComponent<DefianceSpecialController>();
                GenericSkill specialSkill = skillLocator != null ? skillLocator.special : null;
                if (specialController != null && specialController.ConsumeForcedDefianceSession() && specialSkill != null)
                {
                    specialSkill.DeductStock(1);
                }
            }

            RemoveDefianceVisuals();

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}
