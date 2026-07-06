
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class GACLockerConsolidater : AbstractLockerConsolidater
    {
        [HideInInspector] public float[] RecievedLockPosition;
        public Controller_Base target;

        bool locked = true;

        //Return: Success?
        public override bool tryLocking(int index, bool lockState)
        {
            base.tryLocking(index, lockState);

            float lockAngle = float.NaN;
            int checkingIndex = 0;
            bool lockFault = false;
            foreach (bool anRcvd in RecievedLockState)
            {
                locked |= anRcvd;
                if (anRcvd)
                {
                    if (float.IsNaN(lockAngle)) lockAngle = RecievedLockPosition[checkingIndex];
                    else if(lockAngle != RecievedLockPosition[checkingIndex]) lockFault = true;
                }

                checkingIndex++;
            }
            if (lockFault)
            {
                locked = false;
            }
            if (locked)
            {
                target.locked = locked;
                if (!float.IsNaN(lockAngle)) target.SetPosition(lockAngle);
            }

            return !lockFault;
        }
    }
}
