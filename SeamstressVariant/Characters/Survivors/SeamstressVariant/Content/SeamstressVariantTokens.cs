using System;
using SeamstressVariant.Modules;
using SeamstressVariant.Survivors.SeamstressVariant.Achievements;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantTokens
    {
        public static void Init()
        {
            AddSeamstressVariantTokens();

            ////uncomment this to spit out a lanuage file with all the above tokens that people can translate
            ////make sure you set Language.usingLanguageFolder and printingEnabled to true
            //Language.PrintOutput("SeamstressVariant.txt");
            ////refer to guide on how to build and distribute your mod with the proper folders
        }

        public static void AddSeamstressVariantTokens()
        {
            string prefix = SeamstressVariantSurvivor.SEAMSTRESS_VARIANT_PREFIX;

            string desc = "Henry is a skilled fighter who makes use of a wide arsenal of weaponry to take down his foes.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine
             + "< ! > Sword is a good all-rounder while Boxing Gloves are better for laying a beatdown on more powerful foes." + Environment.NewLine + Environment.NewLine
             + "< ! > Pistol is a powerful anti air, with its low cooldown and high damage." + Environment.NewLine + Environment.NewLine
             + "< ! > Blink is a short invulnerable reposition tool that rewards precise timing and spacing." + Environment.NewLine + Environment.NewLine
             + "< ! > Defiant Heart drains Heart each second and grants Defiance, preventing death while active." + Environment.NewLine + Environment.NewLine;

            string outro = "..and so he left, searching for a new identity.";
            string outroFailure = "..and so he vanished, forever a blank slate.";

            Language.Add(prefix + "NAME", "Henry");
            Language.Add(prefix + "DESCRIPTION", desc);
            Language.Add(prefix + "SUBTITLE", "The Chosen One");
            Language.Add(prefix + "LORE", "sample lore");
            Language.Add(prefix + "OUTRO_FLAVOR", outro);
            Language.Add(prefix + "OUTRO_FAILURE", outroFailure);

            #region Skins
            Language.Add(prefix + "MASTERY_SKIN_NAME", "Alternate");
            #endregion

            #region Passive
            Language.Add(prefix + "PASSIVE_NAME", "Bleeding Heart");
            Language.Add(prefix + "PASSIVE_DESCRIPTION", "All healing is converted into <style=cKeywordName>Heart</style> instead of restoring health. Heart capacity is <style=cIsHealing>1:1 with your maximum health</style>. Every second, gain <style=cIsHealing>5 Heart</style> per active <style=cIsDamage>Bleed</style> stack on nearby enemies. Gain <style=cIsDamage>+5% Bleed chance</style>, plus <style=cIsDamage>+1% per 100 Heart</style>, and an extra <style=cIsDamage>+5%</style> while Heart is full.");
            Language.Add("KEYWORD_HEART", "<style=cKeywordName>Heart</style><style=cSub>Stored healing. Capacity is equal to your maximum health (1:1). Increases Bleed chance by +1% per 100 Heart. At full Heart, gain an additional +5% Bleed chance.</style>");
            #endregion

            #region Primary
            Language.Add(prefix + "PRIMARY_SLASH_NAME", "Sword");
            Language.Add(prefix + "PRIMARY_SLASH_DESCRIPTION", Tokens.agilePrefix + $"Swing forward for <style=cIsDamage>{100f * SeamstressVariantStaticValues.swordDamageCoefficient}% damage</style>.");
            #endregion

            #region Secondary
            Language.Add(prefix + "SECONDARY_GUN_NAME", "Handgun");
            Language.Add(prefix + "SECONDARY_GUN_DESCRIPTION", Tokens.agilePrefix + $"Fire a handgun for <style=cIsDamage>{100f * SeamstressVariantStaticValues.gunDamageCoefficient}% damage</style>.");
            #endregion

            #region Utility
            Language.Add(prefix + "UTILITY_BLINK_NAME", "Blink");
            Language.Add(prefix + "UTILITY_BLINK_DESCRIPTION", "Blink a short distance. <style=cIsHealth>Costs health on use</style>. <style=cIsUtility>You cannot be hit during the blink.</style>");
            #endregion

            #region Special
            Language.Add(prefix + "SPECIAL_DEFIANT_HEART_NAME", "Defiant Heart");
            Language.Add(prefix + "SPECIAL_DEFIANT_HEART_DESCRIPTION", "Enter a defiant state while <style=cIsHealing>Heart is above 1</style>. Drain <style=cIsHealing>Heart once per second</style> and gain <style=cIsUtility>Defiance</style>. While Defiance is active, incoming damage <style=cIsUtility>cannot reduce you below 1 health</style>.");
            #endregion

            #region Achievements
            Language.Add(Tokens.GetAchievementNameToken(SeamstressVariantMasteryAchievement.identifier), "Seamstress: Mastery");
            Language.Add(Tokens.GetAchievementDescriptionToken(SeamstressVariantMasteryAchievement.identifier), "As Seamstress, beat the game or obliterate on Monsoon.");
            #endregion
        }
    }
}
