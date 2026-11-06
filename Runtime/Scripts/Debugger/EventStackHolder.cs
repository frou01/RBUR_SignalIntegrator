using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace RBUR_SignalIntegrator
{
    public class EventStackHolder : UdonSharpBehaviour
    {
        [FormerlySerializedAs("debugLog_onAddStack")]
        [SerializeField] bool debugLog = false;
        protected string stackString = "";
        protected string indent;

        bool isNestBottom = true;
        private void Update()
        {
            stackString = "";
        }

        public void AddStack(UdonSharpBehaviour on, string funcName)
        {
            indent += "    ";
            string pointedStack = indent + on.name;
            pointedStack += ".";
            pointedStack += funcName;
            pointedStack += "()";
            pointedStack += "\n";

            stackString += pointedStack;
            isNestBottom = true;
        }
        public void RemoveStack(UdonSharpBehaviour on, string funcName)
        {
            string pointedStack = indent + on.name;
            pointedStack += ".";
            pointedStack += funcName;
            pointedStack += "()";
            pointedStack += "\n";

            if (isNestBottom)
            {
                if (debugLog)
                {
                    Debug.Log("Stacktrace:\n" + stackString, on);
                }
                isNestBottom = false;
            }
            stackString = stackString.Remove(stackString.Length - pointedStack.Length);
            indent = indent.Remove(indent.Length - "    ".Length);
        }
        public string GetCurrentStackString()
        {
            return stackString;
        }
    }
}