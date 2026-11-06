using frou01.RigidBodyTrain;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

#endif
using UnityEngine;

namespace frou01.RBUR.SignalIntegrator
{
    [ExecuteAlways]
    public class PointLeverControlToRouteIndexHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] public List<ControlToRouteIndexMap> control_To_RouteIndexMaps = new List<ControlToRouteIndexMap>();

        public void ApplyToUdonBehaviour()
        {
            PointController pointController = GetComponent<PointController>();
            if (pointController == null)
            {
                Debug.LogError("PointController not found on the same GameObject.", this);
                return;
            }
            pointController.SetControlToRouteIndexMap(get_Control_to_RouteIndex_Map());
            pointController.setPointInstances(get_assignedPointArray());
        }
        public int[][] get_Control_to_RouteIndex_Map()
        {
            return control_To_RouteIndexMaps
                .Select(val => val.Control_To_RouteIndex)//List<ToControllerMap> -> List<int[]>
                .ToArray();
        }

        public AbstractPointSetter[] get_assignedPointArray()
        {
            return control_To_RouteIndexMaps.Select(val => val.linkedPoint).ToArray();
        }

        [SerializeField] public int RouteIndexNum;

        public void Update()
        {
            bool isDirty = false;

            foreach (ControlToRouteIndexMap map in control_To_RouteIndexMaps)
            {
                if (map.Control_To_RouteIndex.Length != RouteIndexNum)
                {
                    int[] newArray = new int[RouteIndexNum];
                    for (int i = 0; i < Mathf.Min(map.Control_To_RouteIndex.Length, RouteIndexNum); i++)
                    {
                        newArray[i] = map.Control_To_RouteIndex[i];
                    }
                    map.Control_To_RouteIndex = newArray;
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                EditorUtility.SetDirty(this);
            }
        }
        [System.Serializable]
        public class ControlToRouteIndexMap
        {
            [SerializeField] public AbstractPointSetter linkedPoint;
            [SerializeField] public int[] Control_To_RouteIndex;

            public ControlToRouteIndexMap(AbstractPointSetter linkedPoint, int[] controlToRouteIndex)
            {
                this.linkedPoint = linkedPoint;
                this.Control_To_RouteIndex = controlToRouteIndex;
            }
        }
#endif
    }
}
