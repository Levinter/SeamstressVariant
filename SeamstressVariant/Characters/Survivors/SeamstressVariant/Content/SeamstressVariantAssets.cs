using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using SeamstressVariant.Modules;
using SeamstressMod.Seamstress.Content;
using System;
using RoR2.Projectile;
using SeamstressVariant.Characters.Survivors.SeamstressVariant.Components;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantAssets
    {
        // particle effects
        public static GameObject swordSwingEffect;
        public static GameObject swordHitImpactEffect;

        public static GameObject bombExplosionEffect;

        public static GameObject defianceEndEffect;

        // networked hit sounds
        public static NetworkSoundEventDef swordHitSoundEvent;

        //projectiles
        public static GameObject bombProjectilePrefab;

        // Simplified scissor projectiles for the secondary skill (straight-line, no homing, no pickup).
        // Ghost visuals are stolen from the OG Seamstress scissor prefabs.
        public static GameObject scissorLProjectile;
        public static GameObject scissorRProjectile;

        private static AssetBundle _assetBundle;

        public static void Init(AssetBundle assetBundle)
        {

            _assetBundle = assetBundle;

            CreateEffects();

            CreateProjectiles();
        }

        #region effects
        private static void CreateEffects()
        {
            CreateDefianceEndEffect();

            // Register OG Seamstress effects used by ClawCombo so OverlapAttack can spawn them.
            Content.CreateAndAddEffectDef(SeamstressAssets.scissorsHitImpactEffect);
        }

        private static void CreateDefianceEndEffect()
        {
            defianceEndEffect = PrefabAPI.InstantiateClone(
                Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarSkillReplacements/LunarDetonatorConsume.prefab").WaitForCompletion(),
                "SeamstressVariantDefianceEndEffect");
            defianceEndEffect.AddComponent<NetworkIdentity>();

            ParticleSystem.MainModule child0 = defianceEndEffect.transform.GetChild(0).GetComponent<ParticleSystem>().main;
            child0.startColor = new ParticleSystem.MinMaxGradient(Color.black);

            ParticleSystem.MainModule child1 = defianceEndEffect.transform.GetChild(1).GetComponent<ParticleSystem>().main;
            child1.startColor = new ParticleSystem.MinMaxGradient(Color.red);

            defianceEndEffect.transform.GetChild(2).GetComponent<ParticleSystemRenderer>().material.SetColor("_TintColor", Color.red);
            defianceEndEffect.transform.GetChild(3).gameObject.SetActive(false);
            defianceEndEffect.transform.GetChild(4).GetComponent<ParticleSystemRenderer>().material.SetColor("_TintColor", Color.red);

            Material needleMat = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/Base/LunarSkillReplacements/matLunarNeedleImpactEffect.mat").WaitForCompletion());
            needleMat.SetColor("_TintColor", Color.red);
            defianceEndEffect.transform.GetChild(5).GetComponent<ParticleSystemRenderer>().material = needleMat;

            defianceEndEffect.transform.GetChild(6).gameObject.SetActive(false);

            Content.CreateAndAddEffectDef(defianceEndEffect);
        }

        #endregion effects

        #region projectiles
        private static void CreateProjectiles()
        {
            CreateScissorProjectiles();
            Content.AddProjectilePrefab(scissorLProjectile);
            Content.AddProjectilePrefab(scissorRProjectile);
        }

        private static void CreateScissorProjectiles()
        {
            // Use ImpVoidspikeProjectile as a clean, straight-line projectile base.
            // It comes with ProjectileSingleTargetImpact (damage on hit) and ProjectileSimple (movement).
            GameObject baseProjectile = Addressables.LoadAssetAsync<GameObject>(
                "RoR2/Base/ImpBoss/ImpVoidspikeProjectile.prefab").WaitForCompletion();

            scissorLProjectile = PrefabAPI.InstantiateClone(baseProjectile, "SeamstressVariantScissorLProjectile");
            scissorRProjectile = PrefabAPI.InstantiateClone(baseProjectile, "SeamstressVariantScissorRProjectile");

            foreach (GameObject proj in new[] { scissorLProjectile, scissorRProjectile })
            {
                ProjectileImpactVFXSFX impactProbe = proj.GetComponent<ProjectileImpactVFXSFX>();
                if (!impactProbe)
                {
                    impactProbe = proj.AddComponent<ProjectileImpactVFXSFX>();
                }
                impactProbe.logOnly = true;

                // OG sets ProjectileImpactExplosion.impactEffect to pickupScissorEffect
                // (MercSwordSlashWhirlwind clone). That effect carries an embedded
                // EffectComponent.soundName (sword swing sound) which fires automatically
                // via EffectManager.SpawnEffect — this is the suspected missing impact SFX.
                ProjectileImpactExplosion pie = proj.GetComponent<ProjectileImpactExplosion>();
                if (pie)
                {
                    pie.impactEffect = SeamstressAssets.pickupScissorEffect;
                    LogEffectSoundInfo($"{proj.name}.ProjectileImpactExplosion.impactEffect", pie.impactEffect);
                }

                ProjectileStickOnImpact stick = proj.GetComponent<ProjectileStickOnImpact>();
                LogStickEventInfo(proj.name, stick);
            }
        }

        private static void LogEffectSoundInfo(string label, GameObject effectPrefab)
        {
            if (!effectPrefab)
            {
                Log.Info($"[SFX DEBUG] {label}: effect prefab is null");
                return;
            }

            EffectComponent effectComponent = effectPrefab.GetComponent<EffectComponent>();
            if (!effectComponent)
            {
                Log.Info($"[SFX DEBUG] {label}: prefab '{effectPrefab.name}' has no EffectComponent");
                return;
            }

            Log.Info($"[SFX DEBUG] {label}: prefab '{effectPrefab.name}' soundName='{effectComponent.soundName}'");
        }

        private static void LogStickEventInfo(string projectileName, ProjectileStickOnImpact stick)
        {
            if (!stick)
            {
                Log.Info($"[SFX DEBUG] {projectileName}: no ProjectileStickOnImpact component found");
                return;
            }

            if (stick.stickEvent == null)
            {
                Log.Info($"[SFX DEBUG] {projectileName}: stickEvent is null");
                return;
            }

            int listenerCount = stick.stickEvent.GetPersistentEventCount();
            Log.Info($"[SFX DEBUG] {projectileName}: stickEvent listeners={listenerCount}");

            for (int i = 0; i < listenerCount; i++)
            {
                UnityEngine.Object target = stick.stickEvent.GetPersistentTarget(i);
                string method = stick.stickEvent.GetPersistentMethodName(i);
                string targetName = target ? target.name : "<null>";
                string targetType = target ? target.GetType().Name : "<null>";
                Log.Info($"[SFX DEBUG] {projectileName}: stickEvent[{i}] target='{targetName}' type='{targetType}' method='{method}'");
            }
        }

        #endregion projectiles
    }
}
