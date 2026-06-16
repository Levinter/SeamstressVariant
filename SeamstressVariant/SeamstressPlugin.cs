using BepInEx;
using R2API.Utils;
using SeamstressVariant.Modules;
using SeamstressVariant.Survivors.SeamstressVariant;
using System.Security;
using System.Security.Permissions;


[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SeamstressVariant
{
    //[BepInDependency("com.rune580.riskofoptions")]
    [BepInDependency("com.kenko.Seamstress", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ProcTypeAPI.PluginGUID)]
    [BepInDependency(Survariants.Survariants.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    public class SeamstressVariantPlugin : BaseUnityPlugin
    {
        public const string MODUID = "com.levinter.SeamstressVariant";
        public const string MODNAME = "SeamstressVariant";
        public const string MODVERSION = "1.0.0";

        // a prefix for name tokens to prevent conflicts- please capitalize all name tokens for convention
        public const string DEVELOPER_PREFIX = "LEVINTER";

        public static SeamstressVariantPlugin instance;

        void Awake()
        {
            instance = this;

            //easy to use logger
            Log.Init(Logger);

            // used when you want to properly set up language folders
            Language.Init();

            // character initialization
            new SeamstressVariantSurvivor().Initialize();

            // make a content pack and add it. this has to be last
            new ContentPacks().Initialize();
        }
    }
}
