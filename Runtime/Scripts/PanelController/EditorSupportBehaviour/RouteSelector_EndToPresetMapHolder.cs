using frou01.RigidBodyTrain;
using frou01.util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RBUR_SignalIntegrator
{
    public class RouteSelector_EndToPresetMapHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] List<SwitchToPresets> Switch_To_EndAndPresetMaps;

        public void ApplyEndToPresetArray(out RouteSelector_EndButton[][] ends,out MultiLeverController[][] presets)
        {
            ends = Switch_To_EndAndPresetMaps.Select(val =>
                val.EndAndPresetMulcon.Select(val => val.end).ToArray()
            ).ToArray();
            presets = Switch_To_EndAndPresetMaps.Select(val => 
                val.EndAndPresetMulcon.Select(val => val.preset).ToArray()
            ).ToArray();
        }

        [Serializable]
        private class SwitchToPresets
        {
            [SerializeField] public List<EndAndMulcon> EndAndPresetMulcon;
        }

        [Serializable]
        private class EndAndMulcon
        {
            [SerializeField] public RouteSelector_EndButton end;
            [SerializeField] public MultiLeverController preset;
        }

        private void OnDrawGizmosSelected()
        {
            GUIStyle guiStyle = new GUIStyle();
            Gizmos.color = new Color(0.2f, 0.2f, 1f, 1f);
            Vector4 colorVec_Start = new Vector4(0.2f, 0.2f, 1f, 1f);
            Vector4 colorVec_End = new Vector4(1f, 0f, 0f, 1f);
            int switchIndex = 0;
            int switchNum = Switch_To_EndAndPresetMaps.Count-1;
            if (switchNum == 0) switchNum = 1;
            foreach (SwitchToPresets switchPresets in Switch_To_EndAndPresetMaps)
            {
                Vector4 colorVec_Switch_Start = Vector4.Lerp(colorVec_Start, colorVec_End, switchIndex / switchNum);
                Vector4 colorVec_Switch_End = Vector4.Lerp(colorVec_Start, colorVec_End, switchIndex+1 / switchNum);
                int presetIndex = 0;
                int presetNum = switchPresets.EndAndPresetMulcon.Count-1;
                if (presetNum == 0)
                {
                    presetNum = 1;
                }
                foreach (EndAndMulcon endAndMulcon in switchPresets.EndAndPresetMulcon)
                {
                    Vector4 currentCol = Vector4.Lerp(colorVec_Switch_Start, colorVec_Switch_End, presetIndex / presetNum);
                    Gizmos.color = new Color(currentCol.x, currentCol.y, currentCol.z, currentCol.w);
                    guiStyle.normal.textColor = Gizmos.color;

                    Vector3 endPos = GizmoExtension.getCenter(endAndMulcon.end.transform);
                    Vector3 presetPos = GizmoExtension.getCenter(endAndMulcon.preset.transform);

                    GizmoExtension.DrawArrow(GizmoExtension.getCenter(this.transform), presetPos, 0.02f, 0.02f);
                    GizmoExtension.DrawArrow(presetPos, endPos, 0.02f, 0.02f);
                    Handles.Label((endPos + presetPos)/2, "End : " + endAndMulcon.end.name + ", MultiLeverPreset : " + presetIndex + " , " + endAndMulcon.preset.name, guiStyle);
                    presetIndex++;
                }
                switchIndex++;
            }
        }
#endif
    }
}
