using frou01.RBUR.SignalIntegrator;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static frou01.util.editor.BuildProcessUtility;

namespace frou01.RBUR.SignalIntegrator.Editor
{
    /// <summary>
    /// Locker への登録、Interlocking との親子参照・affectedLockers の構築と、設定の検証
    /// Panel / GACLink で RouteLocker.Lockers が解決済みであることが前提
    /// </summary>
    public class InterlockPostBuildProcess : IProcessSceneWithReport
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
            Dictionary<AbstractLockerConsolidator, Interlocking> interlockByFromLocker = CollectFromLockers(interlocks);
            WarnMutualLocking(interlocks, interlockByFromLocker);
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
            if (currentInterlock.GetRouteLocker() != null) currentInterlock.GetRouteLocker().SetParentInterlock(currentInterlock);
            foreach (LockerAndMeetPosition stateLinker in currentInterlock.GetInterlockStateLinker())
            {
                stateLinker.SetParentInterlock(currentInterlock);
            }
        }

        /// <summary>
        /// Interlocking に影響を受ける全ての Locker。更新終了検知で使う
        /// </summary>
        private static AbstractLockerConsolidator[] GetAffectedLockers(Interlocking currentInterlock)
        {
            List<AbstractLockerConsolidator> controllers = new List<AbstractLockerConsolidator>();
            foreach (LockerAndMeetPosition stateLinker in currentInterlock.GetInterlockStateLinker())
            {
                controllers.Add(stateLinker.GetLocker());
            }
            if (currentInterlock.GetRouteLocker() != null) controllers.AddRange(currentInterlock.GetRouteLocker().Lockers);
            controllers.Add(currentInterlock.GetFromLocker());
            return controllers.Distinct().ToArray();
        }

        /// <summary>
        /// fromLocker -> Interlocking の対応表。同じ fromLocker を持つ Interlocking が複数あればエラーを出す
        /// </summary>
        private static Dictionary<AbstractLockerConsolidator, Interlocking> CollectFromLockers(List<Interlocking> interlocks)
        {
            Dictionary<AbstractLockerConsolidator, Interlocking> interlockByFromLocker = new Dictionary<AbstractLockerConsolidator, Interlocking>();
            foreach (Interlocking currentInterlock in interlocks)
            {
                if (!currentInterlock.GetFromLocker()) continue;
                if (interlockByFromLocker.ContainsKey(currentInterlock.GetFromLocker()))
                {
                    Debug.LogError("interlock duplicated. Controller: " + currentInterlock.GetFromLocker().name, currentInterlock.GetFromLocker());
                    Debug.LogError("interlock duplicated, Interlock:  " + currentInterlock.name, currentInterlock);
                }
                else
                {
                    interlockByFromLocker.Add(currentInterlock.GetFromLocker(), currentInterlock);
                }
            }
            return interlockByFromLocker;
        }

        /// <summary>
        /// 2つの Interlocking が互いの fromLocker を鎖錠し合っていれば警告する
        /// </summary>
        private static void WarnMutualLocking(List<Interlocking> interlocks, Dictionary<AbstractLockerConsolidator, Interlocking> interlockByFromLocker)
        {
            foreach (Interlocking interlockA in interlocks)
            {
                foreach (AbstractLockerConsolidator lockerAffectedFromA in interlockA.affectedLockers)
                {
                    Interlocking interlockB;
                    if (lockerAffectedFromA == null || !interlockByFromLocker.TryGetValue(lockerAffectedFromA, out interlockB)) continue;
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
