using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantBuffs
    {
        // heart visualization buff
        public static BuffDef heartBuff;

        // active special buff: while present, damage cannot reduce health below 1
        public static BuffDef defianceBuff;

        public static void Init(AssetBundle assetBundle)
        {
            defianceBuff = Modules.Content.CreateAndAddBuff("SeamstressVariantDefiance",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                new Color(0.9f, 0.1f, 0.1f),
                false,
                false);
        }
    }
}
