using SeamstressVariant.Survivors.SeamstressVariant.SkillStates;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public static class SeamstressVariantStates
    {
        public static void Init()
        {
            Modules.Content.AddEntityState(typeof(ClawCombo));

            Modules.Content.AddEntityState(typeof(Blink));

            Modules.Content.AddEntityState(typeof(DefiantDash));

            Modules.Content.AddEntityState(typeof(FireScissors));
        }
    }
}
