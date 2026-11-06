using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using frou01.util;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Linq;
using HarmonyLib;
#endif

namespace frou01.RBUR.SignalIntegrator
{
    [ExecuteAlways]
    public class MultiLeverSwitchToControlMapsHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] public int switchPositionNum = 2;
        [SerializeField] public List<ToControllerMap> Switch_To_ControlMaps = new List<ToControllerMap>();
        public void ApplyToMultiLeverController()
        {
            MultiLeverController targetMulCon = GetComponent<MultiLeverController>();
            if (targetMulCon)
            {
                targetMulCon.SwitchToControllerMap = GetSwitchToControllerMap();
                targetMulCon.ControlledLevers = GetControllers();
            }
        }

        public int[][] GetSwitchToControllerMap()
        {
            return Switch_To_ControlMaps
                .Select(val => val.Switch_To_Control)//List<ToControllerMap> -> List<int[]>
                .ToArray();
        }
        public AbstractIndirectController[] GetControllers()
        {
            return Switch_To_ControlMaps.Select(val => val.linkedController).ToArray();
        }

        [System.Serializable]
        public class ToControllerMap
        {
            [SerializeField] public AbstractIndirectController linkedController;
            [Tooltip("index = switch, value = contro. No overwrite lever value = -1")][SerializeField] public int[] Switch_To_Control;

            public ToControllerMap(AbstractIndirectController linkedController, int[] switchToControl)
            {
                this.linkedController = linkedController;
                this.Switch_To_Control = switchToControl;
            }
        }
        private void OnDrawGizmosSelected()
        {
            GUIStyle guiStyle = new GUIStyle();
            guiStyle.richText = true;
            Vector4 colorVecStart = new Vector4(0.2f, 0.2f, 1f, 1f);
            Vector4 colorVecEnd = new Vector4(1f, 0f, 0f, 1f);
            foreach (ToControllerMap controlSetting in Switch_To_ControlMaps)
            {
                Gizmos.color = new Color(0.2f, 1.0f, 0.0f, 1f);
                Vector3 controlPos = GizmoExtension.getCenter(controlSetting.linkedController.transform);
                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), controlPos, 0.02f, 0.02f);

                string switchSettingText = controlSetting.linkedController.name;
                int switchIdx = 0;
                foreach (int controlOnSwitch in controlSetting.Switch_To_Control)
                {
                    Vector4 switchColor = Vector4.Lerp(colorVecStart, colorVecEnd, (float)switchIdx / (switchPositionNum > 1 ? switchPositionNum - 1 : 1));
                    switchSettingText += "\n" +
                    "<color=#" + ColorToString(switchColor) + ">" + "switch " + switchIdx + " : " + controlOnSwitch + "</color>";
                    switchIdx++;
                }
                guiStyle.normal.textColor = Color.white;
                Handles.Label(controlPos, switchSettingText, guiStyle);
            }
        }

        private string ColorToString(Vector4 color)
        {
            return ((int)(color.x * 255)).ToString("X2") + ((int)(color.y * 255)).ToString("X2") + ((int)(color.z * 255)).ToString("X2") + ((int)(color.w * 255)).ToString("X2");
        }
#endif
    }
}
