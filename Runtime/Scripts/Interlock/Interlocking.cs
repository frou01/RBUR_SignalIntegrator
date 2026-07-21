
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

        [SerializeField] RouteLocker interlock_Route;//進路開通取得と、開通状態で固定用
        [SerializeField] InterlockStateLocker[] interlockStates;//進路以外の進行要件
        [SerializeField] AbstractLockerConsolidater TargetSignalLocker;//GAC以外も制御できるように間を挟む
        [SerializeField] int SignalClosePositionIndex;
        [SerializeField] int[] SignalOpenPositionIndex;
        [HideInInspector] public bool FailSafeCalled;
        protected bool signal_isDirty = false;
        public RouteLocker GetRouteLocker()
        {
            return interlock_Route;
        }
        public InterlockStateLocker[] GetInterlockStateLinker()
        {
            return interlockStates;
        }
        public AbstractLockerConsolidater GetTargetSignalLocker()
        {
            return TargetSignalLocker;
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
        public void UpdateInterlock()//called by Controller Pickup Event
        {
            Debug.Log("UpdateInterlock.Start", this);
            FailSafeCalled = false;
            signal_isDirty = false;

            bool canOpenSignal = interlock_Route.isRouteOpen();
            Debug.Log("UpdateInterlock.RouteCheck " + canOpenSignal, interlock_Route);
            foreach (InterlockStateLocker interlockState in interlockStates)
            {
                Debug.Log("UpdateInterlock.SignCheck " + interlockState.Check(), interlockState);
                canOpenSignal &= interlockState.Check();
            }



            bool isSignalOpenned = false;
            int currentSignalPos = TargetSignalLocker.GetCurrentPosition();
            foreach (int OpenPosition in SignalOpenPositionIndex)
            {
                if (currentSignalPos == OpenPosition)
                {
                    isSignalOpenned = true;
                    break;
                }
            }
            if (isSignalOpenned)
            {
                Debug.Log("UpdateInterlock.RouteLock", this);
                bool LockSuccess = true;
                foreach (InterlockStateLocker interlockState in interlockStates)
                {
                    LockSuccess &= interlockState.UpdateLock(true);
                }
                LockSuccess &= interlock_Route.UpdateLockRoute(true);
                Debug.Log("UpdateInterlock.LockResult " + LockSuccess, this.gameObject);
                if (!LockSuccess)
                {
                    Debug.LogError("inconsistency Interlocking. Reset All Signals", this.gameObject);
                    this.RelayFailSafe();
                }
            }
            else
            {
                Debug.Log("UpdateInterlock.RouteRelease", this);
                foreach (InterlockStateLocker interlockState in interlockStates)
                {
                    interlockState.UpdateLock(false);
                }
                interlock_Route.UpdateLockRoute(false);
                if (!canOpenSignal)
                {
                    Debug.Log("UpdateInterlock.SignalLock", this);
                    signal_isDirty = true;
                    bool LockSuccess = TargetSignalLocker.tryUpdateLocking(this, true, SignalClosePositionIndex);
                    if (!LockSuccess)
                    {
                        Debug.LogError("Interlock Preset Error. Cannot lock in closed signal. Route/SignalState/ControllerLockPositon Settings is inconsistency.", this.gameObject);
                    }
                    foreach (InterlockStateLocker interlockState in interlockStates)
                    {
                        interlockState.UpdateLock(false);
                    }
                    interlock_Route.UpdateLockRoute(false);
                }
                else
                {
                    Debug.Log("UpdateInterlock.SignalRelease", this);
                    TargetSignalLocker.tryUpdateLocking(this, false, SignalClosePositionIndex);
                }

                if (signal_isDirty)
                {
                    TargetSignalLocker.SyncController();
                }
            }

            
        }

        public void RelayFailSafe()
        {
            if (!FailSafeCalled)
            {
                FailSafeCalled = true;
                TargetSignalLocker.SetToFailSafePosition();
                signal_isDirty = true;
                TargetSignalLocker.RelayFailSafe();
                foreach (InterlockStateLocker interlockState in interlockStates)
                {
                    interlockState.RelayFailSafe();
                }
                interlock_Route.RelayFailSafe();
            }
        }
        public void SetupLocker()
        {
            TargetSignalLocker.AddNewLocker(this);
        }
    }

}