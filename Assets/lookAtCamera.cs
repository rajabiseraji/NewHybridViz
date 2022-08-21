using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lookAtCamera : MonoBehaviour
{
    public bool shouldLookAtCamra = true;
    public Transform VRCameraTransform;
    public Visualization parentVis = null;

    // Start is called before the first frame update
    void Start()
    {
        VRCameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Assert(parentVis != null, "Parent Vis of Legened Cannot be null");

        Debug.Assert(VRCameraTransform != null, "VR Camera for Legened Cannot be null");

        if (parentVis.viewType == Visualization.ViewType.Scatterplot3D && shouldLookAtCamra && VRCameraTransform != null)
        {
            //transform.rotation.SetLookRotation(VRCameraTransform.forward); //= Quaternion.LookRotation(VRCameraTransform.forward);
            transform.rotation = Quaternion.LookRotation(VRCameraTransform.forward, VRCameraTransform.up);
            //transform.Rotate
            //transform.LookAt(VRCameraTransform, Vector3.down);
        }
    }
}
