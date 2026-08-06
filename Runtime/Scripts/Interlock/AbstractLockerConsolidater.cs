using System;
using UdonSharp;
using UnityEngine;

namespace RBUR_SignalIntegrator
{
    public class AbstractLockerConsolidater : UdonSharpBehaviour
    {
        [SerializeField] public EventStackHolder eventStackHolder;
        [HideInInspector][SerializeField] protected bool[] SettedLockStates;
        [HideInInspector][SerializeField] protected UdonSharpBehaviour[] lockerInstances;
        [HideInInspector][SerializeField] protected int[] SettedLockIndex;
        [SerializeField] protected int failSafeIndex;
        [HideInInspector] public bool positionUpdated = false;

        protected virtual void Start()
        {

        }
        //Return: Success?
        public bool tryUpdateLocking(UdonSharpBehaviour triedFrom, bool lockState, int lockPositionSelector)
        {
            return tryUpdateLocking(triedFrom, lockState, lockPositionSelector,out int idx);
        }
        //Return: Success?
        public virtual bool trySetPosition(int lockPositionSelector)
        {
            if (isLocked()) return false;
            else
            {
                eventStackHolder.AddStack(this, nameof(trySetPosition));
                applyPositionToController(lockPositionSelector);
                eventStackHolder.RemoveStack(this, nameof(trySetPosition));
                return true;
            }
        }

        //Return: Success?
        public virtual bool tryUpdateLocking(UdonSharpBehaviour triedFromInstance, bool lockState, int lockPositionSelector,out int triedInstanceIndex)
        {
            eventStackHolder.AddStack(this, nameof(tryUpdateLocking));
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
            applyLockToController(locked);
            if (locked)
            {
                if (lockIndex != -1) applyPositionToController(lockIndex);
            }
            eventStackHolder.RemoveStack(this, nameof(tryUpdateLocking));
            return !lockFault;
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

            int[] newSettedLockPosition = new int[SettedLockIndex.Length + 1];
            SettedLockIndex.CopyTo(newSettedLockPosition, 0);
            newSettedLockPosition[SettedLockIndex.Length] = 0;
            SettedLockIndex = newSettedLockPosition;

            return true;
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
            setControllerOwner();
            int idx = 0;
            foreach (bool lockedState in SettedLockStates)
            {
                SettedLockStates[idx] = false;
                SettedLockIndex[idx] = failSafeIndex;

                idx++;
            }
            applyPositionToController(failSafeIndex);
        }

        //Return: Controller positon (On Analog controller, return nearest lock point)
        public virtual int GetCurrentPosition()
        {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
            throw new NotImplementedException();
#endif
#pragma warning disable CS0162 // 到達できないコードが検出されました
            return -1;
#pragma warning restore CS0162 // 到達できないコードが検出されました
        }

        protected virtual void applyLockToController(bool state)
        {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
            throw new NotImplementedException();
#endif
        }
        protected virtual void applyPositionToController(int posIndex)
        {
            if(GetCurrentPosition() != posIndex)
            {
                positionUpdated = true;
            }
        }
        public virtual bool isControllerOwner()
        {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
            throw new NotImplementedException();
#endif
#pragma warning disable CS0162 // 到達できないコードが検出されました
            return false;
#pragma warning restore CS0162 // 到達できないコードが検出されました
        }
        public virtual void setControllerOwner()
        {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
            throw new NotImplementedException();
#endif
        }

        public virtual void SyncController()
        {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
            throw new NotImplementedException();
#endif
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
        /*
        //function override template
        public override int GetCurrentPosition()
        {
            return -1;
        }
        protected override void applyLockToController(bool state)
        {
        }
        protected override void applyPositionToController(int posIndex)
        {
        }
        public override bool isControllerOwner()
        {
            return true;
        }
        public override void setControllerOwner()
        {
        }
        public override void SyncController()
        {
        }
         */
    }
}