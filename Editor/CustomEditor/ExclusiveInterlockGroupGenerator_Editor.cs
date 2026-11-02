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
        SerializedProperty TargetInterlocks;
        SerializedProperty GeneratedGroups;

        void OnEnable()
        {
            TargetInterlocks = serializedObject.FindProperty("targetInterlocks");
            GeneratedGroups = serializedObject.FindProperty("generatedGroups");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(TargetInterlocks);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(GeneratedGroups);
            }
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Generate Groups"))
            {
                Generate((ExclusiveInterlockGroupGenerator)target);
            }
        }

        private static void Generate(ExclusiveInterlockGroupGenerator generator)
        {
            ClearGeneratedGroups(generator);

            List<Interlocking> interlocks = CollectRouteInterlocks(generator);
            List<Interlocking[]> groups = FindGroups(interlocks);

            List<ExclusiveInterlockGroup> generated = new List<ExclusiveInterlockGroup>();
            foreach (Interlocking[] group in groups)
            {
                generated.Add(CreateGroup(generator, group));
            }
            generator.generatedGroups = generated.ToArray();
            EditorUtility.SetDirty(generator);
            Debug.Log($"[ExclusiveInterlockGroupGenerator] {generator.name} : generated {generated.Count} group(s) from {interlocks.Count} route(s)", generator);
        }

        /// <summary>
        /// 前回生成した ExclusiveInterlockGroup を削除する
        /// </summary>
        private static void ClearGeneratedGroups(ExclusiveInterlockGroupGenerator generator)
        {
            //TODO: generatedGroups の GameObject を Undo.DestroyObjectImmediate で削除
        }

        /// <summary>
        /// RouteLocker の TargetPoints / TargetRoute が有効な Interlocking を、TargetPoints の少ない順に返す
        /// </summary>
        private static List<Interlocking> CollectRouteInterlocks(ExclusiveInterlockGroupGenerator generator)
        {
            //TODO: null / From_Locker 無し / RouteLocker 無し / 長さ不一致を警告して除外し、短い順に並べる
            return new List<Interlocking>();
        }

        /// <summary>
        /// 短い進路から順に、それを含む(分岐器を全て同じ開通方向で通る)進路と合わせて群にする
        /// 既存の群に丸ごと含まれる群は作らない
        /// </summary>
        private static List<Interlocking[]> FindGroups(List<Interlocking> interlocks)
        {
            //TODO: 群の割り出し
            return new List<Interlocking[]>();
        }

        /// <summary>
        /// routeA の分岐器を routeB が全て同じ開通方向で通るか
        /// </summary>
        private static bool ContainsRoute(RouteLocker routeA, RouteLocker routeB)
        {
            //TODO
            return false;
        }

        private static ExclusiveInterlockGroup CreateGroup(ExclusiveInterlockGroupGenerator generator, Interlocking[] interlocks)
        {
            //TODO: 子に GameObject を作って ExclusiveInterlockGroup を付け、Undo.RegisterCreatedObjectUndo する
            return null;
        }
    }
}
