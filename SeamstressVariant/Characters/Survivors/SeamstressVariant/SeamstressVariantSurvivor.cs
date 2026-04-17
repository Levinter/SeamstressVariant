using BepInEx.Configuration;
using SeamstressVariant.Modules;
using SeamstressVariant.Modules.Characters;
using RoR2;
using RoR2.Skills;
using SeamstressVariant;
using SeamstressMod;
using System;
using System.Collections.Generic;
using UnityEngine;
using SeamstressVariant.Survivors.SeamstressVariant.Components;
using System.Runtime.CompilerServices;

namespace SeamstressVariant.Survivors.SeamstressVariant
{
    public class SeamstressVariantSurvivor : SurvivorBase<SeamstressVariantSurvivor>
    {
        public override string assetBundleName => "none";
        public override string bodyName => "SeamstressVariantBody";
        public override string masterName => "SeamstressVariantMonsterMaster";
        public override string modelPrefabName => "mdlSeamstress";
        public override string displayPrefabName => "SeamstressDisplay";

        public const string HENRY_PREFIX = SeamstressVariantPlugin.DEVELOPER_PREFIX + "_SEAMSTRESS_";

        //used when registering your survivor's language tokens
        public override string survivorTokenPrefix => HENRY_PREFIX;
        
        public override BodyInfo bodyInfo => new BodyInfo
        {
            bodyName = bodyName,
            bodyNameToken = HENRY_PREFIX + "NAME",
            subtitleNameToken = HENRY_PREFIX + "SUBTITLE",

            characterPortrait = assetBundle.LoadAsset<Texture>("texHenryIcon"),
            bodyColor = Color.white,
            sortPosition = 100,

            crosshair = Asset.LoadCrosshair("Standard"),
            podPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/SurvivorPod"),

            maxHealth = 110f,
            healthRegen = 1.5f,
            armor = 0f,

            jumpCount = 1,
        };

        public override CustomRendererInfo[] customRendererInfos => new CustomRendererInfo[]
        {
                new CustomRendererInfo
                    {
                        childName = "Model"
                    },
                    new CustomRendererInfo
                    {
                        childName = "ScissorLModel"
                    },
                    new CustomRendererInfo
                    {
                        childName = "ScissorRModel"
                    },
                    new CustomRendererInfo
                    {
                        childName = "CrownModel"
                    },
                    new CustomRendererInfo
                    {
                        childName = "HeartModel"
                    }
        };

        public override UnlockableDef characterUnlockableDef => SeamstressVariantUnlockables.characterUnlockableDef;
        
        public override ItemDisplaysBase itemDisplays => new SeamstressVariantItemDisplays();

        //set in base classes
        public override AssetBundle assetBundle { get; protected set; }

        public override GameObject bodyPrefab { get; protected set; }
        public override CharacterBody prefabCharacterBody { get; protected set; }
        public override GameObject characterModelObject { get; protected set; }
        public override CharacterModel prefabCharacterModel { get; protected set; }
        public override GameObject displayPrefab { get; protected set; }

        public override void Initialize()
        {
            //uncomment if you have multiple characters
            //ConfigEntry<bool> characterEnabled = Config.CharacterEnableConfig("Survivors", "Henry");

            //if (!characterEnabled.Value)
            //    return;

            base.Initialize();
        }
        

        public override void InitializeCharacter()
        {
            //need the character unlockable before you initialize the survivordef
            SeamstressVariantUnlockables.Init();

            base.InitializeCharacter();

            SeamstressVariantConfig.Init();
            SeamstressVariantStates.Init();
            SeamstressVariantTokens.Init();

            SeamstressVariantAssets.Init(assetBundle);
            SeamstressVariantBuffs.Init(assetBundle);

            InitializeEntityStateMachines();
            InitializeSkills();
            InitializeSkins();
            InitializeCharacterMaster();

            AdditionalBodySetup();

            AddHooks();
        }

        private void AdditionalBodySetup()
        {
            AddHitboxes();
            bodyPrefab.AddComponent<SeamstressVariantWeaponComponent>();
            
            // Add heart component for passive
            BleedingHeartComponent heart = bodyPrefab.AddComponent<BleedingHeartComponent>();
            // maxHeart will be set to character's max health in Start()

            // Add overlay controller to drive the Heart meter UI (reuses VoidSurvivor corruption bar)
            bodyPrefab.AddComponent<HeartOverlayController>();

            //bodyPrefab.AddComponent<HuntressTrackerComopnent>();
            //anything else here
        }

        public void AddHitboxes()
        {
            Prefabs.SetupHitBoxGroup(characterModelObject, "Sword", "SwordHitbox");
            Prefabs.SetupHitBoxGroup(characterModelObject, "SwordBig", "SwordHitboxBig");
            Prefabs.SetupHitBoxGroup(characterModelObject, "Weave", "WeaveHitbox");
            Prefabs.SetupHitBoxGroup(characterModelObject, "WeaveBig", "WeaveHitboxBig");
            Prefabs.SetupHitBoxGroup(characterModelObject, "Right", "RightScissorHitbox");
            Prefabs.SetupHitBoxGroup(characterModelObject, "Left", "LeftScissorHitbox");
        }

        public override void InitializeEntityStateMachines() 
        {
            //clear existing state machines from your cloned body (probably commando)
            //omit all this if you want to just keep theirs
            Prefabs.ClearEntityStateMachines(bodyPrefab);

            //the main "Body" state machine has some special properties
            Prefabs.AddMainEntityStateMachine(bodyPrefab, "Body", typeof(EntityStates.GenericCharacterMain), typeof(EntityStates.SpawnTeleporterState));
            //if you set up a custom main characterstate, set it up here
                //don't forget to register custom entitystates in your HenryStates.cs

            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon");
            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon2");
            Prefabs.AddEntityStateMachine(bodyPrefab, "Special");
        }

        #region skills
        public override void InitializeSkills()
        {
            //remove the genericskills from the commando body we cloned
            Skills.ClearGenericSkills(bodyPrefab);
            //add our own
            AddPassiveSkill();
            AddPrimarySkills();
            AddSecondarySkills();
            AddUtiitySkills();
            AddSpecialSkills();
        }

        //our passive redirects healing into heart that can be used as a resource
        private void AddPassiveSkill()
        {
            bodyPrefab.GetComponent<SkillLocator>().passiveSkill = new SkillLocator.PassiveSkill
            {
                enabled = true,
                skillNameToken = HENRY_PREFIX + "PASSIVE_NAME",
                skillDescriptionToken = HENRY_PREFIX + "PASSIVE_DESCRIPTION",
                keywordToken = "KEYWORD_HEART",
                icon = assetBundle.LoadAsset<Sprite>("texPassiveIcon"),
            };

            //option 2. a new SkillFamily for a passive, used if you want multiple selectable passives
            GenericSkill passiveGenericSkill = Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, "PassiveSkill");
            SkillDef passiveSkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "HenryPassive",
                skillNameToken = HENRY_PREFIX + "PASSIVE_NAME",
                skillDescriptionToken = HENRY_PREFIX + "PASSIVE_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("texPassiveIcon"),

                //unless you're somehow activating your passive like a skill, none of the following is needed.
                //but that's just me saying things. the tools are here at your disposal to do whatever you like with

                //activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Shoot)),
                //activationStateMachineName = "Weapon1",
                //interruptPriority = EntityStates.InterruptPriority.Skill,

                //baseRechargeInterval = 1f,
                //baseMaxStock = 1,

                //rechargeStock = 1,
                //requiredStock = 1,
                //stockToConsume = 1,

                //resetCooldownTimerOnUse = false,
                //fullRestockOnAssign = true,
                //dontAllowPastMaxStocks = false,
                //mustKeyPress = false,
                //beginSkillCooldownOnSkillEnd = false,

                //isCombatSkill = true,
                //canceledFromSprinting = false,
                //cancelSprintingOnActivation = false,
                //forceSprintDuringState = false,

            });
            Skills.AddSkillsToFamily(passiveGenericSkill.skillFamily, passiveSkillDef1);
        }

        //if this is your first look at skilldef creation, take a look at Secondary first
        private void AddPrimarySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Primary);

            //the primary skill is created using a constructor for a typical primary
            //it is also a SteppedSkillDef. Custom Skilldefs are very useful for custom behaviors related to casting a skill. see ror2's different skilldefs for reference
            SteppedSkillDef primarySkillDef1 = Skills.CreateSkillDef<SteppedSkillDef>(new SkillDefInfo
                (
                    "HenrySlash",
                    HENRY_PREFIX + "PRIMARY_SLASH_NAME",
                    HENRY_PREFIX + "PRIMARY_SLASH_DESCRIPTION",
                    assetBundle.LoadAsset<Sprite>("texPrimaryIcon"),
                    new EntityStates.SerializableEntityStateType(typeof(SkillStates.SlashCombo)),
                    "Weapon",
                    true
                ));
            //custom Skilldefs can have additional fields that you can set manually
            primarySkillDef1.stepCount = 2;
            primarySkillDef1.stepGraceDuration = 0.5f;

            Skills.AddPrimarySkills(bodyPrefab, primarySkillDef1);
        }

        private void AddSecondarySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Secondary);

            //here is a basic skill def with all fields accounted for
            SkillDef secondarySkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "HenryGun",
                skillNameToken = HENRY_PREFIX + "SECONDARY_GUN_NAME",
                skillDescriptionToken = HENRY_PREFIX + "SECONDARY_GUN_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("texSecondaryIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Shoot)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseRechargeInterval = 1f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,

            });

            Skills.AddSecondarySkills(bodyPrefab, secondarySkillDef1);
        }

        private void AddUtiitySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Utility);

            //here's a skilldef of a typical movement skill.
            SkillDef utilitySkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "HenryRoll",
                skillNameToken = HENRY_PREFIX + "UTILITY_ROLL_NAME",
                skillDescriptionToken = HENRY_PREFIX + "UTILITY_ROLL_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Roll)),
                activationStateMachineName = "Body",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseRechargeInterval = 4f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = true,
            });

            Skills.AddUtilitySkills(bodyPrefab, utilitySkillDef1);
        }

        private void AddSpecialSkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Special);

            //a basic skill. some fields are omitted and will just have default values
            SkillDef specialSkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "HenryDefiantHeart",
                skillNameToken = HENRY_PREFIX + "SPECIAL_DEFIANT_HEART_NAME",
                skillDescriptionToken = HENRY_PREFIX + "SPECIAL_DEFIANT_HEART_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texSpecialIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.DefiantHeart)),
                // Dedicated machine so this sustained special does not block Secondary (Weapon2).
                activationStateMachineName = "Special", interruptPriority = EntityStates.InterruptPriority.Skill,

                baseMaxStock = 1,
                baseRechargeInterval = 10f,
                beginSkillCooldownOnSkillEnd = true,

                isCombatSkill = true,
                mustKeyPress = false,
            });

            Skills.AddSpecialSkills(bodyPrefab, specialSkillDef1);
        }
        #endregion skills
        
        #region skins
        public override void InitializeSkins()
        {
            ModelSkinController skinController = prefabCharacterModel.gameObject.GetComponent<ModelSkinController>();
            if (skinController == null)
                skinController = prefabCharacterModel.gameObject.AddComponent<ModelSkinController>();

            CharacterModel.RendererInfo[] defaultRenderers = prefabCharacterModel.baseRendererInfos;

            List<SkinDef> skins = new List<SkinDef>();

            //this creates a SkinDef with all default fields
            SkinDef defaultSkin = Modules.Skins.CreateSkinDef("DEFAULT_SKIN",
                assetBundle.LoadAsset<Sprite>("texMainSkin"),
                defaultRenderers,
                prefabCharacterModel.gameObject);

            //these are your Mesh Coverage Coverage Coverages
            //simply uncomment this and change the action to whatever TF you need
            //defaultSkin.meshReplacements = Modules.Skins.getMeshReplacements(assetBundle, defaultRenderers,
            //    "meshHenrySword",
            //    "meshHenryGun",
            //    "meshHenry");

            skins.Add(defaultSkin);

            //uncomment this when you have a mastery skin
            //MasterySkin
            //SkinDef masterySkin = Modules.Skins.CreateSkinDef(HENRY_PREFIX + "MASTERY_SKIN",
            //    assetBundle.LoadAsset<Sprite>("texMasteryAchievement"),
            //    defaultRenderers,
            //    prefabCharacterModel.gameObject,
            //    SeamstressVariantUnlockables.masterySkinUnlockableDef);

            //masterySkin.meshReplacements = Modules.Skins.getMeshReplacements(assetBundle, defaultRenderers,
            //    "meshHenrySwordAlt",
            //    null,
            //    "meshHenryAlt");

            //skins.Add(masterySkin);

            skinController.skins = skins.ToArray();
        }
        #endregion skins


        //Character Master is what governs the AI of your character when it is not controlled by a player (artifact of vengeance, goobo)
        public override void InitializeCharacterMaster()
        {
            //you must only do one of these. adding duplicate masters breaks the game.

            //if you're lazy or prototyping you can simply copy the AI of a different character to be used
            //Modules.Prefabs.CloneDopplegangerMaster(bodyPrefab, masterName, "Merc");

            //how to set up AI in code
            SeamstressVariantAI.Init(bodyPrefab, masterName);

            //how to load a master set up in unity, can be an empty gameobject with just AISkillDriver components
            //assetBundle.LoadMaster(bodyPrefab, masterName);
        }

        private void AddHooks()
        {
            On.RoR2.HealthComponent.Heal += HealthComponent_Heal;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {
            var heart = sender.GetComponent<BleedingHeartComponent>();

            if (heart != null)
            {
                //base bleed chance
                args.bleedChanceAdd = 5;
                // 1% bleed chance per x(config) Heart.
                args.bleedChanceAdd += heart.GetBleedChanceBonusFromHeart();

                if (heart.IsHeartFull())
                {
                    args.bleedChanceAdd += 5; // Extra 5% bleed chance on full heart
                }
            }
        }

        private float HealthComponent_Heal(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen)
        {
            if (self.body.bodyIndex == BodyCatalog.FindBodyIndex("SeamstressVariantBody") && self.alive){
                float previousHealth = self.health;
                float incomingHeal = Mathf.Max(0f, amount);
                float healed = orig(self, amount, procChainMask, nonRegen);

                if (incomingHeal > 0f)
                {
                    var heart = self.GetComponent<BleedingHeartComponent>();
                    
                    if (heart != null)
                    {
                        // Redirect attempted healing into Heart, even when HP is already full.
                        heart.AddToHeart(incomingHeal);
                        self.health = previousHealth;
                    }
                }

                return healed;
            } else {
                return orig (self, amount, procChainMask, nonRegen);
            }
        }

        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (self != null
                && self.body != null
                && self.body.bodyIndex == BodyCatalog.FindBodyIndex("SeamstressVariantBody")
                && self.body.HasBuff(SeamstressVariantBuffs.defianceBuff)
                && damageInfo != null
                && damageInfo.damage > 0f)
            {
                damageInfo.damageType |= DamageType.NonLethal;
                float maxAllowedDamage = Mathf.Max(0f, self.health - 1f);
                damageInfo.damage = Mathf.Min(damageInfo.damage, maxAllowedDamage);
            }

            orig(self, damageInfo);
        }
    }
}