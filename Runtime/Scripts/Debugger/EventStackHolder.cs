using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace RBUR_SignalIntegrator
{
    public class EventStackHolder : UdonSharpBehaviour
    {
        [FormerlySerializedAs("debugLog_onAddStack")]
        [SerializeField] bool debugLog = false;
        protected string StackString;
        protected string indent;

        bool nestBottom = true;
        private void Update()
        {
            StackString = "";
        }

        public void AddStack(UdonSharpBehaviour on, string funcName)
        {
            indent += "    ";
            string pointedStack = indent + on.name;
            pointedStack += ".";
            pointedStack += funcName;
            pointedStack += "()";
            pointedStack += "\n";

            StackString += pointedStack;
            nestBottom = true;
        }
        public void RemoveStack(UdonSharpBehaviour on, string funcName)
        {
            string pointedStack = indent + on.name;
            pointedStack += ".";
            pointedStack += funcName;
            pointedStack += "()";
            pointedStack += "\n";

            StackString = StackString.Remove(StackString.Length - pointedStack.Length);
            if (nestBottom)
            {
                if (debugLog)
                {
                    Debug.Log("Stacktrace:\n" + StackString, on);
                }
                nestBottom = false;
            }
            indent = indent.Remove(indent.Length - "    ".Length);
        }
        public string GetCurrentStackString()
        {
            return StackString;
        }
    }
}