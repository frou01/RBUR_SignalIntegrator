using frou01.util;
using HarmonyLib;
using NUnit.Compatibility;
using RBUR_SignalIntegrator;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;
using VRC.Udon;

namespace RBUR_SignalIntegrator_Editor
{
    public class Interlock_GACLink_BuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => int.MinValue;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("InterlockLink Start Process");
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                //TODO GACからInterlockへイベントを配送するためPickUpEventLinker/SyncEventLinkerを自動設定する

                foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    UdonBehaviour interlockUdon = null;
                    foreach(UdonBehaviour udonComponent in interlocking.GetComponents<UdonBehaviour>())
                    {
                        //if(udonComponent.programSource is UdonSharpProgramAsset)
                        //{
                        //    Debug.Log("find USharp_UdonComponent", udonComponent);
                        //    Debug.Log("sourceCsScript is " + ((UdonSharpProgramAsset)udonComponent.programSource).sourceCsScript.GetClass(), udonComponent);
                        //    if (typeof(Interlocking).IsAssignableFrom(((UdonSharpProgramAsset)udonComponent.programSource).sourceCsScript.GetClass()))
                        //    {
                        //        Debug.Log("find USharp_UdonComponent", udonComponent);
                        //        interlockUdon = udonComponent;
                        //    }
                        //}
                        if(UdonSharpEditorUtility.GetProxyBehaviour(udonComponent) == interlocking)
                        {
                            interlockUdon = udonComponent;
                        }
                    }

                    bool failed = false;
                    if(interlocking.GetTargetSignalLocker() is GACLockerConsolidater)
                    {
                        VRCPickup Controller = ((GACLockerConsolidater)interlocking.GetTargetSignalLocker()).GetComponentInChildren<VRCPickup>();
                        if (Controller)
                        {
                            SyncEventLinker syncEventLinker = Controller.GetComponent<SyncEventLinker>();
                            PickUpEventLinker pickUpEventLinker = Controller.GetComponent<PickUpEventLinker>();
                            if (!syncEventLinker)
                            {
                                syncEventLinker = Controller.gameObject.AddUdonSharpComponent<SyncEventLinker>();
                            }
                            if (!pickUpEventLinker)
                            {
                                Debug.LogError("PickUpEventLinker not found", Controller.gameObject);
                                failed = true;
                            }
                            syncEventLinker.targets = syncEventLinker.targets.AddItem(interlockUdon).ToArray();
                            pickUpEventLinker.targets = pickUpEventLinker.targets.AddItem(interlockUdon).ToArray();
                        }
                    }
                    if (failed)
                    {
                        throw new BuildFailedException("Add PickUpEventLinker to interlocked Controller");
                    }
                }
            }
            Debug.Log("InterlockLink Process End");

        }
    }
}