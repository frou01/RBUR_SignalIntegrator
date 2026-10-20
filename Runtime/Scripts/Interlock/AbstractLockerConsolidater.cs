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
        [HideInInspector][SerializeField] private protected bool[] SettedLockStates = new bool[0];
        [HideInInspector][SerializeField] private protected UdonSharpBehaviour[] lockerInstances = new UdonSharpBehaviour[0];
        [HideInInspector][SerializeField] private protected int[] SettedLockIndex = new int[0];
        [HideInInspector][SerializeField] private bool PositionUpdatedByLocking = false;

        public void ResetUpdatedDetectorFlag()
        {
            PositionUpdatedByLocking = false;
        }

        protected void SetUpdatedDetectorFlag()
        {
            PositionUpdatedByLocking = true;
        }

        public bool GetUpdatedDetectorFlag()
        {
            return PositionUpdatedByLocking;
        }



        [SerializeField] private protected int failSafeIndex;

        protected virtual void Start()
        {

        }

        /// <summary>
        /// Tries to update the locking state for a given instance.
        /// </summary>
        /// <param name="triedFrom">The instance attempting to update the lock.</param>
        /// <param name="lockState">The desired lock state.</param>
        /// <param name="lockPositionSelector">The position selector for the lock.</param>
        /// <returns>True if the update was successful, false otherwise(lock position conflict).</returns>
        public bool tryUpdateLocking(UdonSharpBehaviour triedFrom, bool lockState, int lockPositionSelector)
        {
            return UpdateLocking(triedFrom, lockState, lockPositionSelector,out int idx);
        }

        //Return: Success?
        /// <summary>
        /// Tries to update the locking state for a given instance.
        /// </summary>
        /// <param name="triedFromInstance">The instance attempting to update the lock.</param>
        /// <param name="lockState">The desired lock state.</param>
        /// <param name="lockPositionSelector">The position selector for the lock.</param>
        /// <param name="triedInstanceIndex">The index of the instance attempting the update.</param>
        /// <returns>True if the update was successful, false otherwise(lock position conflict).</returns>
        protected virtual bool UpdateLocking(UdonSharpBehaviour triedFromInstance, bool lockState, int lockPositionSelector,out int triedInstanceIndex)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(UpdateLocking));
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
                if (lockIndex != -1)
                {
                    if (GetCurrentPosition() != lockIndex)
                    {
                        SetUpdatedDetectorFlag();
                    }
                    applyPositionToController(lockIndex);
                }
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLocking));
            return !lockFault;
        }

        //Return: Success?(Not already added?)
        /// <summary>
        /// Adds a new locker instance to the list of locker instances if it is not already present. Returns true if the instance was added, false if it was already present.
        /// </summary>
        /// <param name="triedFrom">The locker instance to add.</param>
        /// <returns>True if the instance was added, false if it was already present.</returns>
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

        /// <summary>
        /// Checks if the controller is currently locked.
        /// </summary>
        /// <returns>True if the controller is locked, false otherwise.</returns>
        public virtual bool isLocked()
        {
            foreach(bool lockState in SettedLockStates)
            {
                if (lockState) return true;
            }
            return false;
        }

        /// <summary>
        /// Logs the current locking state of the controller and its locker instances.
        /// </summary>
        public void DebugLock()
        {
            Debug.Log("Log locking state " + this.name, this);
            int idx = 0;
            foreach (bool lockState in SettedLockStates)
            {
                if(lockState && lockerInstances[idx]) Debug.Log("locked by " + lockerInstances[idx].name, lockerInstances[idx]);
                idx++;
            }
        }

        /// <summary>
        /// Relays a fail-safe event to all related locker instances.
        /// </summary>
        public virtual void RelayFailSafe()//for exception
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(RelayFailSafe));
            foreach (UdonSharpBehaviour locker in lockerInstances)
            {
                locker.SendCustomEvent(nameof(RelayFailSafe));
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(RelayFailSafe));
        }
        /// <summary>
        /// Sets the controller to its fail-safe position.
        /// </summary>
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
            if (failSafeIndex != -1 && GetCurrentPosition() != failSafeIndex)
            {
                //Debug.Log("Update Position " + GetCurrentPosition() + " -> " + posIndex, this);
                PositionUpdatedByLocking = true;
            }
            applyPositionToController(failSafeIndex);

            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(SetToFailSafePosition));
        }


#if !COMPILER_UDONSHARP && UNITY_EDITOR
        /// <summary>
        /// Get the current position of the controller.
        /// </summary>
        /// <returns>The current position of the controller.</returns>
        public abstract int GetCurrentPosition();
        /// <summary>
        /// Applies the lock state to the controller-Object.
        /// </summary>
        /// <param name="state">The lock state to apply.</param>
        protected abstract void applyLockToController(bool state);
        /// <summary>
        /// Applies the position to the controller-Object.
        /// </summary>
        /// <param name="posIndex">The position index to apply.</param>
        protected abstract void applyPositionToController(int posIndex);

        /// <returns>Is the controller is currently the owner?</returns>
        public abstract bool isControllerOwner();
        /// <summary>
        /// Sets the LocalPlayer as the owner of the controller-Object.
        /// </summary>
        public abstract void setControllerOwner();

        /// <summary>
        /// synchronizes the controller-Object state across the network. This method should be implemented to handle the specifics of how the controller's state is synchronized in your application.
        /// </summary>
        protected abstract void SyncController();
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
        protected virtual void SyncController()
        {
        }
#endif
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