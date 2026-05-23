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

        private BleedingHeartComponent heart;
        private HeartOverlayController heartOverlayController;
        private float nextDrainAt;
        private float startupFreezeEndTime;
        private float currentDrainPerTick;
        private bool startupFreezeActive;
        private bool canReactivate;
        private bool transitioningToReactivate;
        private bool fired;

        private bool CanExitState()
        {
            return isAuthority || NetworkServer.active;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            heart = GetComponent<BleedingHeartComponent>();
            heartOverlayController = GetComponent<HeartOverlayController>();
            nextDrainAt = heartDrainInterval;
            currentDrainPerTick = heartDrainPerTick;
            startupFreezeEndTime = startupFreezeDuration;
            startupFreezeActive = startupFreezeDuration > 0f;
            canReactivate = false;
            transitioningToReactivate = false;

            if (startupFreezeActive && heart != null)
            {
                heart.RequestSetDefiantStartupFreezeActive(true);
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

            if (heart != null)
            {
                heart.RequestSetDefiantStartupFreezeActive(false);
            }
        }

        private void EnterSustainedPhase()
        {
            if (heart == null)
            {
                if (CanExitState())
                {
                    outer.SetNextStateToMain();
                }
                return;
            }

            if (heartOverlayController != null)
            {
                heartOverlayController.SetHeartDrainActive(true);
            }

            if (GetModelAnimator() && (NetworkServer.active || isAuthority))
            {
                PlayAnimation("Gesture, Override", "BufferEmpty");
            }

            heart.RequestSetDefianceVisualsActive(true);

            if (!heart.CanSustainDefiantHeart())
            {
                if (CanExitState())
                {
                    outer.SetNextStateToMain();
                }
                return;
            }
        }

        private void UpdateSustainedPhase()
        {
            if (heart == null)
            {
                if (CanExitState())
                {
                    outer.SetNextStateToMain();
                }
                return;
            }

            if (!heart.CanSustainDefiantHeart())
            {
                if (CanExitState())
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
                    if (CanExitState())
                    {
                        outer.SetNextStateToMain();
                    }
                }
            }
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
            Log.Warning("Exiting Defiant Heart state.");

            EndStartupFreeze();

            if (heartOverlayController != null)
            {
                heartOverlayController.SetHeartDrainActive(false);
            }

            if (NetworkServer.active && characterBody)
            {
                DefianceSpecialController specialController = GetComponent<DefianceSpecialController>();
                GenericSkill specialSkill = skillLocator != null ? skillLocator.special : null;
                if (specialController != null && specialController.ConsumeForcedDefianceSession() && specialSkill != null)
                {   
                    Log.Debug("Defiant Heart onExit. Stocks:" + specialSkill.stock);
                    specialSkill.DeductStock(1);
                    Log.Debug("Defiant Heart onExit after deduct. Stocks:" + specialSkill.stock);
                    RemoveDefiance();
                }
            }

            if (heart != null)
            {
                heart.RequestSetDefianceVisualsActive(false);
            }

            base.OnExit();
        }

        private void RemoveDefiance()
        {
            int defianceCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);
            if (defianceCount > 0 && !transitioningToReactivate)
            {
                characterBody.RemoveBuff(SeamstressVariantBuffs.defianceBuff);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}
