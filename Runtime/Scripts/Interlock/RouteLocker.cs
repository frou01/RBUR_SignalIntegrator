using frou01.RigidBodyTrain;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class RouteLocker : UdonSharpBehaviour
    {
        [SerializeField] protected AbstractPointSetter[] TargetPoints;
        [SerializeField] protected int[] TargetRoute;
        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [HideInInspector][SerializeField] public int[] ControlTargetIndex;
        [HideInInspector][SerializeField] protected AbstractLockerConsolidater[] m_lockers;
        [HideInInspector][SerializeField] protected Interlocking interlocking;

        /// <summary>
        /// Return user preset route
        /// </summary>
        /// <returns>Route array [index = points-index]</returns>
        public int[] getTargetRoute()
        {
            return TargetRoute;
        }
        /// <summary>
        /// Return user preset Points
        /// </summary>
        /// <returns>Points array [index = route-index]</returns>
        public AbstractPointSetter[] GetTargetPoints()
        {
            return TargetPoints;
        }

        /// <summary>
        /// Current found Point controller Locker array / overwrite 
        /// </summary>
        public AbstractLockerConsolidater[] Lockers
        {
            get
            {
                return m_lockers;
            }
            set
            {
                m_lockers = value;
            }
        }

        public void SetupLocker()
        {
            foreach (AbstractLockerConsolidater locker in m_lockers)
            {
                if (locker != null) locker.AddNewLocker(this);
            }
        }

        /// <summary>
        /// Check all points are opened to setted routes.
        /// </summary>
        /// <returns>is route opened</returns>
        public virtual bool isRouteOpen()
        {
            bool isOpen = true;

            int idx = 0;
            foreach (AbstractPointSetter setter in TargetPoints)
            {
                if (TargetRoute[idx] != setter.get_current_To_Index())
                {
                    isOpen = false;
                    break;
                }
                idx++;
            }
            return isOpen;
        }

        /// <summary>
        /// Set Parent Interlock
        /// </summary>
        /// <param name="val"></param>
        public void setParentInterlock(Interlocking val)
        {
            interlocking = val;
        }

        /// <summary>
        /// Lock/UnlockRoute.
        /// State=False, unlockroute
        /// State=True,
        ///     Syncing_mode = True, try to lock witch control overwrite(use on synced).
        ///     Syncing_mode = False, check route open, then try to lock
        /// </summary>
        /// <param name="State">True: lock, False: release</param>
        /// <param name="Syncing_mode">True: overwrite current route, False: dont overwrite current route</param>
        /// <returns></returns>
        public bool UpdateLockRoute(bool State,bool Syncing_mode)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(UpdateLockRoute));
            if (m_lockers.Length == 0)
            {
                if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLockRoute));
                return true;
            }
            if (!State)
            {
                foreach (AbstractLockerConsolidater locker in m_lockers)
                {
                    locker.tryUpdateLocking(this,false,-1);
                }
                if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLockRoute));
                return true;
            }
            else
            {
                if (Syncing_mode && !isRouteOpen())
                {
                    if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLockRoute));
                    return false;
                }

                bool result = true;

                int idx = 0;
                foreach (AbstractLockerConsolidater locker in m_lockers)
                {
                    result &= locker.tryUpdateLocking(this, true, ControlTargetIndex[idx]);
                    idx++;
                }
                if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLockRoute));
                return result;
            }
        }


        bool FailSafeLoopCancel;
        public void RelayFailSafe()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(RelayFailSafe));
            if (!FailSafeLoopCancel)
            {
                FailSafeLoopCancel = true;
                if (!interlocking.FailSafeCalled)
                    foreach (AbstractLockerConsolidater locker in m_lockers)
                    {
                        locker.RelayFailSafe();
                    }
            }
            FailSafeLoopCancel = false;
            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(RelayFailSafe));
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            int point_idx = 0;
            int route_idx;
            if (TargetPoints != null)
            {
                foreach (AbstractPointSetter setter in TargetPoints)
                {
                    route_idx = 0;
                    Gizmos.color = new Color(0f, 0.5f, 0f, 1f);
                    setter.DrawGizmo_From();

                    foreach (Rail_Script route in setter.getRoutes())
                    {
                        if (route_idx == TargetRoute[point_idx]) Gizmos.color = new Color(0f, 0.5f, 0f, 1f);
                        else Gizmos.color = new Color(1f, 0f, 0f, 1f);
                        setter.DrawGizmo_To(route);
                        route_idx++;
                    }
                    point_idx++;
                }
            }
        }
#endif
    }
}