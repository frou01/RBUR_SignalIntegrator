using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace frou01.RBUR.SignalIntegrator
{
    public class EventStackHolder : UdonSharpBehaviour
    {
        [FormerlySerializedAs("debugLog_onAddStack")]
        [FormerlySerializedAs("debugLog")][SerializeField] bool isLogging = false;
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
                if (isLogging)
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