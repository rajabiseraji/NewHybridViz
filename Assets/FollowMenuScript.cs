using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMenuScript : MonoBehaviour
{
    public GameObject Camera;
    public bool FollowCamera = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // tHIS IS JUST A test I guess
        if (FollowCamera)
        { // Code for the menu to follow the camera.	
            Vector3 v = Camera.transform.position - transform.position;
            v.x = v.z = 0.0f;
            transform.LookAt(Camera.transform.position - v);
            transform.Rotate(0, 180, 0);
        }
    }
}
