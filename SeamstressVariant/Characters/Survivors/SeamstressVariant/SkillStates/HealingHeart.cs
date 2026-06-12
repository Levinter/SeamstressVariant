using EntityStates;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using SeamstressMod.Seamstress.Content;
using RoR2.Skills;
using R2API;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    public class HealingHeart : BaseSkillState
    {
        public static SkillDef specialSkillDef;
        public float baseDuration = 0.6f;
        private BleedingHeartComponent heart;
        private Material destealthMaterial;
        private TemporaryOverlayInstance persistentDefianceOverlay;
        private float storedHeart;
        public bool normalExit = true;
        private bool transferApplied;

        public override void OnEnter()
        {
            base.OnEnter();

            destealthMaterial = SeamstressAssets.destealthMaterial;
            heart = GetComponent<BleedingHeartComponent>();

            storedHeart = heart.GetHeart();
            //Log.Warning("HEALING HEART. ForcedDefianceActivation: " + forcedDefianceActivation);
            Log.Warning("HEALING HEART. Is Authority? " + isAuthority);

            if (normalExit)
            {
                Log.Warning("Normal entry to Healing Heart");
                PlayCrossfade("FullBody, Override", "RipHeart", "Dash.playbackRate", baseDuration * 2.25f, 0.05f);
            }

            if (NetworkServer.active && characterBody.HasBuff(SeamstressVariantBuffs.defianceBuff) == false)
            {
                characterBody.AddBuff(SeamstressVariantBuffs.defianceBuff);
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

                    if (NetworkServer.active && !transferApplied)
                    {
                        transferApplied = true;
                        TransferHeartServer();
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

        public override void ModifyNextState(EntityState nextState)
        {
            base.ModifyNextState(nextState);
            if (NetworkServer.active && characterBody.HasBuff(SeamstressVariantBuffs.defianceBuff))
            {
                if (!(nextState is DefiantHeart))
                {
                    characterBody.RemoveBuff(SeamstressVariantBuffs.defianceBuff);
                }
            }
        }

        public override void OnExit()
        {
            Log.Warning("Exiting Healing Heart state.");
            
            base.OnExit();
        }

        private void TransferHeartServer()
        {
            float healAmount = heart.ConsumeHeart(storedHeart);
            if (healAmount > 0f)
            {
                var procChainMask = new ProcChainMask();
                procChainMask.AddModdedProc(SeamstressVariantSurvivor.bypassHeartConversion);

                this.characterBody.healthComponent.Heal(healAmount, procChainMask, true);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}