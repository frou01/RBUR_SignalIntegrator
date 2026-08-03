using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace RBUR_SignalIntegrator
{
    [ExecuteAlways]
    public class MultiLeverMappingHolder : MonoBehaviour
    {
        [SerializeField] List<To_ControllerMap> mulCon_To_ControllerMap;
        [SerializeField] int switchPositionNum;

        void Update()
        {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
            MultiLeverController TargetMulCon;
            TargetMulCon = GetComponent<MultiLeverController>();
            Dictionary<AbstractPanelController, To_ControllerMap> synced_controller_controlPositions_Map = new Dictionary<AbstractPanelController, To_ControllerMap> ();
            foreach (To_ControllerMap syncedControllerMap in mulCon_To_ControllerMap)
            {
                if(syncedControllerMap.linkedController != null && !synced_controller_controlPositions_Map.ContainsKey(syncedControllerMap.linkedController)) 
                    synced_controller_controlPositions_Map.Add(syncedControllerMap.linkedController, syncedControllerMap);
            }


            List<To_ControllerMap> SyncingMulCon_To_ControllerMap = new List<To_ControllerMap>();
            int idx = 0;
            foreach (AbstractPanelController assignedController in TargetMulCon.controlledLevers)
            {
                To_ControllerMap To_ControllerMap;
                if (synced_controller_controlPositions_Map.TryGetValue(assignedController, out To_ControllerMap))
                {
                    To_ControllerMap.onMulConOrder = idx;
                    if (To_ControllerMap.Switch_To_ControlMap.Length > switchPositionNum)
                    {
                        To_ControllerMap.Switch_To_ControlMap = To_ControllerMap.Switch_To_ControlMap
                            .Select((mapArray, i) => (toConMap: mapArray, i))//get index
                            .Where(val => val.i < switchPositionNum)//cut index
                            .Select(val => val.toConMap).ToArray();
                    }
                    else if (To_ControllerMap.Switch_To_ControlMap.Length < switchPositionNum)
                    {
                        To_ControllerMap.Switch_To_ControlMap = To_ControllerMap.Switch_To_ControlMap.AddRangeToArray(new int[switchPositionNum - To_ControllerMap.Switch_To_ControlMap.Length]);
                    }
                    SyncingMulCon_To_ControllerMap.Add(To_ControllerMap);
                }
                else
                {
                    if(assignedController != null) SyncingMulCon_To_ControllerMap.Add(new To_ControllerMap(idx,assignedController, new int[switchPositionNum]));
                }
                idx++;
            }
            SyncingMulCon_To_ControllerMap.OrderBy(toConMap => toConMap.onMulConOrder);
            mulCon_To_ControllerMap.Clear();
            mulCon_To_ControllerMap.AddRange(SyncingMulCon_To_ControllerMap);
#endif
        }

        [System.Serializable]
        class To_ControllerMap
        {
            [SerializeField] public int onMulConOrder;
            [SerializeField] public AbstractPanelController linkedController;
            [SerializeField] public int[] Switch_To_ControlMap;

            public To_ControllerMap(int onMulConOrder, AbstractPanelController linkedController, int[] MulConSwitch_To_ControllerControlMap)
            {
                this.onMulConOrder = onMulConOrder;
                this.linkedController = linkedController;
                this.Switch_To_ControlMap = MulConSwitch_To_ControllerControlMap;
            }
        }
    }
}
