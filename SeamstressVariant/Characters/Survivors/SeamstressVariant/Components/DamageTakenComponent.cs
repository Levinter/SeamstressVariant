using RoR2;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    internal class DamageTakenComponent : NetworkBehaviour, IOnIncomingDamageServerReceiver, IOnTakeDamageServerReceiver
    {
        public CharacterBody body;

        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            
        }
    }
}