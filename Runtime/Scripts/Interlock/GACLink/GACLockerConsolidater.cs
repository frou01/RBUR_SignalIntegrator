
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class GACLockerConsolidater : AbstractLockerConsolidater
    {
        [SerializeField] protected float[] lockPositions;
        [SerializeField] protected int failSafeIndex;
        [SerializeField] protected Controller_Base target;


        public override bool AddNewLocker(UdonSharpBehaviour triedFrom)
        {
            if(!base.AddNewLocker(triedFrom))return false;

            int[] newSettedLockPosition = new int[SettedLockIndex.Length + 1];
            SettedLockIndex.CopyTo(newSettedLockPosition, 0);
            newSettedLockPosition[SettedLockIndex.Length] = 0;
            SettedLockIndex = newSettedLockPosition;
            return true;
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
            Networking.SetOwner(Networking.LocalPlayer,target.gameObject);
            applyToControllerPos(failSafeIndex);
            int idx = 0;
            foreach (bool lockedState in SettedLockStates)
            {
                SettedLockStates[idx] = false;
                SettedLockIndex[idx] = failSafeIndex;

                idx++;
            }
        }

        protected override void applyToControllerLock(bool state)
        {
            target.locked = state;
        }
        protected override void applyToControllerPos(int posIndex)
        {
            target.SetPosition(lockPositions[posIndex]);
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
