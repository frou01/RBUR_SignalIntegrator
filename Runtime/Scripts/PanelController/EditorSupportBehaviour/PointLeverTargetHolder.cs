using frou01.RigidBodyTrain;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
#endif
using UnityEngine;

namespace RBUR_SignalIntegrator
{
    [ExecuteAlways]
    public class PointLeverTargetHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] List<Control_To_RouteIndexMap> control_To_RouteIndexMaps;
        public int[][] get_Control_to_RouteIndex_Map()
        {
            return control_To_RouteIndexMaps
                .Select(val => val.Control_To_RouteIndex)//List<To_ControllerMap> -> List<int[]>
                .ToArray();
        }

        [SerializeField] int RouteIndexNum;

        void Update()
        {
            PointControllerLever TargetPointControllerLever;
            TargetPointControllerLever = GetComponent<PointControllerLever>();
            Dictionary<AbstractPointSetter, Control_To_RouteIndexMap> synced_controller_controlPositions_Map = new Dictionary<AbstractPointSetter, Control_To_RouteIndexMap>();
            foreach (Control_To_RouteIndexMap syncedControllerMap in control_To_RouteIndexMaps)
            {
                if (syncedControllerMap.linkedPoint != null && !synced_controller_controlPositions_Map.ContainsKey(syncedControllerMap.linkedPoint))
                    synced_controller_controlPositions_Map.Add(syncedControllerMap.linkedPoint, syncedControllerMap);
            }


            List<Control_To_RouteIndexMap> SyncingMulCon_Control_To_RouteIndexMap = new List<Control_To_RouteIndexMap>();
            int idx = 0;
            foreach (AbstractPointSetter assignedController in TargetPointControllerLever.getPointInstances())
            {
                Control_To_RouteIndexMap Control_To_RouteIndexMap;
                if (assignedController)
                {
                    if (synced_controller_controlPositions_Map.TryGetValue(assignedController, out Control_To_RouteIndexMap))
                    {
                        Control_To_RouteIndexMap.onControllerOrder = idx;
                        if (Control_To_RouteIndexMap.Control_To_RouteIndex.Length > RouteIndexNum)
                        {
                            Control_To_RouteIndexMap.Control_To_RouteIndex = Control_To_RouteIndexMap.Control_To_RouteIndex
                                .Select((mapArray, i) => (toConMap: mapArray, i))//get index
                                .Where(val => val.i < RouteIndexNum)//cut index
                                .Select(val => val.toConMap).ToArray();
                        }
                        else if (Control_To_RouteIndexMap.Control_To_RouteIndex.Length < RouteIndexNum)
                        {
                            Control_To_RouteIndexMap.Control_To_RouteIndex = Control_To_RouteIndexMap.Control_To_RouteIndex.AddRangeToArray(new int[RouteIndexNum - Control_To_RouteIndexMap.Control_To_RouteIndex.Length]);
                        }
                        SyncingMulCon_Control_To_RouteIndexMap.Add(Control_To_RouteIndexMap);
                    }
                    else
                    {
                        if (assignedController != null) SyncingMulCon_Control_To_RouteIndexMap.Add(new Control_To_RouteIndexMap(idx, assignedController, new int[RouteIndexNum]));
                    }
                }
                idx++;
            }
            SyncingMulCon_Control_To_RouteIndexMap.OrderBy(toConMap => toConMap.onControllerOrder);
            control_To_RouteIndexMaps.Clear();
            control_To_RouteIndexMaps.AddRange(SyncingMulCon_Control_To_RouteIndexMap);
        }
        [System.Serializable]
        class Control_To_RouteIndexMap
        {
            [SerializeField] public int onControllerOrder;
            [SerializeField] public AbstractPointSetter linkedPoint;
            [SerializeField] public int[] Control_To_RouteIndex;

            public Control_To_RouteIndexMap(int onControllerOrder, AbstractPointSetter linkedPoint, int[] Control_To_RouteIndexMap)
            {
                this.onControllerOrder = onControllerOrder;
                this.linkedPoint = linkedPoint;
                this.Control_To_RouteIndex = Control_To_RouteIndexMap;
            }
        }
#endif
    }
}
