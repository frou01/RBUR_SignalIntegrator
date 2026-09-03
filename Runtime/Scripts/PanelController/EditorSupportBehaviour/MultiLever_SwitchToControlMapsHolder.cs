using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
        [SerializeField] List<To_ControllerMap> Switch_To_ControlMaps;
        public int[][] get_mulCon_to_Con_Map()
        {
            return Switch_To_ControlMaps
                .Select(val => val.Switch_To_Control)//List<To_ControllerMap> -> List<int[]>
                .ToArray();
        }

        [SerializeField] int switchPositionNum;

        void Update()
        {
            MultiLeverController TargetMulCon;
            TargetMulCon = GetComponent<MultiLeverController>();
            Dictionary<AbstractPanelController, To_ControllerMap> synced_controller_controlPositions_Map = new Dictionary<AbstractPanelController, To_ControllerMap> ();
            foreach (To_ControllerMap syncedControllerMap in Switch_To_ControlMaps)
            {
                if(syncedControllerMap.linkedController != null && !synced_controller_controlPositions_Map.ContainsKey(syncedControllerMap.linkedController)) 
                    synced_controller_controlPositions_Map.Add(syncedControllerMap.linkedController, syncedControllerMap);
            }


            List<To_ControllerMap> SyncingMulCon_To_ControllerMap = new List<To_ControllerMap>();
            bool isDirty = Switch_To_ControlMaps.Count > TargetMulCon.controlledLevers.Length;
            int idx = 0;
            foreach (AbstractPanelController assignedController in TargetMulCon.controlledLevers)
            {
                To_ControllerMap To_ControllerMap;
                if (synced_controller_controlPositions_Map.TryGetValue(assignedController, out To_ControllerMap))
                {
                    To_ControllerMap.onMulConOrder = idx;
                    if (To_ControllerMap.Switch_To_Control.Length > switchPositionNum)
                    {
                        To_ControllerMap.Switch_To_Control = To_ControllerMap.Switch_To_Control
                            .Select((mapArray, i) => (toConMap: mapArray, i))//get index
                            .Where(val => val.i < switchPositionNum)//cut index
                            .Select(val => val.toConMap).ToArray();
                        isDirty |= true;
                    }
                    else if (To_ControllerMap.Switch_To_Control.Length < switchPositionNum)
                    {
                        To_ControllerMap.Switch_To_Control = To_ControllerMap.Switch_To_Control.AddRangeToArray(new int[switchPositionNum - To_ControllerMap.Switch_To_Control.Length]);
                        isDirty |= true;
                    }
                    SyncingMulCon_To_ControllerMap.Add(To_ControllerMap);
                }
                else
                {
                    if (assignedController != null)
                    {
                        SyncingMulCon_To_ControllerMap.Add(new To_ControllerMap(idx, assignedController, new int[switchPositionNum]));
                        isDirty |= true;
                    }
                }
                idx++;
            }
            if (isDirty)
            {
                SyncingMulCon_To_ControllerMap.OrderBy(toConMap => toConMap.onMulConOrder);
                Switch_To_ControlMaps = SyncingMulCon_To_ControllerMap;
                EditorUtility.SetDirty(this);
            }
        }

        [System.Serializable]
        class To_ControllerMap
        {
            [SerializeField] public int onMulConOrder;
            [SerializeField] public AbstractPanelController linkedController;
            [Tooltip("index = switch, value = contro. No overwrite lever value = -1")][SerializeField] public int[] Switch_To_Control;

            public To_ControllerMap(int onMulConOrder, AbstractPanelController linkedController, int[] MulConSwitch_To_ControllerControlMap)
            {
                this.onMulConOrder = onMulConOrder;
                this.linkedController = linkedController;
                this.Switch_To_Control = MulConSwitch_To_ControllerControlMap;
            }
        }
#endif
    }
}
