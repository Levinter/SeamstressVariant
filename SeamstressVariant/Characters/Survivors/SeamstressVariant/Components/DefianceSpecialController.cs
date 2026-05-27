using UnityEngine;

namespace SeamstressVariant.Survivors.SeamstressVariant.Components
{
    public class DefianceSpecialController : MonoBehaviour
    {
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