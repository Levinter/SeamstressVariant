using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using SeamstressVariant.Modules;
using SeamstressVariant.Characters.Survivors.SeamstressVariant.Components;
using SeamstressMod.Seamstress.Content;
using RoR2.Projectile;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantAssets
    {
        // particle effects
        public static GameObject swordSwingEffect;
        public static GameObject swordHitImpactEffect;

        public static GameObject bombExplosionEffect;

        public static GameObject defianceEndEffect;

        //projectiles
        public static GameObject bombProjectilePrefab;

        // Simplified scissor projectiles for the secondary skill.
        // Ghost visuals are stolen from the OG Seamstress scissor prefabs.
        public static GameObject scissorLProjectile;
        public static GameObject scissorRProjectile;

        // Homing tuning: lower rotation speed produces wider, smoother arcs.
        private const float ScissorHomingRotationSpeed = 200f;
        // Lower travel speed gives the projectile more time to arc into the target.
        private const float ScissorProjectileTravelSpeed = 100f;
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

                ProjectileDamage projectileDamage = proj.GetComponent<ProjectileDamage>();
                if (projectileDamage)
                {
                    // Strip inherited damage flags from ImpVoidspike so the projectile is pure direct hit.
                    projectileDamage.damageType = DamageType.Generic;
                }

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
                targetFinder.lookRange = 0f;
                targetFinder.lookCone = 0f;
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

                ProjectileImpactExplosion impactExplosion = proj.GetComponent<ProjectileImpactExplosion>();
                if (impactExplosion)
                {
                    impactExplosion.impactEffect = SeamstressAssets.blinkEffect;
                    impactExplosion.explosionEffect = SeamstressAssets.genericImpactExplosionEffect;
                    impactExplosion.blastDamageCoefficient = SeamstressVariantStaticValues.scissorDamageCoefficient;
                    impactExplosion.blastProcCoefficient = 1f;
                    impactExplosion.blastRadius = 5f;
                    // Explode on terrain; enemy hits are handled by ProjectileSingleTargetImpact.
                    impactExplosion.destroyOnWorld = true;
                    impactExplosion.destroyOnEnemy = true;
                }

                ProjectileImpactVFXSFX impactVfxSfx = proj.GetComponent<ProjectileImpactVFXSFX>();
                if (!impactVfxSfx)
                {
                    impactVfxSfx = proj.AddComponent<ProjectileImpactVFXSFX>();
                }

                impactVfxSfx.impactSoundString = "sfx_seamstress_scissor_land";
            }
        }

        #endregion projectiles
    }
}
