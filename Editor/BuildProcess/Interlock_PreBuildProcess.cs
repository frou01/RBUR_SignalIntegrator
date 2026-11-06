using frou01.RBUR.SignalIntegrator;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static frou01.RBUR.SignalIntegrator.Editor.BuildProcessUtility;

namespace frou01.RBUR.SignalIntegrator.Editor
{
    /// <summary>
    /// 各Udonへの EventStackHolder の配布と、RouteLocker.Lockers 配列の確保
    /// </summary>
    public class Interlock_PreBuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => -200;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("Interlock Start PreProcess");
            EventStackHolder eventStackHolder = FindEventStackHolder(scene);
            if (eventStackHolder) AssignEventStackHolder(scene, eventStackHolder);
            AllocateRouteLockerLockers(scene);
            Debug.Log("Interlock End PreProcess");
        }

        private static EventStackHolder FindEventStackHolder(Scene scene)
        {
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                EventStackHolder eventStackHolder = obj.GetComponentInChildren<EventStackHolder>(true);
                if (eventStackHolder) return eventStackHolder;
            }
            return null;
        }

        private static void AssignEventStackHolder(Scene scene, EventStackHolder eventStackHolder)
        {
            foreach (SignalEvaluator con in FindComponents<SignalEvaluator>(scene, false))
            {
                con.eventStackHolder = eventStackHolder;
            }
            foreach (MultiLeverController con in FindComponents<MultiLeverController>(scene, false))
            {
                con.eventStackHolder = eventStackHolder;
            }
            foreach (RouteLocker con in FindComponents<RouteLocker>(scene, false))
            {
                con.eventStackHolder = eventStackHolder;
            }
            foreach (LockerAndMeetPosition con in FindComponents<LockerAndMeetPosition>(scene, false))
            {
                con.eventStackHolder = eventStackHolder;
            }
            foreach (Interlocking con in FindComponents<Interlocking>(scene, false))
            {
                con.eventStackHolder = eventStackHolder;
            }
            foreach (AbstractLockerConsolidater con in FindComponents<AbstractLockerConsolidater>(scene, false))
            {
                con.eventStackHolder = eventStackHolder;
            }
        }

        /// <summary>
        /// 後段(Pannel / GACLink)で Lockers[idx] に代入するため、TargetPoints と同じ長さの配列を確保する
        /// </summary>
        private static void AllocateRouteLockerLockers(Scene scene)
        {
            //Pannel / GACLink は非アクティブな RouteLocker も処理するので、ここでも含める
            foreach (RouteLocker routeLocker in FindComponents<RouteLocker>(scene))
            {
                int pointCount = routeLocker.TargetPoints == null ? 0 : routeLocker.TargetPoints.Length;
                if (routeLocker.Lockers == null || routeLocker.Lockers.Length != pointCount)
                {
                    routeLocker.Lockers = new AbstractLockerConsolidater[pointCount];
                }
            }
        }
    }
}
