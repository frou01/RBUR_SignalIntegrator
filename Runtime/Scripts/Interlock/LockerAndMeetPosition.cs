using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    public class LockerAndMeetPosition : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [SerializeField] protected AbstractLockerConsolidator locker;
        [SerializeField] protected int[] MeetPositionIndex;
        [HideInInspector][SerializeField] protected Interlocking interlocking;
        public AbstractLockerConsolidator getLocker()
        {
            return locker;
        }
        public void setParentInterlock(Interlocking val)
        {
            interlocking = val;
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        /// <summary>
        /// ビルド処理から対象の Locker と適合位置を設定する
        /// </summary>
        public void SetLockerAndMeetPosition(AbstractLockerConsolidator locker, int[] meetPositionIndex)
        {
            this.locker = locker;
            MeetPositionIndex = meetPositionIndex;
        }
#endif
        public virtual bool Check()
        {
            foreach (int meet in MeetPositionIndex)
            {
                if (locker.GetCurrentPosition() == meet)
                {
                    return true;
                }
            }
            return false;
        }

        //return: Success?
        public virtual bool UpdateLock(bool isLocking)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(UpdateLock));
            bool res;
            if (Check())
            {
                res = locker.tryUpdateLocking(this, isLocking, locker.GetCurrentPosition());
            }
            else
            {
                if (isLocking && !locker.isControllerOwner()) res = false;
                else
                {
                    res = locker.tryUpdateLocking(this, isLocking, MeetPositionIndex[0]);
                }
            }
            if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLock));
            return res;
        }

        bool isRelayingFailSafe;
        public void RelayFailSafe()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(RelayFailSafe));
            if (!isRelayingFailSafe)
            {
                isRelayingFailSafe = true;
                if (!interlocking.FailSafeCalled)
                    locker.RelayFailSafe();
            }
            isRelayingFailSafe = false;
            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(RelayFailSafe));
        }
        public void SetupLocker()
        {
            locker.AddNewLocker(this);
        }
    }
}