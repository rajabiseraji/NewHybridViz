using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonitorCubeOverlayScript : MonoBehaviour, Grabbable
{
    private static Color HOVER_GREEN = new Color(0, 1, 0, 0.32f);
    private static Color AFFIRMATION_GREEN = new Color(0, 1, 0, 0.6f);
    private static Color CAUTION_YELLOW = new Color(1, 1, 0, 0.32f);
    private static Color ERROR_RED = new Color(1, 0, 0, 0.6f);


    private ComponentListItem _component;
    public GameObject VRCamera;
    public ComponentListItem component { get => _component; set => _component = value; }

    private WandController grabbingController = null;

    public GameObject dotCube = null;

    GameObject ghostCube;

    Color defaultColor; 

    public int GetPriority()
    {
        return 2;
    }

    public void OnDrag(WandController controller)
    {
        //throw new System.NotImplementedException();
    }

    public void OnEnter(WandController controller)
    {
        //throw new System.NotImplementedException();
        GetComponent<Renderer>().material.color = CAUTION_YELLOW;
    }

    public void OnExit(WandController controller)
    {
        //throw new System.NotImplementedException();
        if(grabbingController == null)
            GetComponent<Renderer>().material.color = defaultColor;
    }

    public bool OnGrab(WandController controller)
    {
        // register action for logger
        DataLogger.Instance.LogActionData("ExtrusionFromDesktopStarted", gameObject, controller.gameObject);


        if (_component != null)
        {
            // print("here's the component: " + JsonUtility.ToJson(_component));
            GetComponent<Renderer>().material.color = AFFIRMATION_GREEN;

            grabbingController = controller;
            ghostCube.SetActive(true);
            ghostCube.transform.parent = grabbingController.transform;
        }
        else
            print("not initiated");
        // it shouldn't be moved by the controller so return false
        return false;
    }

    public void OnRelease(WandController controller)
    {
        if(_component != null)
        {
            dotCube.transform.position = controller.transform.position;

            ghostCube.SetActive(false);

            Vector3 distanceVector = controller.transform.position - transform.position;
            float distanceAlongNormal = -Vector3.Dot(transform.forward, distanceVector);
            if (distanceAlongNormal < 0.3f)
            {
                print("NORMAL distance is " + distanceAlongNormal);
                print("Controller should be outside the monitor for release to work!!");

                GetComponent<Renderer>().material.color = defaultColor;
                grabbingController = null;

                // register action for logger
                DataLogger.Instance.LogActionData("ExtrusionFromDesktopReleasedInvalidArea", gameObject, controller.gameObject);


                return;
            }
            else
            {
                print("NORMAL distance is " + distanceAlongNormal);
            }

            // register action for logger
            DataLogger.Instance.LogActionData("ExtrusionFromDesktopFinishedSuccess", gameObject, controller.gameObject);


            if (_component.xAttributeName != null)
                SceneManager.Instance.SetXToBeCreatedAxis(_component.xAttributeName);

            if(_component.yAttributeName != null)
                SceneManager.Instance.SetYToBeCreatedAxis(_component.yAttributeName);

            Vector3 cameraAngles = VRCamera.transform.eulerAngles;

            //dotCube.transform.rotation = Quaternion.Euler(0, cameraAngles.y, 0);
            //dotCube.transform.LookAt(dotCube.transform.position + Camera.main.transform.rotation * Vector3.forward,
            //Camera.main.transform.rotation * Vector3.up);

            print("telling the scene manager to create vis now");
            // This function works in this way: if there are two active Axes under the dekstop cursor, it will create a scatterplot, else it will just make a simple histogram
            SceneManager.Instance.CreateChart(
                controller.transform.position,
                dotCube.transform.rotation,
                dotCube.transform.forward,
                dotCube.transform.right,
                dotCube.transform.up,
                dotCube.transform
            );

        }


        GetComponent<Renderer>().material.color = defaultColor;
        grabbingController = null;
    }

    // Start is called before the first frame update
    void Start()
    {
        VRCamera = GameObject.FindGameObjectWithTag("MainCamera");

        dotCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dotCube.transform.localScale = Vector3.one * 0.02f;
        dotCube.GetComponent<Renderer>().material.color = Color.red;//


        defaultColor = GetComponent<Renderer>().material.color;

        ghostCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ghostCube.transform.parent = transform;
        ghostCube.transform.localScale = Vector3.one;
        ghostCube.transform.position = transform.position;
        ghostCube.transform.rotation = transform.rotation;
        ghostCube.GetComponent<Renderer>().material = GetComponent<Renderer>().material;

        ghostCube.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(!VRCamera)
            VRCamera = GameObject.FindGameObjectWithTag("MainCamera");

        // if the ghost cube is active, make it to be dragged by the controller
        if(grabbingController != null && ghostCube.activeSelf)
        {
            //ghostCube.transform.rotation = grabbingController.transform.rotation;
            //ghostCube.transform.position = grabbingController.transform.position + 
            Vector3 distanceVector = grabbingController.transform.position - transform.position;
            float distanceAlongNormal = -Vector3.Dot(transform.forward, distanceVector);
            // print("distance is " + distanceAlongNormal);
            if (distanceAlongNormal < 0.3f)
            {
                ghostCube.GetComponent<Renderer>().material.color = ERROR_RED;
            } else
            {
                ghostCube.GetComponent<Renderer>().material.color = AFFIRMATION_GREEN;
            }
        }

        // this is to detect if the grabbing controller has let go
        if(grabbingController != null && !grabbingController.gripping)
        {
            OnRelease(grabbingController);
        }
    }

    private void makeGhostCube(WandController controller)
    {
        

    }
}
