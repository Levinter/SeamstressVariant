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

            string desc = "Seamstress Variant is a hyper scaler that uses her health as a resource to fuel her damage and defiance.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine
             + "< ! > Her claw attack is pretty weak, managing her scissors for increased damage is key." + Environment.NewLine + Environment.NewLine
             + "< ! > Stacking health is your priority, the more health, the more bleed chance, the more bleed chance the more healing." + Environment.NewLine + Environment.NewLine
             + "< ! > Blink as no cooldown, but it costs health" + Environment.NewLine + Environment.NewLine
             + "< ! > With enough resources you can maintain Defiant Heart for extenced periods of time, allowing you to tank almost everything." + Environment.NewLine + Environment.NewLine;

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
            Language.Add(prefix + "PASSIVE_NAME", "Bleeding Heart");
            Language.Add(prefix + "PASSIVE_DESCRIPTION", "Nearby <color=#9B3737>Bleeding</color> heals you for <style=cIsHealing>5 health</style> per second, per stack. All healing is stored in your <color=#9B3737>Heart</color>.");
            Language.Add("KEYWORD_HEART_HEMORRHAGE", "<style=cKeywordName>Heart</style><style=cSub>Capacity is equal to your <style=cIsHealing>maximum health</style>. Grants <style=cIsDamage>1% hemorrhage chance</style> per <style=cIsHealing>50 health</style> stored in the Heart (Can overcap).</style><style=cStack>\n\n</style><style=cKeywordName>Hemorrhage</style><style=cSub>Deals <style=cIsDamage>2000% base damage</style> over <style=cIsDamage>15 seconds</style>.</style>");
            #endregion

            #region Primary
            Language.Add(prefix + "PRIMARY_SLASH_NAME", "Claw Slash");
            Language.Add(prefix + "PRIMARY_SLASH_DESCRIPTION", Tokens.agilePrefix + $" Strike with your claws for <style=cIsDamage>{100f * SeamstressVariantStaticValues.clawDamageCoefficient}% damage</style>.");
            #endregion

            #region Secondary
            Language.Add(prefix + "SECONDARY_SCISSORS_NAME", "Symbiotic Scissors");
                Language.Add(prefix + "SECONDARY_SCISSORS_DESCRIPTION", $"Command your scissors to seek out a nearby enemy and strike them, exploding for <style=cIsDamage>{100f * SeamstressVariantStaticValues.scissorExplosionDamageCoefficient}% damage</style> and applying <color=#9B3737>Bleed</color>.");
            Language.Add("KEYWORD_SYMBIOTIC", $"<style=cKeywordName>Symbiotic</style><style=cSub><style=cIsUtility>While off cooldown</style>, your scissors follow you and cause claw attacks to hit an additional time with increased range for <style=cIsDamage>{100f * SeamstressVariantStaticValues.meleeScissorDamageCoefficient}% damage</style>.</style>");
            #endregion

            #region Utility
            Language.Add(prefix + "UTILITY_BLINK_NAME", "Blink");
            Language.Add(prefix + "UTILITY_BLINK_DESCRIPTION", $"<style=cIsUtility>Invulnerable.</style> Blink a short distance. <style=cIsHealth>Costs health on use</style>.");
            #endregion

            #region Special
            Language.Add(prefix + "SPECIAL_HEALING_HEART_NAME", "Healing Heart");
            Language.Add(prefix + "SPECIAL_HEALING_HEART_DESCRIPTION", "Transfer all current <color=#9B3737>Heart</color> to <style=cIsHealing>Health</style> and exit <style=cIsUtility>Defiant Heart</style> if active. Receiving lethal damage will trigger <style=cIsUtility>Defiant Heart</style> if this skill is ready.");
            Language.Add("KEYWORD_DEFIANCE", "<style=cKeywordName>Defiant Heart</style><style=cSub>While active, incoming damage <style=cIsUtility>cannot reduce you below 1 health</style> and you are <style=cIsUtility>Unstoppable</style>, but <color=#9B3737>your heart will bleed out until Death</color>.</style>");
            Language.Add("KEYWORD_UNSTOPPABLE", "<style=cKeywordName>Unstoppable</style><style=cSub>You are immune to <style=cIsUtility>slows</style>, <style=cIsUtility>freeze</style>, <style=cIsUtility>knockback</style>, <style=cIsUtility>roots</style>, <style=cIsUtility>stuns</style>, and <style=cIsUtility>all debuffs</style>.</style>");
            #endregion

            #region Achievements
            Language.Add(Tokens.GetAchievementNameToken(SeamstressVariantMasteryAchievement.identifier), "Seamstress: Mastery");
            Language.Add(Tokens.GetAchievementDescriptionToken(SeamstressVariantMasteryAchievement.identifier), "As Seamstress, beat the game or obliterate on Monsoon.");
            #endregion
        }
    }
}
