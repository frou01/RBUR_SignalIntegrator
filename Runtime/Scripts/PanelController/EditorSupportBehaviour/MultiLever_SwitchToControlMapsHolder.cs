using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using frou01.util;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Linq;
using HarmonyLib;
#endif

namespace RBUR_SignalIntegrator
{
    [ExecuteAlways]
    public class MultiLever_SwitchToControlMapsHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] public int switchPositionNum = 2;
        [SerializeField] public List<To_ControllerMap> Switch_To_ControlMaps;
        public void apply_mulCon_to_Con_Map()
        {
            MultiLeverController TargetMulCon = GetComponent<MultiLeverController>();
            if (TargetMulCon)
            {
                TargetMulCon.Set_switchToControllerMap(Switch_To_ControlMaps.Select(val => val.linkedController).ToArray(),
                    Switch_To_ControlMaps
                    .Select(val => val.Switch_To_Control)//List<To_ControllerMap> -> List<int[]>
                    .ToArray());
            }
        }
        public AbstractIndirectController[] GetControllers()
        {
            return Switch_To_ControlMaps.Select(val => val.linkedController).ToArray();
        }

        [System.Serializable]
        public class To_ControllerMap
        {
            [SerializeField] public int onMulConOrder;
            [SerializeField] public AbstractIndirectController linkedController;
            [Tooltip("index = switch, value = contro. No overwrite lever value = -1")][SerializeField] public int[] Switch_To_Control;

            public To_ControllerMap(int onMulConOrder, AbstractIndirectController linkedController, int[] MulConSwitch_To_ControllerControlMap)
            {
                this.onMulConOrder = onMulConOrder;
                this.linkedController = linkedController;
                this.Switch_To_Control = MulConSwitch_To_ControllerControlMap;
            }
        }
        private void OnDrawGizmosSelected()
        {
            GUIStyle guiStyle = new GUIStyle();
            guiStyle.richText = true;
            Vector4 colorVec_Start = new Vector4(0.2f, 0.2f, 1f, 1f);
            Vector4 colorVec_End = new Vector4(1f, 0f, 0f, 1f);
            foreach (To_ControllerMap ControlSetting in Switch_To_ControlMaps)
            {
                Gizmos.color = new Color(0.2f, 1.0f, 0.0f, 1f);
                Vector3 controlPos = GizmoExtension.getCenter(ControlSetting.linkedController.transform);
                GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), controlPos, 0.02f, 0.02f);

                string switchSettingText = ControlSetting.linkedController.name;
                int switchIdx = 0;
                foreach (int controlOnSwitch in ControlSetting.Switch_To_Control)
                {
                    Vector4 switchColor = Vector4.Lerp(colorVec_Start, colorVec_End, (float)switchIdx / (switchPositionNum > 1 ? switchPositionNum - 1 : 1));
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
