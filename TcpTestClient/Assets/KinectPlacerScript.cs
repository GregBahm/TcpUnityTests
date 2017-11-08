using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SteamVR_TrackedObject))]
public class KinectPlacerScript : MonoBehaviour 
{
    public bool Place;
    public SteamVR_TrackedObject TrackedObject;

    void Start()
    {
        TrackedObject = GetComponent<SteamVR_TrackedObject>();
    }

    void Update ()
    {
        int index = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.First, Valve.VR.ETrackedDeviceClass.GenericTracker);
        if (index > 0)
        {
            TrackedObject.index = (SteamVR_TrackedObject.EIndex)index;

            if (Place)
            {
                Transform child = transform.GetChild(0);
                if (child != null)
                {
                    child.localPosition = Vector3.zero;
                    child.localScale = Vector3.one;
                    child.localRotation = Quaternion.identity;
                    child.parent = null;
                }
            }
        }
        Place = false;
    }
}
