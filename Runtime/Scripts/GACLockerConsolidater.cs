
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class GACLockerConsolidater : UdonSharpBehaviour
    {
        [HideInInspector] public bool[] RecievedBools;
        public Controller_Base target;

        bool locked = true;
        public void perform(int index, bool sent)
        {
            RecievedBools[index] = sent;
            locked = false;
            foreach (bool anRcvd in RecievedBools)
            {
                locked |= anRcvd;
            }
            target.locked = locked;
        }
    }
}
