using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SwitcherController : MonoBehaviour
{
    public GameObject current;
    public bool onlyChilds = true;
    public UnityEvent onSwitch = new UnityEvent();
    private GameObject last;
    // private GameObject oldLast;
    private Type lastType;

    public void switchToAudio(GameObject target)
    {
        AudioSource audioSource = target.GetComponent<AudioSource>();
        switchTo(target, typeof(AudioSource));
    }
    public void switchTo(GameObject target) { switchTo(target, typeof(GameObject)); }
    public void switchTo(AudioSource audioSource) { switchTo(audioSource.gameObject, typeof(AudioSource)); }
    public void switchTo(Animator animator) { switchTo(animator.gameObject, typeof(Animator)); }

    public void switchTo(GameObject target, Type type)
    {
        if (target.transform.parent != transform && onlyChilds) { return; }
        lastType = type;
        target.SetActive(true);

        if (current != target) {  last = current; }
        current = target;

        if (type == typeof(Animator))
        {
            Animator component = null;
            if (last)
            {
                component = last.GetComponent<Animator>();
                if (component)
                {
                    component.ResetTrigger("show");
                    component.SetTrigger("hide");
                }
                // oldLast.SetActive(false);
            }
            component = current.GetComponent<Animator>();
            if (component)
            {
                component.ResetTrigger("hide");
                component.SetTrigger("show");
            }
        }
        else if (type == typeof(AudioSource))
        {
            AudioSource component = null;
            component = current.GetComponent<AudioSource>();
            if (component && !component.isPlaying) { component.Play(); }
            if (last) { last.SetActive(false); }            
        }        
        else
        {
            if (last) { last.SetActive(false); }
            current.SetActive(true);
        }
        onSwitch.Invoke();
    }

    public void disableCurrent()
    {
        if (lastType == null) { return; }

        if (lastType == typeof(Animator))
        {
            Animator component = current.GetComponent<Animator>();
            if (component)
            {
                component.ResetTrigger("show");
                component.SetTrigger("hide");
            }
        }
        else { current.SetActive(false); }

    }

    public void setActiveAll(bool val)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(val);
        }
    }
    public void setActiveLast(bool val)
    {
        if (!last) { return; }
        last.SetActive(val);
    }

    public void setActiveCurrent(bool val)
    {
        if (!current) { return; }
        current.SetActive(val);
    }

    // [ContextMenu("Next")]
    public void next()
    {
        int currentIndex = (current) ? current.transform.GetSiblingIndex() : 0;
        int maxIndex = transform.childCount - 1;
        int nextIndex = currentIndex + 1;
        if (nextIndex > maxIndex) { nextIndex = 0; }
        switchTo(transform.GetChild(nextIndex).gameObject);
    }

    // [ContextMenu("Prev")]
    public void prev()
    {
        int currentIndex = (current) ? current.transform.GetSiblingIndex() : 0;
        int maxIndex = transform.childCount - 1;
        int nextIndex = currentIndex - 1;
        if (nextIndex < 0) { nextIndex = maxIndex; }
        switchTo(transform.GetChild(nextIndex).gameObject);
    }
}
