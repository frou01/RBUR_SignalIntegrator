using RBUR_SignalIntegrator;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static RBUR_SignalIntegrator_Editor.BuildProcessUtility;

namespace RBUR_SignalIntegrator_Editor
{
    /// <summary>
    /// Locker への登録、Interlocking との親子参照・affectedLockers の構築と、設定の検証
    /// Pannel / GACLink で RouteLocker.Lockers が解決済みであることが前提
    /// </summary>
    public class Interlock_PostBuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => -99;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("InterlockLink Start Process");
            //Lockerへの登録順を変えないよう、ルートオブジェクト単位で処理する
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                RegisterToLockers(obj);
                foreach (Interlocking currentInterlock in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    SetParentInterlock(currentInterlock);
                    currentInterlock.affectedLockers = GetAffectedLockers(currentInterlock);
                }
            }

            List<Interlocking> interlocks = FindComponents<Interlocking>(scene);
            Dictionary<AbstractLockerConsolidater, Interlocking> interlock_From_Lockers = CollectFromLockers(interlocks);
            WarnMutualLocking(interlocks, interlock_From_Lockers);
            Debug.Log("InterlockLink Process End");
        }

        private static void RegisterToLockers(GameObject obj)
        {
            foreach (Interlocking interlocking in obj.GetComponentsInChildren<Interlocking>(true))
            {
                interlocking.SetupLocker();
            }
            foreach (LockerAndMeetPosition stateLinker in obj.GetComponentsInChildren<LockerAndMeetPosition>(true))
            {
                stateLinker.SetupLocker();
            }
            foreach (RouteLocker routeLocker in obj.GetComponentsInChildren<RouteLocker>(true))
            {
                routeLocker.SetupLocker();
            }
        }

        /// <summary>
        /// stateLinker, routeLocker から Interlocking への参照を張る
        /// </summary>
        private static void SetParentInterlock(Interlocking currentInterlock)
        {
            if (currentInterlock.GetRouteLocker() != null) currentInterlock.GetRouteLocker().setParentInterlock(currentInterlock);
            foreach (LockerAndMeetPosition stateLinker in currentInterlock.GetInterlockStateLinker())
            {
                stateLinker.setParentInterlock(currentInterlock);
            }
        }

        /// <summary>
        /// Interlocking に影響を受ける全ての Locker。更新終了検知で使う
        /// </summary>
        private static AbstractLockerConsolidater[] GetAffectedLockers(Interlocking currentInterlock)
        {
            List<AbstractLockerConsolidater> controllers = new List<AbstractLockerConsolidater>();
            foreach (LockerAndMeetPosition StateLocker in currentInterlock.GetInterlockStateLinker())
            {
                controllers.Add(StateLocker.getLocker());
            }
            if (currentInterlock.GetRouteLocker() != null) controllers.AddRange(currentInterlock.GetRouteLocker().Lockers);
            controllers.Add(currentInterlock.GetFromLocker());
            return controllers.Distinct().ToArray();
        }

        /// <summary>
        /// From_Locker -> Interlocking の対応表。同じ From_Locker を持つ Interlocking が複数あればエラーを出す
        /// </summary>
        private static Dictionary<AbstractLockerConsolidater, Interlocking> CollectFromLockers(List<Interlocking> interlocks)
        {
            Dictionary<AbstractLockerConsolidater, Interlocking> interlock_From_Lockers = new Dictionary<AbstractLockerConsolidater, Interlocking>();
            foreach (Interlocking currentInterlock in interlocks)
            {
                if (!currentInterlock.GetFromLocker()) continue;
                if (interlock_From_Lockers.ContainsKey(currentInterlock.GetFromLocker()))
                {
                    Debug.LogError("interlock duplicated. Controller: " + currentInterlock.GetFromLocker().name, currentInterlock.GetFromLocker());
                    Debug.LogError("interlock duplicated, Interlock:  " + currentInterlock.name, currentInterlock);
                }
                else
                {
                    interlock_From_Lockers.Add(currentInterlock.GetFromLocker(), currentInterlock);
                }
            }
            return interlock_From_Lockers;
        }

        /// <summary>
        /// 2つの Interlocking が互いの From_Locker を鎖錠し合っていれば警告する
        /// </summary>
        private static void WarnMutualLocking(List<Interlocking> interlocks, Dictionary<AbstractLockerConsolidater, Interlocking> interlock_From_Lockers)
        {
            foreach (Interlocking interlockA in interlocks)
            {
                foreach (AbstractLockerConsolidater lockerAffectedFromA in interlockA.affectedLockers)
                {
                    Interlocking interlockB;
                    if (lockerAffectedFromA == null || !interlock_From_Lockers.TryGetValue(lockerAffectedFromA, out interlockB)) continue;
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
}
