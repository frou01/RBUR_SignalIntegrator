
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class Interlocking : UdonSharpBehaviour
    {
        //TODO Local鎖錠
        //TODO 同期デッドロック防止：両否決

        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [SerializeField] AbstractLockerConsolidater From_Locker;//GAC以外も制御できるように間を挟む
        [SerializeField] RouteLocker interlock_Route;//進路開通取得と、開通状態で固定用
        [SerializeField] Interlock_ToLockerAndMeetPosition[] toLocker_States;//進路以外の進行要件
        [SerializeField] int ReleasePositionIndex;
        [SerializeField] int[] LockPositionIndexes;
        [HideInInspector][SerializeField] public bool FailSafeCalled;
        [HideInInspector][SerializeField] public Interlocking[] affectedInterlockings;
        [HideInInspector][SerializeField] public AbstractLockerConsolidater[] affectedLockers;
        protected bool signal_isDirty = false;
        public RouteLocker GetRouteLocker()
        {
            return interlock_Route;
        }
        public Interlock_ToLockerAndMeetPosition[] GetInterlockStateLinker()
        {
            return toLocker_States;
        }
        public AbstractLockerConsolidater GetFromLocker()
        {
            return From_Locker;
        }

        public void OnDeserialization_()
        {
            UpdateInterlock();
        }
        public void OnOwnershipTransferred_BecomeLocal()
        {
            UpdateInterlock();
        }
        public void OnPickup_()
        {
            UpdateInterlock();
        }
        public void OnDrop_()
        {
            UpdateInterlock();
        }

        [RecursiveMethod]
        public bool UpdateInterlock(bool updateAffected)
        {
            eventStackHolder.AddStack(this, nameof(UpdateInterlock));

            Debug.Log(this.name + ": UpdateInterlock.Start", this);

            foreach (AbstractLockerConsolidater locker in affectedLockers)
            {
                locker.positionUpdated = false;
            }
            FailSafeCalled = false;
            signal_isDirty = false;

            Debug.Log(this.name + ": UpdateInterlock.Check_To");
            bool canOpenSignal = interlock_Route.isRouteOpen();
            foreach (Interlock_ToLockerAndMeetPosition interlockState in toLocker_States)
            {
                canOpenSignal &= interlockState.Check();
            }



            bool isSignalOpenned = false;
            int currentSignalPos = From_Locker.GetCurrentPosition();
            foreach (int OpenPosition in LockPositionIndexes)
            {
                if (currentSignalPos == OpenPosition)
                {
                    isSignalOpenned = true;
                    break;
                }
            }
            if (isSignalOpenned)
            {
                bool LockSuccess = true;
                Debug.Log(this.name + ": UpdateInterlock.Lock_To", this);
                foreach (Interlock_ToLockerAndMeetPosition interlockState in toLocker_States)
                {
                    LockSuccess &= interlockState.UpdateLock(true);
                }
                LockSuccess &= interlock_Route.UpdateLockRoute(true,From_Locker.isControllerOwner());

                Debug.Log(this.name + ": UpdateInterlock.LockResult " + LockSuccess, this.gameObject);
                if (!LockSuccess && From_Locker.isControllerOwner())
                {
                    Debug.LogError(this.name + ": inconsistency Interlocking. Reset All Signals", this.gameObject);
                    this.RelayFailSafe();
                }
            }
            else
            {
                Debug.Log(this.name + ": UpdateInterlock.Release_To", this);
                foreach (Interlock_ToLockerAndMeetPosition interlockState in toLocker_States)
                {
                    interlockState.UpdateLock(false);
                }
                interlock_Route.UpdateLockRoute(false,false);

                if (!canOpenSignal)
                {
                    Debug.Log(this.name + ": UpdateInterlock.Lock_From", this);
                    signal_isDirty = true;
                    bool LockSuccess = From_Locker.tryUpdateLocking(this, true, ReleasePositionIndex);
                    if (!LockSuccess)
                    {
                        Debug.LogError(this.name + ": Interlock Preset Error. Cannot lock From_Locker to close position. Already locked on anohter position", From_Locker);
                    }
                }
                else
                {
                    Debug.Log(this.name + ": UpdateInterlock.Release_From", this);
                    From_Locker.tryUpdateLocking(this, false, ReleasePositionIndex);
                }

                if (signal_isDirty)
                {
                    From_Locker.SyncController();
                }
            }

            if (updateAffected)
            {
                int loopLimit = 10;
                bool needNextUpdate;
                do
                {
                    loopLimit--;
                    needNextUpdate = false;
                    foreach (Interlocking interlock in affectedInterlockings)
                    {
                        needNextUpdate |= interlock.UpdateInterlock(false);
                    }
                } while (needNextUpdate && loopLimit>0);
                if (loopLimit <= 0)
                {
                    Debug.LogError(this.name + ": Interlock Update Looping. Interlock Settings is inconsistency." + this.name, this);
                    foreach (Interlocking interlock in affectedInterlockings)
                    {
                        Debug.LogError(this.name + ":    L" + interlock.name, interlock);
                    }
                }
            }

            bool LockerUpdated = false;
            foreach(AbstractLockerConsolidater locker in affectedLockers)
            {
                if (locker.positionUpdated)
                {
                    LockerUpdated = true;
                    break;
                }
            }

            eventStackHolder.RemoveStack(this, nameof(UpdateInterlock));
            return LockerUpdated;
        }
        public void UpdateInterlock()//called by Controller Pickup Event
        {
            UpdateInterlock(true);
        }

        public void RelayFailSafe()
        {
            eventStackHolder.AddStack(this, nameof(RelayFailSafe));
            if (!FailSafeCalled)
            {
                FailSafeCalled = true;
                From_Locker.SetToFailSafePosition();
                signal_isDirty = true;
                From_Locker.RelayFailSafe();
                foreach (Interlock_ToLockerAndMeetPosition interlockState in toLocker_States)
                {
                    interlockState.RelayFailSafe();
                }
                interlock_Route.RelayFailSafe();
            }
            eventStackHolder.RemoveStack(this, nameof(RelayFailSafe));
        }
        public void SetupLocker()
        {
            From_Locker.AddNewLocker(this);
        }
    }

}