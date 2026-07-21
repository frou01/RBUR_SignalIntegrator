using frou01.RigidBodyTrain;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace RBUR_SignalIntegrator
{
    public class RouteChecker : UdonSharpBehaviour
    {
        [SerializeField] protected AbstractPointSetter[] TargetPoints;
        [SerializeField] protected int[] TargetRoute;

        public AbstractPointSetter[] GetTargetPoints()
        {
            return TargetPoints;
        }

        public void SetupCallback()
        {
            foreach (AbstractPointSetter setter in TargetPoints)
            {
                UdonSharpBehaviour[] newArray = new UdonSharpBehaviour[setter.callbackUdons.Length + 1];
                setter.callbackUdons.CopyTo(newArray, 0);
                newArray[setter.callbackUdons.Length] = this;
                setter.callbackUdons = newArray;
            }
        }
        public virtual void Start()
        {
            SetupCallback();
            PointUpdate();
        }

        public virtual void PointUpdate()//Call via AbstractPointSetter.callbackUdons
        {
        }
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

                    foreach(Rail_Script route in setter.getRoutes())
                    {
                        if(route_idx == TargetRoute[point_idx]) Gizmos.color = new Color(0f, 0.5f, 0f, 1f);
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