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

using UnityEngine;
using System.Collections;
using System.Threading;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#elif UNITY_2017_1_OR_NEWER
using VR = UnityEngine.Experimental.VR; 
#endif

/// <summary>
/// (Deprecated) Contains information about the user's preferences and body dimensions.
/// </summary>
public class OVRProfile : Object
{
	[System.Obsolete]
	public enum State
	{
		NOT_TRIGGERED,
		LOADING,
		READY,
		ERROR
	};

	[System.Obsolete]
	public string id { get { return "000abc123def"; } }
	[System.Obsolete]
	public string userName { get { return "Oculus User"; } }
	[System.Obsolete]
	public string locale { get { return "en_US"; } }

	public float ipd { get { return Vector3.Distance (OVRPlugin.GetNodePose (OVRPlugin.Node.EyeLeft, OVRPlugin.Step.Render).ToOVRPose ().position, OVRPlugin.GetNodePose (OVRPlugin.Node.EyeRight, OVRPlugin.Step.Render).ToOVRPose ().position); } }
	public float eyeHeight { get { return OVRPlugin.eyeHeight; } }
	public float eyeDepth { get { return OVRPlugin.eyeDepth; } }
	public float neckHeight { get { return eyeHeight - 0.075f; } }

	[System.Obsolete]
	public State state { get { return State.READY; } }
}
