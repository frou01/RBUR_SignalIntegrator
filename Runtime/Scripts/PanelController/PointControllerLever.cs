using frou01.RigidBodyTrain;
using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace RBUR_SignalIntegrator
{
    public class PointControllerLever : AbstractPanelController
    {
        [SerializeField] protected AbstractPointSetter pointInstance;
        public AbstractPointSetter getPointInstance()
        {
            return pointInstance;
        }
        [SerializeField] Animator[] PointSideAnimators;
        [SerializeField] string currentRouteParamater = "PointRoute";
        [SerializeField] string progressParamater = "PointMovePos";
        int pointCon_prevControllingPosition = -1;
        protected int PrevPointRouteIndex = int.MinValue;
        [SerializeField] protected int PointRouteIndex;
        [SerializeField] protected float changeProgress;
        [SerializeField] protected float changeTimeLength = 1;
        [SerializeField] bool NetworkUpdate;
        protected override void Start()
        {
            base.Start();
            PointUpdate();
        }
        public override int GetCurrentPosition()
        {
            return PointRouteIndex;
        }
        protected override void applyPositionToController(int posIndex)
        {
            base.applyPositionToController(posIndex);
            if(pointCon_prevControllingPosition != controllingPosition)
            {
                pointInstance.set_route_To(-1);
                this.enabled = true;
                pointCon_prevControllingPosition = controllingPosition;
            }
        }

        private void Update()
        {
            if (PointRouteIndex != controllingPosition)
            {
                int prevChanging = Mathf.RoundToInt(changeProgress / changeTimeLength);
                int Changing = -1;
                if (controllingPosition * changeTimeLength > changeProgress)
                {
                    prevChanging = Mathf.FloorToInt(changeProgress / changeTimeLength);
                    changeProgress += Time.deltaTime;
                    Changing = Mathf.FloorToInt(changeProgress / changeTimeLength);
                }
                else if (controllingPosition * changeTimeLength < changeProgress)
                {
                    prevChanging = Mathf.CeilToInt(changeProgress / changeTimeLength);
                    changeProgress -= Time.deltaTime;
                    Changing = Mathf.CeilToInt(changeProgress / changeTimeLength);
                }
                else
                {

                }
                if (prevChanging != Changing)
                {
                    changeProgress = Changing * changeTimeLength;
                    pointInstance.set_route_To(Changing);
                }
                foreach (Animator animator in PointSideAnimators)
                {
                    animator.enabled = true;
                    animator.SetFloat(progressParamater, changeProgress / changeTimeLength);
                }
            }
            else
            {
                if (NetworkUpdate && !isControllerOwner())
                {
                    NetworkUpdate = false;
                }
                if (!NetworkUpdate)
                {
                    this.enabled = false;
                }
            }
        }

        public void PointUpdate()
        {
            this.enabled = true;
            PointRouteIndex = pointInstance.get_current_To_Index();
            foreach (Animator animator in PointSideAnimators)
            {
                animator.enabled = true;
                animator.SetFloat(currentRouteParamater, (float)PointRouteIndex / (float)(pointInstance.getRoutes().Length-1));
            }
            if(PrevPointRouteIndex != PointRouteIndex)
            {
                PrevPointRouteIndex = PointRouteIndex;
                foreach (Interlocking interlock in interlocks)
                {
                    interlock.UpdateInterlock();
                }
            }
        }
        public override void SyncController()
        {
            this.enabled = true;
            NetworkUpdate = true;
            base.SyncController();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            base.OnPostSerialization(result);
            if (result.success)
            {
                NetworkUpdate = false;
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.1f);
            if(pointInstance)GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(pointInstance.transform), 0.02f, 0.02f);
        }
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in PointSideAnimators)
            {
                Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
                guiStyle.normal.textColor = Gizmos.color;
                GizmoExtension.DrawArrow(this.transform.position, animator.transform.position, 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), this.gameObject.name + ".PointAnimator", guiStyle);
            }
            if (pointInstance)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 1f);
                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), pointInstance.transform.position, 0.02f, 0.02f);
                guiStyle.normal.textColor = Gizmos.color;
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), pointInstance.transform.position, 0.6f), this.gameObject.name + ".PointInstance", guiStyle);

                Gizmos.color = new Color(0f, 1f, 0f, 1f);
                pointInstance.DrawGizmo_From();
                Vector4 colorVec_Start = new Vector4(0f, 1f, 1f, 1f);
                Vector4 colorVec_End = new Vector4(1f, 1f, 0f, 1f);
                float routeNum = pointInstance.getRoutes().Length - 1;
                float idx = 0;
                foreach (Rail_Script route in pointInstance.getRoutes())
                {
                    Vector4 currentCol = Vector4.Lerp(colorVec_Start, colorVec_End, idx / routeNum);
                    Gizmos.color = new Color(currentCol.x, currentCol.y, currentCol.z, currentCol.w);
                    pointInstance.DrawGizmo_To(route);
                    idx++;
                }
            }
        }
#endif
    }
}