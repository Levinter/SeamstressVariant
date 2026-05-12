using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantBuffs
    {
        // heart visualization buff
        public static BuffDef heartBuff;

        public static BuffDef scissorLeftBuff;
        public static BuffDef scissorRightBuff;

        // active special buff: while present, damage cannot reduce health below 1
        public static BuffDef defianceBuff;

        public static void Init(AssetBundle assetBundle)
        {
            scissorLeftBuff = Modules.Content.CreateAndAddBuff("SeamstressVariantScissorLeft",
                Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/VoidSurvivor/texBuffVoidSurvivorCorruptionIcon.tif").WaitForCompletion(),
                new Color(0.545f, 0.133f, 0.133f),
                false,
                false);

            scissorRightBuff = Modules.Content.CreateAndAddBuff("SeamstressVariantScissorRight",
                Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/VoidSurvivor/texBuffVoidSurvivorCorruptionIcon.tif").WaitForCompletion(),
                new Color(0.545f, 0.133f, 0.133f),
                false,
                false);

            defianceBuff = Modules.Content.CreateAndAddBuff("SeamstressVariantDefiance",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                new Color(0.9f, 0.1f, 0.1f),
                false,
                false);
        }
    }
}
