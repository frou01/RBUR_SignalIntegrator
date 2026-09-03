using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class Interlock_ToLockerAndMeetPosition : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [SerializeField] protected AbstractLockerConsolidater locker;
        [SerializeField] protected int[] MeetPositionIndex;
        [HideInInspector][SerializeField] protected Interlocking interlocking;
        public AbstractLockerConsolidater getLocker()
        {
            return locker;
        }
        public void setParentInterlock(Interlocking val)
        {
            interlocking = val;
        }
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

        //return: Sucess?
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

        bool FailSafeLoopCancel;
        public void RelayFailSafe()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(RelayFailSafe));
            if (!FailSafeLoopCancel)
            {
                FailSafeLoopCancel = true;
                if (!interlocking.FailSafeCalled)
                    locker.RelayFailSafe();
            }
            FailSafeLoopCancel = false;
            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(RelayFailSafe));
        }
        public void SetupLocker()
        {
            locker.AddNewLocker(this);
        }
    }
}