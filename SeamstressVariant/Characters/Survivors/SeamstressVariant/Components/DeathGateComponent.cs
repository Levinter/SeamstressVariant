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
        private int prevStock;
        public SkillDef mySkillDef;
        private bool IsAuthority => Util.HasEffectiveAuthority(networkId);
        private NetworkIdentity networkId;
        private EntityStateMachine heartEsm;
        private CharacterBody body;
        private HealthComponent healthComponent;
        private SkillLocator skillLocator;
        private GenericSkill specialSkill;
        private DefianceSpecialController defianceSpecialController;

        public void Awake()
        {
            networkId = GetComponent<NetworkIdentity>();
            body = GetComponent<CharacterBody>();
            skillLocator = GetComponent<SkillLocator>();
            healthComponent = GetComponent<HealthComponent>();
            defianceSpecialController = GetComponent<DefianceSpecialController>();
            heartEsm = EntityStateMachine.FindByCustomName(gameObject, "Special");

            specialSkill = skillLocator?.special;

            // Fail fast once for hard dependencies required by this component.
            if (networkId == null || body == null || healthComponent == null || specialSkill == null)
            {
                Log.Warning("DeathGateComponent: missing required components; disabling.");
                enabled = false;
                return;
            }

            if (mySkillDef == null)
            {
                mySkillDef = HealingHeart.specialSkillDef;
            }

            prevStock = specialSkill.stock;
            specialSkillAvailableServer = specialSkill.stock > 0;
        }

        //Check if the player has the special skill available and update the server value
        private void FixedUpdate()
        {
            if (!enabled || !IsAuthority)
            {
                return;
            }

            if (mySkillDef != null && specialSkill.skillDef != mySkillDef)
            {
                return;
            }

            int currentStock = specialSkill.stock;

            bool hasStock = currentStock > 0;
            if (hasStock != specialSkillAvailableServer)
            {
                CmdSpecialSkillAvailable(hasStock);
            }

            prevStock = currentStock;
        }

        [Command]
        public void CmdSpecialSkillAvailable(bool value)
        {
            specialSkillAvailableServer = value;
            Log.Warning($"DeathGateComponent: Special skill availability updated on server: {specialSkillAvailableServer}.");
        }

        [ClientRpc]
        public void RpcActivateSpecialSkill()
        {
            if (enabled && IsAuthority)
            {
                Log.Warning("DeathGateComponent: Activating special skill.");
                /*specialSkill.ExecuteIfReady();
                specialSkill.AddOneStock();*/

                heartEsm.SetNextState(new DefiantHeart());
            }
        }

        //Check for incoming lethal damage and activate the special skill if the player has it available
        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            if (mySkillDef != null && specialSkill.skillDef != mySkillDef)
            {
                return;
            }

            bool incomingDamageIsLethal = damageInfo.damage >= healthComponent.combinedHealth;
            if (!incomingDamageIsLethal || !specialSkillAvailableServer)
            {
                return;
            }

            defianceSpecialController?.RequestForcedDefianceActivation();

            Log.Warning($"DeathGateComponent: Incoming lethal damage detected. Special skill available: {specialSkillAvailableServer}.");

            if (specialSkillAvailableServer && !body.HasBuff(SeamstressVariantBuffs.defianceBuff))
            {
                damageInfo.damageType |= DamageType.NonLethal;

                body.AddBuff(SeamstressVariantBuffs.defianceBuff);

                RpcActivateSpecialSkill();
            }
            else
            {
                defianceSpecialController?.ClearForcedDefianceActivation();
            }
        }
    }
}