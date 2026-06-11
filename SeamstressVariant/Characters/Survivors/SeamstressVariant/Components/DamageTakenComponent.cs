using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    internal class DamageTakenComponent : NetworkBehaviour, IOnIncomingDamageServerReceiver
    {
        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            throw new System.NotImplementedException();
        }
    }
}