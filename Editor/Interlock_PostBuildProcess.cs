
using frou01.util;
using HarmonyLib;
using RBUR_SignalIntegrator;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RBUR_SignalIntegrator_Editor
{
    public class Interlock_PostBuildProcess : IProcessSceneWithReport
    {
        //public class Interlock_BuildPreProcess : IProcessSceneWithReport
        //{
        //    public int callbackOrder => 0;

        //    public void OnProcessScene(Scene scene, BuildReport report)
        //    {

        //    }

        //}
        public int callbackOrder => -99;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("InterlockLink Start Process");
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (RouteChecker routeChecker in obj.GetComponentsInChildren<RouteChecker>(true))
                {
                    routeChecker.SetupCallback();
                }

                foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    interlocking.SetupLocker();
                }
                foreach (Interlock_ToLockerAndMeetPosition stateLinker in obj.GetComponentsInChildren<Interlock_ToLockerAndMeetPosition>(true))
                {
                    stateLinker.SetupLocker();
                }
                foreach (RouteLocker routeLocker in obj.GetComponentsInChildren<RouteLocker>(true))
                {
                    routeLocker.SetupLocker();
                }
                // stateLinker,routeLockerからInterlockingへの参照を張る
                // Interlockに影響を受ける全てのコントローラーを探索しておく。更新終了検知で使う。
                foreach (Interlocking currentInterlock in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    if(currentInterlock.GetRouteLocker() != null) currentInterlock.GetRouteLocker().setParentInterlock(currentInterlock);
                    foreach (Interlock_ToLockerAndMeetPosition stateLinker in currentInterlock.GetInterlockStateLinker())
                    {
                        stateLinker.setParentInterlock(currentInterlock);
                    }

                    List<AbstractLockerConsolidater> controllers = new List<AbstractLockerConsolidater>();
                    foreach (Interlock_ToLockerAndMeetPosition StateLocker in currentInterlock.GetInterlockStateLinker())
                    {
                        controllers.Add(StateLocker.getLocker());
                    }
                    if (currentInterlock.GetRouteLocker() != null) foreach (AbstractLockerConsolidater locker in currentInterlock.GetRouteLocker().Locker_GTST)
                    {
                        controllers.Add(locker);
                    }
                    controllers.Add(currentInterlock.GetFromLocker());
                    currentInterlock.affectedLockers = currentInterlock.affectedLockers.AddRangeToArray(controllers.ToArray());
                }
            }

            Dictionary<AbstractLockerConsolidater, Interlocking> interlock_From_Lockers = new Dictionary<AbstractLockerConsolidater, Interlocking>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (Interlocking currentInterlock in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    if(currentInterlock.GetFromLocker()) interlock_From_Lockers.Add(currentInterlock.GetFromLocker(), currentInterlock);
                }
            }

            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (Interlocking interlockA in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    foreach (AbstractLockerConsolidater lockerAffectedFromA in interlockA.affectedLockers)
                    {
                        Interlocking interlockB;
                        if (lockerAffectedFromA != null && interlock_From_Lockers.ContainsKey(lockerAffectedFromA) && interlock_From_Lockers.TryGetValue(lockerAffectedFromA, out interlockB))
                        {
                            if (interlockB != interlockA && interlockB.affectedLockers.Contains(interlockA.GetFromLocker()))
                            {
                                Debug.LogWarning("AnController " + interlockA.GetFromLocker().name + " is Locking / Locked by " + interlockB.GetFromLocker().name, interlockA);
                                Debug.LogWarning("This may cause inconsistent sync result.", interlockB);
                                Debug.LogWarning("Remove " + interlockA.GetFromLocker().name + " or " + interlockB.GetFromLocker().name + " from To Locker_States");
                            }
                        }
                    }
                }
            }
            Debug.Log("InterlockLink Process End");

        }

    }
}
