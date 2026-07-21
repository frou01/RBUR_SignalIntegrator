
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class GACLockerConsolidater : AbstractLockerConsolidater
    {
        [HideInInspector][SerializeField] protected int[] SettedLockPosition;
        [SerializeField] protected float[] lockPositions;
        [SerializeField] protected int failSafePosition;
        [SerializeField] protected Controller_Base target;


        //Return: Success?
        public override bool tryUpdateLocking(UdonSharpBehaviour triedFrom, bool lockState, int lockPositionSelector, out int triedIndex)
        {
            base.tryUpdateLocking(triedFrom, lockState, lockPositionSelector,out triedIndex);

            SettedLockPosition[triedIndex] = lockPositionSelector;

            bool locked = isLocked();
            bool lockFault;
            float lockAngle;
            GetLockAngle_And_CheckFault(out lockFault,out lockAngle);

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
        public override bool AddNewLocker(UdonSharpBehaviour triedFrom)
        {
            if(!base.AddNewLocker(triedFrom))return false;

            int[] newSettedLockPosition = new int[SettedLockPosition.Length + 1];
            SettedLockPosition.CopyTo(newSettedLockPosition, 0);
            newSettedLockPosition[SettedLockPosition.Length] = 0;
            SettedLockPosition = newSettedLockPosition;
            return true;
        }

        private void GetLockAngle_And_CheckFault(out bool lockFault,out float lockAngle)
        {
            lockFault = false;
            lockAngle = float.NaN;
            int checkingIndex = 0;
            foreach (bool lockedState in SettedLockStates)
            {
                if (lockedState)
                {
                    if (float.IsNaN(lockAngle)) lockAngle = lockPositions[SettedLockPosition[checkingIndex]];
                    else if (lockAngle != lockPositions[SettedLockPosition[checkingIndex]]) lockFault = true;
                }

                checkingIndex++;
            }
        }

        //Return: Controller positon (On Analog controller, return nearest lock point)
        public override int GetCurrentPosition()
        {
            float controllerPos = target.controllerPosition;
            float diffhistr = float.MaxValue;
            int res = -1;
            for (int idx = 0;idx < lockPositions.Length; idx++)
            {
                float diff = Mathf.Abs(lockPositions[idx] - controllerPos);
                if (diff < diffhistr)
                {
                    diffhistr = diff;
                    res = idx;
                }
            }
            return res;
        }
        public override void SetToFailSafePosition()
        {
            target.SetPosition(lockPositions[failSafePosition]);
            int idx = 0;
            foreach (bool lockedState in SettedLockStates)
            {
                SettedLockStates[idx] = false;
                SettedLockPosition[idx] = failSafePosition;

                idx++;
            }
        }

        public override bool isControlerOwner()
        {
            return Networking.IsOwner(target.gameObject);
        }
    }
}
