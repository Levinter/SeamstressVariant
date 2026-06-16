using RoR2;
using RoR2.Skills;
using SeamstressVariant.Survivors.SeamstressVariant.SkillStates;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    internal class DeathGateComponent : NetworkBehaviour, IOnIncomingDamageServerReceiver
    {
        private bool specialSkillAvailableServer;
        private bool IsAuthority => Util.HasEffectiveAuthority(networkId);
        private NetworkIdentity networkId;
        private EntityStateMachine heartEsm;
        private CharacterBody body;
        private HealthComponent healthComponent;
        private SkillLocator skillLocator;
        private GenericSkill specialSkill;
        private BleedingHeartComponent bleedingHeartComponent;

        public void Awake()
        {
            networkId = GetComponent<NetworkIdentity>();
            body = GetComponent<CharacterBody>();
            skillLocator = GetComponent<SkillLocator>();
            healthComponent = GetComponent<HealthComponent>();
            heartEsm = EntityStateMachine.FindByCustomName(gameObject, "Special");
            bleedingHeartComponent = GetComponent<BleedingHeartComponent>();
        }

        private void Start()
        {
            // timing issues are a bitch
            specialSkill = skillLocator.special;
        }

        //Check if the player has the special skill available and update the server value
        private void FixedUpdate()
        {
            if (!IsAuthority)
            {
                return;
            }

            bool isAvailable = specialSkill.stock > 0 && specialSkill.skillDef == HealingHeart.specialSkillDef;
            if (isAvailable != specialSkillAvailableServer)
            {
                CmdSpecialSkillAvailable(isAvailable);
            }

            specialSkillAvailableServer = isAvailable;
        }

        [Command]
        public void CmdSpecialSkillAvailable(bool value)
        {
            specialSkillAvailableServer = value;
            //Log.Warning($"DeathGateComponent: Special skill availability updated on server: {specialSkillAvailableServer}.");
        }

        [ClientRpc]
        public void RpcActivateSpecialSkill()
        {
            if (IsAuthority)
            {
                //Log.Fatal("DeathGateComponent: Activating special skill.");
                /*specialSkill.ExecuteIfReady();
                specialSkill.AddOneStock();*/

                heartEsm.SetNextState(new DefiantHeart());
                //specialSkill.DeductStock(1);
            }
        }

        //Check for incoming lethal damage and activate the special skill if the player has it available
        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            // dont activate if we are already defying death
            if (body.HasBuff(SeamstressVariantBuffs.defianceBuff))
            {
                return;
            }

            //Log.Warning("Incoming Damage Type: " + damageInfo.damageType);

            bool incomingDamageIsLethal = damageInfo.damage >= healthComponent.combinedHealth && (damageInfo.damageType & DamageType.NonLethal) == 0;
            if (!incomingDamageIsLethal)
            {
                return;
            }

            //Log.Fatal($"DeathGateComponent: Incoming lethal damage detected.");

            // dont activate if we have a different special skill
            if (specialSkill.skillDef != HealingHeart.specialSkillDef)
            {
                return;
            }

            //Log.Warning("DeathGateComponent: Special skill available for activation: " + specialSkillAvailableServer);

            // dont activate if the special skill is not available
            if (!specialSkillAvailableServer)
            {
                return;
            }

            //check if theres something stored in heart
            if (bleedingHeartComponent.GetHeart() <= 0)
            {
                return;
            }
            
            damageInfo.damageType |= DamageType.NonLethal;

            body.AddBuff(SeamstressVariantBuffs.defianceBuff);

            //Log.Warning("Defiance buff applied? " + body.HasBuff(SeamstressVariantBuffs.defianceBuff));

            RpcActivateSpecialSkill();
        }
    }
}