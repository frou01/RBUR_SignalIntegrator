using frou01.RigidBodyTrain;
using frou01.util;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

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
        [SerializeField] string pointAnimationParamater = "PointRoute";
        protected override void applyPositionToController(int posIndex)
        {
            base.applyPositionToController(posIndex);
            if(controllingPosition >= 0) pointInstance.set_current_To(controllingPosition);
        }

        public void PointUpdate()
        {
            foreach (Animator animator in PointSideAnimators)
            {
                animator.enabled = true;
                animator.SetFloat(pointAnimationParamater, (float)pointInstance.get_current_To_Index() / (pointInstance.getRoutes().Length-1));
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.1f);
            GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(pointInstance.transform), 0.02f, 0.02f);
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
            Gizmos.color = new Color(0f, 0f, 1f, 1f);
            GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), pointInstance.transform.position, 0.02f, 0.02f);
            guiStyle.normal.textColor = Gizmos.color;
            Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), pointInstance.transform.position, 0.6f), this.gameObject.name + ".PointInstance", guiStyle);

            Gizmos.color = new Color(0f, 1f, 0f, 1f);
            pointInstance.DrawGizmo_From();
            Vector4 colorVec_Start = new Vector4(0f, 1f, 1f, 1f);
            Vector4 colorVec_End = new Vector4(1f, 1f, 0f, 1f);
            float routeNum = pointInstance.getRoutes().Length-1;
            float idx = 0;
            foreach(Rail_Script route in pointInstance.getRoutes())
            {
                Vector4 currentCol = Vector4.Lerp(colorVec_Start, colorVec_End,idx/routeNum);
                Gizmos.color = new Color(currentCol.x, currentCol.y, currentCol.z, currentCol.w);
                pointInstance.DrawGizmo_To(route);
                idx++;
            }
        }
#endif
    }
}