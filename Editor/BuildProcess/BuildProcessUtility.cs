using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace frou01.RBUR.SignalIntegrator.Editor
{
    /// <summary>
    /// BuildProcess 共通の補助処理
    /// </summary>
    public static class BuildProcessUtility
    {
        /// <summary>
        /// シーン内の T を全て取得する(ルートオブジェクト順)
        /// </summary>
        public static List<T> FindComponents<T>(Scene scene, bool includeInactive = true)
        {
            List<T> result = new List<T>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                result.AddRange(obj.GetComponentsInChildren<T>(includeInactive));
            }
            return result;
        }

        /// <summary>
        /// array に含まれていない items だけを末尾に追加した配列を返す。array が null なら空配列として扱う。
        /// ユーザーも設定する配列に、ビルド処理から要素を足すときに使う(何度処理しても重複しない)
        /// </summary>
        public static T[] AddUnique<T>(this T[] array, params T[] items)
        {
            List<T> result = array == null ? new List<T>() : new List<T>(array);
            foreach (T item in items)
            {
                if (!result.Contains(item)) result.Add(item);
            }
            return result.ToArray();
        }
    }
}
