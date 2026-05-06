using BepInEx.Configuration;
using SeamstressVariant.Modules;
using SeamstressVariant.Modules.Characters;
using RoR2;
using RoR2.Skills;

using SeamstressVariant;
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
        public override string modelPrefabName => "mdlseamstress";
        public override string displayPrefabName => "SeamstressDisplay";

        public const string SEAMSTRESS_VARIANT_PREFIX = SeamstressVariantPlugin.DEVELOPER_PREFIX + "_SEAMSTRESS_";

        //used when registering your survivor's language tokens
        public override string survivorTokenPrefix => SEAMSTRESS_VARIANT_PREFIX;
        
        public override BodyInfo bodyInfo => new BodyInfo
        {
            bodyName = bodyName,
            bodyNameToken = SEAMSTRESS_VARIANT_PREFIX + "NAME",
            subtitleNameToken = SEAMSTRESS_VARIANT_PREFIX + "SUBTITLE",

            characterPortrait = SeamstressMod.Seamstress.Content.SeamstressAssets.mainAssetBundle.LoadAsset<Sprite>("texSeamstressIcon").texture,
            bodyColor = Color.white,
            sortPosition = 100,

            crosshair = Asset.LoadCrosshair("SimpleDot"),
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
                    childName = "SwordModel",
                    material = null,
                },
                new CustomRendererInfo
                {
                    childName = "GunModel",
                },
                new CustomRendererInfo
                {
                    childName = "Model",
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
            
            // Add heart component for passive
            BleedingHeartComponent heart = bodyPrefab.AddComponent<BleedingHeartComponent>();
            // maxHeart will be set to character's max health in Start()

            // Add overlay controller to drive the Heart meter UI (reuses VoidSurvivor corruption bar)
            bodyPrefab.AddComponent<HeartOverlayController>();

            // Add scissor controller — passive independent scissor state, not tied to any skill stock.
            bodyPrefab.AddComponent<ScissorController>();

            // Add tracker — drives the Huntress tracking indicator and is read by FireScissors on cast.
            bodyPrefab.AddComponent<SeamstressTracker>();
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
            AddUtilitySkills();
            AddSpecialSkills();
        }

        //our passive redirects healing into heart that can be used as a resource
        private void AddPassiveSkill()
        {
            bodyPrefab.GetComponent<SkillLocator>().passiveSkill = new SkillLocator.PassiveSkill
            {
                enabled = true,
                skillNameToken = SEAMSTRESS_VARIANT_PREFIX + "PASSIVE_NAME",
                skillDescriptionToken = SEAMSTRESS_VARIANT_PREFIX + "PASSIVE_DESCRIPTION",
                keywordToken = "KEYWORD_HEART_HEMORRHAGE",
                icon = assetBundle.LoadAsset<Sprite>("texItHungersIcon"),
            };
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
                    SEAMSTRESS_VARIANT_PREFIX + "PRIMARY_SLASH_NAME",
                    SEAMSTRESS_VARIANT_PREFIX + "PRIMARY_SLASH_DESCRIPTION",
                    assetBundle.LoadAsset<Sprite>("texFlurryIcon"),
                    new EntityStates.SerializableEntityStateType(typeof(SkillStates.ClawCombo)),
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

            SkillDef secondarySkillDef1 = Skills.CreateSkillDef<SeamstressTrackingSkillDef>(new SkillDefInfo
            {
                skillName = "FireScissors",
                skillNameToken = SEAMSTRESS_VARIANT_PREFIX + "SECONDARY_SCISSORS_NAME",
                skillDescriptionToken = SEAMSTRESS_VARIANT_PREFIX + "SECONDARY_SCISSORS_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_SYMBIOTIC" },
                skillIcon =  assetBundle.LoadAsset<Sprite>("texSkewerIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.FireScissors)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseRechargeInterval = 10f,
                baseMaxStock = 2,

                rechargeStock = 2,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = false,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = true,
                forceSprintDuringState = false,

            });

            Skills.AddSecondarySkills(bodyPrefab, secondarySkillDef1);
        }

        private void AddUtilitySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Utility);

            //here's a skilldef of a typical movement skill.
            SkillDef utilitySkillDef1 = Skills.CreateSkillDef<BlinkSkillDef>(new SkillDefInfo
            {
                skillName = "HenryBlink",
                skillNameToken = SEAMSTRESS_VARIANT_PREFIX + "UTILITY_BLINK_NAME",
                skillDescriptionToken = SEAMSTRESS_VARIANT_PREFIX + "UTILITY_BLINK_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texImpTouchedIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Blink)),
                activationStateMachineName = "Body",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseRechargeInterval = 0.2f,
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

            // DefiantDash is now the unified special skill (Dash + Sustained Defiance)
            SkillDef specialSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "HenryDefiantDash",
                skillNameToken = SEAMSTRESS_VARIANT_PREFIX + "SPECIAL_DEFIANT_DASH_NAME",
                skillDescriptionToken = SEAMSTRESS_VARIANT_PREFIX + "SPECIAL_DEFIANT_DASH_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texGlimpseOfPurityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.DefiantDash)),
                activationStateMachineName = "Special",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                keywordTokens = new string[] { "KEYWORD_DEFIANCE", "KEYWORD_HEART" },

                baseMaxStock = 1,
                baseRechargeInterval = 16f,
                beginSkillCooldownOnSkillEnd = true,

                isCombatSkill = true,
                mustKeyPress = true,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = true,
            });

            Skills.AddSpecialSkills(bodyPrefab, specialSkillDef);
        }
        #endregion skills
        
        #region skins
        public override void InitializeSkins()
        {
            ModelSkinController skinController = prefabCharacterModel.gameObject.GetComponent<ModelSkinController>()
                ?? prefabCharacterModel.gameObject.AddComponent<ModelSkinController>();

            SkinDef defaultSkin = Skins.CreateSkinDef("DEFAULT_SKIN", assetBundle.LoadAsset<Sprite>("texMainSkin"), prefabCharacterModel.baseRendererInfos, prefabCharacterModel.gameObject);
            skinController.skins = new[] { defaultSkin };
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
            GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {
        }

        private void GlobalEventManager_onServerDamageDealt(DamageReport report)
        {
            DamageInfo damageInfo = report?.damageInfo;
            CharacterBody attackerBody = report?.attackerBody;
            CharacterBody victimBody = report?.victimBody;

            if (attackerBody == null || victimBody == null || damageInfo == null)
            {
                return;
            }

            if (attackerBody.bodyIndex != BodyCatalog.FindBodyIndex(bodyName))
            {
                return;
            }

            if (damageInfo.damage <= 0f || damageInfo.procCoefficient <= 0f)
            {
                return;
            }

            if ((damageInfo.damageType & DamageType.DoT) != 0 || damageInfo.dotIndex != DotController.DotIndex.None)
            {
                return;
            }

            BleedingHeartComponent heart = attackerBody.GetComponent<BleedingHeartComponent>();
            if (heart == null)
            {
                return;
            }

            float hemorrhageChance = heart.GetBleedChanceBonusFromHeart();
            if (!Util.CheckRoll(hemorrhageChance * damageInfo.procCoefficient, attackerBody.master))
            {
                return;
            }

            InflictDotInfo inflictDotInfo = new InflictDotInfo
            {
                victimObject = victimBody.gameObject,
                attackerObject = attackerBody.gameObject,
                hitHurtBox = damageInfo.inflictedHurtbox,
                dotIndex = DotController.DotIndex.SuperBleed,
                duration = 15f * damageInfo.procCoefficient,
                damageMultiplier = 1f
            };

            DotController.InflictDot(ref inflictDotInfo);
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