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

        public void Awake()
        {
            networkId = GetComponent<NetworkIdentity>();
            body = GetComponent<CharacterBody>();
            skillLocator = GetComponent<SkillLocator>();
            healthComponent = GetComponent<HealthComponent>();
            heartEsm = EntityStateMachine.FindByCustomName(gameObject, "Special");
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
                Log.Fatal("DeathGateComponent: Activating special skill.");
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

            bool incomingDamageIsLethal = damageInfo.damage >= healthComponent.combinedHealth;
            if (!incomingDamageIsLethal)
            {
                return;
            }

            Log.Fatal($"DeathGateComponent: Incoming lethal damage detected.");

            // dont activate if we have a different special skill
            if (specialSkill.skillDef != HealingHeart.specialSkillDef)
            {
                return;
            }

            Log.Warning("DeathGateComponent: Special skill available for activation: " + specialSkillAvailableServer);

            // dont activate if the special skill is not available
            if (!specialSkillAvailableServer)
            {
                return;
            }
            
            damageInfo.damageType |= DamageType.NonLethal;

            body.AddBuff(SeamstressVariantBuffs.defianceBuff);

            Log.Warning("Defiance buff applied? " + body.HasBuff(SeamstressVariantBuffs.defianceBuff));

            RpcActivateSpecialSkill();
        }
    }
}