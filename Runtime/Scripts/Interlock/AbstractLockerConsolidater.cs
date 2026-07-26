using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class AbstractLockerConsolidater : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] protected bool[] SettedLockStates;
        [HideInInspector][SerializeField] protected UdonSharpBehaviour[] lockerInstances;
        [HideInInspector][SerializeField] protected int[] SettedLockIndex;

        //Return: Success?
        public bool tryUpdateLocking(UdonSharpBehaviour triedFrom, bool lockState, int lockPositionSelector)
        {
            return tryUpdateLocking(triedFrom, lockState, lockPositionSelector,out int idx);
        }
        //Return: Success?
        public bool trySetPosition(UdonSharpBehaviour triedFrom, int lockPositionSelector)
        {
            return trySetPosition(triedFrom, lockPositionSelector, out int idx);
        }

        //Return: Success?
        public virtual bool tryUpdateLocking(UdonSharpBehaviour triedFromInstance, bool lockState, int lockPositionSelector,out int triedInstanceIndex)
        {
            getCallingBehaviourIndex(triedFromInstance, out triedInstanceIndex);
            if (lockerInstances.Length == triedInstanceIndex)
            {
                AddNewLocker(triedFromInstance);
            }

            SettedLockStates[triedInstanceIndex] = lockState;
            SettedLockIndex[triedInstanceIndex] = lockPositionSelector;

            bool locked = isLocked();
            bool lockFault;
            int lockIndex;
            CheckFaultAndGetLockIndex(out lockFault, out lockIndex);

            if (lockFault)
            {
                locked = false;
            }
            applyToControllerLock(locked);
            if (locked)
            {
                if (lockIndex != -1) applyToControllerPos(lockIndex); 
            }

            return !lockFault;
        }
        public virtual bool trySetPosition(UdonSharpBehaviour triedFromInstance, int lockPositionSelector, out int triedInstanceIndex)
        {
            getCallingBehaviourIndex(triedFromInstance, out triedInstanceIndex);
            if (isLocked()) return false;
            else {
                applyToControllerPos(lockPositionSelector);
                return true;
            }
        }

        //Return: Success?(Not already added?)

        public virtual bool AddNewLocker(UdonSharpBehaviour triedFrom)
        {
            foreach (UdonSharpBehaviour locker in lockerInstances)
            {
                if (triedFrom == locker) return false;
            }

            UdonSharpBehaviour[] newLockingScripts = new UdonSharpBehaviour[lockerInstances.Length+1];
            lockerInstances.CopyTo(newLockingScripts, 0);
            newLockingScripts[lockerInstances.Length] = triedFrom;
            lockerInstances = newLockingScripts;

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

        //Return: Controller positon (On Analog controller, return nearest lock point)
        public virtual bool SetCurrentPosition()
        {
            return false;
        }

        public virtual bool isLocked()
        {
            foreach(bool lockState in SettedLockStates)
            {
                if (lockState) return true;
            }
            return false;
        }

        public virtual void RelayFailSafe()//for exception
        {
            foreach (UdonSharpBehaviour locker in lockerInstances)
            {
                locker.SendCustomEvent(nameof(RelayFailSafe));
            }
        }
        public virtual void SetToFailSafePosition()//for exception
        {
        }

        protected virtual void applyToControllerLock(bool state)
        {
        }
        protected virtual void applyToControllerPos(int posIndex)
        {
        }
        public virtual bool isControlerOwner()
        {
            return false;
        }

        public virtual void SyncController()
        {
        }
        protected void CheckFaultAndGetLockIndex(out bool lockFault, out int lockIndex)
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

        protected void getCallingBehaviourIndex(UdonSharpBehaviour triedFrom, out int triedIndex)
        {
            triedIndex = 0;
            foreach (UdonSharpBehaviour locker in lockerInstances)
            {
                triedIndex++;
                if (triedFrom == locker)
                {
                    triedIndex--;
                    break;
                }
            }
        }
    }
}