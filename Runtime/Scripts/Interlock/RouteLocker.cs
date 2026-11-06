using frou01.RigidBodyTrain;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    public class RouteLocker : UdonSharpBehaviour
    {
        /// <summary>
        /// Route array [index = points-index]
        /// </summary>
        [FormerlySerializedAs("TargetPoints")]
        [FormerlySerializedAs("m_TargetPoints")][SerializeField] protected AbstractPointSetter[] targetPoints;
        
        public AbstractPointSetter[] TargetPoints//TODO: カスタムエディタを用意して分岐器-進路を外部設定にするべき。EditorGUI.BeginChangeCheck()を使えば探索を組める。
        {
            get
            {
                return targetPoints;
            }
            set
            {
                targetPoints = value;
            }
        }
        [FormerlySerializedAs("TargetRoute")]
        [FormerlySerializedAs("m_TargetRoute")][SerializeField] protected int[] targetRoutes;
        public int[] TargetRoutes
        {
            get
            {
                return targetRoutes;
            }
            set
            {
                targetRoutes = value;
            }
        }

        [FormerlySerializedAs("Route_Rails")][HideInInspector][SerializeField] public Rail_Script[] routeRails;//カスタムエディタ向けの経路情報保持用

        [HideInInspector][SerializeField] public EventStackHolder eventStackHolder;
        [FormerlySerializedAs("ControlTargetIndex")][HideInInspector][SerializeField] public int[] controlTargetIndexes = new int[0];
        [FormerlySerializedAs("m_lockers")][HideInInspector][SerializeField] protected AbstractLockerConsolidator[] lockers = new AbstractLockerConsolidator[0];
        [HideInInspector][SerializeField] protected Interlocking interlocking;
        /// <summary>
        /// Return user preset Points
        /// </summary>
        /// <returns>Points array [index = route-index]</returns>
        public AbstractPointSetter[] GetTargetPoints()
        {
            return targetPoints;
        }

        /// <summary>
        /// Current found Point controller Locker array / overwrite 
        /// </summary>
        public AbstractLockerConsolidator[] Lockers
        {
            get
            {
                return lockers;
            }
            set
            {
                lockers = value;
            }
        }

        public void SetupLocker()
        {
            foreach (AbstractLockerConsolidator locker in lockers)
            {
                if (locker != null) locker.AddNewLocker(this);
            }
        }

        /// <summary>
        /// Check all points are opened to setted routes.
        /// </summary>
        /// <returns>is route opened</returns>
        public virtual bool IsRouteOpen()
        {
            bool isOpen = true;

            int idx = 0;
            foreach (AbstractPointSetter setter in targetPoints)
            {
                if (targetRoutes[idx] != setter.get_current_To_Index())
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
        public void SetParentInterlock(Interlocking val)
        {
            interlocking = val;
        }

        /// <summary>
        /// Lock/UnlockRoute.
        /// State=False, unlockroute
        /// State=True,
        ///     syncingMode = True, try to lock witch control overwrite(use on synced).
        ///     syncingMode = False, check route open, then try to lock
        /// </summary>
        /// <param name="state">True: lock, False: release</param>
        /// <param name="syncingMode">True: overwrite current route, False: dont overwrite current route</param>
        /// <returns></returns>
        public bool UpdateLockRoute(bool state,bool syncingMode)
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(UpdateLockRoute));
            if (lockers.Length == 0)
            {
                if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLockRoute));
                return true;
            }
            if (!state)
            {
                foreach (AbstractLockerConsolidator locker in lockers)
                {
                    locker.TryUpdateLocking(this,false,-1);
                }
                if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLockRoute));
                return true;
            }
            else
            {
                if (syncingMode && !IsRouteOpen())
                {
                    if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLockRoute));
                    return false;
                }

                bool result = true;

                int idx = 0;
                foreach (AbstractLockerConsolidator locker in lockers)
                {
                    result &= locker.TryUpdateLocking(this, true, controlTargetIndexes[idx]);
                    idx++;
                }
                if(eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(UpdateLockRoute));
                return result;
            }
        }


        bool isRelayingFailSafe;
        public void RelayFailSafe()
        {
            if(eventStackHolder != null)eventStackHolder.AddStack(this, nameof(RelayFailSafe));
            if (!isRelayingFailSafe)
            {
                isRelayingFailSafe = true;
                if (!interlocking.isFailSafeCalled)
                    foreach (AbstractLockerConsolidator locker in lockers)
                    {
                        locker.RelayFailSafe();
                    }
            }
            isRelayingFailSafe = false;
            if (eventStackHolder != null)eventStackHolder.RemoveStack(this, nameof(RelayFailSafe));
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            int pointIdx = 0;
            int routeIdx;
            if (targetPoints != null)
            {
                foreach (AbstractPointSetter setter in targetPoints)
                {
                    routeIdx = 0;
                    Gizmos.color = new Color(0f, 0.5f, 0f, 1f);
                    setter.DrawGizmo_From();

                    foreach (Rail_Script route in setter.getRoutes())
                    {
                        if (routeIdx == targetRoutes[pointIdx]) Gizmos.color = new Color(0f, 0.5f, 0f, 1f);
                        else Gizmos.color = new Color(1f, 0f, 0f, 1f);
                        setter.DrawGizmo_To(route);
                        routeIdx++;
                    }
                    pointIdx++;
                }
            }
        }
#endif
    }
}