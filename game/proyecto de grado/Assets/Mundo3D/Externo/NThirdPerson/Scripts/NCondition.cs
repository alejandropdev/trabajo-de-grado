using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

namespace NComponent
{
    [DisallowMultipleComponent]
    public class NCondition : MonoBehaviour
    {
        public Condition condition;
        public bool evaluateOnStart;
        public bool evaluateOnChange = true;
        public int numExpected;
        public int numValue;
        public int numMin;
        public int numMax = 1;
        public bool boolExpected;
        public bool boolValue;
        public string textExpected;
        public string textValue;
        public bool textMatchCase;
        public UnityEvent onTrue;
        public UnityEvent onFalse;
        public UnityEvent<bool> onSetBool;
        public UnityEvent<string> onSetText;
        public UnityEvent<string> onSetNumText;
        public UnityEvent<int> onSetNum;

        private void Start() { if (evaluateOnStart) { nEvaluate(); } }

        public void nSetNumExpected(int val) { numExpected = val; }
        public void nSetBoolExpected(bool val) { boolExpected = val; }
        public void nSetTextExpected(string val) { textExpected = val; }

        public void nSetNumValue(int val, char operat)
        {
            int nextVal = numValue;

            if (operat == '=') { nextVal = val; }
            if (operat == '+') { nextVal += val; }
            if (operat == '-') { nextVal -= val; }

            if (nextVal < numMin) { nextVal = numMin; }
            if (nextVal > numMax) { nextVal = numMax; }

            bool changed = (nextVal != numValue);
            if (changed)
            {
                numValue = nextVal;
                if (evaluateOnChange) { nEvaluate(); }
                onSetNum?.Invoke(numValue);
                onSetNumText?.Invoke(numValue.ToString());
            }
        }
        public void nSetNumValueAdd(int val)
        {
            if (val > 0) { nSetNumValue(val, '+'); }
            else
            {
                val *= -1;
                nSetNumValue(val, '-');
            }
        }
        public void nSetNumValue(int val) { nSetNumValue(val, '='); }
        public void nSetNumValueIncrease(int val) { nSetNumValue(val, '+'); }
        public void nSetNumValueDecrease(int val) { nSetNumValue(val, '-'); }

        public void nSetBoolValue(bool val) { boolValue = val; onSetBool?.Invoke(val); if (evaluateOnChange) { nEvaluate(); } }
        public void nSetTextValue(string val) { textValue = val; onSetText?.Invoke(val); if (evaluateOnChange) { nEvaluate(); } }
        public void nSetTextValueAdd(string val) { textValue += val; onSetText?.Invoke(textValue); if (evaluateOnChange) { nEvaluate(); } }

        public void nEvaluate(int val) { nSetNumValue(val); nEvaluate(); }
        public void nEvaluate(bool val) { nSetBoolValue(val); nEvaluate(); }
        public void nEvaluate(string val) { nSetTextValue(val); nEvaluate(); }

        public void nEvaluate()
        {
            bool val = false;
            if (condition == Condition.ActiveInHierarchy) { val = gameObject.activeInHierarchy; }
            else if (condition == Condition.ActiveSelf) { val = gameObject.activeSelf; }
            else if (condition == Condition.BoolEqual) { val = (boolExpected == boolValue); }
            else if (condition == Condition.NumEqual) { val = (numExpected == numValue); }
            else if (condition == Condition.NumLower) { val = (numExpected > numValue); }
            else if (condition == Condition.NumGreater) { val = (numExpected < numValue); }
            else if (condition == Condition.UnityEditor) { val = Application.isEditor; }
            else if (condition == Condition.TextEqual)
            {
                string checkValue = textValue;
                string checkExpected = textExpected;

                if (!textMatchCase)
                {
                    checkValue = nFormatString(textValue);
                    checkExpected = nFormatString(textExpected);
                }

                val = (checkExpected == checkValue);

                NDebug.nLog($"TextEqual : (Expected ({checkExpected}) / Input ({checkValue}))", this);
            }
            if (val == true) { onTrue?.Invoke(); }
            else { onFalse?.Invoke(); }
        }

        public void nOnBool(bool val)
        {
            if (val) { nOnTrue(); }
            else { nOnFalse(); }
        }

        public void nOnSetBool(bool val) { onSetBool?.Invoke(val); }
        public void nOnSetText(string val) { onSetText?.Invoke(val); }
        public void nOnSetNum(int val) { onSetNum?.Invoke(val); }
        public void nOnSetNumText(int val) { onSetNumText?.Invoke(val.ToString()); }
        public void nOnTrue() { onTrue?.Invoke(); }
        public void nOnFalse() { onFalse?.Invoke(); }

        public string nFormatString(string input)
        {
            // Quitar acentos
            input = input.Normalize(NormalizationForm.FormD);
            input = Regex.Replace(input, "[^\\u0000-\\u007F]", "");

            // Convertir la cadena a minúsculas
            input = input.ToLower();

            // Eliminar espacios en blanco
            input = input.Replace(" ", "");

            // Eliminar caracteres especiales, excepto números
            input = Regex.Replace(input, "[^a-zA-Z0-9_.&()]+", "", RegexOptions.Compiled);

            return input;
        }

        public enum Condition
        {
            ActiveInHierarchy,
            ActiveSelf,
            BoolEqual,
            NumEqual,
            NumLower,
            NumGreater,
            TextEqual,
            UnityEditor
        }
    }
}