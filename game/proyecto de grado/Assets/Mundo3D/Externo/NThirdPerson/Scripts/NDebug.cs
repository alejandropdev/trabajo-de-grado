using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NComponent
{
    public static class NDebug
    {
        private static bool enabled = true;
        public static void nSetEnabled(bool val, Object target)
        {
            nLog(string.Format("NDebug Enabled ({0})", val), target, LogType.Normal);
            enabled = val;
        }
        public static void nLog(string message, Object target, LogType logType = LogType.Normal)
        {
            if (!enabled) { return; }

            string className = (target != null) ? target.GetType().Name : "NAppDebug";
            message = string.Format("[{0}] [{1}]", className, message);
            if (logType == LogType.Normal) { Debug.Log(message, target); }
            else if (logType == LogType.Warning) { Debug.LogWarning(message, target); }
            else if (logType == LogType.Error) { Debug.LogError(message, target); }
        }

        public enum LogType { Normal, Warning, Error }
    }
}