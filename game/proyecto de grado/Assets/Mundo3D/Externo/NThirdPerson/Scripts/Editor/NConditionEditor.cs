using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace NComponent
{

    [CustomEditor(typeof(NCondition))]
    [CanEditMultipleObjects]
    public class NConditionEditor : Editor
    {
        private NCondition script;
        private SerializedProperty monoscript;
        private SerializedProperty conditionProp;
        private SerializedProperty evaluateOnStartProp;
        private SerializedProperty evaluateOnChangeProp;
        private SerializedProperty numExpectedProp;
        private SerializedProperty numValueProp;
        private SerializedProperty numMinProp;
        private SerializedProperty numMaxProp;
        private SerializedProperty boolExpectedProp;
        private SerializedProperty boolValueProp;
        private SerializedProperty textExpectedProp;
        private SerializedProperty textValueProp;
        private SerializedProperty textMatchCaseProp;
        private SerializedProperty onTrueProp;
        private SerializedProperty onFalseProp;
        private SerializedProperty onSetBoolProp;
        private SerializedProperty onSetTextProp;
        private SerializedProperty onSetNumProp;
        private SerializedProperty onSetNumTextProp;

        void OnEnable()
        {
            script = target as NCondition;
            monoscript = serializedObject.FindProperty("m_Script");
            conditionProp = serializedObject.FindProperty("condition");
            evaluateOnStartProp = serializedObject.FindProperty("evaluateOnStart");
            evaluateOnChangeProp = serializedObject.FindProperty("evaluateOnChange");
            numExpectedProp = serializedObject.FindProperty("numExpected");
            numValueProp = serializedObject.FindProperty("numValue");
            numMinProp = serializedObject.FindProperty("numMin");
            numMaxProp = serializedObject.FindProperty("numMax");
            boolExpectedProp = serializedObject.FindProperty("boolExpected");
            boolValueProp = serializedObject.FindProperty("boolValue");
            textExpectedProp = serializedObject.FindProperty("textExpected");
            textValueProp = serializedObject.FindProperty("textValue");
            textMatchCaseProp = serializedObject.FindProperty("textMatchCase");
            onTrueProp = serializedObject.FindProperty("onTrue");
            onFalseProp = serializedObject.FindProperty("onFalse");
            onSetBoolProp = serializedObject.FindProperty("onSetBool");
            onSetTextProp = serializedObject.FindProperty("onSetText");
            onSetNumProp = serializedObject.FindProperty("onSetNum");
            onSetNumTextProp = serializedObject.FindProperty("onSetNumText");
        }

        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();
            //base.OnInspectorGUI();
            serializedObject.Update();
            GUI.enabled = false;
            EditorGUILayout.PropertyField(monoscript, true, new GUILayoutOption[0]);
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(target, "NCondition Edited");

            EditorGUILayout.PropertyField(conditionProp);
            EditorGUILayout.PropertyField(evaluateOnStartProp);
            EditorGUILayout.PropertyField(evaluateOnChangeProp);

            if (script.condition == NCondition.Condition.NumEqual || script.condition == NCondition.Condition.NumGreater || script.condition == NCondition.Condition.NumLower)
            {
                if (script.numMax < script.numExpected) { script.numMax = script.numExpected; EditorUtility.SetDirty(target); }
                EditorGUILayout.PropertyField(numExpectedProp, new GUIContent("Expected"));
                EditorGUILayout.PropertyField(numMinProp, new GUIContent("Min"));
                EditorGUILayout.PropertyField(numMaxProp, new GUIContent("Max"));                
                EditorGUILayout.PropertyField(numValueProp, new GUIContent("Value"));                
                GUILayout.Space(10);
                EditorGUILayout.PropertyField(onSetNumTextProp);
                EditorGUILayout.PropertyField(onSetNumProp);

            }
            else if (script.condition == NCondition.Condition.BoolEqual)
            {
                EditorGUILayout.PropertyField(boolExpectedProp, new GUIContent("Expected"));                
                EditorGUILayout.PropertyField(boolValueProp, new GUIContent("Value"));                
                GUILayout.Space(10);
                EditorGUILayout.PropertyField(onSetBoolProp);
            }
            else if (script.condition == NCondition.Condition.TextEqual)
            {
                EditorGUILayout.PropertyField(textExpectedProp, new GUIContent("Expected"));
                EditorGUILayout.PropertyField(textMatchCaseProp, new GUIContent("Match Case"));                
                EditorGUILayout.PropertyField(textValueProp, new GUIContent("Value"));                
                GUILayout.Space(10);
                EditorGUILayout.PropertyField(onSetTextProp);
                // GUILayout.Space(10);
                // EditorGUILayout.PropertyField(onSetTextProp);
            }

            EditorGUILayout.PropertyField(onTrueProp);
            EditorGUILayout.PropertyField(onFalseProp);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
            }
        }

        public static void drawSeparator(int prevSpace = 0, int nextSpace = 0)
        {
            GUILayout.Space(prevSpace);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            GUILayout.Space(nextSpace);
        }
    }

}