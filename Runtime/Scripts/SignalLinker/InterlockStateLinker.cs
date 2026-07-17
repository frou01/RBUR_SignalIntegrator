using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class InterlockStateLinker : UdonSharpBehaviour
    {
        [SerializeField] AbstractLockerConsolidater locker;
        [SerializeField] int MeetPosition;
        [HideInInspector][SerializeField] Interlocking interlocking;
        public virtual bool Check()
        {
            return locker.GetCurrentPosition() == MeetPosition;
        }

        //return: Sucess?
        public virtual bool UpdateLock(bool isLocking)
        {
            return locker.tryUpdateLocking(this, isLocking, MeetPosition);
        }
        public void RelayFailSafe()
        {
            if (!interlocking.FailSafeCalled)
                locker.RelayFailSafe();
        }
        public void SetupLocker()
        {
            locker.AddNewLocker(this);
        }
    }
}