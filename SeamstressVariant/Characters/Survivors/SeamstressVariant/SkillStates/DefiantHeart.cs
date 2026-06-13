using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using SeamstressMod.Seamstress.Content;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
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
        private bool fired;
        private bool exitingDueToHeartExhaustion;

        private bool CanExitState()
        {
            return isAuthority;
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

            if (isAuthority)
            {
                heart.RequestSetDefiantStartupFreezeActive(true);
            }

            PlayCrossfade("FullBody, Override", "RipHeart", "Dash.playbackRate", animDuration, 0.05f);
            Util.PlaySound("Play_imp_overlord_attack2_tell", gameObject);

            ApplyTransformEnterEffect();

            Log.Warning("Entered Defiant Heart state.");
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

            if (fired)
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

            if (isAuthority)
            {
                heart.RequestSetDefiantStartupFreezeActive(false);
            }
        }

        private void EnterSustainedPhase()
        {
            if (heartOverlayController != null)
            {
                heartOverlayController.SetHeartDrainActive(true);
            }

            PlayAnimation("Gesture, Override", "BufferEmpty");

            if (isAuthority)
            {
                heart.RequestSetDefianceVisualsActive(true);
            }

            if (isAuthority && !heart.CanSustainDefiantHeart())
            {
                outer.SetNextStateToMain();
            }
        }

        private void UpdateSustainedPhase()
        {

            if (isAuthority && !heart.CanSustainDefiantHeart())
            {
                outer.SetNextStateToMain();
                return;
            }

            if (isAuthority && inputBank.skill4.justPressed)
            {
                Log.Warning("Trying to transition from Defiant Heart to Healing Heart.");
                outer.SetNextState(new HealingHeart());
                return;
            }

            if (fixedAge >= nextDrainAt)
            {
                nextDrainAt += heartDrainInterval;

                if (NetworkServer.active)
                {
                    heart.ConsumeHeart(currentDrainPerTick);

                    DotController.InflictDot(
                        characterBody.gameObject,
                        characterBody.gameObject,
                        characterBody.mainHurtBox,
                        DotController.DotIndex.Bleed,
                        1f, 1f, (uint)currentDrainPerTick);
                }

                currentDrainPerTick += 1f;

                if (!heart.CanSustainDefiantHeart())
                {
                    if (NetworkServer.active)
                    {
                        exitingDueToHeartExhaustion = true;
                    }

                    if (CanExitState())
                    {
                        outer.SetNextStateToMain();
                    }
                }
            }
        }

        private void ApplyTransformEnterEffect()
        {
            //Log.Debug("Attempting to apply transform enter effect.");
            if (!SeamstressVariantAssets.defiantTransformEnterEffect || !characterBody)
            {
                return;
            }

            float envelopeScale = 3.5f;
            Vector3 origin = characterBody.corePosition + Vector3.up * (characterBody.radius * 0.75f);

            EffectManager.SpawnEffect(SeamstressVariantAssets.defiantTransformEnterEffect, new EffectData
            {
                origin = origin,
                rotation = Quaternion.identity,
                scale = envelopeScale
            }, false);
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
            }, false);
        }
        public override void ModifyNextState(EntityState nextState)
        {
            base.ModifyNextState(nextState);
            if (nextState is HealingHeart healingHeart)
            {
                healingHeart.normalExit = false;
                if (NetworkServer.active)
                    TransferHeartServer();
                    healingHeart.skillLocator.special.DeductStock(1);
            }
        }

        private void TransferHeartServer()
        {
            Log.Warning("DefiantHeart: Transferring heart on server. Current heart: " + heart.GetHeart());
            // not entirely sure about this one. maybe need to give `healingHeart.storedHeart` this value?
            float healAmount = heart.ConsumeHeart(heart.GetHeart());
            if (healAmount > 0f)
            {
                var procChainMask = new ProcChainMask();
                procChainMask.AddModdedProc(SeamstressVariantSurvivor.bypassHeartConversion);

                this.characterBody.healthComponent.Heal(healAmount, procChainMask, true);
                Log.Warning("DefiantHeart: Healed for " + healAmount);
            }
        }


        public override void OnExit()
        {
            Log.Warning("Exiting Defiant Heart state.");

            EndStartupFreeze();
            ApplyTransformExitEffect();

            heartOverlayController?.SetHeartDrainActive(false);

            if (NetworkServer.active)
            {
                DotController.RemoveAllDots(gameObject);

                if (characterBody.HasBuff(SeamstressVariantBuffs.defianceBuff))
                    characterBody.RemoveBuff(SeamstressVariantBuffs.defianceBuff);

                if (exitingDueToHeartExhaustion)
                {
                    characterBody.healthComponent.Suicide();
                }
            }

            if (isAuthority)
                heart?.RequestSetDefianceVisualsActive(false);

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}
