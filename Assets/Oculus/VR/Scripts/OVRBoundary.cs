/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus Utilities SDK License Version 1.31 (the "License"); you may not use
the Utilities SDK except in compliance with the License, which is provided at the time of installation
or download, or which otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at
https://developer.oculus.com/licenses/utilities-1.31

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/


using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

#if UNITY_2020_1_OR_NEWER
using UnityEngine.XR;

#elif UNITY_2017_2_OR_NEWER
using Boundary = UnityEngine.Experimental.XR.Boundary;
using UnityEngine.XR;

#elif UNITY_2017_1_OR_NEWER
using Boundary = UnityEngine.Experimental.VR.Boundary;
using UnityEngine.Experimental.VR;

#endif

/// <summary>
/// Provides access to the Oculus boundary system.
/// </summary>
public class OVRBoundary
{
	/// <summary>
	/// Specifies a tracked node that can be queried through the boundary system.
	/// </summary>
	public enum Node
	{
		HandLeft           = OVRPlugin.Node.HandLeft,  ///< Tracks the left hand node.
		HandRight          = OVRPlugin.Node.HandRight, ///< Tracks the right hand node.
		Head               = OVRPlugin.Node.Head,      ///< Tracks the head node.
	}

	/// <summary>
	/// Specifies a boundary type surface.
	/// </summary>
	public enum BoundaryType
	{
		OuterBoundary      = OVRPlugin.BoundaryType.OuterBoundary, ///< Outer boundary that closely matches the user's configured walls.
		PlayArea           = OVRPlugin.BoundaryType.PlayArea,      ///< Smaller convex area inset within the outer boundary.
	}

	/// <summary>
	/// Provides test results of boundary system queries.
	/// </summary>
	public struct BoundaryTestResult
	{
		public bool IsTriggering;                              ///< Returns true if the queried test would violate and/or trigger the tested boundary types.
		public float ClosestDistance;                          ///< Returns the distance between the queried test object and the closest tested boundary type.
		public Vector3 ClosestPoint;                           ///< Returns the closest point to the queried test object.
		public Vector3 ClosestPointNormal;                     ///< Returns the normal of the closest point to the queried test object.
	}

	/// <summary>
	/// Returns true if the boundary system is currently configured with valid boundary data.
	/// </summary>
	public bool GetConfigured()
	{
		if (OVRManager.loadedXRDevice == OVRManager.XRDevice.Oculus)
			return OVRPlugin.GetBoundaryConfigured();
		else
		{
		#if UNITY_2020_1_OR_NEWER
			List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
			SubsystemManager.GetInstances(subsystems);
			foreach (var subsystem in subsystems)
			{
				if (subsystem.GetTrackingOriginMode() != TrackingOriginModeFlags.Unknown)
				{
					return true;
				}
			}
			return false;
		#elif UNITY_2017_1_OR_NEWER
			return Boundary.configured;
		#else
			return false;
		#endif
		}
	}

	/// <summary>
	/// Returns the results of testing a tracked node against the specified boundary type.
	/// All points are returned in local tracking space shared by tracked nodes and accessible through OVRCameraRig's trackingSpace anchor.
	/// </summary>
	public OVRBoundary.BoundaryTestResult TestNode(OVRBoundary.Node node, OVRBoundary.BoundaryType boundaryType)
	{
		OVRPlugin.BoundaryTestResult ovrpRes = OVRPlugin.TestBoundaryNode((OVRPlugin.Node)node, (OVRPlugin.BoundaryType)boundaryType);

		OVRBoundary.BoundaryTestResult res = new OVRBoundary.BoundaryTestResult()
		{
			IsTriggering = (ovrpRes.IsTriggering == OVRPlugin.Bool.True),
			ClosestDistance = ovrpRes.ClosestDistance,
			ClosestPoint = ovrpRes.ClosestPoint.FromFlippedZVector3f(),
			ClosestPointNormal = ovrpRes.ClosestPointNormal.FromFlippedZVector3f(),
		};

		return res;
	}

	/// <summary>
	/// Returns the results of testing a 3d point against the specified boundary type.
	/// The test point is expected in local tracking space.
	/// All points are returned in local tracking space shared by tracked nodes and accessible through OVRCameraRig's trackingSpace anchor.
	/// </summary>
	public OVRBoundary.BoundaryTestResult TestPoint(Vector3 point, OVRBoundary.BoundaryType boundaryType)
	{
		OVRPlugin.BoundaryTestResult ovrpRes = OVRPlugin.TestBoundaryPoint(point.ToFlippedZVector3f(), (OVRPlugin.BoundaryType)boundaryType);

		OVRBoundary.BoundaryTestResult res = new OVRBoundary.BoundaryTestResult()
		{
			IsTriggering = (ovrpRes.IsTriggering == OVRPlugin.Bool.True),
			ClosestDistance = ovrpRes.ClosestDistance,
			ClosestPoint = ovrpRes.ClosestPoint.FromFlippedZVector3f(),
			ClosestPointNormal = ovrpRes.ClosestPointNormal.FromFlippedZVector3f(),
		};

		return res;
	}

	private static int cachedVector3fSize = Marshal.SizeOf(typeof(OVRPlugin.Vector3f));
	private static OVRNativeBuffer cachedGeometryNativeBuffer = new OVRNativeBuffer(0);
	private static float[] cachedGeometryManagedBuffer = new float[0];
	private List<Vector3> cachedGeometryList = new List<Vector3>();
	/// <summary>
	/// Returns an array of 3d points (in clockwise order) that define the specified boundary type.
	/// All points are returned in local tracking space shared by tracked nodes and accessible through OVRCameraRig's trackingSpace anchor.
	/// </summary>
	public Vector3[] GetGeometry(OVRBoundary.BoundaryType boundaryType)
	{
		if (OVRManager.loadedXRDevice != OVRManager.XRDevice.Oculus)
		{
		#if UNITY_2020_1_OR_NEWER
			List<Vector3> boundaryPoints = new List<Vector3>();
			List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
			SubsystemManager.GetInstances(subsystems);
			foreach (var subsystem in subsystems)
			{
				if (subsystem.TryGetBoundaryPoints(boundaryPoints))
				{
					return boundaryPoints.ToArray();
				}
			}
			return new Vector3[0];
		#elif UNITY_2017_1_OR_NEWER
			if (Boundary.TryGetGeometry(cachedGeometryList, (boundaryType == BoundaryType.PlayArea) ? Boundary.Type.PlayArea : Boundary.Type.TrackedArea))
			{
				Vector3[] arr = cachedGeometryList.ToArray();
				return arr;
			}
		#else
			Debug.LogError("This functionality is not supported in your current version of Unity.");
			return null;
		#endif
		}

		int pointsCount = 0;
		if (OVRPlugin.GetBoundaryGeometry2((OVRPlugin.BoundaryType)boundaryType, IntPtr.Zero, ref pointsCount))
		{
			if (pointsCount > 0)
			{
				int requiredNativeBufferCapacity = pointsCount * cachedVector3fSize;
				if (cachedGeometryNativeBuffer.GetCapacity() < requiredNativeBufferCapacity)
					cachedGeometryNativeBuffer.Reset(requiredNativeBufferCapacity);

				int requiredManagedBufferCapacity = pointsCount * 3;
				if (cachedGeometryManagedBuffer.Length < requiredManagedBufferCapacity)
					cachedGeometryManagedBuffer = new float[requiredManagedBufferCapacity];

				if (OVRPlugin.GetBoundaryGeometry2((OVRPlugin.BoundaryType)boundaryType, cachedGeometryNativeBuffer.GetPointer(), ref pointsCount))
				{
					Marshal.Copy(cachedGeometryNativeBuffer.GetPointer(), cachedGeometryManagedBuffer, 0, requiredManagedBufferCapacity);

					Vector3[] points = new Vector3[pointsCount];

					for (int i = 0; i < pointsCount; i++)
					{
						points[i] = new OVRPlugin.Vector3f()
						{
							x = cachedGeometryManagedBuffer[3 * i + 0],
							y = cachedGeometryManagedBuffer[3 * i + 1],
							z = cachedGeometryManagedBuffer[3 * i + 2],
						}.FromFlippedZVector3f();
					}

					return points;
				}
			}
		}

		return new Vector3[0];
	}

	/// <summary>
	/// Returns a vector that indicates the spatial dimensions of the specified boundary type. (x = width, y = height, z = depth)
	/// </summary>
	public Vector3 GetDimensions(OVRBoundary.BoundaryType boundaryType)
	{
		if (OVRManager.loadedXRDevice == OVRManager.XRDevice.Oculus)
		{
			// Use Oculus-specific API for dimensions
			return OVRPlugin.GetBoundaryDimensions((OVRPlugin.BoundaryType)boundaryType).FromVector3f();
		}
		else
		{
			#if UNITY_2020_1_OR_NEWER
			// Use XR Plugin Management in Unity 2020+ for boundary points and dimensions
			List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
			SubsystemManager.GetInstances(subsystems);
			
			foreach (var subsystem in subsystems)
			{
				List<Vector3> boundaryPoints = new List<Vector3>();
				
				if (subsystem.TryGetBoundaryPoints(boundaryPoints) && boundaryPoints.Count > 0)
				{
					// Calculate the boundary size by finding the max and min points
					Vector3 minPoint = boundaryPoints[0];
					Vector3 maxPoint = boundaryPoints[0];
					
					foreach (var point in boundaryPoints)
					{
						minPoint = Vector3.Min(minPoint, point);
						maxPoint = Vector3.Max(maxPoint, point);
					}
					
					Vector3 dimensions = maxPoint - minPoint;
					return new Vector3(dimensions.x, 0, dimensions.z); // Assuming x and z are the dimensions
				}
			}
			return Vector3.zero;

			#elif UNITY_2017_1_OR_NEWER
			// Fallback for Unity 2017-2019 using the older Boundary API
			Vector3 dimensions;
			if (Boundary.TryGetDimensions(out dimensions, (boundaryType == BoundaryType.PlayArea) ? Boundary.Type.PlayArea : Boundary.Type.TrackedArea))
			{
				return dimensions;
			}
			return Vector3.zero;

			#else
			return Vector3.zero;
			#endif
		}
	}



	/// <summary>
	/// Returns true if the boundary system is currently visible.
	/// </summary>
	public bool GetVisible()
	{
		if (OVRManager.loadedXRDevice == OVRManager.XRDevice.Oculus)
			return OVRPlugin.GetBoundaryVisible();
		else
		{
		#if UNITY_2020_1_OR_NEWER
			List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
			SubsystemManager.GetInstances(subsystems);
			foreach (var subsystem in subsystems)
			{
				if (subsystem.GetTrackingOriginMode() == TrackingOriginModeFlags.Floor)
				{
					return true;
				}
			}
			return false;
		#elif UNITY_2017_1_OR_NEWER
			return Boundary.visible;
		#else
			return false;
		#endif
		}
	}

	/// <summary>
	/// Requests that the boundary system visibility be set to the specified value.
	/// The actual visibility can be overridden by the system (i.e., proximity trigger) or by the user (boundary system disabled)
	/// </summary>
	public void SetVisible(bool value)
	{
		if (OVRManager.loadedXRDevice == OVRManager.XRDevice.Oculus)
		{
			OVRPlugin.SetBoundaryVisible(value);
		}
		else
		{
		#if UNITY_2020_1_OR_NEWER
			// In Unity 2020+, use the XRInputSubsystem to handle boundary visibility
			List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
			SubsystemManager.GetInstances(subsystems);

			foreach (var subsystem in subsystems)
			{
				subsystem.TrySetTrackingOriginMode(value ? TrackingOriginModeFlags.Floor : TrackingOriginModeFlags.Unknown);
			}

		#elif UNITY_2017_1_OR_NEWER
			Boundary.visible = value;
		#endif
		}
	}
}