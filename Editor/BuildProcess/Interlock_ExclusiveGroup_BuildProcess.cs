using frou01.RBUR.SignalIntegrator;
using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace frou01.RBUR.SignalIntegrator.Editor
{
    /// <summary>
    /// ExclusiveInterlockGroup から、排他条件の LockerAndMeetPosition を生成して Interlocking に登録する。
    /// 生成したLockerAndMeetPositionにもeventStackHolderを配らせるため、Interlock_PreBuildProcess(-200)より前に実行する。
    /// </summary>
    public class Interlock_ExclusiveGroup_BuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => -250;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Debug.Log("[ExclusiveInterlockGroup] Start Process");
            foreach (ExclusiveInterlockGroup group in BuildProcessUtility.FindComponents<ExclusiveInterlockGroup>(scene))
            {
                SetupGroup(group);
            }
            Debug.Log("[ExclusiveInterlockGroup] End Process");
        }

        private static void SetupGroup(ExclusiveInterlockGroup group)
        {
            Interlocking[] interlocks = group.exclusiveInterlocks.Where(val => val != null).Distinct().ToArray();
            foreach (Interlocking interlock in interlocks)
            {
                if (!interlock.GetFromLocker())
                {
                    Debug.LogWarning($"[ExclusiveInterlockGroup] {group.name} : {interlock.name} has no From_Locker. Skipped", interlock);
                }
            }
            interlocks = interlocks.Where(val => val.GetFromLocker() != null).ToArray();

            //片方向の条件だけで排他になる(Aが進行中はBを停止位置に鎖錠し、Bが進行中はAが進行条件を満たさない)
            //双方向に張ると Interlock_PostBuildProcess の相互鎖錠警告に当たるので i<j の向きだけ生成する
            for (int i = 0; i < interlocks.Length; i++)
            {
                for (int j = i + 1; j < interlocks.Length; j++)
                {
                    Interlocking interlockA = interlocks[i];
                    Interlocking interlockB = interlocks[j];
                    if (HasStateLinker(interlockA, interlockB.GetFromLocker()) || HasStateLinker(interlockB, interlockA.GetFromLocker()))
                    {
                        continue;//手動で設定済み
                    }
                    AddExclusiveStateLinker(group, interlockA, interlockB);
                }
            }
        }

        private static bool HasStateLinker(Interlocking interlock, AbstractLockerConsolidator locker)
        {
            return interlock.GetInterlockStateLinker().Any(val => val != null && val.getLocker() == locker);
        }

        /// <summary>
        /// interlockA に「interlockB の信号が停止位置であること」を進行条件として追加する
        /// </summary>
        private static void AddExclusiveStateLinker(ExclusiveInterlockGroup group, Interlocking interlockA, Interlocking interlockB)
        {
            GameObject linkerObject = new GameObject($"Exclusive_{interlockA.name}_{interlockB.name}");
            linkerObject.transform.SetParent(group.transform, false);
            LockerAndMeetPosition stateLinker = linkerObject.AddUdonSharpComponent<LockerAndMeetPosition>();
            stateLinker.SetLockerAndMeetPosition(interlockB.GetFromLocker(), new int[] { interlockB.GetClosePositionIndex() });
            interlockA.AddInterlockStateLinker(stateLinker);
            Debug.Log($"[ExclusiveInterlockGroup] {group.name} : {interlockA.name} requires {interlockB.name} at position {interlockB.GetClosePositionIndex()} ({interlockA.GetInterlockStateLinker().Length} state linker(s))", interlockA);
        }
    }
}
