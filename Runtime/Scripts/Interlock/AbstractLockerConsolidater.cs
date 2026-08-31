using UdonSharp;
using UnityEngine;

namespace RBUR_SignalIntegrator
{
    public

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        abstract
#endif
        class AbstractLockerConsolidater : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [HideInInspector][SerializeField] protected bool[] SettedLockStates;
        [HideInInspector][SerializeField] protected UdonSharpBehaviour[] lockerInstances;
        [HideInInspector][SerializeField] protected int[] SettedLockIndex;
        [HideInInspector][SerializeField] public bool positionUpdated = false;
        [SerializeField] protected int failSafeIndex;

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
                if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(trySetPosition));
                applyPosition(lockPositionSelector);
                if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(trySetPosition));
                return true;
            }
        }

        //Return: Success?
        public virtual bool tryUpdateLocking(UdonSharpBehaviour triedFromInstance, bool lockState, int lockPositionSelector,out int triedInstanceIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(tryUpdateLocking));
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
                if (lockIndex != -1) applyPosition(lockIndex);
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(tryUpdateLocking));
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

        public void DebugLock()
        {
            Debug.Log("Log locking state " + this.name, this);
            int idx = 0;
            foreach (bool lockState in SettedLockStates)
            {
                if(lockState) Debug.Log("locked by " + lockerInstances[idx].name, lockerInstances[idx]);
                idx++;
            }
        }

        public virtual void RelayFailSafe()//for exception
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(RelayFailSafe));
            foreach (UdonSharpBehaviour locker in lockerInstances)
            {
                locker.SendCustomEvent(nameof(RelayFailSafe));
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(RelayFailSafe));
        }
        public virtual void SetToFailSafePosition()//for exception
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(SetToFailSafePosition));
            setControllerOwner();
            int idx = 0;
            foreach (bool lockedState in SettedLockStates)
            {
                SettedLockStates[idx] = false;
                SettedLockIndex[idx] = failSafeIndex;

                idx++;
            }
            applyPosition(failSafeIndex);
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(SetToFailSafePosition));
        }

        //Return: Controller positon (On Analog controller, return nearest lock point)

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public abstract int GetCurrentPosition();
        protected abstract void applyLockToController(bool state);
        protected abstract void applyPositionToController(int posIndex);
        public abstract bool isControllerOwner();
        public abstract void setControllerOwner();
        public abstract void SyncController();
#else
        public virtual int GetCurrentPosition()
        {
            return -1;
        }
        protected virtual void applyLockToController(bool state)
        {
        }
        protected virtual void applyPositionToController(int posIndex)
        {
        }
        public virtual bool isControllerOwner()
        {
            return false;
        }
        public virtual void setControllerOwner()
        {
        }
        public virtual void SyncController()
        {
        }
#endif

        protected void applyPosition(int posIndex)
        {
            if(posIndex != -1 && GetCurrentPosition() != posIndex)
            {
                Debug.Log("Update Position " + GetCurrentPosition() + " -> " + posIndex, this);
                positionUpdated = true;
            }
            applyPositionToController(posIndex);
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