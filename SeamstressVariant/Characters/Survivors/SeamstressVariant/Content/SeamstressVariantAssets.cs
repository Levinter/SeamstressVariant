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

        private static AssetBundle _assetBundle;

        public static void Init(AssetBundle assetBundle)
        {

            _assetBundle = assetBundle;

            swordHitSoundEvent = Content.CreateAndAddNetworkSoundEventDef("HenrySwordHit");

            CreateEffects();

            CreateProjectiles();
        }

        #region effects
        private static void CreateEffects()
        {
            CreateBombExplosionEffect();
            CreateDefianceEndEffect();

            swordSwingEffect = _assetBundle.LoadEffect("HenrySwordSwingEffect", true);
            swordHitImpactEffect = _assetBundle.LoadEffect("ImpactHenrySlash");

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

        private static void CreateBombExplosionEffect()
        {
            bombExplosionEffect = _assetBundle.LoadEffect("BombExplosionEffect", "HenryBombExplosion");

            if (!bombExplosionEffect)
                return;

            ShakeEmitter shakeEmitter = bombExplosionEffect.AddComponent<ShakeEmitter>();
            shakeEmitter.amplitudeTimeDecay = true;
            shakeEmitter.duration = 0.5f;
            shakeEmitter.radius = 200f;
            shakeEmitter.scaleShakeRadiusWithLocalScale = false;

            shakeEmitter.wave = new Wave
            {
                amplitude = 1f,
                frequency = 40f,
                cycleOffset = 0f
            };

        }
        #endregion effects

        #region projectiles
        private static void CreateProjectiles()
        {
            CreateBombProjectile();
            Content.AddProjectilePrefab(bombProjectilePrefab);
        }

        private static void CreateBombProjectile()
        {
            //highly recommend setting up projectiles in editor, but this is a quick and dirty way to prototype if you want
            bombProjectilePrefab = Asset.CloneProjectilePrefab("CommandoGrenadeProjectile", "HenryBombProjectile");

            //remove their ProjectileImpactExplosion component and start from default values
            UnityEngine.Object.Destroy(bombProjectilePrefab.GetComponent<ProjectileImpactExplosion>());
            ProjectileImpactExplosion bombImpactExplosion = bombProjectilePrefab.AddComponent<ProjectileImpactExplosion>();
            
            bombImpactExplosion.blastRadius = 16f;
            bombImpactExplosion.blastDamageCoefficient = 1f;
            bombImpactExplosion.falloffModel = BlastAttack.FalloffModel.None;
            bombImpactExplosion.destroyOnEnemy = true;
            bombImpactExplosion.lifetime = 12f;
            bombImpactExplosion.impactEffect = bombExplosionEffect;
            bombImpactExplosion.lifetimeExpiredSound = Content.CreateAndAddNetworkSoundEventDef("HenryBombExplosion");
            bombImpactExplosion.timerAfterImpact = true;
            bombImpactExplosion.lifetimeAfterImpact = 0.1f;

            ProjectileController bombController = bombProjectilePrefab.GetComponent<ProjectileController>();

            if (_assetBundle.LoadAsset<GameObject>("HenryBombGhost") != null)
                bombController.ghostPrefab = _assetBundle.CreateProjectileGhostPrefab("HenryBombGhost");
            
            bombController.startSound = "";
        }
        #endregion projectiles
    }
}
