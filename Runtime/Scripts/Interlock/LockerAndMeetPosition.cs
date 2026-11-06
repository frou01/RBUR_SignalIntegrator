using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    public class LockerAndMeetPosition : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [SerializeField] protected AbstractLockerConsolidator locker;
        [FormerlySerializedAs("MeetPositionIndex")][SerializeField] protected int[] meetPositionIndexes;
        [HideInInspector][SerializeField] protected Interlocking interlocking;
        public AbstractLockerConsolidator GetLocker()
        {
            return locker;
        }
        public void SetParentInterlock(Interlocking val)
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
            meetPositionIndexes = meetPositionIndex;
        }
#endif
        public virtual bool Check()
        {
            foreach (int meet in meetPositionIndexes)
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
                res = locker.TryUpdateLocking(this, isLocking, locker.GetCurrentPosition());
            }
            else
            {
                if (isLocking && !locker.IsControllerOwner()) res = false;
                else
                {
                    res = locker.TryUpdateLocking(this, isLocking, meetPositionIndexes[0]);
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
                if (!interlocking.isFailSafeCalled)
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