using System.Reflection;
using System;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using RoR2.UI;
using RoR2.Projectile;
using Path = System.IO.Path;
using SeamstressVariant;

namespace SeamstressVariant.Modules
{
    internal static class Asset
    {
        //cache bundles if multiple characters use the same one
        internal static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

        internal static AssetBundle LoadAssetBundle(string bundleName)
        {

            if (bundleName == "myassetbundle")
            {
                Log.Error($"AssetBundle name hasn't been changed. not loading any assets to avoid conflicts.\nMake sure to rename your assetbundle filename and rename the AssetBundleName field in your character setup code ");
                return null;
            }

            if (loadedBundles.ContainsKey(bundleName))
            {
                return loadedBundles[bundleName];
            }

            AssetBundle assetBundle = null;
            try
            {
                assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(SeamstressVariantPlugin.instance.Info.Location), "AssetBundles", bundleName));
            }
            catch (System.Exception e)
            {
                Log.Error($"Error loading asset bundle, {bundleName}. Your asset bundle must be in a folder next to your mod dll called 'AssetBundles'. Follow the guide to build and install your mod correctly!\n{e}");
            }

            loadedBundles[bundleName] = assetBundle;

            return assetBundle;

        }

        internal static void DebugLogBundleContents(this AssetBundle assetBundle)
        {
            if (assetBundle == null)
            {
                Log.Error("DebugLogBundleContents called with a null AssetBundle.");
                return;
            }

            string[] assetNames = assetBundle.GetAllAssetNames();
            if (assetNames == null)
            {
                Log.Warning($"AssetBundle '{assetBundle.name}' returned null from GetAllAssetNames().");
                return;
            }

            string[] prefabNames = assetNames
                .Where(name => name.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name)
                .ToArray();

            string[] materialNames = assetNames
                .Where(name => name.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name)
                .ToArray();

            string[] meshNames = assetNames
                .Where(name => name.EndsWith(".mesh", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name)
                .ToArray();

            string[] textureNames = assetNames
                .Where(name => name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".tif", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name)
                .ToArray();

            string[] animationNames = assetNames
                .Where(name => name.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".controller", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".mask", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name)
                .ToArray();

            Log.Info($"AssetBundle '{assetBundle.name}' contains {assetNames.Length} assets total.");
            Log.Info($"Reusable groups: {prefabNames.Length} prefabs, {materialNames.Length} materials, {meshNames.Length} meshes/models, {textureNames.Length} textures, {animationNames.Length} animation assets.");

            if (prefabNames.Length > 0)
            {
                Log.Info($"Prefabs in '{assetBundle.name}':");
                foreach (string prefabName in prefabNames)
                {
                    Log.Info($"[Prefab] {Path.GetFileNameWithoutExtension(prefabName)} <- {prefabName}");
                }
            }

            if (materialNames.Length > 0)
            {
                Log.Info($"Materials in '{assetBundle.name}':");
                foreach (string materialName in materialNames)
                {
                    Log.Info($"[Material] {Path.GetFileNameWithoutExtension(materialName)} <- {materialName}");
                }
            }

            if (meshNames.Length > 0)
            {
                Log.Info($"Meshes/models in '{assetBundle.name}':");
                foreach (string meshName in meshNames)
                {
                    Log.Info($"[Mesh] {Path.GetFileNameWithoutExtension(meshName)} <- {meshName}");
                }
            }

            if (textureNames.Length > 0)
            {
                Log.Info($"Textures in '{assetBundle.name}':");
                foreach (string textureName in textureNames)
                {
                    Log.Info($"[Texture] {Path.GetFileNameWithoutExtension(textureName)} <- {textureName}");
                }
            }

            if (animationNames.Length > 0)
            {
                Log.Info($"Animation assets in '{assetBundle.name}':");
                foreach (string animationName in animationNames)
                {
                    Log.Info($"[Animation] {Path.GetFileNameWithoutExtension(animationName)} <- {animationName}");
                }
            }
        }

        internal static GameObject CloneTracer(string originalTracerName, string newTracerName)
        {
            if (RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/" + originalTracerName) == null) 
                return null;

            GameObject newTracer = PrefabAPI.InstantiateClone(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/" + originalTracerName), newTracerName, true);

            if (!newTracer.GetComponent<EffectComponent>()) newTracer.AddComponent<EffectComponent>();
            if (!newTracer.GetComponent<VFXAttributes>()) newTracer.AddComponent<VFXAttributes>();
            if (!newTracer.GetComponent<NetworkIdentity>()) newTracer.AddComponent<NetworkIdentity>();
            
            newTracer.GetComponent<Tracer>().speed = 250f;
            newTracer.GetComponent<Tracer>().length = 50f;

            Modules.Content.CreateAndAddEffectDef(newTracer);

            return newTracer;
        }

        internal static void ConvertAllRenderersToHopooShader(GameObject objectToConvert)
        {
            if (!objectToConvert) return;

            foreach (MeshRenderer i in objectToConvert.GetComponentsInChildren<MeshRenderer>())
            {
                if (i)
                {
                    if (i.sharedMaterial)
                    {
                        i.sharedMaterial.ConvertDefaultShaderToHopoo();
                    }
                }
            }

            foreach (SkinnedMeshRenderer i in objectToConvert.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (i)
                {
                    if (i.sharedMaterial)
                    {
                        i.sharedMaterial.ConvertDefaultShaderToHopoo();
                    }
                }
            }
        }

        internal static GameObject LoadCrosshair(string crosshairName)
        {
            GameObject loadedCrosshair = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Crosshair/" + crosshairName + "Crosshair");
            if (loadedCrosshair == null)
            {
                Log.Error($"could not load crosshair with the name {crosshairName}. defaulting to Standard");

                return RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Crosshair/StandardCrosshair");
            }

            return loadedCrosshair;
        }

        internal static GameObject LoadEffect(this AssetBundle assetBundle, string resourceName, bool parentToTransform) => LoadEffect(assetBundle, resourceName, "", parentToTransform);
        internal static GameObject LoadEffect(this AssetBundle assetBundle, string resourceName, string soundName = "", bool parentToTransform = false)
        {
            GameObject newEffect = assetBundle.LoadAsset<GameObject>(resourceName);

            if (!newEffect)
            {
                Log.ErrorAssetBundle(resourceName, assetBundle.name);
                return null;
            }

            newEffect.AddComponent<DestroyOnTimer>().duration = 12;
            newEffect.AddComponent<NetworkIdentity>();
            newEffect.AddComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
            EffectComponent effect = newEffect.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.parentToReferencedTransform = parentToTransform;
            effect.positionAtReferencedTransform = true;
            effect.soundName = soundName;

            Modules.Content.CreateAndAddEffectDef(newEffect);

            return newEffect;
        }

        internal static GameObject CreateProjectileGhostPrefab(this AssetBundle assetBundle, string ghostName)
        {
            GameObject ghostPrefab = assetBundle.LoadAsset<GameObject>(ghostName);
            if (ghostPrefab == null)
            {
                Log.Error($"Failed to load ghost prefab {ghostName}");
            }
            if (!ghostPrefab.GetComponent<NetworkIdentity>()) ghostPrefab.AddComponent<NetworkIdentity>();
            if (!ghostPrefab.GetComponent<ProjectileGhostController>()) ghostPrefab.AddComponent<ProjectileGhostController>();

            Modules.Asset.ConvertAllRenderersToHopooShader(ghostPrefab);

            return ghostPrefab;
        }

        internal static GameObject CloneProjectilePrefab(string prefabName, string newPrefabName)
        {
            GameObject newPrefab = PrefabAPI.InstantiateClone(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/" + prefabName), newPrefabName);
            return newPrefab;
        }

        internal static GameObject LoadAndAddProjectilePrefab(this AssetBundle assetBundle, string newPrefabName)
        {
            GameObject newPrefab = assetBundle.LoadAsset<GameObject>(newPrefabName);
            if(newPrefab == null)
            {
                Log.ErrorAssetBundle(newPrefabName, assetBundle.name);
                return null;
            }

            Content.AddProjectilePrefab(newPrefab);
            return newPrefab;
        }
    }
}