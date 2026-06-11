using RoR2;
using RoR2.Skills;
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
            specialSkill = skillLocator?.special;
            prevStock = specialSkill.stock;
            specialSkillAvailableServer = specialSkill.stock > 0;
        }

        //Check if the player has the special skill available and update the server value
        private void FixedUpdate()
        {
            int currentStock = specialSkill.stock;
            bool hasStock = currentStock > 0;

            if(specialSkill.skillDef != mySkillDef && IsAuthority)
            {
                if(specialSkill.stock != prevStock)
                {
                    CmdSpecialSkillAvailable(hasStock);
                }
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
            if (IsAuthority)
            {
                Log.Warning("DeathGateComponent: Activating special skill.");
                specialSkill.ExecuteIfReady();
                specialSkill.AddOneStock();
            }
        }

        //Check for incoming lethal damage and activate the special skill if the player has it available
        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            bool incomingDamageIsLethal = damageInfo.damage >= healthComponent.health;
            if (!incomingDamageIsLethal || !specialSkillAvailableServer)
            {
                return;
            }

            defianceSpecialController?.RequestForcedDefianceActivation();

            Log.Warning($"DeathGateComponent: Incoming lethal damage detected. Special skill available: {specialSkillAvailableServer}.");

            if (specialSkillAvailableServer)
            {
                // Let the lethal hit resolve to exactly 1 HP instead of preserving current HP.
                float damageToLeaveOneHp = Mathf.Max(healthComponent.health - 1f, 0f);
                damageInfo.damageType |= DamageType.NonLethal;
                damageInfo.damage = damageToLeaveOneHp;

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