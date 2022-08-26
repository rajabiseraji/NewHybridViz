using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using System.Linq;
using Valve.VR;

public interface Grabbable
{
    int GetPriority();

    // return true if this grabbable is actually grabbable and should attach to the controller
    bool OnGrab(WandController controller);
    void OnRelease(WandController controller);
    void OnDrag(WandController controller);

    void OnEnter(WandController controller);
    void OnExit(WandController controller);
}

public interface Brushable
{
    void OnBrush(WandController controller, Vector3 position, bool is3D);
    void OnBrushRelease(WandController controller);
    void OnDetailOnDemand(WandController controller, Vector3 position, Vector3 localPosition);
    void OnDetailOnDemandRelease(WandController controller);

}

public class WandController : MonoBehaviour
{
    //public OVRInput.Controller OculusController;

    public bool isOculusRift = false;
    //Debug test
    // This is the game object that will be shown when brushing the data
    GameObject brushingPoint;

    //Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    //Valve.VR.EVRButtonId padButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;

    /*
      This is for handling the new Steam VR input
     */
    public SteamVR_Action_Boolean triggerGrabAction;
    public SteamVR_Action_Boolean gripGrabAction;
    public SteamVR_Action_Boolean touchpadLeftAction;
    public SteamVR_Action_Boolean touchpadRightAction;
    public SteamVR_Action_Boolean touchpadUpAction;
    public SteamVR_Action_Boolean touchpadDownAction;

    // tells us which hand it is
    public SteamVR_Input_Sources handType;

    // this is for the haptic action 
    public SteamVR_Action_Vibration hapticAction;

    bool isTouchDown;

    public bool gripDown = false;
    public bool gripUp = false;
    public bool gripping = false;
    public bool padPressDown = false;
    public bool padPressUp = false;


    Collider intersectingCollider;
    List<Collider> intersectingGrabbables = new List<Collider>();

    List<GameObject> draggingObjects = new List<GameObject>();

    Collider brushableCollider;

    // The objects that are being tracked in the code
    // initially it's just a list of 10 (0, 0, 0) vectors
    List<Vector3> tracking = new List<Vector3>();

    //touch pad interaction
    float previousYValuePad = 0f;
    float incrementYValuePad = 0f;
    float yvaluePadTouchDown = 0f;

    GameObject currentBrushView = null;
    GameObject currentDetailView = null;

    public Material theBrushingMaterial;

    public Vector3 Velocity
    {
        get
        {
            return tracking[0] - tracking[tracking.Count - 1];
        }
    }

    [SerializeField] UnityEvent OnLeftPadPressed;
    [SerializeField] UnityEvent OnRightPadPressed;

    void Start()
    {
        //if (!isOculusRift) controller = SteamVR_Controller.Input((int)trackedObject.index); 

        // this is the part that creates the brushing point 
        triggerGrabAction.AddOnStateDownListener(handleGripDown, handType);
        triggerGrabAction.AddOnStateUpListener(handleGripUp, handType);
        touchpadDownAction.AddOnStateDownListener(handleTouchpadDownDirectionDown, handType);
        touchpadDownAction.AddOnStateUpListener(handleTouchpadDownDirectionUp, handType);
        touchpadUpAction.AddOnStateDownListener(handleTouchpadUpDirectionDown, handType);
        touchpadUpAction.AddOnStateUpListener(handleTouchpadUpDirectionUp, handType);
        touchpadLeftAction.AddOnStateDownListener(handleTouchpadLeft, handType);
        touchpadRightAction.AddOnStateDownListener(handleTouchpadRight, handType);

        brushingPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        brushingPoint.transform.localScale = new Vector3(0.01f, 0.01f, 0.0f);

        brushingPoint.GetComponent<SphereCollider>().enabled = false;// isTrigger = false;
        brushingPoint.GetComponent<MeshRenderer>().material = theBrushingMaterial;

    }

    void Awake()
    {

        //trackedObject = GetComponent<SteamVR_TrackedObject>();
        tracking.AddRange(Enumerable.Repeat<Vector3>(Vector3.zero, 10));
    }

    public void handleGripDown(
        SteamVR_Action_Boolean fromAction,
        SteamVR_Input_Sources fromSource
    )
    {
        //Debug.Log("Trigger is down pressed");
        gripping = true;
        if (intersectingGrabbables.Any(x => x != null) && draggingObjects.Count == 0)
        {
            var potentialDrags = intersectingGrabbables.Where(x => x != null).ToList();
            potentialDrags.Sort((x, y) => y.GetComponent<Grabbable>().GetPriority() - x.GetComponent<Grabbable>().GetPriority());
            if (potentialDrags.Count() > 0)
            {
                PropergateOnGrab(potentialDrags.First().gameObject);
            }
        }
    }
    
    public void handleGripUp(
        SteamVR_Action_Boolean fromAction,
        SteamVR_Input_Sources fromSource
    )
    {
        Debug.Log("Trigger is up pressed " + Time.realtimeSinceStartup + " count is " + draggingObjects.Count);
        if (draggingObjects.Count > 0)
        {
            draggingObjects.Where(x => x != null).ForEach(x => x.GetComponent<Grabbable>().OnRelease(this));
            draggingObjects.Clear();
        }
        gripping = false;
    }
    
    public void handleTouchpadLeft(
        SteamVR_Action_Boolean fromAction,
        SteamVR_Input_Sources fromSource
    )
    {
        Debug.Log("Touchpad left is pressed");

        // Do this: 
        // OnLeftPadPressed.Invoke();

        //    if(controller.GetAxis().x != 0 && controller.GetAxis().y != 0) {
        //        if(controller.GetAxis().x < 0)
        //            OnLeftPadPressed.Invoke();
        //        else if(controller.GetAxis().x >= 0)
        //            OnRightPadPressed.Invoke();  
        //    }
    }
    public void handleTouchpadRight(
        SteamVR_Action_Boolean fromAction,
        SteamVR_Input_Sources fromSource
    )
    {
        Debug.Log("Touchpad right is pressed");
    }
    public void handleTouchpadUpDirectionDown(
        SteamVR_Action_Boolean fromAction,
        SteamVR_Input_Sources fromSource
    )
    {
        Debug.Log("Touchpad Up is pressed");
        padPressUp = true;
    }
    public void handleTouchpadUpDirectionUp(
        SteamVR_Action_Boolean fromAction,
        SteamVR_Input_Sources fromSource
    )
    {
        Debug.Log("Touchpad Up is released!");
        padPressUp = true;
    }

    public void handleTouchpadDownDirectionDown(
        SteamVR_Action_Boolean fromAction,
        SteamVR_Input_Sources fromSource
    )
    {
        Debug.Log("Touchpad down is down pressed");
        padPressDown = true;
    }
    public void handleTouchpadDownDirectionUp(
        SteamVR_Action_Boolean fromAction,
        SteamVR_Input_Sources fromSource
    )
    {
        Debug.Log("Touchpad down is released!");
        padPressDown = false;
    }




    public void PropergateOnGrab(GameObject g)
    {
        if (g.GetComponent<Grabbable>() != null && g.GetComponent<Grabbable>().OnGrab(this))
        {
            draggingObjects.Add(g.gameObject);
        }
    }

    GameObject cube = null;

    void Update()
    {
        
        if (gripping && draggingObjects.Count > 0)
        {
            draggingObjects.Where(x => x != null).ForEach(x => x.GetComponent<Grabbable>().OnDrag(this));           
        }
        
        //if (draggingObjects.Count > 0)
        //{
        //    //if(!isOculusRift)
        //    //controller.TriggerHapticPulse(100);
        //}

        //brush actions : SteamVR_Controller.ButtonMask.Grip
        
        #region details on demand
        //detail on demand actions
        // this is the details on pressing the touch button
        // TODO: change the way it's visualized, including its colors and all!
        if (VisualisationAttributes.detailsOnDemand)
        {
            if (padPressDown)
            {
                bool detail3Dscatterplots = false;
                GameObject[] listCandidatesBrush3D = GameObject.FindGameObjectsWithTag("Scatterplot3D");
                for (int i = 0; i < listCandidatesBrush3D.Length; i++)
                {
                    {
                        if (Vector3.Distance(listCandidatesBrush3D[i].transform.position, transform.position) < 0.3f)
                        {
                            detail3Dscatterplots = true;
                            brushingPoint.gameObject.SetActive(true);
                            //brushingPoint.transform.localScale = new Vector3(brushingPoint.transform.localScale.x, brushingPoint.transform.localScale.y, 0.01f);

                            currentDetailView = listCandidatesBrush3D[i];
                            // The brushing point will be 10cm in front of the controller poisition
                            brushingPoint.transform.position = transform.position + transform.forward * 0.05f;
                            brushingPoint.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);

                            if (currentDetailView.GetComponent<Visualization>() != null)
                            {
                                // Q: what does the world to local point translation does here? 
                                // TODO: Change back!
                                currentDetailView.GetComponent<Visualization>().OnDetailOnDemand(this,
                                    brushingPoint.transform.position,
                                    currentDetailView.transform.InverseTransformPoint(brushingPoint.transform.position),
                                    true);
                                //currentDetailView.GetComponent<Visualization>().OnBrush(this,
                                //    brushingPoint.transform.position,
                                //    true);
                            }
                            else
                            {
                                Debug.Log("the object is null/...");
                            }
                        }
                    }
                }
                // This is for when the brushed thing is not in 3D! 
                if (!detail3Dscatterplots)
                {
                    RaycastHit hit;
                    Ray downRay = new Ray(transform.position, transform.forward);
                    if (Physics.Raycast(downRay, out hit))
                    {
                        if (hit.transform.gameObject.GetComponent<Brushable>() != null)
                        {
                            brushingPoint.gameObject.SetActive(true);
                            currentDetailView = hit.transform.gameObject;
                            brushingPoint.transform.position = hit.point;
                            brushingPoint.transform.rotation = currentDetailView.transform.rotation;
                            brushingPoint.transform.localScale = new Vector3(0.01f, 0.01f, 0.0f);

                            // TODO: Turn this back into normal 
                             currentDetailView.GetComponent<Visualization>().OnDetailOnDemand(
                                 this, 
                                 hit.point, 
                                 currentDetailView.transform.InverseTransformPoint(hit.point),
                                 false);
                            //currentDetailView.GetComponent<Visualization>().OnBrush(
                            //    this,
                            //    hit.point,
                            //    false);
                        }

                    }
                }
            } else
            {
                // this is just to reset everything for brushing
                // and just deactivate the brush point and all
                if(currentDetailView != null)
                {
                    currentDetailView.GetComponent<Visualization>().OnDetailOnDemandRelease(this);


                    //currentDetailView.GetComponent<Visualization>().OnDetailOnDemand(null, Vector3.negativeInfinity, Vector3.zero, false);

                    //currentDetailView = null;
                    //brushingPoint.gameObject.SetActive(false);
                    /////////////////////////////
                    //currentDetailView.GetComponent<Visualization>().OnBrush(null, Vector3.zero, false);

                    //currentDetailView.GetComponent<Visualization>().OnBrushRelease(this);
                    currentDetailView = null;
                    brushingPoint.gameObject.SetActive(false);
                }
            }
            // Checks to see if we're done with pressing the touchbar
            //TODO: fix the naming of this from Up to release or sth
            if (padPressUp)
            {
                if (currentDetailView != null)
                {
                    Debug.Log("i'm in the press up in update thingy!");
                    // currentDetailView.GetComponent<Visualization>().OnDetailOnDemand(null, Vector3.zero, Vector3.zero,false);

                    // currentDetailView.GetComponent<Visualization>().OnDetailOnDemandRelease(this);
                    // currentDetailView = null;
                    // brushingPoint.gameObject.SetActive(false);
                    //currentDetailView.GetComponent<Visualization>().OnBrush(null, Vector3.zero, false);

                    //currentDetailView.GetComponent<Visualization>().OnBrushRelease(this);
                    //currentDetailView = null;
                    //brushingPoint.gameObject.SetActive(false);

                }
            }
        }
#endregion
        
        tracking.RemoveAt(0);
        tracking.Add(transform.TransformPoint(new Vector3(0, -0.04f, 0)));

    }

    // this method gets active when another collider hits the controller
    /* There's a list of intersecting grabbables for each controller, it sorts the grabbables based on the priority and then sets the first one as the active grabbable component */
    public void OnTriggerEnter(Collider col)
    {
        if (draggingObjects.Count > 0)
            return;

        var grabble = col.GetComponent<Grabbable>(); 
        if (grabble != null && !intersectingGrabbables.Contains(col))
        {
            Collider activeGrabbable = intersectingGrabbables.FirstOrDefault();
            intersectingGrabbables.Add(col);
            intersectingGrabbables.RemoveAll(x => x == null);
            intersectingGrabbables.Sort((x, y) => y.GetComponent<Grabbable>().GetPriority() - x.GetComponent<Grabbable>().GetPriority());
            if (intersectingGrabbables[0] == col){
                if (activeGrabbable != null && activeGrabbable != intersectingGrabbables[0])
                {
                    activeGrabbable.GetComponent<Grabbable>().OnExit(this);
                }
                grabble.OnEnter(this);
            }
        }
        if (col.GetComponent<Brushable>() != null)
        {
            brushableCollider = col;
        }
    }

    public void OnTriggerExit(Collider col)
    {
        intersectingGrabbables.RemoveAll(x => x == null);

        var grabbable = col.GetComponent<Grabbable>();
        if (grabbable != null && intersectingGrabbables.Contains(col))
        {
            if (col == intersectingGrabbables[0]){
                grabbable.OnExit(this);
                intersectingGrabbables.RemoveAt(0);
                if (intersectingGrabbables.Count > 0){
                    intersectingGrabbables[0].GetComponent<Grabbable>().OnEnter(this);
                }
            } else {
                intersectingGrabbables.Remove(col);
            }
            brushableCollider = null;
        }
    }

    void SetIntersectingCollider(Collider col)
    {
        if (col != null){
            if (intersectingCollider != null && col.GetComponent<Grabbable>().GetPriority() >= intersectingCollider.GetComponent<Grabbable>().GetPriority())
            {
                intersectingCollider.GetComponent<Grabbable>().OnExit(this);
            }
            intersectingCollider = col;
            intersectingCollider.GetComponent<Grabbable>().OnEnter(this);
            
        } else {
            intersectingCollider.GetComponent<Grabbable>().OnExit(this);
            intersectingCollider = null;
        }        
    }

    public bool IsDragging(Grabbable grab)
    {
        return draggingObjects.Any(x => x.GetComponent<Grabbable>() == grab);
    }

    public bool IsDragging()
    {
        return draggingObjects.Count > 0;
    }

    public void shake(float duration, float frequency, float amplitude, SteamVR_Input_Sources source)
    {
        // Deactivated for the controller's battery
        // TODO: reactivate
        //hapticAction.Execute(0, duration, frequency, amplitude, source);

        //print("shake " + source.ToString());
    }

    IEnumerator ShakeCoroutine()
    {
        for (int i = 0; i < 15; ++i)
        {
            //controller.TriggerHapticPulse((ushort)(3900 * (15 - i) / 15.0f));
            yield return new WaitForEndOfFrame();
        }
    }

    //length is how long the vibration should go for
    //strength is vibration strength from 0-1
    IEnumerator TriggerHaptics(float length, float strength) {
        if (!isOculusRift)
        {
            for (float i = 0; i < length; i += Time.deltaTime)
            {
                //controller.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, strength));
                yield return new WaitForEndOfFrame();
            }
        }
    }

    public void Shake()
    {
        //if (!isOculusRift)
        //StartCoroutine(ShakeCoroutine());
        shake(1f, 150, 0.5f, handType);
    }

    public void OnApplicationQuit()
    {
       
    }

}
