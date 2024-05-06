using UnityEngine;
using C2M2.Utils;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C2M2.Interaction
{
    /// <summary>
    /// Controls the scaling of a transform
    /// </summary>
    [RequireComponent(typeof(OVRGrabbable))]
    public class GrabRescaler : MonoBehaviour
    {
        private OVRGrabbable grabbable = null;
        private MeshRenderer meshrender = null;
        private SphereCollider pivotcollider = null;
        private Vector3 origScale;
        private Vector3 minScale;
        private Vector3 maxScale;
        public float scaler = 50f;
        public float scaleRate = 0.04f;
        public float minPercentage = 0.1f;
        public float maxPercentage = 5f;
        public bool xScale = true;
        public bool yScale = true;
        public bool zScale = true;
        public OVRInput.RawButton LeftX = OVRInput.RawButton.X;
        public KeyCode incKey = KeyCode.UpArrow;
        public KeyCode decKey = KeyCode.DownArrow;
        public KeyCode visibilityToggleKey = KeyCode.R;
        public Transform target = null;
        public Vector3 median = Vector3.zero;
        public List<Vector3> neuronPositions = new List<Vector3>(new Vector3[1]);
        public GameObject[] neuronList;
        private GameObject[] pivotPoints;
        public GameObject pivot;

        private float ChangeScaler
        {
            get
            {
                ///<returns>A float between -1 and 1, where -1 means the thumbstick y axis is completely down and 1 implies it is all the way up</returns>
                if (GameManager.instance.vrDeviceManager.VRActive) return (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y + OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y);
                else if (Input.GetKey(incKey) && !Input.GetKey(decKey)) return 1f;
                else if (Input.GetKey(decKey) && !Input.GetKey(incKey)) return -1f;
                return 0;
            }
        }

        ///<returns>A boolean of whether the joystick is pressed</returns>
        private void VisibilityCheck()
        {
            if (GameManager.instance.vrDeviceManager.VRActive)
            {
                if (OVRInput.GetDown(LeftX) && !OVRInput.Get(OVRInput.RawButton.Y))
                {
                    meshrender.enabled = !meshrender.enabled;
                    pivotcollider.enabled = !pivotcollider.enabled;
                }
            }
            else
            {
                if (Input.GetKeyDown(visibilityToggleKey))
                {
                    meshrender.enabled = !meshrender.enabled;
                    pivotcollider.enabled = !pivotcollider.enabled;
                }
            }
        }

        private void Start()
        {
            // transform refers to the transform of the SimulationSpace object which contains all neurons
            if (target == null) target = transform;

            grabbable = GetComponent<OVRGrabbable>();
            meshrender = GetComponent<MeshRenderer>();

            pivotPoints = GameObject.FindGameObjectsWithTag("NeuronPivotPoint");

            pivot = pivotPoints[0];

            // Set minScale and maxScale limits based on initial scale of the SimulationSpace object
            origScale = target.localScale;
            minScale = minPercentage * origScale;
            if (maxPercentage == float.PositiveInfinity) maxScale = Vector3.positiveInfinity;
            else maxScale = maxPercentage * origScale;
        }

        void Update()
        {
            // If the rescale buttons aren't pressed, do not rescale
            neuronList = GameObject.FindGameObjectsWithTag("Neuron");
            if (neuronList.Count() > 1) VisibilityCheck();
            GeometricMedian(target);
            if (!grabbable.isGrabbed && median == median)
            {
                SetNeuronParents(target);
                pivot.transform.position = median;
                if (ChangeScaler != 0) Rescale();
            }
            if (grabbable.isGrabbed)
            {
                SetNeuronParents(pivot.transform);
            }
        }

        public void Rescale()
        {
            // Use of scaleStep ensures pace of changes is consistent regardless of simulation FPS
            float scaleStep = scaler * Time.deltaTime;
            // Equivalent to exponential rescaling of scale by factor of scaleRate*ChangeScaler per frame
            Vector3 newLocalScale = target.localScale*Mathf.Pow(1f + scaleRate*ChangeScaler, scaleStep);

            // Makes sure the new scale is within the determined range
            newLocalScale = C2M2.Utils.Math.Clamp(newLocalScale, minScale, maxScale);

            RelativeScale(target, pivot.transform.position, newLocalScale);
        }

        // Use to rescale transform from a different pivot point
        private void RelativeScale(Transform target, Vector3 pivotPoint, Vector3 newScale)
        {
            // Calculate offset from pivot point and relative scale factor
            var localPivot = target.InverseTransformPoint(pivotPoint);

            // Calculate final position and rescale appropriately
            target.localScale = newScale;
            var newPivot = target.TransformPoint(localPivot);
            target.position += pivotPoint - newPivot;
        }

        // Determine if two lists of Vector3 objects are identical
        // Used to check if neurons have changed position since last rescale
        private static bool ContainsSameItems (List<Vector3> list1, List<Vector3> list2)
        {
            // If the lists don't have the same count, they can't be identical
            if (list1.Count != list2.Count) return false;
            // Otherwise, this uses SequenceEqual to compare the lists ordered by x, then y, then z coordinates
            return list1.OrderBy(v => v.x).ThenBy(v => v.y).ThenBy(v => v.z).SequenceEqual(list2.OrderBy(v => v.x).ThenBy(v => v.y).ThenBy(v => v.z));
        }

        private void SetNeuronParents(Transform transform)
        {
            if (neuronList[0].transform.parent != transform)
            {
                for (int i = 0; i < neuronList.Count(); i++)
                {
                    neuronList[i].transform.SetParent(transform);
                }
            }
        }

        ///<returns>The Geometric Median of all of the active neurons</returns>
        private Vector3 GeometricMedian(Transform target)
        {
            // Update the list of neurons to include all objects tagged "Neuron"
            // TODO: Create and track this in NDSimulationLoader by adding neurons to a list as they're generated
            int activeNeurons = neuronList.Count();

            // If there is only one neuron, geometric median is its position
            if (activeNeurons == 1)
            {
                median = neuronList[0].transform.position;
                return median;
            }

            // Initialize variables for neuron positions and temporary median value
            Vector3 tempMedian = Vector3.zero;
            List<Vector3> currentNeuronPositions = new List<Vector3>(new Vector3[activeNeurons]);
            List<Vector3> currentLocalNeuronPositions = new List<Vector3>(new Vector3[activeNeurons]);

            // Initialize tempMedian to the centroid (average position) while determining if neurons have moved since last rescale
            for (int i = 0; i < activeNeurons; i++)
            {
                // Add current neuron positions and global positions to the previously initialized lists
                currentNeuronPositions[i] = neuronList[i].transform.position;
                currentLocalNeuronPositions[i] = neuronList[i].transform.localPosition;

                if (i == activeNeurons-1) // If we've iterated through all neurons
                {
                    // If the neurons have the same global positions as the neurons from the previous call
                    if (ContainsSameItems(currentLocalNeuronPositions, neuronPositions))
                    {
                        return median; // Return and use previous geometric median to rescale
                    }
                    else neuronPositions = currentLocalNeuronPositions; // Otherwise store changed global neuron positions
                }
                tempMedian += currentNeuronPositions[i]; // tempMedian becomes the sum of coordinate values of all neurons
            }
            tempMedian /= (activeNeurons); // tempMedian is now the average of neuron coordinates
            /*
            // Implementation of Weiszfeld's Algorithm for iterative numerical approximation of Geometric Median
            const int maxIterations = 10;
            const float tolerance = 1e-5f;

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                Vector3 numerator = Vector3.zero;
                float denominator = 0;

                for (int i = 0; i < activeNeurons; i++)
                {
                    Vector3 pos = neuronList[i].transform.position;
                    float dist = Vector3.Distance(tempMedian, pos); // Distance between each neuron and last approximation

                    if (dist != 0)
                    {
                        numerator += pos / dist;
                        denominator += 1 / dist;
                    }
                }

                // New approximation is determined
                Vector3 newMedian = numerator / denominator;

                // Check if it's within the tolerance threshold for adjustments
                if (Vector3.Distance(newMedian, tempMedian) < tolerance)
                {
                    median = newMedian;
                    return median;
                }
                // Otherwise store it as the new approximation, and continue to next iteration
                else tempMedian = newMedian;
            }*/
            // After iteration limit, use most recent approximation even if tolerance hasn't been met
            median = tempMedian;
            Debug.Log("Recalculated Geometric Median");
            return median;
        }
    }
}