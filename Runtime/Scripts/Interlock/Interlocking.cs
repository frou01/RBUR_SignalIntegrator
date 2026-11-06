
using frou01.GrabController;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    public class Interlocking : UdonSharpBehaviour
    {
        [FormerlySerializedAs("logging")][SerializeField] bool isLogging;

        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [FormerlySerializedAs("From_Locker")][SerializeField] AbstractLockerConsolidator fromLocker;//GAC以外も制御できるように間を挟む
        [FormerlySerializedAs("interlock_Route")][SerializeField] RouteLocker routeLocker;//進路開通取得と、開通状態で固定用
        [FormerlySerializedAs("toLocker_States")][SerializeField] LockerAndMeetPosition[] stateLinkers = new LockerAndMeetPosition[0];//進路以外の進行要件
        [FormerlySerializedAs("ReleasePositionIndex")]
        [FormerlySerializedAs("ClosePositionIndex")][SerializeField] int closePositionIndex;
        [FormerlySerializedAs("LockPositionIndexes")]
        [FormerlySerializedAs("OpenPositionIndexes")][SerializeField] int[] openPositionIndexes;
        [FormerlySerializedAs("FailSafeCalled")][HideInInspector][SerializeField] public bool isFailSafeCalled;
        [HideInInspector][SerializeField] public Interlocking[] affectedInterlockings;//自身を含む関係する鎖錠
        [HideInInspector][SerializeField] public AbstractLockerConsolidator[] affectedLockers;//直接自身に関係する鎖錠

        public void Start()
        {
            if (fromLocker) fromLocker.AddNewLocker(this);
            if (routeLocker) routeLocker.SetParentInterlock(this);
            UpdateInterlock(false);
        }
        public RouteLocker GetRouteLocker()
        {
            if (!routeLocker) routeLocker = GetComponentInChildren<RouteLocker>();
            if (routeLocker) routeLocker.SetParentInterlock(this);
            return routeLocker;
        }
        public LockerAndMeetPosition[] GetInterlockStateLinker()
        {
            return stateLinkers;
        }
        public AbstractLockerConsolidator GetFromLocker()
        {
            return fromLocker;
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public int GetClosePositionIndex()
        {
            return closePositionIndex;
        }
        /// <summary>
        /// ビルド処理から進行要件を追加する
        /// </summary>
        public void AddInterlockStateLinker(LockerAndMeetPosition stateLinker)
        {
            LockerAndMeetPosition[] newStates = new LockerAndMeetPosition[stateLinkers.Length + 1];
            stateLinkers.CopyTo(newStates, 0);
            newStates[stateLinkers.Length] = stateLinker;
            stateLinkers = newStates;
        }
#endif

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

        bool isInitialFetch = true;
        bool prevIsSignalOpened;
        bool prevCanOpenSignal;
        /// <summary>
        /// 鎖錠の更新を行う。From_Lockerの状態と、stateLinkers、RouteLocker(進路鎖錠)の状態を確認する
        /// From_LockerがOpen位置であればtoLocker_StatesとRouteLockerを位置変更及びロックする。From_Lockerが閉鎖位置であれば、toLocker_StatesとRouteLockerを解放する。
        /// toLocker_StatesとRouteLockerいずれかが適合位置でない場合、From_Lockerを閉鎖位置にロックする。
        /// </summary>
        /// <param name="updateAffectedInterlock">関係する鎖錠も更新するかどうか</param>
        /// <returns>関係するLockerについて、位置の変更を行ったかどうか</returns>
        [RecursiveMethod]
        public bool UpdateInterlock(bool updateAffectedInterlock)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(UpdateInterlock));

            if (isLogging) Debug.Log(this.name + ": UpdateInterlock.Start", this);


            if (isLogging) Debug.Log(this.name + ": UpdateInterlock.Check_To");

            //Fetching State
            bool canOpenSignal = GetRouteLocker().IsRouteOpen();
            bool isSignalOpened = false;
            foreach (LockerAndMeetPosition interlockState in stateLinkers)
            {
                canOpenSignal &= interlockState.Check();
            }
            int currentSignalPos = fromLocker.GetCurrentPosition();
            foreach (int openPosition in openPositionIndexes)
            {
                if (currentSignalPos == openPosition)
                {
                    isSignalOpened = true;
                    break;
                }
            }

            if (isInitialFetch || (isSignalOpened != prevIsSignalOpened) || (canOpenSignal != prevCanOpenSignal))//Edge Only
            {
                isInitialFetch = false;//For detouring on first call.


                //Initialize controller for update detection.
                foreach (AbstractLockerConsolidator locker in affectedLockers)
                {
                    locker.ResetFlagPosChangeOnLockDetector();
                }
                //Flag for Affected Object Search
                isFailSafeCalled = false;

                prevIsSignalOpened = isSignalOpened;
                prevCanOpenSignal = canOpenSignal;

                if (isSignalOpened)
                {
                    bool lockSucceeded = true;
                    if (isLogging) Debug.Log(this.name + ": UpdateInterlock.Lock_To", this);
                    foreach (LockerAndMeetPosition interlockState in stateLinkers)
                    {
                        lockSucceeded &= interlockState.UpdateLock(true);
                    }
                    lockSucceeded &= GetRouteLocker().UpdateLockRoute(true, fromLocker.IsControllerOwner());

                    if (isLogging) Debug.Log(this.name + ": UpdateInterlock.LockResult " + lockSucceeded, this.gameObject);
                    if (!lockSucceeded && fromLocker.IsControllerOwner())
                    {
                        Debug.LogError(this.name + ": inconsistency Interlocking. Reset All Signals", this.gameObject);
                        this.RelayFailSafe();
                    }
                    if (canOpenSignal) fromLocker.TryUpdateLocking(this, false, closePositionIndex);
                }
                else
                {
                    if (isLogging) Debug.Log(this.name + ": UpdateInterlock.Release_To", this);
                    foreach (LockerAndMeetPosition interlockState in stateLinkers)
                    {
                        interlockState.UpdateLock(false);
                    }
                    GetRouteLocker().UpdateLockRoute(false, false);

                    if (!canOpenSignal)
                    {
                        if (isLogging) Debug.Log(this.name + ": UpdateInterlock.Lock_From", this);
                        bool lockSucceeded = fromLocker.TryUpdateLocking(this, true, closePositionIndex);
                        if (!lockSucceeded)
                        {
                            Debug.LogError(this.name + ": Interlock Preset Error. Cannot lock fromLocker to close position. Already locked on another position", fromLocker);
                        }
                    }
                    else
                    {
                        if (isLogging) Debug.Log(this.name + ": UpdateInterlock.Release_From", this);
                        fromLocker.TryUpdateLocking(this, false, closePositionIndex);
                    }
                }
            }

            bool lockerUpdated = false;
            foreach (AbstractLockerConsolidator locker in affectedLockers)
            {
                if (locker.GetFlagPosChangeOnLockDetector())
                {
                    lockerUpdated = true;
                    break;
                }
            }
            foreach (AbstractLockerConsolidator locker in affectedLockers)
            {
                locker.ResetFlagPosChangeOnLockDetector();//フラグを降ろしておかないと関係他鎖錠の更新時に壊れる事になる
            }

            if (lockerUpdated && updateAffectedInterlock)
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
            return lockerUpdated;
        }
        public void UpdateInterlock()//called by Controller Pickup Event
        {
            UpdateInterlock(true);
        }

        public void RelayFailSafe()
        {
            //Search all Interlock/
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(RelayFailSafe));
            if (!isFailSafeCalled)
            {
                isFailSafeCalled = true;
                fromLocker.SetToFailSafePosition();
                fromLocker.RelayFailSafe();
                foreach (LockerAndMeetPosition interlockState in stateLinkers)
                {
                    interlockState.RelayFailSafe();
                }
                GetRouteLocker().RelayFailSafe();
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(RelayFailSafe));
        }
        public void SetupLocker()
        {
            if(fromLocker)fromLocker.AddNewLocker(this);
        }
    }

}