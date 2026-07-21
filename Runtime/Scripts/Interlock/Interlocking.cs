
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
        [HideInInspector]public bool FailSafeCalled;
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

        public void UpdateInterlock()//called by Controller Pickup Event
        {
            FailSafeCalled = false;
            bool canOpenSignal = interlock_Route.isRouteOpen();
            foreach(InterlockStateLocker interlockState in interlockStates)
            {
                canOpenSignal &= interlockState.Check();
            }


            if (!canOpenSignal)
            {
                bool LockSuccess = TargetSignalLocker.tryUpdateLocking(this,true, SignalClosePositionIndex);
                if (!LockSuccess)
                {
                    Debug.LogError("Interlock Preset Error. Cannot lock in closed signal. Route/SignalState/ControllerLockPositon Settings is inconsistency.",this.gameObject);
                }
                foreach(InterlockStateLocker interlockState in interlockStates)
                {
                    interlockState.UpdateLock(false);
                }
                interlock_Route.UpdateLockRoute(false);
            }



            if(TargetSignalLocker.isControlerOwner())
            {
                bool isOpen = false;
                int currentSignalPos = TargetSignalLocker.GetCurrentPosition();
                foreach (int OpenPosition in SignalOpenPositionIndex)
                {
                    if (currentSignalPos == OpenPosition)
                    {
                        isOpen = true;
                        break;
                    }
                }
                if (isOpen)
                {
                    bool LockSuccess = true;
                    foreach (InterlockStateLocker interlockState in interlockStates)
                    {
                        LockSuccess &= interlockState.UpdateLock(true);
                    }
                    LockSuccess &= interlock_Route.UpdateLockRoute(true);
                    if (!LockSuccess)
                    {
                        Debug.LogError("inconsistency Interlocking. Reset All Signals", this.gameObject);
                        this.RelayFailSafe();
                    }
                }
            }
        }

        public void RelayFailSafe()
        {
            if (!FailSafeCalled)
            {
                FailSafeCalled = true;
                if(TargetSignalLocker.isControlerOwner()) TargetSignalLocker.SetToFailSafePosition();
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