using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowHead : MonoBehaviour
{
    public Transform CameraHead;
    public Vector3 CamOffset = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = CameraHead.position + this.CamOffset;
        this.transform.rotation = CameraHead.rotation;
    }
}
