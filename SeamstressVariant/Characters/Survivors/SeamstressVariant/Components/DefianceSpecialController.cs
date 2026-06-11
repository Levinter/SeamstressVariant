using UnityEngine;
using UnityEngine.Networking;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    public class DefianceSpecialController : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnDefianceActivationChanged))]
        private bool forceDefianceActivation;
        //private bool forcedDefianceSessionActive;

        public void RequestForcedDefianceActivation()
        {
            forceDefianceActivation = true;
        }

        public void ClearForcedDefianceActivation()
        {
            forceDefianceActivation = false;
        }

        public bool ConsumeForcedDefianceActivation()
        {
            bool wasRequested = forceDefianceActivation;
            forceDefianceActivation = false;
            return wasRequested;
        }

        private void OnDefianceActivationChanged(bool newValue)
        {
            forceDefianceActivation = newValue;
        }

        /*public void MarkForcedDefianceSession()
        {
            forcedDefianceSessionActive = true;
        }

        public bool ConsumeForcedDefianceSession()
        {
            bool shouldConsumeStock = forcedDefianceSessionActive;
            forcedDefianceSessionActive = false;
            return shouldConsumeStock;
        }*/
    }
}