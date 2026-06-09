using EntityStates;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using SeamstressMod.Seamstress.Content;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    public class HealingHeart : BaseSkillState
    {
        public float baseDuration = 0.6f;

        private BleedingHeartComponent heart;
        private Material destealthMaterial;
        private TemporaryOverlayInstance persistentDefianceOverlay;
        private float storedHeart;
        private bool transferApplied;
        private bool forcedTransitionToDefiantHeart;
        private bool normalExit;

        public override void OnEnter()
        {
            base.OnEnter();

            transferApplied = false;
            forcedTransitionToDefiantHeart = false;
            normalExit = false;
            destealthMaterial = SeamstressAssets.destealthMaterial;
            heart = GetComponent<BleedingHeartComponent>();
            DefianceSpecialController specialController = GetComponent<DefianceSpecialController>();

            if (heart == null || healthComponent == null || characterBody == null)
            {
                if (isAuthority || NetworkServer.active)
                {
                    outer.SetNextStateToMain();
                }
                return;
            }

            storedHeart = heart.GetHeart();

            if (isAuthority || NetworkServer.active)
            {
                bool forcedDefianceActivation = specialController != null && specialController.ConsumeForcedDefianceActivation();
                int defianceCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);

                if (forcedDefianceActivation)
                {
                    Log.Warning("HealingHeart: Forced Defiance activation detected on enter. Transitioning to DefiantHeart.");
                    forcedTransitionToDefiantHeart = true;
                    outer.SetNextState(new DefiantHeart());
                    return;
                }

                if (defianceCount > 0)
                {
                    Log.Warning("Instant. Applying Heart Transfer on DefiantHeart Exit");
                    if (NetworkServer.active)
                    {
                        ApplyHeartTransfer();
                    }
                }
                
                if (defianceCount == 0)
                {
                    Log.Warning("Normal entry to Healing Heart");
                    characterBody.AddBuff(SeamstressVariantBuffs.defianceBuff);
                    PlayCrossfade("FullBody, Override", "RipHeart", "Dash.playbackRate", baseDuration * 2.25f, 0.05f);
                    normalExit = true;
                }
            }

            
        }

        public void PlayDestealthAnimation()
        {
            Animator anim = GetModelAnimator();
            if (anim && destealthMaterial && persistentDefianceOverlay == null)
            {
                persistentDefianceOverlay = TemporaryOverlayManager.AddOverlay(gameObject);
                persistentDefianceOverlay.duration = 1f;
                persistentDefianceOverlay.destroyComponentOnEnd = true;
                persistentDefianceOverlay.originalMaterial = destealthMaterial;
                persistentDefianceOverlay.inspectorCharacterModel = anim.gameObject.GetComponent<CharacterModel>();
                persistentDefianceOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                persistentDefianceOverlay.animateShaderAlpha = true;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (fixedAge >= baseDuration)
            {
                if (normalExit)
                {
                    PlayDestealthAnimation();

                    if (NetworkServer.active)
                    {
                        ApplyHeartTransfer();
                    }

                    Util.PlaySound("Play_voidman_transform_return", gameObject);
                    normalExit = false;
                }

                if (isAuthority)
                {
                    outer.SetNextStateToMain();
                }
            }
        }

        public override void OnExit()
        {
            Log.Warning("Exiting Healing Heart state.");
            

            if (NetworkServer.active)
            {
                if (!forcedTransitionToDefiantHeart)
                {
                    RemoveDefiance();
                }

            }
            base.OnExit();
        }

        private void ApplyHeartTransfer()
        {
            if (transferApplied || heart == null || healthComponent == null || characterBody == null)
            {
                return;
            }

            float transferred = heart.ConsumeHeart(storedHeart);
            if (transferred > 0f)
            {
                float currentMaxHealth = healthComponent.fullHealth;
                healthComponent.Networkhealth = Mathf.Clamp(healthComponent.health + transferred, 1f, currentMaxHealth);
            }

            transferApplied = true;
        }

        private void RemoveDefiance()
        {
            int defianceCount = characterBody.GetBuffCount(SeamstressVariantBuffs.defianceBuff);
            if (defianceCount > 0)
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