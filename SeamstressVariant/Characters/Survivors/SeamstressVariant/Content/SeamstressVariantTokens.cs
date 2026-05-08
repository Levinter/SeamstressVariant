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

            string outro = "..and so she left, her heart still beating.";
            string outroFailure = "..and so she vanished, a silent heart, finally at rest.";

            Language.Add(prefix + "NAME", "Seamstress Variant");
            Language.Add(prefix + "DESCRIPTION", desc);
            Language.Add(prefix + "SUBTITLE", "The Tattered Maiden");
            Language.Add(prefix + "LORE", "sample lore");
            Language.Add(prefix + "OUTRO_FLAVOR", outro);
            Language.Add(prefix + "OUTRO_FAILURE", outroFailure);

            #region Skins
            Language.Add(prefix + "MASTERY_SKIN_NAME", "Alternate");
            #endregion

            #region Passive
            Language.Add(prefix + "PASSIVE_NAME", "Her Bleeding Heart");
            Language.Add(prefix + "PASSIVE_DESCRIPTION", "Nearby <color=#9B3737>Bleeding</color> enemies heal you for <style=cIsHealing>4 health</style> per second, per stack. All healing is stored in your <color=#9B3737>Heart</color> instead of restoring health.");
            Language.Add("KEYWORD_HEART", "<style=cKeywordName>Heart</style><style=cSub>Stores healing. Capacity is equal to your <style=cIsHealing>maximum health</style>. Grants <style=cIsDamage>1% hemorrhage chance</style> per <style=cIsHealing>100 health</style> stored in the Heart.</style>");
            Language.Add("KEYWORD_HEMORRHAGE", "<style=cKeywordName>Hemorrhage</style><style=cSub>Deals <style=cIsDamage>2000% base damage</style> over <style=cIsDamage>15 seconds</style>.</style>");
            Language.Add("KEYWORD_HEART_HEMORRHAGE", "<style=cKeywordName>Heart</style><style=cSub>Stores healing. Capacity is equal to your <style=cIsHealing>maximum health</style>. Grants <style=cIsDamage>1% hemorrhage chance</style> per <style=cIsHealing>100 health</style> stored in the Heart.</style><style=cStack>\n\n</style><style=cKeywordName>Hemorrhage</style><style=cSub>Deals <style=cIsDamage>2000% base damage</style> over <style=cIsDamage>15 seconds</style>.</style>");
            #endregion

            #region Primary
            Language.Add(prefix + "PRIMARY_SLASH_NAME", "Claw Slash");
            Language.Add(prefix + "PRIMARY_SLASH_DESCRIPTION", Tokens.agilePrefix + $" Strike with your claws for <style=cIsDamage>{100f * SeamstressVariantStaticValues.clawDamageCoefficient}% damage</style>.");
            #endregion

            #region Secondary
            Language.Add(prefix + "SECONDARY_SCISSORS_NAME", "Symbiotic Scissors");
                Language.Add(prefix + "SECONDARY_SCISSORS_DESCRIPTION", $"Command your scissors to seek out a nearby enemy and strike them for <style=cIsDamage>{100f * SeamstressVariantStaticValues.scissorImpactDamageCoefficient}% damage</style>, then explode for <style=cIsDamage>{100f * SeamstressVariantStaticValues.scissorExplosionDamageCoefficient}% damage</style>.");
            Language.Add("KEYWORD_SYMBIOTIC", $"<style=cKeywordName>Symbiotic</style><style=cSub><style=cIsUtility>While off cooldown</style>, your scissors follow you and cause claw attacks to hit an additional time with increased range for <style=cIsDamage>{100f * SeamstressVariantStaticValues.meleeScissorDamageCoefficient}% damage</style>.</style>");
            #endregion

            #region Utility
            Language.Add(prefix + "UTILITY_BLINK_NAME", "Blink");
            Language.Add(prefix + "UTILITY_BLINK_DESCRIPTION", $"<style=cIsUtility>Invulnerable.</style> Blink a short distance. <style=cIsHealth>Costs health on use</style>. Health cost increases with level.");
            #endregion

            #region Special
            Language.Add(prefix + "SPECIAL_DEFIANT_HEART_NAME", "Defiant Heart");
            Language.Add(prefix + "SPECIAL_DEFIANT_HEART_DESCRIPTION", $"Requires <style=cKeywordName>Heart</style> above 0. Drain <style=cIsHealing>1 Heart per second</style> to gain <style=cKeywordName>Defiance</style>. While active, incoming damage <style=cIsUtility>cannot reduce you below 1 health</style>.");

            Language.Add(prefix + "SPECIAL_DEFIANT_DASH_NAME", "Defiant Dash");
            Language.Add(prefix + "SPECIAL_DEFIANT_DASH_DESCRIPTION", $"Dash forward, dealing <style=cIsDamage>{100f * SeamstressVariantStaticValues.dashDamageCoefficient}% damage + 25% of current <color=#9B3737>Heart</color></style> to enemies in your path and gaining <style=cIsUtility>Defiance</style>. Recast to end early, converting all <color=#9B3737>Heart</color> to <style=cIsHealing>health</style>.");
            Language.Add("KEYWORD_DEFIANCE", "<style=cKeywordName>Defiance</style><style=cSub>While active, incoming damage <style=cIsUtility>cannot reduce you below 1 health</style> and you are <style=cIsUtility>unstoppable</style>, but <style=cIsHealth>drains an increasing amount of Heart per second active</style>.</style>");
            #endregion

            #region Achievements
            Language.Add(Tokens.GetAchievementNameToken(SeamstressVariantMasteryAchievement.identifier), "Seamstress: Mastery");
            Language.Add(Tokens.GetAchievementDescriptionToken(SeamstressVariantMasteryAchievement.identifier), "As Seamstress, beat the game or obliterate on Monsoon.");
            #endregion
        }
    }
}
