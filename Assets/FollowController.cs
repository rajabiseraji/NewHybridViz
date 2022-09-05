using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FollowController : MonoBehaviour
{
    public WandController controller;
    public GameObject placeholderCube;

    public Vector3 optionalOffset = Vector3.zero;

    private Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        //Debug.Assert(controller != null, "Controller shouldn't be null");
        Debug.Assert(placeholderCube != null, "Placeholder Cube shouldn't be null");
        offset = placeholderCube.transform.position - transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position = placeholderCube.transform.position + optionalOffset;
        //transform.eulerAngles = placeholderCube.transform.eulerAngles;

        this.transform.position = controller.transform.position + this.optionalOffset;
        this.transform.rotation = controller.transform.rotation;
    }
}
