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
        //private float storedHeart;
        public bool normalExit = true;
        //private bool transferApplied = false;

        public override void OnEnter()
        {
            base.OnEnter();

            Log.Warning("Entered Healing Heart state. NormalExit: " + normalExit);

            destealthMaterial = SeamstressAssets.destealthMaterial;
            heart = GetComponent<BleedingHeartComponent>();

            if (normalExit)
            {
                PlayCrossfade("FullBody, Override", "RipHeart", "Dash.playbackRate", baseDuration * 2.25f, 0.05f);
                Util.PlaySound("Play_voidman_transform_return", gameObject);

                if (NetworkServer.active && characterBody.HasBuff(SeamstressVariantBuffs.defianceBuff) == false)
                {
                    characterBody.AddBuff(SeamstressVariantBuffs.defianceBuff);
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
            if (normalExit)
            {
                PlayDestealthAnimation();

                if (NetworkServer.active)
                {
                    TransferHeartServer();
                }
            }

            Log.Warning("Exiting HealingHeart state. NormalExit: " + normalExit);
            base.OnExit();
        }

        private void TransferHeartServer()
        {
            Log.Warning("HealingHeart: Transferring heart on server. Current heart: " + heart.GetHeart());
            float healAmount = heart.ConsumeHeart(heart.GetHeart());
            if (healAmount > 0f)
            {
                var procChainMask = new ProcChainMask();
                procChainMask.AddModdedProc(SeamstressVariantSurvivor.bypassHeartConversion);

                this.characterBody.healthComponent.Heal(healAmount, procChainMask, true);
                Log.Warning("HealingHeart: Healed for " + healAmount);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}