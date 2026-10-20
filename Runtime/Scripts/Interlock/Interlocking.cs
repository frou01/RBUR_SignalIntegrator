
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class Interlocking : UdonSharpBehaviour
    {
        [SerializeField] bool logging;

        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [SerializeField] AbstractLockerConsolidater From_Locker;//GAC以外も制御できるように間を挟む
        [SerializeField] RouteLocker interlock_Route;//進路開通取得と、開通状態で固定用
        [SerializeField] LockerAndMeetPosition[] toLocker_States = new LockerAndMeetPosition[0];//進路以外の進行要件
        [FormerlySerializedAs("ReleasePositionIndex")]
        [SerializeField] int ClosePositionIndex;
        [FormerlySerializedAs("LockPositionIndexes")]
        [SerializeField] int[] OpenPositionIndexes;
        [HideInInspector][SerializeField] public bool FailSafeCalled;
        [HideInInspector][SerializeField] public Interlocking[] affectedInterlockings;//自身を含む関係する鎖錠
        [HideInInspector][SerializeField] public AbstractLockerConsolidater[] affectedLockers;//直接自身に関係する鎖錠

        public void Start()
        {
            if (From_Locker) From_Locker.AddNewLocker(this);
            if (interlock_Route) interlock_Route.setParentInterlock(this);
            UpdateInterlock(false);
        }
        public RouteLocker GetRouteLocker()
        {
            if (!interlock_Route) interlock_Route = GetComponentInChildren<RouteLocker>();
            if (interlock_Route) interlock_Route.setParentInterlock(this);
            return interlock_Route;
        }
        public LockerAndMeetPosition[] GetInterlockStateLinker()
        {
            return toLocker_States;
        }
        public AbstractLockerConsolidater GetFromLocker()
        {
            return From_Locker;
        }

        public void OnDeserialization_()// for SyncEventLinker
        {
            UpdateInterlock();
        }
        public void OnOwnershipTransferred_BecomeLocal()// for SyncEventLinker
        {
            UpdateInterlock();
        }
        public void OnPickup_()// for PickUpEventLinker
        {
            UpdateInterlock();
        }
        public void OnDrop_()// for PickUpEventLinker
        {
            UpdateInterlock();
        }

        bool Initial_Fetching = true;
        bool Prev_SignalOpenned;
        bool Prev_canOpenSignal;
        /// <summary>
        /// 鎖錠の更新を行う。From_Lockerの状態と、toLocker_States、RouteLocker(進路鎖錠)の状態を確認する
        /// From_LockerがOpen位置であればtoLocker_StatesとRouteLockerを位置変更及びロックする。From_Lockerが閉鎖位置であれば、toLocker_StatesとRouteLockerを解放する。
        /// toLocker_StatesとRouteLockerいずれかが適合位置でない場合、From_Lockerを閉鎖位置にロックする。
        /// </summary>
        /// <param name="update_AffectedInterlock">関係する鎖錠も更新するかどうか</param>
        /// <returns>関係するLockerについて、位置の変更を行ったかどうか</returns>
        [RecursiveMethod]
        public bool UpdateInterlock(bool update_AffectedInterlock)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(UpdateInterlock));

            if (logging) Debug.Log(this.name + ": UpdateInterlock.Start", this);


            if (logging) Debug.Log(this.name + ": UpdateInterlock.Check_To");

            //Fetching State
            bool canOpenSignal = GetRouteLocker().isRouteOpen();
            bool isSignalOpenned = false;
            foreach (LockerAndMeetPosition interlockState in toLocker_States)
            {
                canOpenSignal &= interlockState.Check();
            }
            int currentSignalPos = From_Locker.GetCurrentPosition();
            foreach (int OpenPosition in OpenPositionIndexes)
            {
                if (currentSignalPos == OpenPosition)
                {
                    isSignalOpenned = true;
                    break;
                }
            }

            if (Initial_Fetching || (isSignalOpenned != Prev_SignalOpenned) || (canOpenSignal != Prev_canOpenSignal))//Edge Only
            {
                Initial_Fetching = false;//For detouring on first call.


                //Initialize controller for update detection.
                foreach (AbstractLockerConsolidater locker in affectedLockers)
                {
                    locker.ResetUpdatedDetectorFlag();
                }
                //Flag for Affected Object Search
                FailSafeCalled = false;

                Prev_SignalOpenned = isSignalOpenned;
                Prev_canOpenSignal = canOpenSignal;

                if (isSignalOpenned)
                {
                    bool LockSuccess = true;
                    if (logging) Debug.Log(this.name + ": UpdateInterlock.Lock_To", this);
                    foreach (LockerAndMeetPosition interlockState in toLocker_States)
                    {
                        LockSuccess &= interlockState.UpdateLock(true);
                    }
                    LockSuccess &= GetRouteLocker().UpdateLockRoute(true, From_Locker.isControllerOwner());

                    if (logging) Debug.Log(this.name + ": UpdateInterlock.LockResult " + LockSuccess, this.gameObject);
                    if (!LockSuccess && From_Locker.isControllerOwner())
                    {
                        Debug.LogError(this.name + ": inconsistency Interlocking. Reset All Signals", this.gameObject);
                        this.RelayFailSafe();
                    }
                    if (canOpenSignal) From_Locker.tryUpdateLocking(this, false, ClosePositionIndex);
                }
                else
                {
                    if (logging) Debug.Log(this.name + ": UpdateInterlock.Release_To", this);
                    foreach (LockerAndMeetPosition interlockState in toLocker_States)
                    {
                        interlockState.UpdateLock(false);
                    }
                    GetRouteLocker().UpdateLockRoute(false, false);

                    if (!canOpenSignal)
                    {
                        if (logging) Debug.Log(this.name + ": UpdateInterlock.Lock_From", this);
                        bool LockSuccess = From_Locker.tryUpdateLocking(this, true, ClosePositionIndex);
                        if (!LockSuccess)
                        {
                            Debug.LogError(this.name + ": Interlock Preset Error. Cannot lock From_Locker to close position. Already locked on anohter position", From_Locker);
                        }
                    }
                    else
                    {
                        if (logging) Debug.Log(this.name + ": UpdateInterlock.Release_From", this);
                        From_Locker.tryUpdateLocking(this, false, ClosePositionIndex);
                    }
                }
            }

            bool LockerUpdated = false;
            foreach (AbstractLockerConsolidater locker in affectedLockers)
            {
                if (locker.GetUpdatedDetectorFlag())
                {
                    LockerUpdated = true;
                    break;
                }
            }

            if (LockerUpdated && update_AffectedInterlock)
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

            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateInterlock));
            return LockerUpdated;
        }
        public void UpdateInterlock()//called by Controller Pickup Event
        {
            UpdateInterlock(true);
        }

        public void RelayFailSafe()
        {
            //Search all Intelock/
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(RelayFailSafe));
            if (!FailSafeCalled)
            {
                FailSafeCalled = true;
                From_Locker.SetToFailSafePosition();
                From_Locker.RelayFailSafe();
                foreach (LockerAndMeetPosition interlockState in toLocker_States)
                {
                    interlockState.RelayFailSafe();
                }
                GetRouteLocker().RelayFailSafe();
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(RelayFailSafe));
        }
        public void SetupLocker()
        {
            if(From_Locker)From_Locker.AddNewLocker(this);
        }
    }

}