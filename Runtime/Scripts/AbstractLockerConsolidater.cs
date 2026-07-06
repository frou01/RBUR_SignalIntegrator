using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class AbstractLockerConsolidater : UdonSharpBehaviour
    {
        [HideInInspector] public bool[] RecievedLockState;
        [HideInInspector] public Interlocking[] linkedInterlocks;
        //Return: Success?
        public virtual bool tryLocking(int index, bool lockState)
        {
            RecievedLockState[index] = lockState;
            return true;
        }
    }
}