using SeamstressVariant.Survivors.SeamstressVariant.SkillStates;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantStates
    {
        public static void Init()
        {
            Modules.Content.AddEntityState(typeof(SlashCombo));

            Modules.Content.AddEntityState(typeof(Shoot));

            Modules.Content.AddEntityState(typeof(Blink));

            Modules.Content.AddEntityState(typeof(DefiantDash));
        }
    }
}
