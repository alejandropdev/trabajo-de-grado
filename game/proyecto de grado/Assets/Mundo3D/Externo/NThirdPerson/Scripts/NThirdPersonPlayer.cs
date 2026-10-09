using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using NThirdPerson;
using UnityEngine;
using UnityEngine.Events;

namespace AppScript.PlayerController
{
    [RequireComponent(typeof(CharacterController))]
    public class NThirdPersonPlayer : MonoBehaviour
    {
        public bool canRun = true;
        public float runSpeed = 5;
        public float walkSpeed = 3;
        public float gravity = 9.8f;
        public float mass = 2;
        public float jumpForce = 2;
        public int jumpsMax = 1;
        public float inAirDelay = 0.2f;

        public Camera cameraPlayer;
        public CinemachineFreeLook cinemachineLook;
        public float cineMachineSpeed = 1;
        public Animator anim;
        public bool lockCursor = true;

        // public GroundDetector groundDetector;

        [Header("Controls")]
        public KeyCode walkKey = KeyCode.LeftShift;
        public KeyCode jumpKey = KeyCode.Space;
        private bool isGrounded = false;
        private CharacterController controller;

        [Header("Events")]
        public UnityEvent onSaveCheckPoint = new UnityEvent();
        public UnityEvent onDeath = new UnityEvent();

        private Vector3 input;
        private Vector3 moveDirection;
        private Vector3 cameraDirection;
        private float fallVelocity;
        private int jumpsCount;
        private float lookSpeed = 0.1f;
        private float moveSpeed;

        private bool inAir;
        private float airTimeElapsed;
        private ControllerData controllerDataMax;
        private ControllerData controllerDataMin;
        private ControllerData controllerData;
        private Transform cameraPivot;
        private Rigidbody rb;
        private Rigidbody hitRigidbody;
        private CollisonEvents3D hitCollisonEvents;
        private Vector3 checkPoint;
        private Collider lastPlatform;

        private bool cinemachineUpdating = false;
        private float t = 0;
        public bool isPaused = false;

        public void nSetGravity(float gravity) { this.gravity = gravity; }
        public void nSetCanRun(bool canRun) { this.canRun = canRun; }
        public void nSetRunSpeed(float speed) { runSpeed = speed; }
        public void nSetWalkSpeed(float speed) { walkSpeed = speed; }
        public void nSetMass(float mass) { this.mass = mass; }
        public void nSetJumpForce(float jumpForce) { this.jumpForce = jumpForce; }
        public void nSetJumpsMax(int jumpsMax) { this.jumpsMax = jumpsMax; }
        public void nSetAirDelay(int inAirDelay) { this.inAirDelay = inAirDelay; }
        public void nSetCameraPlayer(Camera cameraPlayer) { this.cameraPlayer = cameraPlayer; }
        public void nSetCheckPoint(Transform target) { this.checkPoint = target.position; }
        public void nSaveCheckPoint() { this.checkPoint = transform.position; onSaveCheckPoint.Invoke(); }
        public void nKill() { onDeath.Invoke(); nLoadCheckPoint(); }
        public void nLoadCheckPoint() { transform.position = checkPoint; }
        public void nSetParent(Transform parent) { transform.SetParent(parent); }
        public void nUnparent() { transform.SetParent(null); }


        public void nLoadCheckPointDelayed(float delay)
        {
            if (delay > 0) { Invoke("nLoadCheckPoint", delay); }
            else { nLoadCheckPoint(); }
        }

        public void nSetLockedCursor(bool lockCursor)
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void nSetCineMachineMiddle(float val)
        {
            cinemachineLook.m_Orbits[1].m_Height = val;
        }

        public void nPause()
        {
            isPaused = true;
            cinemachineLook.enabled = false;
        }
        
        public void nUnPause()
        {
            isPaused = false;
            cinemachineLook.enabled = true;
        }


        private void Start()
        {
            controller = GetComponent<CharacterController>();

            controllerDataMax = new ControllerData(controller);
            controllerDataMin = new ControllerData(new Vector3(0, 0.19f, 0), 0.25f);

            nSetLockedCursor(lockCursor);
            nSetCheckPoint(transform);
        }


        private void Update()
        {
            if (isPaused) { return; }
            t  =  1 * Time.deltaTime;
            handleMove();
            handleGravity();
            handleJump();
            handleRotation();
        }

        private void handleMove()
        {
            input = new Vector3(EntradaNueva.Eje("Horizontal"), 0, EntradaNueva.Eje("Vertical"));
            input = Vector3.ClampMagnitude(input, 1);

            float mag = input.magnitude;
            if (EntradaNueva.Tecla(walkKey) && mag > 0) { mag = 0.9f; }
            moveSpeed = (mag > 0.95f && canRun) ? runSpeed : walkSpeed;

            moveDirection = (input.x * cameraPlayer.transform.right.normalized + input.z * cameraPlayer.transform.forward.normalized) * moveSpeed;
            moveDirection.y = fallVelocity;


            if (!canRun && mag > 0.95f) { mag = 0.9f; }

            anim.SetFloat("Move", mag);

            controller.Move(moveDirection * t);
            // Debug.Log(moveDirection.magnitude);

            // isGrounded = Physics.CheckSphere(groundDetector.transform.position, groundDetector.radius, groundDetector.layerMask) || controller.isGrounded;
            isGrounded = controller.isGrounded;

            if (isGrounded)
            {
                inAir = false;
                airTimeElapsed = 0;
            }
            else
            {
                airTimeElapsed += t;
                if (airTimeElapsed > inAirDelay) { inAir = true; }
            }
            anim.SetBool("Grounded", !inAir);
        }

        private void handleRotation()
        {
            if (input.magnitude >= 0.1f)
            {
                float lookAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + cameraPlayer.transform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, lookAngle, ref lookSpeed, 0.15f);
                transform.rotation = Quaternion.Euler(0, angle, 0);
                anim.transform.localPosition = Vector3.zero;
            }
        }

        private void handleJump()
        {
            if (isGrounded)
            {
                jumpsCount = 0;
                anim.transform.localPosition = Vector3.zero;
            }

            if (EntradaNueva.TeclaPulsada(jumpKey))
            {
                if (jumpsCount < jumpsMax)
                {
                    jumpsCount++;
                    fallVelocity = jumpForce;
                    moveDirection.y = fallVelocity;
                    inAir = true;
                    isGrounded = false;
                    anim.SetBool("Grounded", isGrounded);
                }
            }
        }

        private void handleGravity()
        {
            if (isGrounded) { fallVelocity = -gravity / 4; }
            else { fallVelocity -= gravity * t; }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Debug.Log(hit.gameObject,hit.gameObject);
            if (hitCollisonEvents) { hitCollisonEvents.callEvent(gameObject, "Collision", "Exit"); }

            if (!hitCollisonEvents) { hitCollisonEvents = hit.gameObject.GetComponent<CollisonEvents3D>(); }
            if (hitCollisonEvents) { hitCollisonEvents.callEvent(gameObject, "Collision", "Enter"); hitCollisonEvents = null; }

            hitRigidbody = hit.collider.attachedRigidbody;
            if (!hitRigidbody || hitRigidbody.isKinematic) { return; }
            if (hit.moveDirection.y < -0.3) { return; }

            Vector3 pushDir = hit.moveDirection;
            pushDir.y = 0;
            hitRigidbody.linearVelocity = pushDir * mass / hitRigidbody.mass;
        }


        [System.Serializable]
        public class ControllerData
        {
            public Vector3 center;
            // public float radius;
            public float height;

            public ControllerData(Vector3 center, float height)
            {
                this.center = center;
                this.height = height;
            }
            public ControllerData(CharacterController controller)
            {
                this.center = controller.center;
                this.height = controller.height;
                // this.radius = controller.radius;
            }
        }

        [System.Serializable]
        public class GroundDetector
        {
            public Transform transform;
            public float radius = 0.2f;
            public LayerMask layerMask;
        }
    }
}