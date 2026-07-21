using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class AbstractLockerConsolidater : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] protected bool[] SettedLockStates;
        [HideInInspector][SerializeField] protected UdonSharpBehaviour[] lockingScripts;

        //Return: Success?
        public virtual bool tryUpdateLocking(UdonSharpBehaviour triedFrom, bool lockState, int lockPositionSelector)
        {
            int idx;
            return tryUpdateLocking(triedFrom, lockState, lockPositionSelector,out idx);
        }

        //Return: Success?
        public virtual bool tryUpdateLocking(UdonSharpBehaviour triedFrom, bool lockState, int lockPositionSelector,out int triedIndex)
        {
            triedIndex = 0;
            foreach(UdonSharpBehaviour locker in lockingScripts)
            {
                triedIndex++;
                if (triedFrom == locker)
                {
                    triedIndex--;
                }
            }
            if(lockingScripts.Length == triedIndex)
            {
                AddNewLocker(triedFrom);
            }

            SettedLockStates[triedIndex] = lockState;
            return true;
        }

        //Return: Success?(Not already added?)

        public virtual bool AddNewLocker(UdonSharpBehaviour triedFrom)
        {
            foreach (UdonSharpBehaviour locker in lockingScripts)
            {
                if (triedFrom == locker) return false;
            }

            UdonSharpBehaviour[] newLockingScripts = new UdonSharpBehaviour[lockingScripts.Length+1];
            lockingScripts.CopyTo(newLockingScripts, 0);
            newLockingScripts[lockingScripts.Length] = triedFrom;
            lockingScripts = newLockingScripts;

            bool[] newSettedLockStates = new bool[SettedLockStates.Length + 1];
            SettedLockStates.CopyTo(newSettedLockStates, 0);
            newSettedLockStates[SettedLockStates.Length] = false;
            SettedLockStates = newSettedLockStates;

            return true;
        }

        //Return: Controller positon (On Analog controller, return nearest lock point)
        public virtual int GetCurrentPosition()
        {
            return -1;
        }

        protected virtual bool isLocked()
        {
            foreach(bool lockState in SettedLockStates)
            {
                if (lockState) return true;
            }
            return false;
        }

        public virtual void RelayFailSafe()
        {
            foreach (UdonSharpBehaviour locker in lockingScripts)
            {
                locker.SendCustomEvent(nameof(RelayFailSafe));
            }
        }
        public virtual void SetToFailSafePosition()
        {
        }

        public virtual bool isControlerOwner()
        {
            return false;
        }
    }
}