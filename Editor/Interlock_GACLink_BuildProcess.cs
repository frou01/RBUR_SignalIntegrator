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
        public int callbackOrder => -100;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("InterlockLink GAC Start Process");
            List<Controller_Base> controllers = new List<Controller_Base>();
            List<GACLockerConsolidater> gacLockers = new List<GACLockerConsolidater>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                controllers.AddRange(obj.GetComponentsInChildren<Controller_Base>(true));
            }
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                gacLockers.AddRange(obj.GetComponentsInChildren<GACLockerConsolidater>(true));
            }
            bool failed = false;
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (RouteLocker routeLocker in obj.GetComponentsInChildren<RouteLocker>(true))
                {
                    int idx = -1;
                    foreach (AbstractPointSetter points in routeLocker.GetTargetPoints())
                    {
                        //ポイントのAnimatorを制御しているController_Baseを抽出し、
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

                        GACLockerConsolidater pointLocker = null;
                        foreach (GACLockerConsolidater checkingLockers in gacLockers)
                        {
                            if (pointController == checkingLockers.getGACController())
                            {
                                pointLocker = checkingLockers;
                                break;
                            }
                        }
                        if(pointLocker == null)
                        {
                            Debug.LogError("Correspond GACLockerConsolidater not found", pointController);
                        }
                        else
                        {
                            routeLocker.Locker_GTST[idx] = pointLocker;
                        }
                    }
                }
                foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    //Setup EventLinkers
                    UdonBehaviour interlockUdon = UdonSharpEditorUtility.GetBackingUdonBehaviour(interlocking);
                    List<GACLockerConsolidater> lockers = new List<GACLockerConsolidater>();//関係するLockerについてGACLockerConsolidaterだけを抽出する
                    if (interlocking.GetFromLocker() is GACLockerConsolidater)
                    {
                        lockers.Add((GACLockerConsolidater)interlocking.GetFromLocker());
                    }
                    if(interlocking.GetRouteLocker()) foreach (AbstractLockerConsolidater locker in interlocking.GetRouteLocker().Locker_GTST)
                    {
                        if (locker is GACLockerConsolidater)
                        {
                            lockers.Add((GACLockerConsolidater)locker);
                        }
                    }
                    foreach (Interlock_ToLockerAndMeetPosition StateLocker in interlocking.GetInterlockStateLinker())
                    {
                        if (StateLocker.getLocker() is GACLockerConsolidater)
                        {
                            lockers.Add((GACLockerConsolidater)StateLocker.getLocker());
                        }
                    }
                    foreach (GACLockerConsolidater locker in lockers)
                    {
                        VRCPickup Controller = locker.GetComponentInChildren<VRCPickup>();
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
                                continue;
                            }
                            syncEventLinker.targets = syncEventLinker.targets.AddItem(interlockUdon).ToArray();
                            pickUpEventLinker.targets = pickUpEventLinker.targets.AddItem(interlockUdon).ToArray();
                        }
                    }
                }
            }
            if (failed)
            {
                throw new BuildFailedException("Add PickUpEventLinker to interlocked Controller");
            }
            Debug.Log("InterlockLink GAC Process End");

        }
    }
}