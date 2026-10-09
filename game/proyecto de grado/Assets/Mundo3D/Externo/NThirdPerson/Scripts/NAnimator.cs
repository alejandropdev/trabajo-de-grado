using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace NComponent
{
    [RequireComponent(typeof(Animator))]
    public class NAnimator : MonoBehaviour
    {
        public float speed = 1;
        public ClipHandler showClip;
        public ClipHandler hideClip;

        public List<ClipHandler> clipHandlers = new List<ClipHandler>();

        private Animator animator;
        private AnimatorClipInfo[] animatorinfo;
        private AnimatorStateInfo animatorState;
        private string prevAnimation;
        private string currentAnimation;
        private float startHandleTime;
        private float clipCurrentTime;
        private float animatorTime;
        private float animatorSpeed;
        private string lastTrigger;
        private bool isControled;
        private ClipHandler handleClip;

        public void nSetLastTrigger(string trigger) { lastTrigger = trigger; }

        private void Start() { animator = GetComponent<Animator>(); }

        private void Update()
        {
            // Debug.Log(currentAnimation);
            if (!animator || isControled) { return; }
            animatorState = animator.GetCurrentAnimatorStateInfo(0);
            animatorinfo = animator.GetCurrentAnimatorClipInfo(0);

            animatorTime = animatorState.normalizedTime;
            clipCurrentTime = animatorTime - startHandleTime;
            currentAnimation =  animatorinfo[0].clip.name;

            if (currentAnimation != prevAnimation)
            {
                clipCurrentTime = 0;
                startHandleTime = animatorTime;
            }

            handleClip = getClipHandler(animatorinfo[0].clip);
            if (currentAnimation != "" && handleClip == null) { return; }

            clipCurrentTime = animatorTime - startHandleTime;
            prevAnimation = currentAnimation;

            if (clipCurrentTime == 0)
            {
                handleClip.onStart.Invoke();
                handleClip.finished = false;
                animatorSpeed = animator.speed;
            }
            else if (clipCurrentTime > 0 && clipCurrentTime < 1)
            {
                animator.speed = speed;
                handleClip.onPlaying.Invoke(clipCurrentTime);
            }
            else
            {
                if (!handleClip.clip.isLooping && !handleClip.finished)
                {
                    handleClip.onEnd.Invoke();
                    handleClip.finished = true;
                    animator.speed = animatorSpeed;                    
                    prevAnimation = "";
                    if (handleClip.disable) { gameObject.SetActive(false); handleClip.finished = false; }
                }
            }
        }

        private bool isReady()
        {
            bool ready = true;
            if (!animator) { animator = GetComponent<Animator>(); }
            if (!animator) { ready = false; }
            // if (!gameObject.activeInHierarchy) { ready = false; }
            return ready;
        }

        public void nShow()
        {
            if (!isReady()) { return; }
            if (showClip == null) { return; }
            if (!showClip.clip && showClip.stateName.Length < 1) { return; }
            gameObject.SetActive(true);
            if (showClip.clip) { nPlayAnimation(showClip.clip); }
            else if (showClip.stateName.Length > 0) { nPlayAnimation(showClip.stateName); }
        }

        public void nHide()
        {
            if (!isReady()) { return; }
            if (hideClip == null) { return; }
            if (!hideClip.clip && hideClip.stateName.Length < 1) { return; }
            gameObject.SetActive(true);
            if (hideClip.clip) { nPlayAnimation(hideClip.clip); }
            else if (hideClip.stateName.Length > 0) { nPlayAnimation(hideClip.stateName); }
        }

        public void nSetTrigger(string trigger)
        {
            if (!isReady()) { return; }
            if (lastTrigger == trigger) { return; }
            animator.SetTrigger(trigger);
        }

        public void nSetSpeed(float speed)
        {
            if (!isReady()) { return; }
            animator.speed = speed;
            this.speed = speed;
        }

        public void nPlayAnimation(string clipName)
        {
            if (!isReady()) { return; }
            isControled = false;
            animator.Play(clipName);
        }
        public void nPlayAnimation(AnimationClip clip)
        {
            if (!isReady()) { return; }
            isControled = false;
            animator.speed = speed;
            nPlayAnimation(clip.name);
        }

        public void nControlAnimation(float time)
        {
            if (!isReady()) { return; }
            animator.speed = 0;
            isControled = true;
            animatorState = animator.GetCurrentAnimatorStateInfo(0);
            if (animatorState.length <= animatorState.normalizedTime) { animator.Play(animator.runtimeAnimatorController.animationClips[0].name); }
            animatorinfo = animator.GetCurrentAnimatorClipInfo(0);
            time = Mathf.Clamp(time, 0, 1);
            animator.Play(0, 0, time);
        }

        private ClipHandler getCurrentClip()
        {
            ClipHandler clipHandle = null;

            if (showClip.clip && animatorState.IsName(showClip.clip.name)) { clipHandle = showClip; }
            else if (hideClip.clip && animatorState.IsName(hideClip.clip.name)) { clipHandle = hideClip; }
            if (clipHandle != null) { return clipHandle; }

            for (int i = 0; i < clipHandlers.Count; i++)
            {
                if (!clipHandlers[i].clip) { continue; }
                if (animatorState.IsName(clipHandlers[i].clip.name)) { clipHandle = clipHandlers[i]; }
                if (clipHandle != null) { break; }
            }
            return clipHandle;
        }

        private ClipHandler getClipHandler(AnimationClip clip)
        {
            ClipHandler found = null;
            if (showClip.clip && showClip.clip.name == clip.name) { found = showClip; }
            else if (hideClip.clip && hideClip.clip.name == clip.name) { found = hideClip; }
            if (found != null) { return found; }

            for (int i = 0; i < clipHandlers.Count; i++)
            {   
                ClipHandler clipHandler = clipHandlers[i];
                if (clipHandler.clip && clipHandler.clip.name == clip.name) { found = clipHandler; }
                else if (clipHandler.stateName.Length > 0 && clipHandler.stateName == clip.name) { clipHandler.clip = clip; found = clipHandler; }
                if (found != null) { break; }
            }
            return found;
        }

        [System.Serializable]
        public class ClipHandler
        {
            public AnimationClip clip;
            public string stateName = "";
            public bool disable = false;
            public bool finished = false;
            public UnityEvent onStart = new UnityEvent();
            public UnityEvent<float> onPlaying = new UnityEvent<float>();
            public UnityEvent onEnd = new UnityEvent();
            public bool expanded = true;
        }
    }
}