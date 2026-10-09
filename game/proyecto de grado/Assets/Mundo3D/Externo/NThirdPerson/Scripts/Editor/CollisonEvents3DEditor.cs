using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static CollisonEvents3D;
using System;

[CanEditMultipleObjects]
[CustomEditor(typeof(CollisonEvents3D))]
public class CollisonEvents3DEditor : Editor
{
    private CollisonEvents3D script;
    private SerializedProperty handlerEvents;
    MonoScript monoScript = null;

    private void OnEnable()
    {
        monoScript = MonoScript.FromMonoBehaviour((CollisonEvents3D)target);
    }
    public override void OnInspectorGUI()
    {
        script = target as CollisonEvents3D;
        handlerEvents = serializedObject.FindProperty("collision3DHandlers");

        EditorGUILayout.BeginVertical();
        EditorGUI.BeginDisabledGroup(true);
        monoScript = EditorGUILayout.ObjectField("Script", monoScript, typeof(MonoScript), false) as MonoScript;
        EditorGUI.EndDisabledGroup();
        script.eventDetect = (EventDetect)EditorGUILayout.EnumPopup("Event Detect", script.eventDetect);
        if (script.eventDetect == EventDetect.Collision) 
        { 
            Collider col = script.GetComponent<Collider>();
            if (col) { col.isTrigger = false; }
        } 
        if (script.eventDetect == EventDetect.Trigger) 
        { 
            Collider col = script.GetComponent<Collider>();
            if (col) { col.isTrigger = true; }
        }         
        drawList();
        EditorGUILayout.EndVertical();
        drawSeparator(1);
        if (GUILayout.Button("Add")) { script.collision3DHandlers.Add(new Collision3DHandler()); }
        serializedObject.ApplyModifiedProperties();
    }

    private void drawList()
    {
        for (int i=0; i<script.collision3DHandlers.Count; i++)
        {
            Collision3DHandler handler = script.collision3DHandlers[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(getHandlerHeader(handler), EditorStyles.label)) { handler.collapsed = !handler.collapsed; }
            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(18))) { script.collision3DHandlers.Remove(handler); }
            EditorGUILayout.EndHorizontal();

            if (getStatusInfo(handler) != "")
            {
                Color defaultColor = GUI.color;
                drawSeparator(0);
                GUI.color = getStatusColor(handler);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.color = defaultColor;
 

                EditorGUILayout.LabelField(getStatusInfo(handler), EditorStyles.miniLabel);
                GUI.color = getStatusColor(handler);

                EditorGUILayout.EndVertical();
                GUI.color = defaultColor;
            }           

            if (!handler.collapsed)
            {
                drawSeparator(0);
                handler.targetDetect = (TargetDetect)EditorGUILayout.EnumPopup("Target Detect", handler.targetDetect);

                if (handler.targetDetect == TargetDetect.GameObject) { handler.targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", handler.targetObject, typeof(GameObject), true); }
                if (handler.targetDetect == TargetDetect.Tag) { handler.targetTag = EditorGUILayout.TextField("Target Tag", handler.targetTag); }

                drawSpace(3);
                EditorGUILayout.BeginHorizontal();
                //handler.detectEnter = EditorGUILayout.Toggle(handler.detectEnter, "Toogle Me", buttonStyle);
                if (GUILayout.Button("On Enter", getToggleButtonStyle(handler.detectEnter))) { handler.detectEnter = !handler.detectEnter; }
                if (GUILayout.Button("On Stay", getToggleButtonStyle(handler.detectStay))) { handler.detectStay = !handler.detectStay; }
                if (GUILayout.Button("On Exit", getToggleButtonStyle(handler.detectExit))) { handler.detectExit = !handler.detectExit; }
                EditorGUILayout.EndHorizontal();

                SerializedProperty property = handlerEvents.GetArrayElementAtIndex(i);
                drawSpace(3);
                if (handler.detectEnter) { EditorGUILayout.PropertyField(property.FindPropertyRelative("onEnter")); }
                if (handler.detectStay) { EditorGUILayout.PropertyField(property.FindPropertyRelative("onStay")); }
                if (handler.detectExit) { EditorGUILayout.PropertyField(property.FindPropertyRelative("onExit")); }
            }
            EditorGUILayout.EndVertical();
        }
    }

    private string getHandlerHeader(Collision3DHandler collision3DHandler)
    {
        string targetType = "";
        if (collision3DHandler.targetDetect == TargetDetect.GameObject) 
        { 
            if (collision3DHandler.targetObject)
            targetType = string.Format("[{0} ({1})]", collision3DHandler.targetDetect.ToString(), collision3DHandler.targetObject.name); 
        }
        if (collision3DHandler.targetDetect == TargetDetect.Tag) { targetType = string.Format("[{0} ({1})]", collision3DHandler.targetDetect.ToString(), collision3DHandler.targetTag); }

        string eventDetected = "En St Ex";
        if (!collision3DHandler.detectEnter) { eventDetected = eventDetected.Replace("En ", ""); }
        if (!collision3DHandler.detectStay) { eventDetected = eventDetected.Replace("St ", ""); }
        if (!collision3DHandler.detectExit) { eventDetected = eventDetected.Replace(" Ex", ""); eventDetected = eventDetected.Replace("Ex", ""); }

        return string.Format ("{0} {1} [{2}]",script.eventDetect,targetType, eventDetected);
    }

    private string getStatusInfo(Collision3DHandler collision3DHandler)
    {
        string info = "";
        if (!collision3DHandler.detectEnter && !collision3DHandler.detectStay && !collision3DHandler.detectStay) { info = "No se ah asignado ningún evento"; }
        if (collision3DHandler.targetDetect == TargetDetect.GameObject && collision3DHandler.targetObject == null) { info = "Debe asignar un objeto a TargetObject"; }
        if (collision3DHandler.targetDetect == TargetDetect.Tag && collision3DHandler.targetTag == "") { info = "Debe asignar un valor a TargetTag"; }

        Collider collider = script.gameObject.GetComponent<Collider>();
        if (!collider) { info = "Debe agregar un Collider a este objeto"; }
        return info;
    }

    private Color getStatusColor(Collision3DHandler collision3DHandler)
    {
        float alpha = 0.4f;
        Color statusColor = GUI.color;
        Color warningColor = new Color(255, 255, 0, alpha); 
        Color dangerColor = new Color(255, 0, 0, alpha); 

        if (!collision3DHandler.detectEnter && !collision3DHandler.detectStay && !collision3DHandler.detectStay) { statusColor = warningColor; }
        if (collision3DHandler.targetDetect == TargetDetect.GameObject && collision3DHandler.targetObject == null) { statusColor = dangerColor; }
        if (collision3DHandler.targetDetect == TargetDetect.Tag && collision3DHandler.targetTag == "") { statusColor = dangerColor; }
        Collider collider = script.gameObject.GetComponent<Collider>();
        if (!collider) { statusColor = warningColor; }
        return statusColor;
    }

    private GUIStyle getToggleButtonStyle(bool pressed)
    {
        GUIStyle styleNormal = new GUIStyle();
        styleNormal = "MiniButton";

        GUIStyle styleToggled = new GUIStyle(styleNormal);
        styleToggled.normal.background = styleNormal.active.background;

        if (pressed) { return styleToggled; }
        else { return styleNormal; }
    }

    private void drawSeparator(int space = 5)
    {
        drawSpace(space);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        drawSpace(space);
    }
    private void drawSpace(int space = 5) { GUILayout.Space(space); }
}
