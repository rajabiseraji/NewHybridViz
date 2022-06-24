using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MonitorBoardInteractions : MonoBehaviour, Grabbable
{

    public Visualization localCollidedVisualization;
    public Transform localCollidedVisualizationTransform;
    public WandController grabbingController;
    public WandController visualizationGrabbingController;
    public GameObject dotSphere;
    public GameObject dotCube;
    public GameObject VRCamera;
    public Vector3 positionToCreateExtrudedVis;

    public bool isBeingGrabbed = false;
    public bool isBeingDropped = false;
    public bool isControllerInsideMonitor = false;

    public int GetPriority()
    {
        // We want a low priority so that the controller doesn't grab this
        return 1;
    }

    public void OnDrag(WandController controller)
    {

    }

    public void OnEnter(WandController controller)
    {
        Debug.Log("Hey i have enetered the monitor!");
    }

    public void OnExit(WandController controller)
    {
        //throw new System.NotImplementedException();
    }

    public bool OnGrab(WandController controller)
    {

        grabbingController = controller;
        isBeingGrabbed = true;


        Debug.Log("Hey i have been grabbed by the controller" + Time.deltaTime);
        // here we need to send a websocket msg to tell the system that we are trying to extrude something in this position
        dotSphere.transform.position = controller.transform.position;
        // if a hand or a controller collided into us
        // get the result of the collision and make monitor raycast
        Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((controller.transform.position - transform.position), transform.forward);

        Vector3 dirForRaycast = (transform.position + projectedDistanceOnPlane) - controller.transform.position;
        var result = GetComponent<uDesktopDuplication.Texture>().RayCast(controller.transform.position, dirForRaycast);

        if (result.hit)
        {
            print("controller is grabbing the monitor from now");
            // make a cube or small sphere at the point that the two collide with one ancollidedObjectWithMe 

            print("Raycast has hit somethig");
            print(result.desktopCoord.x);
            print(result.desktopCoord.y);
            WebSocketMsg msg;
            msg = new WebSocketMsg(1,
                result.desktopCoord,
                "EXTRUDE",
                null);
            GameObject.FindGameObjectWithTag("WebSocketManager").GetComponent<WsClient>().SendMsgToDesktop(msg);
        }



        // This should be false if we don't want the controller to be able to move the monitor 
        // itself
        return false;
    }

    public void OnRelease(WandController controller)
    {
        isBeingGrabbed = false;
        grabbingController = null;

        Vector3 distanceVector = controller.transform.position - transform.position;
        float distanceAlongNormal = -Vector3.Dot(transform.forward, distanceVector);
        if (distanceAlongNormal < 0.3f)
        {
            print("NORMAL distance is " + distanceAlongNormal);
            print("Controller should be outside the monitor for release to work!!");
            return;
        }
        else
        {
            print("NORMAL distance is " + distanceAlongNormal);
            isControllerInsideMonitor = false;
        }

        dotCube.transform.position = controller.transform.position;
        positionToCreateExtrudedVis = controller.transform.position;

        Vector3 cameraAngles = VRCamera.transform.eulerAngles;
        dotCube.transform.rotation = Quaternion.Euler(0, cameraAngles.y, 0);

        //if (Vector3.Dot(VRCamera.transform.forward, dotCube.transform.forward) < 0)
        //{
        //    dotCube.transform.Rotate(dotCube.transform.up, 180f);
        //}

        print("telling the scene manager to create vis now");
        // This function works in this way: if there are two active Axes under the dekstop cursor, it will create a scatterplot, else it will just make a simple histogram
        SceneManager.Instance.CreateChart(controller.transform.position, dotCube.transform.rotation, dotCube.transform.forward, dotCube.transform.right, dotCube.transform.up);

        // here's the point where the trigger gets released, the things we do here are: 
        // 1- disable all sorts of collision interactions with the plane for 3, 4 seconds using a global flag
        // 2- make sure that we have received the info from the previous extrusion call made in the OnGrab method
        // 3- extract the name of the axes we have from the websocket msg 
        // 4- make a call to make those axes 
        // 5- optional, if you've showing some sort of a ghost when onGrab was called, make that ghost disappear

        Debug.Log("Hey i have been released into the wild!" + Time.time);
        //throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        VRCamera = GameObject.FindGameObjectWithTag("MainCamera");
        
        Debug.Assert((VRCamera != null), "In Monitor board: The VR Camera object cannot be null");

        dotSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dotSphere.transform.localScale = Vector3.one * 0.01f;
        dotSphere.GetComponent<Renderer>().material.color = Color.red;
        
        dotCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dotCube.transform.localScale = Vector3.one * 0.02f;
        dotCube.GetComponent<Renderer>().material.color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
        // Here's when we call functions that are to be called when the controller is not releasing anymore
        if(grabbingController == null || !grabbingController.gripping)
        {
            // This part is to handle what happens when we're grabbing something from the monitor to the outside
            if(isBeingGrabbed)
            {
                OnRelease(grabbingController);
            } 
        }
        
        if(visualizationGrabbingController != null && !visualizationGrabbingController.gripping)
        {
            // This part is to handle what happens when we're grabbing something from the monitor to the outside
            if(isBeingDropped)
            {
                DropVisInDesktop();
            }
            
        }


    }

    void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<WandController>())
        {
            isControllerInsideMonitor = true;
        }

        // in order to user the uddTexture.Raycast function
        // we need a to (Transform) and a from (Transform) to make a raycast

        // TODO: make this later
        // for now I'm just gonna detect a collision and that's it! 

        // if a visualization or axis object collieded with the monitor plane
        if (other.GetComponent<Visualization>())
        {
            Visualization collidedVisualization = other.GetComponent<Visualization>();
            
            if (!this.localCollidedVisualization)
            {
                this.localCollidedVisualization = collidedVisualization;
                this.localCollidedVisualizationTransform = other.transform;
                isBeingDropped = true;

                // set the vis grabbing controller
                visualizationGrabbingController = collidedVisualization.axes[0].grabbingController;

            } else if (this.localCollidedVisualization.GetInstanceID() == collidedVisualization.GetInstanceID())
            {
                //Debug.Log("they are equal");
                //Debug.Log("collided Axis instance ID is " + collidedAxis.GetInstanceID());
                //Debug.Log("axis instance ID is " + a.GetInstanceID());
                return;
            }

        }
    }

    private void OnTriggerExit(Collider other)
    {
        //if (other.GetComponent<WandController>())
        //{
        //    isControllerInsideMonitor = false;
        //}
        // here we need to check if we're just passing the monitor, in that case we want to
        // just set the isBeingDropped and isBeingDragged to false and nullify everything 
        // to reset

        // This part is to handle a visualization that is just passing through the monitor for whatever reason! 
        if (other.GetComponent<Visualization>() && (this.localCollidedVisualization || isBeingDropped))
        {
            this.localCollidedVisualization = null;
            visualizationGrabbingController = null;
            isBeingDropped = false;
        }
        
    }

    void DropVisInDesktop()
    {
        // once we're done with dropping we can just set this to false
        visualizationGrabbingController = null;
        isBeingDropped = false;


        //Debug.Log("they are NOOOOOOT equal");
        // Find the point of entrance
        List<Axis> axisList = this.localCollidedVisualization.axes;

        Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((this.localCollidedVisualization.transform.position - transform.position), transform.forward);

        Vector3 dirForRaycast = (transform.position + projectedDistanceOnPlane) - this.localCollidedVisualization.transform.position;
        var result = GetComponent<uDesktopDuplication.Texture>().RayCast(this.localCollidedVisualization.transform.position, dirForRaycast);

        Sequence seq = DOTween.Sequence();
        seq.Append(localCollidedVisualizationTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
        // test
        foreach (var axis in axisList)
        {
            seq.Join(axis.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
        }
        seq.AppendCallback(() => {

            // The visualization destroyer thingy will take care of the axes too
            localCollidedVisualizationTransform.GetComponent<Visualization>().DestroyVisualization();
        });

        if (result.hit)
        {
            print("I've hit somethig");
            print(result.desktopCoord.x);
            print(result.desktopCoord.y);
            WebSocketMsg msg;
            if (this.localCollidedVisualization.axes.Count == 1)
            {
                Axis XAxis = this.localCollidedVisualization.axes[0].IsHorizontal ? this.localCollidedVisualization.axes[0] : null;
                Axis YAxis = !this.localCollidedVisualization.axes[0].IsHorizontal ? this.localCollidedVisualization.axes[0] : null;
                msg = new WebSocketMsg(1,
                    result.desktopCoord,
                    this.localCollidedVisualization.axes.Count,
                    XAxis,
                    YAxis,
                    null,
                    this.localCollidedVisualization.name,
                    "CREATE");
            }
            else
            {
                msg = new WebSocketMsg(1,
                    result.desktopCoord,
                    localCollidedVisualization.axes.Count,
                    localCollidedVisualization.ReferenceAxis1.horizontal,
                    localCollidedVisualization.ReferenceAxis1.vertical,
                    localCollidedVisualization.ReferenceAxis1.depth,
                    localCollidedVisualization.name,
                    "CREATE");
            }
            GameObject.FindGameObjectWithTag("WebSocketManager").GetComponent<WsClient>().SendMsgToDesktop(msg);
        }
    }


}
