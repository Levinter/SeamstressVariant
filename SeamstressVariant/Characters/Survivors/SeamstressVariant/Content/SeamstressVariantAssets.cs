using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using SeamstressVariant.Modules;
using SeamstressMod.Seamstress.Content;
using RoR2.Projectile;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantAssets
    {
        // particle effects
        public static GameObject swordSwingEffect;
        public static GameObject swordHitImpactEffect;
        public static GameObject defianceEndEffect;
        public static GameObject scissorImpactEffect;
        public static GameObject defiantTransformEnterEffect;
        public static GameObject defiantTransformExitEffect;
        public static CharacterCameraParams defiantTransformCameraParams;

        // Simplified scissor projectiles for the secondary skill.
        // Ghost visuals are stolen from the OG Seamstress scissor prefabs.
        public static GameObject scissorLProjectile;
        public static GameObject scissorRProjectile;

        // Homing tuning: lower rotation speed produces wider, smoother arcs.
        private const float ScissorHomingRotationSpeed = 250f;
        // Lower travel speed gives the projectile more time to arc into the target.
        private const float ScissorProjectileTravelSpeed = 90f;
        // This only matters when no target is already assigned at spawn.
        private const float ScissorTargetSearchInterval = 0.5f;

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
            CreateScissorImpactEffect();
            CreateDefiantTransformEffects();

            // Register OG Seamstress effects used by ClawCombo so OverlapAttack can spawn them.
            Content.CreateAndAddEffectDef(SeamstressAssets.scissorsHitImpactEffect);
        }

        private static void CreateDefiantTransformEffects()
        {
            GameObject baseTransformEffect = Addressables
                .LoadAssetAsync<GameObject>("RoR2/DLC1/VoidSurvivor/VoidSurvivorCorruptDeathCharge.prefab")
                .WaitForCompletion();

            if (baseTransformEffect)
            {
                defiantTransformEnterEffect = PrefabAPI.InstantiateClone(baseTransformEffect, "SeamstressVariantDefiantTransformEnterEffect");
                defiantTransformExitEffect = PrefabAPI.InstantiateClone(baseTransformEffect, "SeamstressVariantDefiantTransformExitEffect");
                EnsureEffectPrefabRequirements(defiantTransformEnterEffect);
                EnsureEffectPrefabRequirements(defiantTransformExitEffect);

                Content.CreateAndAddEffectDef(defiantTransformEnterEffect);
                Content.CreateAndAddEffectDef(defiantTransformExitEffect);
            }

            defiantTransformCameraParams = Addressables
                .LoadAssetAsync<CharacterCameraParams>("RoR2/DLC1/VoidSurvivor/ccpCorruptionTransitionCamera.asset")
                .WaitForCompletion();
        }

        private static void EnsureEffectPrefabRequirements(GameObject effectPrefab)
        {
            if (!effectPrefab)
            {
                return;
            }

            if (!effectPrefab.GetComponent<EffectComponent>())
            {
                EffectComponent effectComponent = effectPrefab.AddComponent<EffectComponent>();
                effectComponent.applyScale = true;
            }

            if (!effectPrefab.GetComponent<VFXAttributes>())
            {
                VFXAttributes attributes = effectPrefab.AddComponent<VFXAttributes>();
                attributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;
                attributes.vfxPriority = VFXAttributes.VFXPriority.Medium;
            }

            if (!effectPrefab.GetComponent<NetworkIdentity>())
            {
                effectPrefab.AddComponent<NetworkIdentity>();
            }
        }

        private static void CreateScissorImpactEffect()
        {
            scissorImpactEffect = PrefabAPI.InstantiateClone(SeamstressAssets.blinkEffect, "SeamstressVariantScissorImpactEffect");

            EffectComponent effectComponent = scissorImpactEffect.GetComponent<EffectComponent>();
            if (effectComponent)
            {
                effectComponent.soundName = "sfx_seamstress_scissor_land";
            }

            Content.CreateAndAddEffectDef(scissorImpactEffect);
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

            SetupScissorProjectile(scissorLProjectile);
            SetupScissorProjectile(scissorRProjectile);
        }

        private static void SetupScissorProjectile(GameObject proj)
        {
            ProjectileTargetComponent targetComponent = proj.GetComponent<ProjectileTargetComponent>();
            if (!targetComponent)
            {
                targetComponent = proj.AddComponent<ProjectileTargetComponent>();
            }

            ProjectileSteerTowardTarget steerTowardTarget = proj.GetComponent<ProjectileSteerTowardTarget>();
            if (!steerTowardTarget)
            {
                steerTowardTarget = proj.AddComponent<ProjectileSteerTowardTarget>();
            }
            steerTowardTarget.yAxisOnly = false;
            steerTowardTarget.rotationSpeed = ScissorHomingRotationSpeed;
            steerTowardTarget.enabled = true;

            ProjectileSimple projectileSimple = proj.GetComponent<ProjectileSimple>();
            if (projectileSimple)
            {
                projectileSimple.desiredForwardSpeed = ScissorProjectileTravelSpeed;
            }

            /*ProjectileDamage projectileDamage = proj.GetComponent<ProjectileDamage>();
            if (projectileDamage)
            {
                // Strip inherited damage flags from ImpVoidspike so the projectile is pure direct hit.
                projectileDamage.damageType = DamageType.Generic;
            }*/

            ProjectileStickOnImpact stickOnImpact = proj.GetComponent<ProjectileStickOnImpact>();
            if (stickOnImpact)
            {
                UnityEngine.Object.Destroy(stickOnImpact);
            }

            ProjectileDirectionalTargetFinder targetFinder = proj.GetComponent<ProjectileDirectionalTargetFinder>();
            if (!targetFinder)
            {
                targetFinder = proj.AddComponent<ProjectileDirectionalTargetFinder>();
            }
            targetFinder.lookRange = 60f;
            targetFinder.lookCone = 60f;
            targetFinder.targetSearchInterval = ScissorTargetSearchInterval;
            targetFinder.onlySearchIfNoTarget = true;
            targetFinder.allowTargetLoss = false;
            targetFinder.testLoS = true;
            targetFinder.ignoreAir = false;
            targetFinder.flierAltitudeTolerance = float.PositiveInfinity;
            targetFinder.enabled = true;

            // Add OG-matching trail VFX as a child of the projectile.
            if (SeamstressAssets.trailEffect)
            {
                UnityEngine.Object.Instantiate(SeamstressAssets.trailEffect, proj.transform);
            }

            ProjectileSingleTargetImpact singleTargetImpact = proj.GetComponent<ProjectileSingleTargetImpact>();
            if (singleTargetImpact)
            {
                singleTargetImpact.destroyOnWorld = true;
            }

            ProjectileImpactExplosion impactExplosion = proj.GetComponent<ProjectileImpactExplosion>();
            if (impactExplosion)
            {
                impactExplosion.impactEffect = scissorImpactEffect ? scissorImpactEffect : SeamstressAssets.blinkEffect;
                //impactExplosion.explosionEffect = SeamstressAssets.genericImpactExplosionEffect;
                impactExplosion.blastDamageCoefficient = SeamstressVariantStaticValues.scissorExplosionDamageCoefficient;
                impactExplosion.blastProcCoefficient = 1f;
                impactExplosion.blastRadius = 5f;
                impactExplosion.destroyOnWorld = true;
            }
        }

        #endregion projectiles
    }
}
