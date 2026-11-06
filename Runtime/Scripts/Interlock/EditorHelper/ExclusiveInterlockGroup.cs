using UnityEngine;

namespace frou01.RBUR.SignalIntegrator
{
    /// <summary>
    /// 互いに排他とする信号(Interlocking)のグループ。
    /// ビルド時に InterlockExclusiveGroupBuildProcess が、各組について「相手の信号が停止位置であること」の LockerAndMeetPosition を生成する。
    /// </summary>
    public class ExclusiveInterlockGroup : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] public Interlocking[] exclusiveInterlocks = new Interlocking[0];
#endif
    }
}
