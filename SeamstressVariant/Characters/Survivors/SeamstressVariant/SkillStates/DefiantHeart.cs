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
        private const float HeartDrainSegmentPercent = SeamstressVariantStaticValues.bleedDrainSegments;

        public static float heartDrainPerTick = 1f;
        public static float heartDrainInterval = 0.25f;
        public static float startupFreezeDuration = 1.0f;
        public static float animDuration = 1.5f;

        private BleedingHeartComponent heart;
        private HeartOverlayController heartOverlayController;
        private float nextDrainAt;
        private float startupFreezeEndTime;
        private float currentDrainPerTick;
        private int completedDrainSegments;
        private int drainTicksAtCurrentRate;
        private int drainTicksPerIncrease;
        private bool startupFreezeActive;
        private bool fired;
        private bool exitingDueToHeartExhaustion;

        private EffectManagerHelper bleedEffect;

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
            completedDrainSegments = 0;
            drainTicksAtCurrentRate = 0;
            drainTicksPerIncrease = Mathf.Max(1, Mathf.RoundToInt(1f / heartDrainInterval));
            startupFreezeEndTime = startupFreezeDuration;
            startupFreezeActive = startupFreezeDuration > 0f;

            if (isAuthority)
            {
                heart.RequestSetDefiantStartupFreezeActive(true);
            }

            PlayCrossfade("FullBody, Override", "RipHeart", "Dash.playbackRate", animDuration, 0.05f);
            Util.PlaySound("Play_imp_overlord_attack2_tell", gameObject);

            ApplyTransformEnterEffect();

            //Log.Warning("Entered Defiant Heart state.");

            characterBody.AddBuff(RoR2Content.Buffs.SuperBleed);

            bleedEffect = EffectManager.GetAndActivatePooledEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/BleedEffect"), characterBody.coreTransform ?? transform, inResetLocal: true);
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
                //Log.Warning("Trying to transition from Defiant Heart to Healing Heart.");
                outer.SetNextState(new HealingHeart());
                return;
            }

            if (fixedAge >= nextDrainAt)
            {
                nextDrainAt += heartDrainInterval;
                float maxDrainPerSegment = heart.GetMaxHeart() * HeartDrainSegmentPercent;
                int drainAmount = Mathf.FloorToInt(completedDrainSegments * maxDrainPerSegment
                    + Mathf.Min(currentDrainPerTick, maxDrainPerSegment));

                if (NetworkServer.active)
                {
                    heart.ConsumeHeart(drainAmount);

                    DamageInfo damageInfo = new DamageInfo
                    {
                        damage = drainAmount,
                        damageColorIndex = DamageColorIndex.Bleed,
                        damageType = DamageType.BypassArmor | DamageType.BypassBlock | DamageType.DoT,
                        attacker = null,
                        inflictor = null,
                        position = characterBody.corePosition,
                        force = Vector3.zero,
                        crit = false,
                        procCoefficient = 0f
                    };
                    
                    characterBody.healthComponent.TakeDamage(damageInfo);
                }

                drainTicksAtCurrentRate++;
                if (drainTicksAtCurrentRate >= drainTicksPerIncrease)
                {
                    if (currentDrainPerTick >= maxDrainPerSegment)
                    {
                        completedDrainSegments++;
                        currentDrainPerTick = Mathf.Min(1f, maxDrainPerSegment);
                    }
                    else
                    {
                        currentDrainPerTick = Mathf.Min(currentDrainPerTick * 2f, maxDrainPerSegment);
                    }

                    drainTicksAtCurrentRate = 0;
                }

                //Log.Warning ("Currently bleeding out for " + drainAmount);

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
            //Log.Warning("DefiantHeart: Transferring heart on server. Current heart: " + heart.GetHeart());
            float healAmount = heart.ConsumeHeart(heart.GetHeart());
            if (healAmount > 0f)
            {
                var procChainMask = new ProcChainMask();
                procChainMask.AddModdedProc(SeamstressVariantSurvivor.bypassHeartConversion);

                this.characterBody.healthComponent.Heal(healAmount, procChainMask, true);
                //Log.Warning("DefiantHeart: Healed for " + healAmount);
            }
        }


        public override void OnExit()
        {
            //Log.Warning("Exiting Defiant Heart state.");

            EndStartupFreeze();

            heartOverlayController?.SetHeartDrainActive(false);

            if (NetworkServer.active)
            {
                characterBody.AddTimedBuff(SeamstressVariantBuffs.rejectBuff, 1.0f); //pls dont die

                DotController.RemoveAllDots(gameObject);

                if (characterBody.HasBuff(SeamstressVariantBuffs.defianceBuff))
                {
                    characterBody.RemoveBuff(SeamstressVariantBuffs.defianceBuff);
                    characterBody.RemoveBuff(RoR2Content.Buffs.SuperBleed);
                }

                bleedEffect?.ReturnToPool();

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
