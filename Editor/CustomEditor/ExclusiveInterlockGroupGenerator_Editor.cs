using frou01.RigidBodyTrain;
using RBUR_SignalIntegrator;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RBUR_SignalIntegrator_Editor
{
    [CustomEditor(typeof(ExclusiveInterlockGroupGenerator))]
    public class ExclusiveInterlockGroupGenerator_Editor : Editor
    {
        SerializedProperty targetInterlocksProperty;
        SerializedProperty generatedGroupsProperty;

        void OnEnable()
        {
            targetInterlocksProperty = serializedObject.FindProperty("targetInterlocks");
            generatedGroupsProperty = serializedObject.FindProperty("generatedGroups");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            //標準の描画より先にドロップを処理するため、矩形を先に確保する
            Rect targetRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(targetInterlocksProperty, true));
            HandleDropChildren(targetRect, targetInterlocksProperty);
            EditorGUI.PropertyField(targetRect, targetInterlocksProperty, true);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(generatedGroupsProperty);
            }
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Generate Groups"))
            {
                Generate((ExclusiveInterlockGroupGenerator)target);
            }
        }

        /// <summary>
        /// rect に GameObject がドロップされたら、子の Interlocking も含めて property に追加する
        /// 子に Interlocking が無ければ標準の挙動に任せる
        /// </summary>
        private static void HandleDropChildren(Rect rect, SerializedProperty property)
        {
            Event evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
            if (!rect.Contains(evt.mousePosition)) return;

            List<Interlocking> dropped = new List<Interlocking>();
            bool hasChildInterlock = false;
            foreach (GameObject obj in DragAndDrop.objectReferences.OfType<GameObject>())
            {
                if (EditorUtility.IsPersistent(obj)) continue;//プレハブアセットはシーンから参照できない
                foreach (Interlocking interlock in obj.GetComponentsInChildren<Interlocking>(true))
                {
                    if (interlock.gameObject != obj) hasChildInterlock = true;
                    dropped.Add(interlock);
                }
            }
            if (!hasChildInterlock) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                List<Object> current = new List<Object>();
                for (int i = 0; i < property.arraySize; i++)
                {
                    current.Add(property.GetArrayElementAtIndex(i).objectReferenceValue);
                }
                int added = 0;
                foreach (Interlocking interlock in dropped.Distinct())
                {
                    if (current.Contains(interlock)) continue;
                    property.arraySize++;
                    property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = interlock;
                    added++;
                }
                Debug.Log($"[ExclusiveInterlockGroupGenerator] added {added} interlock(s) from dropped object(s)");
            }
            evt.Use();
        }

        private static void Generate(ExclusiveInterlockGroupGenerator generator)
        {
            Undo.SetCurrentGroupName("Generate Exclusive Interlock Groups");
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RecordObject(generator, "Generate Exclusive Interlock Groups");

            ClearGeneratedGroups(generator);

            List<Interlocking> interlocks = CollectRouteInterlocks(generator);
            List<Interlocking[]> groups = FindGroups(interlocks);

            List<ExclusiveInterlockGroup> generated = new List<ExclusiveInterlockGroup>();
            for (int i = 0; i < groups.Count; i++)
            {
                generated.Add(CreateGroup(generator, groups[i], i));
            }
            generator.generatedGroups = generated.ToArray();
            EditorUtility.SetDirty(generator);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[ExclusiveInterlockGroupGenerator] {generator.name} : generated {generated.Count} group(s) from {interlocks.Count} route(s)", generator);
        }

        /// <summary>
        /// 前回生成した ExclusiveInterlockGroup を削除する
        /// </summary>
        private static void ClearGeneratedGroups(ExclusiveInterlockGroupGenerator generator)
        {
            if (generator.generatedGroups == null) return;
            foreach (ExclusiveInterlockGroup group in generator.generatedGroups)
            {
                if (group == null) continue;//手動で削除済み
                Undo.DestroyObjectImmediate(group.gameObject);
            }
            generator.generatedGroups = new ExclusiveInterlockGroup[0];
        }

        /// <summary>
        /// RouteLocker の TargetPoints / TargetRoute が有効な Interlocking を、TargetPoints の少ない順に返す
        /// </summary>
        private static List<Interlocking> CollectRouteInterlocks(ExclusiveInterlockGroupGenerator generator)
        {
            List<Interlocking> interlocks = new List<Interlocking>();
            if (generator.targetInterlocks == null) return interlocks;
            foreach (Interlocking interlock in generator.targetInterlocks.Where(val => val != null).Distinct())
            {
                if (!interlock.GetFromLocker())
                {
                    Debug.LogWarning($"[ExclusiveInterlockGroupGenerator] {generator.name} : {interlock.name} has no From_Locker. Skipped", interlock);
                    continue;
                }
                RouteLocker routeLocker = interlock.GetRouteLocker();
                if (!routeLocker || routeLocker.TargetPoints == null || routeLocker.TargetPoints.Count(val => val != null) == 0)
                {
                    Debug.LogWarning($"[ExclusiveInterlockGroupGenerator] {generator.name} : {interlock.name} has no RouteLocker points. Skipped", interlock);
                    continue;
                }
                if (routeLocker.TargetRoute == null || routeLocker.TargetRoute.Length != routeLocker.TargetPoints.Length)
                {
                    Debug.LogWarning($"[ExclusiveInterlockGroupGenerator] {generator.name} : {interlock.name} TargetPoints and TargetRoute have different lengths. Skipped", interlock);
                    continue;
                }
                interlocks.Add(interlock);
            }
            //OrderBy は安定ソートなので、同じ長さの進路は targetInterlocks の順を保つ
            return interlocks.OrderBy(val => val.GetRouteLocker().TargetPoints.Count(point => point != null)).ToList();
        }

        /// <summary>
        /// 短い進路から順に、それを含む(分岐器を全て同じ開通方向で通る)進路と合わせて群にする
        /// 既存の群に丸ごと含まれる群は作らない。一部だけ重なる組は2つだけの群にする
        /// </summary>
        private static List<Interlocking[]> FindGroups(List<Interlocking> interlocks)
        {
            List<Dictionary<AbstractPointSetter, int>> routeMaps = interlocks.Select(val => GetRouteMap(val.GetRouteLocker())).ToList();
            List<Interlocking[]> groups = new List<Interlocking[]>();
            List<HashSet<Interlocking>> groupSets = new List<HashSet<Interlocking>>();

            for (int i = 0; i < interlocks.Count; i++)
            {
                //群の中の進路は全て進路i の分岐器を同じ開通方向で通るので、どの2つも同時に開通できる
                List<Interlocking> group = new List<Interlocking> { interlocks[i] };
                for (int j = 0; j < interlocks.Count; j++)
                {
                    if (i != j && ContainsRoute(routeMaps[i], routeMaps[j])) group.Add(interlocks[j]);
                }
                if (group.Count < 2) continue;
                HashSet<Interlocking> groupSet = new HashSet<Interlocking>(group);
                if (groupSets.Any(val => groupSet.IsSubsetOf(val))) continue;
                groups.Add(group.ToArray());//先頭が進路i、以降は短い順
                groupSets.Add(groupSet);
            }

            //どちらも相手を含まないが一部の分岐器を同じ開通方向で共有する組も、同時に開通できるので必ず排他にする
            for (int i = 0; i < interlocks.Count; i++)
            {
                for (int j = i + 1; j < interlocks.Count; j++)
                {
                    if (!CanOpenTogether(routeMaps[i], routeMaps[j])) continue;
                    if (groupSets.Any(val => val.Contains(interlocks[i]) && val.Contains(interlocks[j]))) continue;
                    groups.Add(new Interlocking[] { interlocks[i], interlocks[j] });
                    groupSets.Add(new HashSet<Interlocking> { interlocks[i], interlocks[j] });
                }
            }
            return groups;
        }

        private static Dictionary<AbstractPointSetter, int> GetRouteMap(RouteLocker routeLocker)
        {
            Dictionary<AbstractPointSetter, int> routeMap = new Dictionary<AbstractPointSetter, int>();
            for (int i = 0; i < routeLocker.TargetPoints.Length; i++)
            {
                if (routeLocker.TargetPoints[i] == null) continue;
                routeMap[routeLocker.TargetPoints[i]] = routeLocker.TargetRoute[i];
            }
            return routeMap;
        }

        /// <summary>
        /// routeA の分岐器を routeB が全て同じ開通方向で通るか
        /// </summary>
        private static bool ContainsRoute(Dictionary<AbstractPointSetter, int> routeA, Dictionary<AbstractPointSetter, int> routeB)
        {
            foreach (KeyValuePair<AbstractPointSetter, int> pair in routeA)
            {
                int route;
                if (!routeB.TryGetValue(pair.Key, out route) || route != pair.Value) return false;
            }
            return true;
        }

        /// <summary>
        /// 共有する分岐器が1つ以上あり、その開通方向が全て一致するか
        /// </summary>
        private static bool CanOpenTogether(Dictionary<AbstractPointSetter, int> routeA, Dictionary<AbstractPointSetter, int> routeB)
        {
            bool shared = false;
            foreach (KeyValuePair<AbstractPointSetter, int> pair in routeA)
            {
                int route;
                if (!routeB.TryGetValue(pair.Key, out route)) continue;
                if (route != pair.Value) return false;
                shared = true;
            }
            return shared;
        }

        private static ExclusiveInterlockGroup CreateGroup(ExclusiveInterlockGroupGenerator generator, Interlocking[] interlocks, int index)
        {
            GameObject groupObject = new GameObject($"ExclusiveGroup_{index}_{interlocks[0].name}");
            Undo.RegisterCreatedObjectUndo(groupObject, "Generate Exclusive Interlock Groups");
            groupObject.transform.SetParent(generator.transform, false);
            ExclusiveInterlockGroup group = groupObject.AddComponent<ExclusiveInterlockGroup>();
            group.exclusiveInterlocks = interlocks;
            return group;
        }
    }
}
