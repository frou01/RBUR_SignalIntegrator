using frou01.RigidBodyTrain;
using RBUR_SignalIntegrator;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RBUR_SignalIntegrator_Editor
{
    [CustomEditor(typeof(RouteLocker))]
    [CanEditMultipleObjects]
    public class RouteLocker_Editor : Editor
    {
        SerializedProperty targetPointsProperty;
        SerializedProperty targetRouteProperty;
        SerializedProperty routeRailsProperty;

        void OnEnable()
        {
            targetPointsProperty = serializedObject.FindProperty("m_TargetPoints");
            targetRouteProperty = serializedObject.FindProperty("m_TargetRoute");
            routeRailsProperty = serializedObject.FindProperty("Route_Rails");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(targetPointsProperty);
            EditorGUILayout.PropertyField(targetRouteProperty);
            EditorGUILayout.PropertyField(routeRailsProperty);
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("AutoSetup-Route"))
            {
                AutoSetupRoutes(targets.OfType<RouteLocker>().ToArray());
            }
        }

        /// <summary>
        /// 選択中の全ての RouteLocker に AutoSetup-Route を行う。Undo は1回分にまとめる
        /// </summary>
        private static void AutoSetupRoutes(RouteLocker[] routeLockers)
        {
            Undo.SetCurrentGroupName("AutoSetup-Route");
            int undoGroup = Undo.GetCurrentGroup();
            AbstractPointSetter[] allPoints = GetAllPoints();
            int succeeded = 0;
            foreach (RouteLocker routeLocker in routeLockers)
            {
                if (AutoSetupRoute(routeLocker, allPoints)) succeeded++;
            }
            Undo.CollapseUndoOperations(undoGroup);
            if (routeLockers.Length > 1) Debug.Log($"[RouteLocker] auto setup : {succeeded} / {routeLockers.Length} succeeded");
        }

        /// <summary>
        /// Route_Rails を辿って TargetPoints / TargetRoute を設定する
        /// 複数編集時に共通の serializedObject へ書くと全員に同じ値が入るので、個別の SerializedObject を使う
        /// </summary>
        private static bool AutoSetupRoute(RouteLocker routeLocker, AbstractPointSetter[] allPoints)
        {
            List<AbstractPointSetter> points;
            List<int> routes;
            if (!SearchRoute(routeLocker.Route_Rails, allPoints, out points, out routes))
            {
                Debug.LogWarning($"[RouteLocker] {routeLocker.name} : auto setup failed", routeLocker);
                return false;
            }
            SerializedObject routeLockerObject = new SerializedObject(routeLocker);
            SerializedProperty assignedPoints = routeLockerObject.FindProperty("m_TargetPoints");
            SerializedProperty assignedRoutes = routeLockerObject.FindProperty("m_TargetRoute");
            assignedPoints.arraySize = points.Count;
            assignedRoutes.arraySize = routes.Count;
            for (int i = 0; i < points.Count; i++)
            {
                assignedPoints.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
                assignedRoutes.GetArrayElementAtIndex(i).intValue = routes[i];
            }
            routeLockerObject.ApplyModifiedProperties();
            Debug.Log($"[RouteLocker] {routeLocker.name} : assigned {points.Count} point(s)", routeLocker);
            return true;
        }

        private static AbstractPointSetter[] GetAllPoints()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            GameObject[] rootGameObjects = currentScene.GetRootGameObjects();
            List<AbstractPointSetter> allPoints = new List<AbstractPointSetter>();
            foreach (GameObject rootGameObject in rootGameObjects)
            {
                allPoints.AddRange(rootGameObject.GetComponentsInChildren<AbstractPointSetter>(true));
            }
            return allPoints.ToArray();
        }

        /// <summary>
        /// 探索状態 : 今いるレールと、抜けていく端(true=next側, false=prev側)
        /// </summary>
        private struct RailState
        {
            public Rail_Script rail;
            public bool isNext;
            public RailState(Rail_Script rail, bool isNext)
            {
                this.rail = rail;
                this.isNext = isNext;
            }
        }

        /// <summary>
        /// 遷移 : 遷移先と、そのために必要な分岐器の進路(不要ならpoint=null)
        /// </summary>
        private struct Transition
        {
            public RailState to;
            public AbstractPointSetter point;
            public int routeIndex;
        }

        /// <summary>
        /// Route_Rails を [0]->[1]->...->[end] の順に辿り、通過する分岐器と進路を集める
        /// </summary>
        private static bool SearchRoute(Rail_Script[] routeRails, AbstractPointSetter[] allPoints, out List<AbstractPointSetter> points, out List<int> routes)
        {
            points = new List<AbstractPointSetter>();
            routes = new List<int>();
            if (routeRails == null || routeRails.Length < 2)
            {
                Debug.LogWarning("[RouteLocker] Route_Rails needs at least 2 rails");
                return false;
            }
            if (routeRails.Any(rail => rail == null))
            {
                Debug.LogWarning("[RouteLocker] Route_Rails contains an empty element");
                return false;
            }
            foreach (AbstractPointSetter point in allPoints)
            {
                if ((point.getFroms()?.Length ?? 0) != (point.getFromsEdge()?.Length ?? 0))
                {
                    Debug.LogWarning($"[RouteLocker] {point.name} : getFroms() and getFromsEdge() have different lengths", point);
                }
            }

            //最初の区間はどちら向きに出発するか不明なので両方から探索
            List<RailState> starts = new List<RailState> { new RailState(routeRails[0], true), new RailState(routeRails[0], false) };
            for (int i = 0; i < routeRails.Length - 1; i++)
            {
                List<Transition> path;
                if (!SearchSegment(starts, routeRails[i + 1], allPoints, out path))
                {
                    Debug.LogWarning($"[RouteLocker] {routeRails[i].name} -> {routeRails[i + 1].name} : no path found");
                    return false;
                }
                foreach (Transition transition in path)
                {
                    if (!transition.point) continue;
                    int idx = points.IndexOf(transition.point);
                    if (idx < 0)
                    {
                        points.Add(transition.point);
                        routes.Add(transition.routeIndex);
                    }
                    else if (routes[idx] != transition.routeIndex)
                    {
                        Debug.LogWarning($"[RouteLocker] Point {transition.point.name} is passed with conflicting routes ({routes[idx]} / {transition.routeIndex})", transition.point);
                        return false;
                    }
                }
                //次の区間は到着した向きのまま続ける
                starts = new List<RailState> { path[path.Count - 1].to };
            }
            return true;
        }

        /// <summary>
        /// 向きを考慮した幅優先探索で starts から goal までの遷移列を求める
        /// </summary>
        private static bool SearchSegment(List<RailState> starts, Rail_Script goal, AbstractPointSetter[] allPoints, out List<Transition> path)
        {
            path = new List<Transition>();
            Dictionary<RailState, (RailState from, Transition transition)> parents = new Dictionary<RailState, (RailState, Transition)>();
            HashSet<RailState> visited = new HashSet<RailState>(starts);
            Queue<RailState> queue = new Queue<RailState>(starts);

            while (queue.Count > 0)
            {
                RailState current = queue.Dequeue();
                foreach (Transition transition in GetTransitions(current, allPoints))
                {
                    if (visited.Contains(transition.to)) continue;
                    visited.Add(transition.to);
                    parents[transition.to] = (current, transition);
                    if (transition.to.rail == goal)
                    {
                        //ゴールから辿って経路を復元
                        RailState state = transition.to;
                        while (parents.ContainsKey(state))
                        {
                            path.Add(parents[state].transition);
                            state = parents[state].from;
                        }
                        path.Reverse();
                        return true;
                    }
                    queue.Enqueue(transition.to);
                }
            }
            return false;
        }

        /// <summary>
        /// current の抜ける端から進める遷移を列挙
        /// </summary>
        private static List<Transition> GetTransitions(RailState current, AbstractPointSetter[] allPoints)
        {
            List<Transition> transitions = new List<Transition>();
            bool controlledByPoint = false;

            foreach (AbstractPointSetter point in allPoints)
            {
                Rail_Script[] froms = point.getFroms() ?? new Rail_Script[0];
                bool[] edges = point.getFromsEdge() ?? new bool[0];
                Rail_Script[] pointRoutes = point.getRoutes() ?? new Rail_Script[0];
                int fromCount = Mathf.Min(froms.Length, edges.Length);

                for (int i = 0; i < fromCount; i++)
                {
                    //対向 : currentのこの端を書き換える分岐器 → 各進路へ
                    if (froms[i] == current.rail && edges[i] == current.isNext)
                    {
                        controlledByPoint = true;
                        for (int k = 0; k < pointRoutes.Length; k++)
                        {
                            RailState to;
                            if (!TryEnter(pointRoutes[k], current.rail, allPoints, out to)) continue;
                            transitions.Add(new Transition { to = to, point = point, routeIndex = k });
                        }
                        break;
                    }
                }

                //背向 : currentが進路で、この端がFromに繋がっている分岐器 → Fromへ
                int routeIndex = System.Array.IndexOf(pointRoutes, current.rail);
                if (routeIndex < 0) continue;
                for (int i = 0; i < fromCount; i++)
                {
                    bool connectedEdge;
                    if (!froms[i] || !TryGetConnectedEdge(current.rail, froms[i], allPoints, out connectedEdge)) continue;
                    if (connectedEdge != current.isNext) continue;
                    controlledByPoint = true;
                    //Fromの分岐端から入るので、反対側の端へ抜ける
                    transitions.Add(new Transition { to = new RailState(froms[i], !edges[i]), point = point, routeIndex = routeIndex });
                }
            }

            //分岐器の無い端は next/prev の固定接続を辿る
            if (!controlledByPoint)
            {
                RailState to;
                if (TryEnter(current.isNext ? current.rail.next : current.rail.prev, current.rail, allPoints, out to))
                {
                    transitions.Add(new Transition { to = to, point = null, routeIndex = -1 });
                }
            }
            return transitions;
        }

        /// <summary>
        /// previous から rail に入ったときの状態(入った端の反対側へ抜ける)を返す
        /// </summary>
        private static bool TryEnter(Rail_Script rail, Rail_Script previous, AbstractPointSetter[] allPoints, out RailState state)
        {
            state = default;
            bool enteredEdge;
            if (!rail || !TryGetConnectedEdge(rail, previous, allPoints, out enteredEdge)) return false;
            state = new RailState(rail, !enteredEdge);
            return true;
        }

        /// <summary>
        /// rail のどちらの端が neighbor に繋がっているか(true=next側)
        /// 固定接続(next/prev)か、railの端を neighbor へ書き換えうる分岐器で判定
        /// </summary>
        private static bool TryGetConnectedEdge(Rail_Script rail, Rail_Script neighbor, AbstractPointSetter[] allPoints, out bool isNext)
        {
            isNext = false;
            foreach (AbstractPointSetter point in allPoints)
            {
                Rail_Script[] froms = point.getFroms() ?? new Rail_Script[0];
                bool[] edges = point.getFromsEdge() ?? new bool[0];
                Rail_Script[] pointRoutes = point.getRoutes() ?? new Rail_Script[0];
                for (int i = 0; i < Mathf.Min(froms.Length, edges.Length); i++)
                {
                    if (froms[i] == rail && pointRoutes.Contains(neighbor))
                    {
                        isNext = edges[i];
                        return true;
                    }
                }
            }
            if (rail.next == neighbor)
            {
                isNext = true;
                return true;
            }
            if (rail.prev == neighbor)
            {
                isNext = false;
                return true;
            }
            return false;
        }
    }
}
