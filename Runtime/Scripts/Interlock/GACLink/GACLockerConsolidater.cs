
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class GACLockerConsolidater : AbstractLockerConsolidater
    {
        [HideInInspector][SerializeField] protected int[] SettedLockIndex;
        [SerializeField] protected float[] lockPositions;
        [SerializeField] protected int failSafeIndex;
        [SerializeField] protected Controller_Base target;


        //Return: Success?
        public override bool tryUpdateLocking(UdonSharpBehaviour triedFrom, bool lockState, int lockPositionSelector, out int triedIndex)
        {
            base.tryUpdateLocking(triedFrom, lockState, lockPositionSelector,out triedIndex);

            SettedLockIndex[triedIndex] = lockPositionSelector;

            bool locked = isLocked();
            bool lockFault;
            int lockIndex;
            GetLockAngle_And_CheckFault(out lockFault,out lockIndex);

            if (lockFault)
            {
                locked = false;
            }
            if (locked)
            {
                target.locked = locked;
                if (lockIndex != -1) target.SetPosition(lockPositions[lockIndex]);
            }

            return !lockFault;
        }
        public override bool AddNewLocker(UdonSharpBehaviour triedFrom)
        {
            if(!base.AddNewLocker(triedFrom))return false;

            int[] newSettedLockPosition = new int[SettedLockIndex.Length + 1];
            SettedLockIndex.CopyTo(newSettedLockPosition, 0);
            newSettedLockPosition[SettedLockIndex.Length] = 0;
            SettedLockIndex = newSettedLockPosition;
            return true;
        }

        private void GetLockAngle_And_CheckFault(out bool lockFault,out int lockIndex)
        {
            lockFault = false;
            lockIndex = -1;
            int checkingIndex = 0;
            foreach (bool lockedState in SettedLockStates)
            {
                if (lockedState)
                {
                    if (lockIndex == -1) lockIndex = SettedLockIndex[checkingIndex];
                    else if (lockIndex != SettedLockIndex[checkingIndex]) lockFault = true;
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
            target.SetPosition(lockPositions[failSafeIndex]);
            int idx = 0;
            foreach (bool lockedState in SettedLockStates)
            {
                SettedLockStates[idx] = false;
                SettedLockIndex[idx] = failSafeIndex;

                idx++;
            }
        }

        public override bool isControlerOwner()
        {
            return Networking.IsOwner(target.gameObject);
        }
        public override void SyncController()
        {
            if (isControlerOwner())
            {
                target.RequestSerialization();
            }
        }
    }
}
