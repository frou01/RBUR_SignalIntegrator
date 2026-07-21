using frou01.GrabController;
using frou01.RigidBodyTrain;
using frou01.util;
using HarmonyLib;
//using NUnit.Compatibility;
using RBUR_SignalIntegrator;
using System.Collections.Generic;
using System.Linq;
//using UdonSharp;
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
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("InterlockLink Start Process");
            List<Controller_Base> controllers = new List<Controller_Base>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                controllers.AddRange(obj.GetComponentsInChildren<Controller_Base>(true));
            }
            bool failed = false;
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
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
                }
                foreach (RouteLocker routeLocker in obj.GetComponentsInChildren<RouteLocker>(true))
                {
                    int idx = -1;
                    if(routeLocker.Locker_GTST == null || routeLocker.Locker_GTST.Length == 0)
                    {
                        routeLocker.Locker_GTST = new AbstractLockerConsolidater[routeLocker.GetTargetPoints().Length];
                    }
                    foreach (AbstractPointSetter points in routeLocker.GetTargetPoints())
                    {
                        idx++;
                        Animator pointAnimator = points.GetComponent<Animator>();
                        if (!pointAnimator) continue;

                        Controller_Base pointController = null;
                        foreach(Controller_Base checkingController in controllers)
                        {
                            if(pointAnimator == checkingController.TargetAnimator || checkingController.MultiTargetAnimators.Contains(pointAnimator))
                            {
                                pointController = checkingController;
                                break;
                            }
                        }
                        if (!pointController) continue;
                        AbstractLockerConsolidater locker = pointController.GetComponentInParent<AbstractLockerConsolidater>();
                        if(locker == null)
                        {
                            Debug.LogError("PickUpEventLinker not found", pointController);
                        }
                        else
                        {
                            routeLocker.Locker_GTST[idx] = locker;
                        }
                    }
                }
                if (failed)
                {
                    throw new BuildFailedException("Add PickUpEventLinker to interlocked Controller");
                }
            }
            Debug.Log("InterlockLink Process End");

        }
    }
}