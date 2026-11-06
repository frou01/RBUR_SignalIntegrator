using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RBUR.SignalIntegrator
{
    public class RouteSelectorEndButton : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField] public RouteSelectorStartLever[] StartLevers;
        public void _OnPerform()
        {
            //Route End Button Pushed
            foreach (RouteSelectorStartLever startLever in StartLevers)
            {
                startLever.SelectRoute(this);
            }
        }
    }
}