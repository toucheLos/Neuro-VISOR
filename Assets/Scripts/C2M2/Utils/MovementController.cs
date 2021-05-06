﻿using System.Collections;
using UnityEngine;

namespace C2M2.Utils
{
    /// <summary>
    /// Allow an object to be moved through keyboard controls
    /// </summary>
    public class MovementController : MonoBehaviour
    {
        public KeyCode forward = KeyCode.W;
        public KeyCode backward = KeyCode.S;
        public KeyCode left = KeyCode.A;
        public KeyCode right = KeyCode.D;
        public KeyCode upDown = KeyCode.LeftControl;
        public bool limitPos = false;
        public Vector3 maxPos = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public Vector3 minPos = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        private bool ForwardPress { get { return Input.GetKey(forward); } }
        private bool BackwardPress { get { return Input.GetKey(backward); } }
        private bool LeftPress { get { return Input.GetKey(left); } }
        private bool RightPress { get { return Input.GetKey(right); } }
        private bool UpDownPress { get { return Input.GetKey(upDown); } }

        private Coroutine moveRoutine = null;
        public bool isMoving { get; private set; } = false;
        public float speed = 0.01f;
        public Transform relativeTo = null;

        public float rotateSpeed = 2.0f;

        private float x = 0.0f;
        private float y = 0.0f;

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                Cursor.lockState = CursorLockMode.Locked;

                x += rotateSpeed * Input.GetAxis("Mouse X");
                y -= rotateSpeed * Input.GetAxis("Mouse Y");

                transform.eulerAngles = new Vector3(y, x, 0.0f);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }

        }

        private void OnEnable ()
        {
            EnableMovement();
        }

        private void OnDisable()
        {
            DisableMovement();
        }

        public void EnableMovement()
        {
            moveRoutine = StartCoroutine(Movement());
            isMoving = true;
        }
        public void DisableMovement()
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
            isMoving = false;
        }

        private IEnumerator Movement()
        {
            while (true)
            {
                float x = 0f, y = 0f, z = 0f;
                if (ForwardPress)
                    if (UpDownPress)
                        y += speed;             // Move up
                    else
                        z += speed;             // Move forward
                if (BackwardPress)
                    if (UpDownPress)
                        y -= speed;             // Move down 
                    else
                        z -= speed;             // Move backward
                if (LeftPress)
                    x -= speed;                 // Move left
                if (RightPress)
                    x += speed;                 // Move right

                if (relativeTo == null)
                    transform.Translate(new Vector3(x, y, z), Space.World);
                else
                    transform.Translate(new Vector3(x, y, z), relativeTo);

                if (limitPos) 
                    transform.position = transform.position.Clamp(minPos, maxPos);

                yield return new WaitForFixedUpdate();
            }
        }
    }
}