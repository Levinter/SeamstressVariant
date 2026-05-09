using EntityStates;
using RoR2;
using SeamstressMod.Seamstress.Content;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.SkillStates
{
    public class SeamstressSpawnState : BaseSkillState
    {
        public GameObject spawnPrefab = SeamstressAssets.spawnPrefab;

        public static float duration = 2f;

        private static Material dissolveMaterial;

        private bool skipCustomSpawn;

        private bool hasSpawnEffectFired;

        public override void OnEnter()
        {
            base.OnEnter();

            // Only play this custom spawn sequence on the initial run spawn.
            skipCustomSpawn = Run.instance != null && Run.instance.stageClearCount > 0;
            if (skipCustomSpawn)
            {
                return;
            }

            if (NetworkServer.active)
            {
                characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
            }
            PlayAnimation("Body", "Spawn");
            
            Transform modelTransform = GetModelTransform();
            if (modelTransform)
            {
                if (dissolveMaterial == null)
                {
                    dissolveMaterial = Addressables.LoadAssetAsync<Material>((object)"RoR2/Base/Imp/matImpDissolve.mat").WaitForCompletion();
                }

                TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(gameObject);
                temporaryOverlayInstance.duration = 1.5f;
                temporaryOverlayInstance.animateShaderAlpha = true;
                temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlayInstance.destroyComponentOnEnd = true;
                temporaryOverlayInstance.originalMaterial = dissolveMaterial;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (skipCustomSpawn)
            {
                if (isAuthority)
                {
                    // Hand off to vanilla teleporter spawn state so stage-transition animation plays.
                    outer.SetNextState(new SpawnTeleporterState());
                }
                return;
            }
            
            if (fixedAge > -2f && !hasSpawnEffectFired)
            {
                hasSpawnEffectFired = true;
                EffectData effectData = new EffectData();
                effectData.origin = transform.position;
                EffectManager.SpawnEffect(spawnPrefab, effectData, false);
                Util.PlaySound("sfx_seamstress_spawn", gameObject);
            }

            if (fixedAge >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            if (!skipCustomSpawn && NetworkServer.active)
            {
                characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}
