using frou01.RigidBodyTrain;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace frou01.RBUR.SignalIntegrator
{
    [ExecuteAlways]
    public class PointLeverControlToRouteIndexHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [FormerlySerializedAs("control_To_RouteIndexMaps")][SerializeField] public List<ControlToRouteIndexMap> controlToRouteIndexMaps = new List<ControlToRouteIndexMap>();

        public void ApplyToUdonBehaviour()
        {
            PointController pointController = GetComponent<PointController>();
            if (pointController == null)
            {
                Debug.LogError("PointController not found on the same GameObject.", this);
                return;
            }
            pointController.SetControlToRouteIndexMap(GetControlToRouteIndexMap());
            pointController.SetPointInstances(GetAssignedPoints());
        }
        public int[][] GetControlToRouteIndexMap()
        {
            return controlToRouteIndexMaps
                .Select(val => val.controlToRouteIndex)//List<ToControllerMap> -> List<int[]>
                .ToArray();
        }

        public AbstractPointSetter[] GetAssignedPoints()
        {
            return controlToRouteIndexMaps.Select(val => val.linkedPoint).ToArray();
        }

        [FormerlySerializedAs("RouteIndexNum")][SerializeField] public int routeIndexNum;

        public void Update()
        {
            bool isDirty = false;

            foreach (ControlToRouteIndexMap map in controlToRouteIndexMaps)
            {
                if (map.controlToRouteIndex.Length != routeIndexNum)
                {
                    int[] newArray = new int[routeIndexNum];
                    for (int i = 0; i < Mathf.Min(map.controlToRouteIndex.Length, routeIndexNum); i++)
                    {
                        newArray[i] = map.controlToRouteIndex[i];
                    }
                    map.controlToRouteIndex = newArray;
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
            [FormerlySerializedAs("Control_To_RouteIndex")][SerializeField] public int[] controlToRouteIndex;

            public ControlToRouteIndexMap(AbstractPointSetter linkedPoint, int[] controlToRouteIndex)
            {
                this.linkedPoint = linkedPoint;
                this.controlToRouteIndex = controlToRouteIndex;
            }
        }
#endif
    }
}
