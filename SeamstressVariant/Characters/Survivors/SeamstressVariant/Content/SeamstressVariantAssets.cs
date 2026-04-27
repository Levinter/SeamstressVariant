using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using SeamstressVariant.Modules;
using SeamstressMod.Seamstress.Content;
using System;
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
                // Remove stick-on-impact so the projectile passes through without embedding.
                ProjectileStickOnImpact stick = proj.GetComponent<ProjectileStickOnImpact>();
                if (stick) UnityEngine.Object.Destroy(stick);

                // Match OG: disable root collider (unused) and keep rotation stable.
                Rigidbody rb = proj.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.useGravity = false;
                    rb.freezeRotation = true;
                }

                // Root collider is not used for hit detection — disable it (OG does the same).
                SphereCollider rootSc = proj.GetComponent<SphereCollider>();
                if (rootSc)
                {
                    rootSc.radius = 1f;
                    rootSc.enabled = false;
                }

                // The real hitbox is on the child at GetChild(0).GetChild(5), matching OG radius of 6.
                SphereCollider childSc = proj.transform.GetChild(0).GetChild(5).GetComponent<SphereCollider>();
                if (childSc) childSc.radius = 6f;

                ProjectileSimple simple = proj.GetComponent<ProjectileSimple>();
                if (simple)
                {
                    simple.desiredForwardSpeed = 150f;
                    simple.lifetime = 5f;
                }

                // ImpVoidspikeProjectile's ProjectileImpactExplosion has dotIndex = Bleed.
                // Clear it so the scissor hit does not apply a bleed dot.
                // Also enable destroy-on-impact so the projectile disappears on hit.
                ProjectileImpactExplosion pie = proj.GetComponent<ProjectileImpactExplosion>();
                if (pie)
                {
                    pie.dotIndex = RoR2.DotController.DotIndex.None;
                    pie.destroyOnEnemy = true;
                    pie.destroyOnWorld = true;
                }

                // Also clear any inherited bleed/stun flags from the base damage type.
                ProjectileDamage pd = proj.GetComponent<ProjectileDamage>();
                if (pd) pd.damageType = DamageType.Generic;

                // Homing — steer toward nearest target, same settings as OG.
                ProjectileSteerTowardTarget steer = proj.AddComponent<ProjectileSteerTowardTarget>();
                steer.yAxisOnly = false;
                steer.rotationSpeed = 700f;
                steer.enabled = true;

                ProjectileDirectionalTargetFinder finder = proj.AddComponent<ProjectileDirectionalTargetFinder>();
                finder.lookRange = 0f;
                finder.lookCone = 0f;
                finder.targetSearchInterval = 0.2f;
                finder.onlySearchIfNoTarget = true;
                finder.allowTargetLoss = false;
                finder.testLoS = true;
                finder.ignoreAir = false;
                finder.flierAltitudeTolerance = float.PositiveInfinity;
                finder.enabled = true;
            }

            // Swap in scissor ghost visuals from the OG Seamstress prefabs.
            scissorLProjectile.GetComponent<ProjectileController>().ghostPrefab =
                SeamstressAssets.scissorLPrefab.GetComponent<ProjectileController>().ghostPrefab;
            scissorRProjectile.GetComponent<ProjectileController>().ghostPrefab =
                SeamstressAssets.scissorRPrefab.GetComponent<ProjectileController>().ghostPrefab;
        }

        #endregion projectiles
    }
}
