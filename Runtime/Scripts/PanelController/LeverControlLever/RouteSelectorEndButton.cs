using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    public class RouteSelectorEndButton : UdonSharpBehaviour
    {
        [FormerlySerializedAs("StartLevers")][HideInInspector][SerializeField] public RouteSelectorStartLever[] startLevers;
        public void _OnPerform()
        {
            //Route End Button Pushed
            foreach (RouteSelectorStartLever startLever in startLevers)
            {
                startLever.SelectRoute(this);
            }
        }
    }
}