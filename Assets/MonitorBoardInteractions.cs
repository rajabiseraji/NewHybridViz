using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MonitorBoardInteractions : MonoBehaviour, Grabbable
{

    public WandController grabbingController;
    public GameObject dotSphere;
    public GameObject dotCube;
    public GameObject VRCamera;
    public Vector3 positionToCreateExtrudedVis;

    public bool isBeingGrabbed = false;
    public bool isControllerInsideMonitor = false;

    public GameObject DropPromptPrefab = null;
    public GameObject DropPromptGameObject = null;

    public WsClient WebsocketManager;

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


        Debug.Log("From " + name + ": Hey i have been grabbed by the controller" + Time.deltaTime);
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
        WebsocketManager = GameObject.FindGameObjectWithTag("WebSocketManager").GetComponent<WsClient>();

        Debug.Assert((DropPromptPrefab != null), "In Monitor board: The Drop Prompt Prefab cannot be null");
        Debug.Assert((VRCamera != null), "In Monitor board: The VR Camera object cannot be null");
        Debug.Assert((WebsocketManager != null), "In Monitor board: The Websocket manager object cannot be null");


        DropPromptGameObject = Instantiate(DropPromptPrefab, transform);
        DropPromptGameObject.GetComponent<RectTransform>().localPosition += new Vector3(0, 0, -0.02f);
        DropPromptGameObject.SetActive(false);

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

        Debug.Assert((WebsocketManager != null), "In Monitor board: The Websocket manager object cannot be null");
        if(WebsocketManager == null)
            WebsocketManager = GameObject.FindGameObjectWithTag("WebSocketManager").GetComponent<WsClient>();

        // Here's when we call functions that are to be called when the controller is not releasing anymore
        if (grabbingController == null || !grabbingController.gripping)
        {
            // This part is to handle what happens when we're grabbing something from the monitor to the outside
            if(isBeingGrabbed)
            {
                OnRelease(grabbingController);
            } 
        }

    }

    void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<WandController>())
        {
            isControllerInsideMonitor = true;
        } else if (other.GetComponent<Axis>() || other.GetComponent<Visualization>())
        {
            print("just a vis got it!");
            // show the drop prompt 
            Debug.Assert(DropPromptGameObject != null, "In Monitor Plane: Drop Prompt gameobject is Null!");
            DropPromptGameObject.SetActive(true);
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<WandController>())
        {
            isControllerInsideMonitor = false;
        }
        else if (other.GetComponent<Axis>() || other.GetComponent<Visualization>())
        {
            // hide the drop prompt 
            Debug.Assert(DropPromptGameObject != null, "In Monitor Plane: Drop Prompt gameobject is Null!");
            DropPromptGameObject.SetActive(false);
        }
    }

    /*
     let's do the drag and drop in this way

     inside Visualization class, use OnTriggerEnter to find out if the Axis has collided with a MonitorPlane Game object
    Then set a flag inside the Visualization class that says "ImCollidingWithMonitorPlane = true" 
    
    Then, we go to the OnRelease function of the Vis class
    that function is going to be called once the controller let's go of the Visualization
    Here we check whether or not "ImCollidingWithMonitorPlane" is true, if yes then we call a function of the Monitor plane (which we can find by assiging it to a private Vis class field)
    That function receives the visualization reference, 
    it will send a desktop msg to WsClient
    then it will call the destroy function of the visualization
    and then we should be done! 

    Some of the things to have in mind, we need to change the value of ImCollidingWithMonitorPlane inside OnTriggerEnter and OnTriggerExit so that we don't call it by mistake.
     
     */

    public void DropVisInDesktop(List<Visualization> droppedVisualizations)
    {
        droppedVisualizations.ForEach((collidedVis) =>
        {
            // If it was already going to be sent to desktop by another Axis collision, set this dirty flag to not duplicate it
            print("is vis " + collidedVis.name  + " " + collidedVis.GetInstanceID());
            print("is duplicate vis" + collidedVis.isGoingToBeSentToDesktop + " " + collidedVis.GetInstanceID());
            if (collidedVis.isGoingToBeSentToDesktop == true)
            {
                return;
            }


            collidedVis.isGoingToBeSentToDesktop = true;
            List<Axis> axisList = collidedVis.axes;

            Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((collidedVis.transform.position - transform.position), transform.forward);

            Vector3 pointOffTheScreen = (transform.position + transform.forward * -0.3f);
            
            Vector3 dirForRaycast = (transform.position + projectedDistanceOnPlane) - pointOffTheScreen;

            var result = GetComponent<uDesktopDuplication.Texture>().RayCast(pointOffTheScreen, dirForRaycast);

            if (result.hit)
            {
                // hide the drop vis gameobject thingy
                DropPromptGameObject.SetActive(false);


                print("I've hit somethig");
                print(result.desktopCoord.x);
                print(result.desktopCoord.y);
                WebSocketMsg msg;
                if (axisList.Count == 1)
                {
                    Axis XAxis = axisList[0].IsHorizontal ? axisList[0] : null;
                    Axis YAxis = !axisList[0].IsHorizontal ? axisList[0] : null;
                    msg = new WebSocketMsg(1,
                        result.desktopCoord,
                        axisList.Count,
                        XAxis,
                        YAxis,
                        null,
                        collidedVis.name,
                        "CREATE");
                }
                else
                {
                    msg = new WebSocketMsg(1,
                        result.desktopCoord,
                        collidedVis.axes.Count,
                        collidedVis.ReferenceAxis1.horizontal,
                        collidedVis.ReferenceAxis1.vertical,
                        collidedVis.ReferenceAxis1.depth,
                        collidedVis.name,
                        "CREATE");
                }
                WebsocketManager.SendMsgToDesktop(msg);
                collidedVis.DestroyVisualization();
            }

        });

    }


}
