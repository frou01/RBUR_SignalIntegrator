using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RBUR_SignalIntegrator
{
    public class RouteSelector_EndToPresetMapHolder : MonoBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [SerializeField] List<SwitchToPresets> Switch_To_EndAndPresetMaps;

        public void ApplyEndToPresetArray(out RouteSelector_EndButton[][] ends,out MultiLeverController[][] presets)
        {
            ends = Switch_To_EndAndPresetMaps.Select(val =>
                val.EndAndPresetMulcon.Select(val => val.end).ToArray()
            ).ToArray();
            presets = Switch_To_EndAndPresetMaps.Select(val => 
                val.EndAndPresetMulcon.Select(val => val.preset).ToArray()
            ).ToArray();
        }

        [Serializable]
        private class SwitchToPresets
        {
            [SerializeField] public List<EndAndMulcon> EndAndPresetMulcon;
        }

        [Serializable]
        private class EndAndMulcon
        {
            [SerializeField] public RouteSelector_EndButton end;
            [SerializeField] public MultiLeverController preset;
        }
#endif
    }
}
