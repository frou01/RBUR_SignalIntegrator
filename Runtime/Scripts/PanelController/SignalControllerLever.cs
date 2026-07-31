using UnityEngine;
using frou01.util;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
#endif

namespace RBUR_SignalIntegrator
{
    public class SignalControllerLever : AbstractPanelController
    {
        [SerializeField] Animator[] SignalSideAnimators;
        [SerializeField] string signalAnimationParamater = "SignalRelayPosition";
        [SerializeField] int signalRelayPositionNum;
        protected override void applyPositionToController(int posIndex)
        {
            base.applyPositionToController(posIndex);

            foreach (Animator animator in SignalSideAnimators)
            {
                animator.enabled = true;
                animator.SetFloat(signalAnimationParamater, (float)controllingPosition/ (signalRelayPositionNum-1));
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base .OnDrawGizmosSelected();
            GUIStyle guiStyle = new GUIStyle();
            foreach (Animator animator in SignalSideAnimators)
            {
                Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
                guiStyle.normal.textColor = Gizmos.color;

                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.02f, 0.02f);
                Handles.Label(Vector3.Lerp(GizmoExtension.getCenter(this.transform), GizmoExtension.getCenter(animator.transform), 0.8f), this.gameObject.name + ".SignalSideAnimator", guiStyle);
            }
        }
#endif
    }
}