using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NComponent
{
    public class NKeyEvent : MonoBehaviour
    {
        public KeyCode key;
        public bool doubleEvent;
        private bool isfinalDown = false;

        public UnityEvent onFirtsDown;
        public UnityEvent onSecondDown;

        public void nSetFinalDown(bool isfinalDown) { this.isfinalDown = isfinalDown; }
        public void nOnFirtsDown() { onFirtsDown?.Invoke(); }
        public void nOnSecondDown() { onSecondDown?.Invoke(); }


        // Update is called once per frame
        void Update()
        {
            if (NThirdPerson.EntradaNueva.TeclaPulsada(key))
            {
                // Debug.Log(isfinalDown);
                if (!isfinalDown)
                {
                    onFirtsDown.Invoke();
                    if (doubleEvent) { isfinalDown = true; }
                }
                else
                {
                    onSecondDown.Invoke();
                    isfinalDown = false;
                }
            }
        }
    }
}