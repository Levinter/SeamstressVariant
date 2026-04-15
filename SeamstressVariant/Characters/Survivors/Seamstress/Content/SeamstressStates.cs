using SeamstressVariant.Survivors.Seamstress.SkillStates;

namespace SeamstressVariant.Survivors.Seamstress
{
    public static class SeamstressStates
    {
        public static void Init()
        {
            Modules.Content.AddEntityState(typeof(SlashCombo));

            Modules.Content.AddEntityState(typeof(Shoot));

            Modules.Content.AddEntityState(typeof(Roll));

            Modules.Content.AddEntityState(typeof(DefiantHeart));
        }
    }
}
