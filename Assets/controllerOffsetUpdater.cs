using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class controllerOffsetUpdater : MonoBehaviour
{
    public Vector3 offsetThingy = Vector3.zero;
    public SteamVR_Behaviour_Pose poseScript;
    // Start is called before the first frame update
    void Start()
    {
        if (poseScript != null)
            poseScript.onTransformUpdatedEvent += TransformUpdated;
    }

    public void TransformUpdated(SteamVR_Behaviour_Pose pos, SteamVR_Input_Sources sources)
    {
        //var newPos = t.position + offsetThingy;
        //print("Im updating with" + pos.poseAction[sources].localPosition);
        //print("Im updating with" + t.rotation);
        //print("Im updating with" + newPos);
        transform.position = transform.parent.TransformPoint(pos.poseAction[sources].localPosition) + offsetThingy;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
