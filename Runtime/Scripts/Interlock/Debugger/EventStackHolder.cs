using UdonSharp;
using UnityEngine;

namespace RBUR_SignalIntegrator
{
    public class EventStackHolder : UdonSharpBehaviour
    {
        [SerializeField] bool debugLog_onAddStack = false;
        protected string StackString;
        protected string indent;
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
            if (debugLog_onAddStack)
            {
                Debug.Log("EventStackHolder\nAdd:" + pointedStack + "Result:\n" + StackString, on);
            }
        }
        public void RemoveStack(UdonSharpBehaviour on, string funcName)
        {
            string pointedStack = indent + on.name;
            pointedStack += ".";
            pointedStack += funcName;
            pointedStack += "()";
            pointedStack += "\n";

            StackString = StackString.Remove(StackString.Length - pointedStack.Length);
            if (debugLog_onAddStack)
            {
                Debug.Log("EventStackHolder\nRemove:" + pointedStack + "Result:\n" + StackString, on);
            }
            indent = indent.Remove(indent.Length - "    ".Length);
        }
        public string GetCurrentStackString()
        {
            return StackString;
        }
    }
}