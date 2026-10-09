using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

namespace NComponent
{
    [DisallowMultipleComponent]
    public class NPause : MonoBehaviour
    {
        public bool handleCursor = true;
        public bool pauseAudios = true;
        public bool pauseVideos = true;
        public bool pauseAnimators = true;
        public bool pauseRigidbodies = true;

        public LayerMask excludeLayers;
        public List<GameObject> exclude = new List<GameObject>();

        private List<AudioSource> audiosPaused = new List<AudioSource>();
        private List<VideoPlayer> videosPaused = new List<VideoPlayer>();
        private List<Animator> animatorsPaused = new List<Animator>();
        private List<PausedRigidbody> rigidbodiesPaused = new List<PausedRigidbody>();

        // public bool pauseTime;

        [Header("Events")]
        public UnityEvent onPause = new UnityEvent();
        public UnityEvent onUnPause = new UnityEvent();

        private bool paused;

        private void nSetPause()
        {
            if (paused)
            {
                if (handleCursor) { nShowCursor(); }
                //Audios
                if (pauseAudios)
                {
                    AudioSource[] components = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
                    for (int i = 0; i < components.Length; i++)
                    {
                        AudioSource component = components[i];
                        if (component.isPlaying && !exclude.Contains(component.gameObject) && !nExcludedFromLayer(component.gameObject.layer))
                        {
                            audiosPaused.Add(component);
                            component.Pause();
                        }
                    }
                }
                //Videos
                if (pauseAudios)
                {
                    VideoPlayer[] components = FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
                    for (int i = 0; i < components.Length; i++)
                    {
                        VideoPlayer component = components[i];
                        if (component.isPlaying && !exclude.Contains(component.gameObject) && !nExcludedFromLayer(component.gameObject.layer))
                        {
                            videosPaused.Add(component);
                            component.Pause();
                        }
                    }
                }
                //Animators
                if (pauseAnimators)
                {
                    Animator[] components = FindObjectsByType<Animator>(FindObjectsSortMode.None);
                    for (int i = 0; i < components.Length; i++)
                    {
                        Animator component = components[i];
                        if (!exclude.Contains(component.gameObject) && !nExcludedFromLayer(component.gameObject.layer))
                        {
                            animatorsPaused.Add(component);
                            component.enabled = false;
                        }
                    }
                }
                //Rigidbodies
                if (pauseRigidbodies)
                {
                    Rigidbody[] components = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
                    for (int i = 0; i < components.Length; i++)
                    {
                        Rigidbody component = components[i];
                        if (!exclude.Contains(component.gameObject) && !nExcludedFromLayer(component.gameObject.layer))
                        {
                            PausedRigidbody pausedRigidbody = new PausedRigidbody(component);
                            // component.isKinematic = true;
                            rigidbodiesPaused.Add(pausedRigidbody);
                            component.Sleep();
                        }
                    }
                }

                onPause.Invoke();
                // Time.timeScale = 0;
                // AudioListener.pause = true;
            }
            else
            {
                if (handleCursor) { nHideCursor(); }
                //Audios
                for (int i = 0; i < audiosPaused.Count; i++)
                {
                    AudioSource component = audiosPaused[i];
                    if (component.gameObject.activeInHierarchy) { component.UnPause(); }
                }
                //Videos
                for (int i = 0; i < videosPaused.Count; i++)
                {
                    VideoPlayer component = videosPaused[i];
                    if (component.gameObject.activeInHierarchy && component.isPaused) { component.Play(); }
                }
                //Animators
                for (int i = 0; i < animatorsPaused.Count; i++)
                {
                    Animator component = animatorsPaused[i];
                    component.enabled = true;
                }
                //Rigidbodies
                for (int i = 0; i < rigidbodiesPaused.Count; i++)
                {
                    PausedRigidbody component = rigidbodiesPaused[i];
                    // if (component.unPause) { component.WakeUp(); }
                    component.nUnPause();
                }
                // Time.timeScale = 1;
                // AudioListener.pause = false;

                audiosPaused.Clear();
                videosPaused.Clear();
                animatorsPaused.Clear();
                rigidbodiesPaused.Clear();
                onUnPause.Invoke();
            }
        }

        private bool nExcludedFromLayer(int layer) { return excludeLayers == (excludeLayers | (1 << layer)); }

        public void nPause() { nPause(0); }
        public void nUnPause() { nUnPause(0); }
        public void nSwitchPause() { nSwitchPause(0); }

        public void nPause(float delay)
        {
            paused = true;
            if (delay > 0) { Invoke("setPause", delay); }
            else { nSetPause(); }
        }

        public void nUnPause(float delay)
        {
            paused = false;
            if (delay > 0) { Invoke("setPause", delay); }
            else { nSetPause(); }
        }

        public void nSwitchPause(float delay)
        {
            if (paused) { nUnPause(delay); }
            else { nPause(delay); }
        }

        public void nShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void nHideCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        [System.Serializable]
        public class PausedRigidbody
        {
            public Rigidbody rigidbody;
            public Vector3 velocity;
            public Vector3 angularVelocity;
            // private bool isKinematic;

            public PausedRigidbody(Rigidbody rb) { this.rigidbody = rb; nPause(); }

            public void nPause()
            {
                velocity = rigidbody.linearVelocity;
                angularVelocity = rigidbody.angularVelocity;
                rigidbody.isKinematic = true;
            }
            public void nUnPause()
            {
                rigidbody.isKinematic = false;
                rigidbody.AddForce(velocity, ForceMode.VelocityChange);
                rigidbody.AddTorque(angularVelocity, ForceMode.VelocityChange);
            }
        }
    }
}