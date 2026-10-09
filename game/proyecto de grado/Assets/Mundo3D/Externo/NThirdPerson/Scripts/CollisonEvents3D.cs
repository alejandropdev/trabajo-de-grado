using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class CollisonEvents3D : MonoBehaviour
{
    public EventDetect eventDetect = EventDetect.Collision;
    public List<Collision3DHandler> collision3DHandlers = new List<Collision3DHandler>();

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (!rb) 
        { 
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    //Collision
    private void OnCollisionEnter(Collision collision) { callEvent(collision.gameObject, "Collision" , "Enter"); }
    private void OnCollisionStay(Collision collision) { callEvent(collision.gameObject, "Collision" , "Stay"); }
    private void OnCollisionExit(Collision collision) { callEvent(collision.gameObject, "Collision" , "Exit"); }

    //Triggers
    private void OnTriggerEnter(Collider collider) { callEvent(collider.gameObject, "Trigger", "Enter"); }
    private void OnTriggerStay(Collider collider) { callEvent(collider.gameObject, "Trigger", "Stay"); }
    private void OnTriggerExit(Collider collider) { callEvent(collider.gameObject, "Trigger", "Exit"); }

    public enum EventDetect { Collision, Trigger }
    public enum TargetDetect { GameObject, Tag }

    public void callEvent(GameObject go, string eventTrigger, string eventDetected)
    {
        /*
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
        {
            Debug.Log(string.Format("[CollisonEvents3D]-[{0} {1} -> (GO:{2} - TAG:{3})]", eventTrigger, eventDetected, go.name, go.tag));
        }
        */

        if (!enabled) { return; }
        foreach (Collision3DHandler handler in collision3DHandlers)
        {
            if (eventDetect == EventDetect.Collision && eventTrigger == "Collision")
            {
                if (handler.targetDetect == TargetDetect.GameObject && go != handler.targetObject) { continue; }
                if (handler.targetDetect == TargetDetect.Tag && go.tag != handler.targetTag) { continue; }

                if (eventDetected == "Enter") { if (handler.detectEnter) { handler.onEnter.Invoke(); } }
                if (eventDetected == "Stay") { if (handler.detectStay) { handler.onStay.Invoke(); } }
                if (eventDetected == "Exit") { if (handler.detectExit) { handler.onExit.Invoke(); } }
            }

            if (eventDetect == EventDetect.Trigger && eventTrigger == "Trigger")
            {
                if (handler.targetDetect == TargetDetect.GameObject && go != handler.targetObject) { continue; }
                if (handler.targetDetect == TargetDetect.Tag && go.tag != handler.targetTag) { continue; }

                if (eventDetected == "Enter")
                {
                    if (handler.detectEnter)
                    {
                        handler.onEnter.Invoke(); 
                        //StartCoroutine(progressivelyEventsIE(handler.onEnter));
                    }
                }
                if (eventDetected == "Stay") { if (handler.detectStay) { handler.onStay.Invoke(); } }
                if (eventDetected == "Exit") { if (handler.detectExit) { handler.onExit.Invoke(); } }
            }

            if (handler.detectEnter || handler.detectStay || handler.detectExit) { }
        }
    }

    // private bool isWaiting = false;
    private IEnumerator progressivelyEventsIE(UnityEvent unityEvent) 
    {
        for (int i=0; i<unityEvent.GetPersistentEventCount(); i++)
        {
         
            UnityEngine.Object @object = unityEvent.GetPersistentTarget(i);
            string method = unityEvent.GetPersistentMethodName(i);
            //string method = unityEvent.ge;

            MethodInfo[] methodsInfo = @object.GetType().GetMethods();
            object classInstance = Activator.CreateInstance(@object.GetType(), null);

            foreach (MethodInfo methodInfo in methodsInfo)
            {
                if (methodInfo != null && methodInfo.Name == method)
                {
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    Debug.Log("Called " + methodInfo.Name + " Params" + parameters.Length);

                    //UnityAction methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction),  classInstance, methodInfo) as UnityAction;
                    //methodDelegate.Invoke();

                    if (parameters.Length < 1) { methodInfo.Invoke(@object, null); }
                    //else { methodInfo.Invoke(@object, castParameters(parameters)); }
                    else { methodInfo.Invoke(@object, castParameters(parameters)); }
                }
                //yield return new WaitForSeconds(waitInterval);
                yield return null;
            }

            //UnityAction methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction), @object, method) as UnityAction;
            //UnityEventTools.AddPersistentListener(ActionTarget, methodDelegate);
            //methodDelegate.Invoke();
            //yield return new WaitForSeconds(2);
        }
    }

    public object[] castParameters(ParameterInfo[] parameters)
    {
        bool hasParams = false;
        if (parameters.Length > 0)
            hasParams = parameters[parameters.Length - 1].GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0;

            object[] realParams = new object[parameters.Length];
        if (hasParams)
        {
            int lastParamPosition = parameters.Length - 1;

            for (int i = 0; i < lastParamPosition; i++)
                realParams[i] = parameters[i];

            Type paramsType = parameters[lastParamPosition].ParameterType.GetElementType();
            Array extra = Array.CreateInstance(paramsType, parameters.Length - lastParamPosition);
            for (int i = 0; i < extra.Length; i++)
                extra.SetValue(parameters[i + lastParamPosition], i);

            realParams[lastParamPosition] = extra;
        }

        return realParams;
    }

    [System.Serializable]
    public class Collision3DHandler
    {        
        public TargetDetect targetDetect = TargetDetect.Tag;
        public string targetTag = "";
        public GameObject targetObject = null;
        public bool detectEnter = false;
        public bool detectStay = false;
        public bool detectExit = false;
        public UnityEvent onEnter;
        public UnityEvent onStay;
        public UnityEvent onExit;
        public bool collapsed = false;
    }
}


