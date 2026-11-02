using UnityEngine;

namespace RBUR_SignalIntegrator
{
    /// <summary>
    /// 対象の信号(Interlocking)の RouteLocker から、同時に開通できる進路の組を割り出して ExclusiveInterlockGroup を子に生成する。
    /// 生成はカスタムエディタ(ExclusiveInterlockGroupGenerator_Editor)のボタンから行う。
    /// </summary>
    public class ExclusiveInterlockGroupGenerator : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] public Interlocking[] targetInterlocks = new Interlocking[0];        //作り直し時に削除する対象。手動で作った ExclusiveInterlockGroup は含まない
        [SerializeField] public ExclusiveInterlockGroup[] generatedGroups = new ExclusiveInterlockGroup[0];
#endif
    }
}
