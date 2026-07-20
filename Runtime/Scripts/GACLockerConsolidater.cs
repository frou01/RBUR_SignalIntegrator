
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
        public Controller_Base target;


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

            bool[] newSettedLockPosition = new bool[SettedLockPosition.Length + 1];
            SettedLockPosition.CopyTo(newSettedLockPosition, 0);
            newSettedLockPosition[SettedLockPosition.Length] = false;
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
            //TODO もっとも近い固定位置を返す
            return -1;
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
