using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace frou01.RBUR.SignalIntegrator
{
    public

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        abstract
#endif
        class AbstractLockerConsolidator : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [FormerlySerializedAs("SettedLockStates")][HideInInspector][SerializeField] private protected bool[] lockStates = new bool[0];
        [HideInInspector][SerializeField] private protected UdonSharpBehaviour[] lockerInstances = new UdonSharpBehaviour[0];
        [FormerlySerializedAs("SettedLockIndex")][HideInInspector][SerializeField] private protected int[] lockIndexes = new int[0];
        [FormerlySerializedAs("PositionUpdatedByLocking")][HideInInspector][SerializeField] private bool isPositionUpdatedByLocking = false;

        public void ResetFlagPosChangeOnLockDetector()
        {
            isPositionUpdatedByLocking = false;
        }

        protected void SetFlagPosChangeOnLockDetector()
        {
            isPositionUpdatedByLocking = true;
        }

        public bool GetFlagPosChangeOnLockDetector()
        {
            return isPositionUpdatedByLocking;
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
        public bool TryUpdateLocking(UdonSharpBehaviour triedFrom, bool lockState, int lockPositionSelector)
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
            GetCallingBehaviourIndex(triedFromInstance, out triedInstanceIndex);
            if (lockerInstances.Length == triedInstanceIndex)
            {
                AddNewLocker(triedFromInstance);
            }

            lockStates[triedInstanceIndex] = lockState;
            lockIndexes[triedInstanceIndex] = lockPositionSelector;

            bool locked = IsLocked();
            bool lockFault;
            int lockIndex;
            CheckFaultAndGetLockIndex(out lockFault, out lockIndex);

            if (lockFault)
            {
                locked = false;
            }
            ApplyLockToController(locked);
            if (locked)
            {
                if (lockIndex != -1)
                {
                    if (GetCurrentPosition() != lockIndex)
                    {
                        SetFlagPosChangeOnLockDetector();

                        Debug.Log("SetUpdatedDetectorFlag " + this.name + ", GetCurrentPosition: " + GetCurrentPosition() + ", lockIndex: " + lockIndex, this);
                    }
                    ApplyPositionToController(lockIndex);
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

            UdonSharpBehaviour[] newLockerInstances = new UdonSharpBehaviour[lockerInstances.Length+1];
            lockerInstances.CopyTo(newLockerInstances, 0);
            newLockerInstances[lockerInstances.Length] = triedFrom;
            lockerInstances = newLockerInstances;

            bool[] newLockStates = new bool[lockStates.Length + 1];
            lockStates.CopyTo(newLockStates, 0);
            newLockStates[lockStates.Length] = false;
            lockStates = newLockStates;

            int[] newLockIndexes = new int[lockIndexes.Length + 1];
            lockIndexes.CopyTo(newLockIndexes, 0);
            newLockIndexes[lockIndexes.Length] = 0;
            lockIndexes = newLockIndexes;

            return true;
        }

        /// <summary>
        /// Checks if the controller is currently locked.
        /// </summary>
        /// <returns>True if the controller is locked, false otherwise.</returns>
        public virtual bool IsLocked()
        {
            foreach(bool lockState in lockStates)
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
            foreach (bool lockState in lockStates)
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
            SetControllerOwner();
            int idx = 0;
            foreach (bool lockedState in lockStates)
            {
                lockStates[idx] = false;
                lockIndexes[idx] = failSafeIndex;

                idx++;
            }
            if (failSafeIndex != -1 && GetCurrentPosition() != failSafeIndex)
            {
                //Debug.Log("Update Position " + GetCurrentPosition() + " -> " + posIndex, this);
                isPositionUpdatedByLocking = true;
            }
            ApplyPositionToController(failSafeIndex);

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
        protected abstract void ApplyLockToController(bool state);
        /// <summary>
        /// Applies the position to the controller-Object.
        /// </summary>
        /// <param name="posIndex">The position index to apply.</param>
        protected abstract void ApplyPositionToController(int posIndex);

        /// <returns>Is the controller is currently the owner?</returns>
        public abstract bool IsControllerOwner();
        /// <summary>
        /// Sets the LocalPlayer as the owner of the controller-Object.
        /// </summary>
        public abstract void SetControllerOwner();

        /// <summary>
        /// synchronizes the controller-Object state across the network. This method should be implemented to handle the specifics of how the controller's state is synchronized in your application.
        /// </summary>
        protected abstract void SyncController();
#else
        public virtual int GetCurrentPosition()
        {
            return -1;
        }
        protected virtual void ApplyLockToController(bool state)
        {
        }
        protected virtual void ApplyPositionToController(int posIndex)
        {
        }
        public virtual bool IsControllerOwner()
        {
            return false;
        }
        public virtual void SetControllerOwner()
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
            foreach (bool lockedState in lockStates)
            {
                if (lockedState)
                {
                    if (lockIndex == -1) lockIndex = lockIndexes[checkingIndex];
                    else if (lockIndex != lockIndexes[checkingIndex]) lockFault = true;
                }

                checkingIndex++;
            }
        }

        protected void GetCallingBehaviourIndex(UdonSharpBehaviour triedFrom, out int triedIndex)
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
        protected override void ApplyLockToController(bool state)
        {
        }
        protected override void ApplyPositionToController(int posIndex)
        {
        }
        public override bool IsControllerOwner()
        {
            return true;
        }
        public override void SetControllerOwner()
        {
        }
        public override void SyncController()
        {
        }
         */
    }
}