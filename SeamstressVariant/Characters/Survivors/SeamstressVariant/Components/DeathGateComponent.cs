using HG;
using RoR2;
using RoR2.Skills;
using SeamstressVariant.Survivors.SeamstressVariant.SkillStates;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    internal class DeathGateComponent : NetworkBehaviour
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

            bool isAvailable = specialSkill.stock > 0;
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

        internal void TryActivateDefiance()
        {
            //If we already have defiance we stop
            if (body.HasBuff(SeamstressVariantBuffs.defianceBuff))
            {
                return;
            }

            //If no special available, we die
            if (!specialSkillAvailableServer)
            {
                return;
            }

            //If zero heart we die 
            if (bleedingHeartComponent.GetHeart() <= 0)
            {
                return;
            }

            body.AddBuff(SeamstressVariantBuffs.defianceBuff);

            //Our health should be zero or less, so we set it to 1 to avoid dying
            //healthComponent.Networkhealth = 1f;
            
            DefianceBenefitsController benefitsController = GetComponent<DefianceBenefitsController>();
            benefitsController.AsValidOrNull()?.ApplyDefianceImmediately();
            RpcActivateSpecialSkill();
        }
    }
}