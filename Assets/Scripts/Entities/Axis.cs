using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using Staxes;
using UnityEngine.Events;
using System.Linq;
using System.IO;

public class Axis : MonoBehaviour, Grabbable {

    public static float AXIS_ROD_LENGTH = 0.2660912f;
    public static float AXIS_ROD_WIDTH = 0.02059407f;
    public static float AXIS_ROD_DEPTH = 0.02059408f;
    public static float CONTROLLER_VELOCITY_FOR_DELETION = 0.25f;

    [SerializeField] public TextMeshPro label;
    [SerializeField] TextMeshPro minimumValueDimensionLabel;
    [SerializeField] TextMeshPro maximumValueDimensionLabel;

    // The Id is useful for ID in the data panels (aka protoytpes   )
    public int axisId;

    // Q: What is this for? 
    // This is active when the axis is on the data shelf and has not been dragged to the main scene
    public bool isPrototype = false;

    // Determines if the axis is on a 2D plane or not
    public bool isOn2DPanel = false;

    public bool isClonedByCloningWidget = false;

    public bool isCollidingWithMonitor = false;
    public GameObject collidingMonitor = null;
    
    public Visualization collidingVisForPrompt = null;
    public bool isCollidingWithColorPrompt = false;
    public bool isCollidingWithSizePrompt = false;

    // To detect if the dataShelf panel is moving
    public bool parentIsMoving = false;
    Vector3 parentPrevPosition = Vector3.zero;

    //temporary hack 

    // these values are used with the setInitOrigin in order to set the initial postiion and rotation of the axes
    // This is mostly useful for setting the data inside the data panels (they call the data panel, prototypes)
    Vector3 originPosition;
    Vector3 originScale;
    Quaternion originRotation;

    // These are the literal game objects for filtering that are there
    // If you want to change these objectst you can just pass a different one instead of them, just make sure they make sense as max and min knobs
    // TODO: get rid of the knobs for the time being and replace them with the actual filters
    [SerializeField] Transform minFilterObject;
    [SerializeField] Transform maxFilterObject;
    
    [SerializeField] Transform minNormaliserObject;
    [SerializeField] Transform maxNormaliserObject;
    [SerializeField] public GameObject cloningWidgetGameObject;

    [SerializeField] Renderer ticksRenderer;

    [Space(10)]

    [SerializeField] UnityEvent OnEntered;
    [SerializeField] UnityEvent OnExited;

    // Q: What is this for? 
    // This is used to handle Axis's collision with other objects including the controller object
    // this is mainly controlled by the AxisAnchor script and class
    public HashSet<Axis> ConnectedAxis = new HashSet<Axis>();

    public Vector3 ZeulerAnglesBefore2DRotation = Vector3.zero;
    public Vector3 positionBefore2Drotation = Vector3.zero;
    public float previousControllerRotationAngle = -999f;
    public Vector3 previousControllerPosition = Vector3.zero;
    public float MinFilter;
    public float MaxFilter;

    public float MinNormaliser;
    public float MaxNormaliser;

    // this gets true while grabbing the object 
    public bool isDirty;

    public bool isInSplom;

    // There to handle if the object is doing a DOTween animation or not
    bool isTweening;
    // Mostly to identify which was a first proto that we cloned it from
    public int SourceIndex = -1;

    // Here's the filteing event system that handles the filtering process
    // Filter event is just a simple unity event with two args (floats both of them)
    public class FilterEvent : UnityEvent<float, float> { };

    // Right now the only place that this is used, is when we have a histogram! I don't get why to be honest!
    // TODO: just extend it to the whole visualization class!
    public FilterEvent OnFiltered = new FilterEvent();

    public class NormalizeEvent : UnityEvent<float, float> { };
    public NormalizeEvent OnNormalized = new NormalizeEvent();

    //ticker and file path (etc) for logging activity
   
    //SteamVR_TrackedObject trackedObject;
    List<Vector3> tracking = new List<Vector3>();
    
    // This comes from the DimensionRange in DataObject
    // x of this vector2 is min and y of this vector2 is max
    public Vector2 AttributeRange;

    // This changes the scale size of the ticks 
    float ticksScaleFactor = 1.0f;

    // ghost properties
    // Q: What does ghost property means? 
    Axis ghostSourceAxis = null;
    public AxisGhost ghostCube = null;

    Transform originalParent = null;

    public WandController grabbingController = null;

    public List<AttributeFilter> AttributeFilters = new List<AttributeFilter>();

    public float axisScaleFactor = 1f;

    // This is for holding the color configurations of a visualization
    // This is to overcome the stupid design of the ParseScene function and nothing else! 
    // TODO: in the future just re-write the ParseScene function to accepct individual configs
    //[SerializeField] 
    public List<Color> correspondingVisColors = new List<Color>();
    public int correspondingVisColorAxisId = -1;
    public List<float> correspondingVisSizes = new List<float>();
    public int correspondingVisSizeAxisId = -1;

    public List<string> correspondingVisualizationHashes = new List<string>();


    // TODO: make the value for each tick more clear! it's now not clear what's the value when the user gets over there! 
    // This is called from the sceneManager script which basically sets up the scene that we have at the beginning
    // TODO: Change the SceneManager scene to get to where I want it to be
    public void Init(DataBinding.DataObject srcData, int idx, bool isPrototype = false, float scaleFactor = 1f)
    {
        transform.localScale *= scaleFactor;
        this.axisScaleFactor = scaleFactor;

        SourceIndex = idx;
        axisId = idx;
        name = "axis " + srcData.indexToDimension(idx);

        //  This basically sets the range of the data dimensions that are going to be in the file
        // SrcData is the whole of the data
        // it's the raw range of the data attribute and it's NOT between 0 and 1
        AttributeRange = srcData.DimensionsRange[axisId];
        label.text = srcData.Identifiers[idx];
        UpdateRangeText();

        this.isPrototype = isPrototype;

        CalculateTicksScale(srcData);
        UpdateTicks();

    }

    // The function that generates min and max texts based on the type of the data
    // The type of the data comes from DataObject class
    // TODO: Add a value to the histogram rows of data or maybe when we hover over them make them chanage color and also show the value
    void UpdateRangeText()
    {
        string type = SceneManager.Instance.dataObject.TypeDimensionDictionary1[SourceIndex];

        if (type == "float")
        {
            minimumValueDimensionLabel.text = Mathf.Lerp(AttributeRange.x, AttributeRange.y, MinNormaliser + 0.5f).ToString("0.000");
            maximumValueDimensionLabel.text = Mathf.Lerp(AttributeRange.x, AttributeRange.y, MaxNormaliser + 0.5f).ToString("0.000");
        }

        else if (type == "string")
        {
            float minValue = Mathf.Lerp(AttributeRange.x, AttributeRange.y, MinNormaliser + 0.5f);
            float maxValue = Mathf.Lerp(AttributeRange.x, AttributeRange.y, MaxNormaliser + 0.5f);

            float nearestMinValue = UtilMath.ClosestTo(SceneManager.Instance.dataObject.TextualDimensions.Keys.ToList(), minValue);
            float nearestMaxValue = UtilMath.ClosestTo(SceneManager.Instance.dataObject.TextualDimensions.Keys.ToList(), maxValue);

            minimumValueDimensionLabel.text = SceneManager.Instance.dataObject.TextualDimensions[nearestMinValue].ToString();
            maximumValueDimensionLabel.text = SceneManager.Instance.dataObject.TextualDimensions[nearestMaxValue].ToString();
        }
    }


    void CalculateTicksScale(DataBinding.DataObject srcData)
    {
        // TODO: we should somehow show how each of these tick marks show 5, 10, 50, or more values
        float range = AttributeRange.y - AttributeRange.x;
        // bincount: this is initially Min(RawmaxDimension - RawminDimension + 1, 200)
        // we can set it manually in metadataPreset.BinSizePreset by making a metadata preset
        if (srcData.Metadata[axisId].binCount > range + 2)
        {
            ticksScaleFactor = 1.0f / (srcData.Metadata[axisId].binCount / 10);
        }
        else if (range < 20)
        {
            // each tick mark represents one increment
            ticksScaleFactor = 1;
        }
        else if (range < 50)
        {
            ticksScaleFactor = 5;
        }
        else if (range < 200)
        {
            // each tick mark represents ten increment
            ticksScaleFactor = 10;
        }
        else if (range < 600)
        {
            ticksScaleFactor = 50;
        }
        else if (range < 3000)
        {
            ticksScaleFactor = 100;
        }
        else
        {
            ticksScaleFactor = 500;
        }
    }

    // This functions calculates the range and scale of the ticks based on the changed values of the min and max normalizers and also the max and min of the AttributeRange and then displays it in the last line
    void UpdateTicks()
    {
        // Lerp is used for tweening between the min value of the AttributeRange and the max value of it
        // range is going to be between 0 and 1 times the attribute range 
        float range = Mathf.Lerp(AttributeRange.x, AttributeRange.y, MaxNormaliser + 0.5f) - Mathf.Lerp(AttributeRange.x, AttributeRange.y, MinNormaliser + 0.5f);
        float scale = range / ticksScaleFactor;
        print("in Axis " + name + "range is " + range);
        print("in Axis " + name + "tickscaleFactor is " + ticksScaleFactor);
        print("in Axis " + name + "scale is " + scale);
        // whatever this scale is determines how many ticks we're showing
        // the number of shown ticks is floor(scale * 1)
        ticksRenderer.material.mainTextureScale = new Vector3(1, scale);
    }

    public void setDebug(string dbg)
    {
        DataBinding.DataObject srcData = SceneManager.Instance.dataObject;
        label.text = srcData.Identifiers[axisId] + "(" + dbg + ")";
    }

    // the first position that the AXIS appears in 
    public void InitOrigin(Vector3 originPosition, Quaternion originRotation)
    {
        this.originPosition = originPosition;
        this.originRotation = originRotation;
        this.originScale = transform.localScale + new Vector3(0, 0, AXIS_ROD_WIDTH/2);
    }

    public void initOriginalParent(Transform originalParent){
        this.originalParent = originalParent;
    }

    void Start()
    {
        Debug.Assert(ghostCube != null, "The ghost Axis game object cannot be null");
        //all colliders from this object should ignore raycast
        // TODO: Maybe remove the non-raycast collider thingy from this object! 
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var item in colliders)
        {
            item.gameObject.layer = 2;
        }       

    }

    void OnDestroy()
    {
        print("on destroy is called on " + axisId + " " + Time.realtimeSinceStartup);
        SceneManager.Instance.sceneAxes.Remove(this);
        if (ghostSourceAxis != null)
        {
            ghostSourceAxis.OnFiltered.RemoveListener(Ghost_OnFiltered);
            ghostSourceAxis.OnNormalized.RemoveListener(Ghost_OnNormalized);
        }
    }

    public void Update()
    {
        // This is the part that clones and shakes the clone when it's departing
        // it checks if the axes is part of the data shelf first
        // Then puts a clone in the original place of the data component
        // In the end adds the axis that is cloned to the list of present axes on the scene

        handleParentDataShelfMovement();

    }

    public void LateUpdate()
    {
        isDirty = false;
    }

    // filtering operations 
    // Whenever a filter is changed we need to invoke the Onfilter event to make sure it's affecting the thing
    public void SetMinFilter(float val)
    {
        MinFilter = val;
        Debug.Log(name + "'s filter is set to: " + val);
        OnFiltered.Invoke(MinFilter, MaxFilter);
    }

    public void SetMaxFilter(float val)
    {
        MaxFilter = val;
        OnFiltered.Invoke(MinFilter, MaxFilter);
    }

    public void SetMinNormalizer(float val)
    {
        MinNormaliser = Mathf.Clamp(val, -0.505f, 0.505f);
        UpdateRangeText();
        OnNormalized.Invoke(MinNormaliser, MaxNormaliser);
        UpdateTicks();
    }

    public void SetMaxNormalizer(float val)
    {
        MaxNormaliser = Mathf.Clamp(val, -0.505f, 0.505f);
        UpdateRangeText();
        OnNormalized.Invoke(MinNormaliser, MaxNormaliser);
        UpdateTicks();
    }

    // TODO: get the cloning features into the visualiztion class too
    public GameObject Clone()
    {
        GameObject clone = Instantiate(gameObject, transform.position, transform.rotation, null);
        clone.name = gameObject.name;
        Axis axis = clone.GetComponent<Axis>();
        axis.InitOrigin(originPosition, originRotation);
        axis.isClonedByCloningWidget = isClonedByCloningWidget;
        axis.ticksRenderer.material = Instantiate(ticksRenderer.material) as Material;

        return clone;
    }
    
    public GameObject Clone(Vector3 position, Quaternion rotation)
    {
        GameObject clone = Instantiate(gameObject, position, rotation, null);
        clone.name = gameObject.name;
        Axis axis = clone.GetComponent<Axis>();
        axis.Init(SceneManager.Instance.dataObject, axisId, false);
        axis.InitOrigin(position, rotation);
        axis.isClonedByCloningWidget = isClonedByCloningWidget;
        axis.isPrototype = false;
        axis.isOn2DPanel = false;
        axis.ticksRenderer.material = Instantiate(ticksRenderer.material) as Material;
        axis.OnExited.Invoke();

        return clone;
    }

    // This is a misc method for duplicating any object, it can be used fron the outside of the class, too
    public GameObject Dup(GameObject go, Vector3 tp, Quaternion tr)
    {
        GameObject clone = Instantiate(go, tp, tr, null);
        Axis axis = clone.GetComponent<Axis>();
        axis.InitOrigin(originPosition, originRotation);
        axis.ticksRenderer.material = Instantiate(ticksRenderer.material) as Material;

        return clone;
    }



    #region euclidan functions

    // calculates the project of the transform tr (assumed to be the user's hand) onto the axis
    // as a float between 0...1
    // Is mostly used by the AxisWidget class
    public float CalculateLinearMapping(Transform tr)
    {
        Vector3 direction = MaxPosition - MinPosition;
        float length = direction.magnitude;
        direction.Normalize();

        Vector3 displacement = tr.position - MinPosition;

        return Vector3.Dot(displacement, direction) / length;
    }

    public bool IsHorizontal
    {
        get
        {
            float dp = Vector3.Dot(this.Up, Vector3.up);
            return dp > -0.25f && dp < 0.25f;
        }
    }

    public bool IsVertical
    {
        get
        {
            float dp = Vector3.Dot(this.Up, Vector3.up);
            return dp > 0.9f || dp < -0.9f;

        }
    }
    public bool isPerependicular(Axis axis)
    {
        return Vector3.Dot(Up, axis.Up) > -0.2f && Vector3.Dot(Up, axis.Up) < 0.2f;
    }

    public bool IsParallel(Axis axis) {
        return Vector3.Dot(Up, axis.Up) > 0.5f;
    }

    // TODO: clean-up this thing! 
    public bool IsColinear(Axis axis)
    {
        if (axis.IsHorizontal)
        {
            return Vector3.Dot(Up, axis.Up) > 0.95f;// 0.1f && Vector3.Dot(Up, axis.Up) > -0.1f;
        }
        else { return Vector3.Dot(Up, axis.Up) > 0.95f; }
    }

    // Transform form the local coords into the world coords
    public Vector3 Up
    {
        get { return transform.TransformDirection(Vector3.up); }
    }

    Vector3 _maxPos;
    public Vector3 MaxPosition
    {
        get { return _maxPos; }
    }

    Vector3 _minPos;
    public Vector3 MinPosition
    {
        get { return _minPos; }
    }

    // Sets the min and max point of each axis
    // The length of an axis is 1 meters in unity world
    public void UpdateCoords()
    {
        _minPos = transform.TransformPoint(Vector3.down * 0.51f);
        _maxPos = transform.TransformPoint(Vector3.up * 0.51f);
    }

    // Takes the distance from the midpoint (Unity posistion) of the axis to another axis
    public float Distance(Axis axes)
    {
        Vector3 pos_a = transform.position;
        Vector3 pos_b = axes.transform.position;
        return Vector3.Distance(pos_a, pos_b);
    }

    // returns the top and bottom points of this axis in world coordinates
    public List<Vector3> Points()
    {
        return new List<Vector3> {
            transform.TransformPoint(Vector3.up * 0.5f),
            transform.TransformPoint(Vector3.down * 0.5f)
            };
    }

    #endregion

    // Priority for the grabbing action of the controller! So that the controller knows that between this and the visualization, it should always grab this!
    // This makes it easy to detach an axis from the visualization
    int Grabbable.GetPriority()
    {
        return 5;
    }

    public bool OnGrab(WandController controller)
    {
        // don't let any interactions happen if we're tweening
        if (isTweening || DOTween.IsTweening(transform))
            return false;

        // What happens if the Axis is clonable
        if(isPrototype)
        {
            // This part is for registering the action for undo and redo stuff
            EventManager.TriggerAxisEvent(ApplicationConfiguration.OnAxisCloned, this);

            // if it's not on the 2D panel, switch it back to non-proto as soon as 
            // we call the cloning function
            isPrototype = isOn2DPanel ? isPrototype : false;
            
            // we don't want it to move if it's a prototype that is on the 2d panel
            activateGhost(controller);
            return false;
        } 
        else
        {
            // registering actions for undo and redo
            EventManager.TriggerAxisEvent(ApplicationConfiguration.OnAxisGrabbed, this);

            if (!isOn2DPanel)
            {
                // meaning that if we're dragging it in the 3D space, just let the 
                // axis drag it around by making it the child of the controller
                transform.parent = controller.transform;
            } else
            {
                // Save the positin and the rotation of the axis before we start a 2D movement
                ZeulerAnglesBefore2DRotation = transform.eulerAngles;
                positionBefore2Drotation = transform.position;
                // Save the position and the rotation of the controller before a 2D movement
                previousControllerRotationAngle = controller.transform.eulerAngles.z;
                previousControllerPosition = controller.transform.position;
            }
        }

        GetComponent<Rigidbody>().isKinematic = true;
        isDirty = true;
        return true;
    }

    public void OnRelease(WandController controller)
    {

        // here's the logic for releasing it inside the monitor (not the 2D plane but the desktop monitor)
        if(isCollidingWithMonitor)
        {
            print("here's the colliding monitor name " + collidingMonitor.name);

            // for the time being, I'll get all of visualizations that this axis is involved in and send them to the monitor board interactions to create a vis on dekstop
            // TODO: later we want to detect if it's a 3D visualization, only send two of the axes that are closest to the monitor and then send the third on as color or size
            isCollidingWithMonitor = false;
            collidingMonitor.GetComponent<MonitorBoardInteractions>().DropVisInDesktop(correspondingVisualizations());
            collidingMonitor = null;

        }

        // First save the original parent transform somewhere
        originalParent = transform.parent;

        // Takes botht the axis and its corresponding visualization out of the data shelf (or any other parent)
        transform.parent = null;
        foreach (var visu in correspondingVisualizations())
        {
            visu.transform.parent = null;

            // TODO: Set the cloning knob of other axes to disable and this one to enable
        }

        // Logic for destroying the Axis upon throwing 
        if (!isPrototype)
        {
            // destroy the axis
            // This is the part that controls to see if we're throwing it with some velicity, it needs to be dstoryed
            if (controller.Velocity.magnitude > CONTROLLER_VELOCITY_FOR_DELETION)
            {
                Rigidbody body = GetComponent<Rigidbody>();
                body.isKinematic = false;
                body.useGravity = true;
                body.AddForce(controller.Velocity * -1000);
                gameObject.layer = LayerMask.NameToLayer("TransparentFX");
                
                Sequence seq = DOTween.Sequence();
                seq.Append(transform.DOScale(0.0f, 0.5f).SetEase(Ease.InBack));

                return;
            }
        }


        #region Animating Axis to move for each Visualization

        List<Visualization> lv = correspondingVisualizations();
        // TODO: this part is importnat for handling the other vizes when one gets released
        /* haxis: horizontal axis! DUH! */
        foreach (var visu in lv)
        {
            if (visu.viewType == Visualization.ViewType.Scatterplot2D)
            {
                var haxis = visu.ReferenceAxis1.horizontal;
                var vaxis = visu.ReferenceAxis1.vertical;

                var vu = vaxis.Up;
                var hu = haxis.Up;

                Vector3.OrthoNormalize(ref vu, ref hu);

                var q1 = Quaternion.LookRotation(-Vector3.Cross(vu, hu), vu);
                var q2 = Quaternion.LookRotation(Vector3.Cross(vu, hu), hu);

                // find out which direction the horizontal is facing
                var urvec = Vector3.Cross(-Vector3.Cross(vu, hu), vu);
                float d = Vector3.Dot(urvec, (haxis.transform.position - vaxis.transform.position));

                // determine the position of the horizontal axis
                Vector3 hpos = vaxis.transform.position +
                    -vu * vaxis.transform.localScale.y * 0.5f +
                    -Mathf.Sign(d) * hu * haxis.transform.localScale.y * 0.5f;

                vaxis.AnimateTo(vaxis.transform.position, q1);
                haxis.AnimateTo(hpos, q2);
            }
            else if (visu.viewType == Visualization.ViewType.Scatterplot3D)
            {
                if (visu != null && visu.ReferenceAxis1.vertical != null && visu.ReferenceAxis1.horizontal != null && visu.ReferenceAxis1.depth != null)
                {
                    var haxis = visu.ReferenceAxis1.horizontal;
                    var vaxis = visu.ReferenceAxis1.vertical;
                    var daxis = visu.ReferenceAxis1.depth;

                    var vu = vaxis.Up;
                    var hu = haxis.Up;
                    var du = daxis.Up;

                    Vector3.OrthoNormalize(ref vu, ref hu, ref du);

                    var q1 = Quaternion.LookRotation(-du, vu);
                    var q2 = Quaternion.LookRotation(du, hu);
                    var q3 = Quaternion.LookRotation(-hu, du);

                    Vector3 hpos = vaxis.transform.position +
                        -vu * vaxis.transform.localScale.y * 0.5f +
                        hu * haxis.transform.localScale.y * 0.5f;

                    Vector3 dpos = vaxis.transform.position +
                        -vu * vaxis.transform.localScale.y * 0.5f +
                        du * daxis.transform.localScale.y * 0.5f;

                    vaxis.AnimateTo(vaxis.transform.position, q1);
                    haxis.AnimateTo(hpos, q2);
                    daxis.AnimateTo(dpos, q3);                    
                }
            }
        } // end for each 

        // align this axis correctly to the SPLOM
        foreach (SPLOM3D splom in CorrespondingSPLOMS())
        {
            splom.AlignAxisToSplom(this);
        }

        #endregion

        GetComponent<Rigidbody>().isKinematic = false;
        ZeulerAnglesBefore2DRotation = transform.eulerAngles;
        isDirty = false;

        grabbingController = null;
        
        // This part is there to handle the Undo and Redo and trigger the events that are necessary for that
        if(!isPrototype) {
            // Debug.Log("I'm being released: " + isDirty + " and pos is: " + transform.position);
            // Call the event that sets the whole thing up! 
            
            if(!correspondingVisualizations().Any()) { // if the axis is part of none of the other visualizations
                EventManager.TriggerAxisEvent(ApplicationConfiguration.OnAxisReleased, this);
            } else {
                EventManager.TriggerAxisEvent(ApplicationConfiguration.OnAxisReleasedInVis, this);
            }
        }

        // This part is the logic for the activation of the Cloning knob on the Axis
        if(!isPrototype && correspondingVisualizations().Any()) {
            /* Change the knob position in this case */
            foreach (var vis in correspondingVisualizations())
            {
                if(vis.axesCount == 1)
                    continue;
                foreach (var axis in vis.axes)
                {
                    axis.cloningWidgetGameObject.SetActive(false);
                }
                cloningWidgetGameObject.transform.position = vis.fbl + (vis.transform.right * -0.05f) + (vis.transform.up * -0.04f);
            }
            cloningWidgetGameObject.SetActive(true);
        }

        // Handle Axis drop inside a color prompt
        // TODO: should this be called before or after all the ones we have above us?
        handleReleaseInVisualizationPrompt();

    }

    private void handleReleaseInVisualizationPrompt()
    {
        if (!isPrototype && collidingVisForPrompt != null && (isCollidingWithColorPrompt || isCollidingWithSizePrompt))
        {
            if (isCollidingWithColorPrompt)
            {
                // Call the visualization method that handles color assignments
                collidingVisForPrompt.setVisualizationColors(this);
                // Reset everything after the assignment happens
                setCollidedVisualizationForPrompt(null, PromptCollisionHandler.PromptType.Color);

            } else if (isCollidingWithSizePrompt)
            {
                // Call the visualization method that handles size assignments
                collidingVisForPrompt.setVisualizationSizes(this);

                // Reset everything after the assignment happens
                setCollidedVisualizationForPrompt(null, PromptCollisionHandler.PromptType.Size);
            }


            // no matter if it's size of color, we need it destroyed after the drop happens

            //now scale and destroy the axis!
            gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            //gameObject.SetActive(false);

            // TODO: this is just a hack 
            // ImAxisRecognizer is written in a way that SP and SPLOMS3D array won't be cleared 
            // unless the position of an axis changes, so here's a hack to drop the thing instead 
            // of dealing with re-writing and refactoring the whole ImAxisRecognizer code!

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(0.0f, 0.3f).SetEase(Ease.InBack));
            seq.Append(transform.DOMoveY(-10000.0f, 0.5f).SetEase(Ease.InBack));
            seq.AppendCallback(() =>
            {
                Destroy(gameObject);
            });
        }
    }

    private void handleParentDataShelfMovement()
    {
        // This part is to handle what happens if the parent of this object moves around
        if (transform.parent != null && transform.parent.tag == "DataShelfPanel")
        {

            // While the data panel is moving, don't clone anything (aka keep the isProto to false)
            if (parentPrevPosition != Vector3.zero && parentPrevPosition.Equals(transform.parent.position))
            {
                parentIsMoving = false;
            }
            else
            {
                parentIsMoving = true;
            }

            // Making sure that the parent of the axis and its corresponding visualizations are the same
            foreach (var visu in correspondingVisualizations())
            {
                if (visu.transform.parent == null || visu.transform.parent.tag != "DataShelfPanel")
                {
                    visu.transform.SetParent(transform.parent);
                }
            }

            // Keep the last position of the parent in this variable for comparison
            parentPrevPosition = transform.parent.position;

            if (parentIsMoving)
            {
                // update the origin position and rotation for when the axes move around
                this.originPosition = transform.position;
                this.originRotation = transform.rotation;
            }
        }
    }

    // This function is only called if the OnGrab function returns true 
    // and the WandController is dragging the object
    public void OnDrag(WandController controller)
    {
        if (grabbingController == null || grabbingController != controller)
            grabbingController = controller;

        isDirty = true;

        if (DOTween.IsTweening(transform))
            return;

        // this only happens if the we're manipulating the axis on the 2D plane 
        if (isOn2DPanel) { 

            Transform TwoPanel = GameObject.FindGameObjectWithTag("2DPanel").transform;
            // We need the distance in the direction of the normal vector of the plane
            Vector3 distanceWithPlane = Vector3.Project(controller.transform.position - TwoPanel.position, TwoPanel.forward);

            // if the axis is not a clonable one, just move it on 2D plane
            MoveAxisOn2dPlane(controller);

            //We need the distance in the direction of the normal vector of the plane
            //Vector3 controllerOrthogonalDistance = Vector3.Project(controller.transform.position - originPosition, TwoPanel.forward);

            // This handles how the axis moves out of the 2D plane
            if(distanceWithPlane.magnitude > TwoDimensionalPanelScript.COLLISION_DISTANCE_BOUNDARY)
            {
                print("distance with plane is " + distanceWithPlane.magnitude);
                MoveOutOf2DBoard(controller);
            }
            
        } 
        
    }

    public void OnEnter(WandController controller)
    {
        OnEntered.Invoke();
    }

    public void OnExit(WandController controller)
    {
        OnExited.Invoke();
    }

    public void MoveOutOf2DBoard(WandController controller)
    {
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        transform.SetParent(null);

        // ask the twoD panel to remove this axis from its list of connectedAxes
       

        AnimateTo(controller.transform.position, transform.rotation, originScale);

        isOn2DPanel = false;
        OnGrab(controller);
    }
    
    private void MoveAxisOn2dPlane(WandController controller)
    {
        // Handling the correct rotation of the axes
        Quaternion desiredRotation = Quaternion.Euler(0, 0, controller.transform.eulerAngles.z - previousControllerRotationAngle);

        if (Vector3.Dot(controller.transform.forward, transform.forward) < 0)
        {
            desiredRotation = Quaternion.Inverse(desiredRotation);
        }

        // Don't do the rotation and all when it's on the datashelf
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, ZeulerAnglesBefore2DRotation.z) * (desiredRotation);

        // Map the direction of the movement to the plane of our 2D thing and then add it to the position point
        Vector3 planarMappingOfDirection = Vector3.ProjectOnPlane(controller.transform.position - previousControllerPosition, transform.forward);

        // Don't do the translation and all when it's on the datashelf
        transform.position = positionBefore2Drotation + planarMappingOfDirection;

    }

    // deprecated
    // used to return the axis back to the data shelf, we're not doing that anymore
    void ReturnToOrigin()
    {
        print("return to origin called at " + Time.realtimeSinceStartup);

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DORotate(originRotation.eulerAngles, 0.7f, RotateMode.Fast).SetEase(Ease.OutSine));
        seq.Join(transform.DOMove(originPosition, 0.7f).SetEase(Ease.OutElastic));
        if (!isClonedByCloningWidget) 
            seq.AppendCallback(() => GetComponent<Axis>().isPrototype = true);
        else 
            seq.AppendCallback(() => GetComponent<Axis>().isPrototype = false);

        foreach (var c in GetComponentsInChildren<ProximityDetector>())
        {
            c.ForceExit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {   
        if(other.GetComponent<MonitorBoardInteractions>())
        {
            isCollidingWithMonitor = true;
            collidingMonitor = other.gameObject;
            print("just collided with a monitor " + other.gameObject.name);

        }


        //Visualization collidedVis = other.GetComponent<Visualization>();
        //if (collidedVis && !correspondingVisualizations().Contains(collidedVis))
        //{
        //    print("I've collided with a vis ");

        //    // don't do it when the axis is part of any other visualizations that are not histograms
        //    if(correspondingVisualizations().Count() > 1)
        //    {
        //        print("in Axis: can't do attribute assignment for non-histograms!");
        //        return;
        //    }
        //    this.collidingVis = collidedVis;
        //    //collidedVis.showColorPrompt();
        //}
        //print("in AXIS: " + other.gameObject.name);
    }

    private void OnTriggerExit(Collider other)
    {
        Visualization collidedVis = other.GetComponent<Visualization>();
        if (other.GetComponent<MonitorBoardInteractions>())
        {
            if (isCollidingWithMonitor && collidingMonitor != null)
            {
                isCollidingWithMonitor = false;
                collidingMonitor = null;
                print("just GOT OUT of a monitor " + other.gameObject.name);
            }
        }
        //else if (collidedVis && this.collidingVis.transform.GetInstanceID() == collidingVis.transform.GetInstanceID())
        //{
        //    print("I'm getting out of a vis ");
        //    //this.collidingVis.hideColorPrompt();
        //    this.collidingVis = null;

        //}
    }

    public void setCollidedVisualizationForPrompt(Visualization collided, PromptCollisionHandler.PromptType promptType)
    {
        if(!collided)
        {
            if (promptType == PromptCollisionHandler.PromptType.Color)
                isCollidingWithColorPrompt = false;
            else if (promptType == PromptCollisionHandler.PromptType.Size)
                isCollidingWithSizePrompt = false;

            collidingVisForPrompt = null;
            return;
        } else
        {
            if (promptType == PromptCollisionHandler.PromptType.Color)
                isCollidingWithColorPrompt = true;
            else if (promptType == PromptCollisionHandler.PromptType.Size)
                isCollidingWithSizePrompt = true;

            collidingVisForPrompt = collided;
        }

    }


    // This finds all fo the visualizations that this axis is a part of ... and yes, an axis could be a part of multiple data visualziations
    public List<Visualization> correspondingVisualizations()
    {
        return GameObject.FindObjectsOfType<Visualization>().Where(x => x.axes.Contains(this)).ToList();
    }

    public void addToCorrespondingVisualizationHashes(string visToBeAddedHash)
    {
        if(!correspondingVisualizationHashes.Contains(visToBeAddedHash))
        {
            print("In Axis new visualization with Hash " + visToBeAddedHash + "and count of " + correspondingVisualizationHashes.Count());
            correspondingVisualizationHashes.Add(visToBeAddedHash);
        }
    }

    public bool CheckIfWasInVisualization(string visHash)
    {
        return correspondingVisualizationHashes.Contains(visHash);
    }

    public List<SPLOM3D> CorrespondingSPLOMS()
    {
        return FindObjectsOfType<SPLOM3D>().Where(x => x.Axes.Contains(this)).ToList();
    }

    public void OnApplicationQuit()
    {
        
    }

    // Ghosts are for 3D SPLOMs only
    // This sounds like the item that is there to show us a ghosted preview of the data when it's being filtered.
    public void Ghost(Axis sourceAxis)
    {
        sourceAxis.OnFiltered.AddListener(Ghost_OnFiltered);
        sourceAxis.OnNormalized.AddListener(Ghost_OnNormalized);

        foreach (Renderer r in transform.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = false;
        }
        foreach (Collider c in transform.GetComponentsInChildren<Collider>(true))
        {
            c.enabled = false;
        }
    }

    void Ghost_OnFiltered(float minFilter, float maxFilter)
    {
        MinFilter = minFilter;
        MaxFilter = maxFilter;
        OnFiltered.Invoke(MinFilter, MaxFilter);
    }

    void Ghost_OnNormalized(float minNorm, float maxNorm)
    {
        MinNormaliser = minNorm;
        MaxNormaliser = maxNorm;
        OnNormalized.Invoke(MinNormaliser, MaxNormaliser);
    }

    public void AnimateTo(Vector3 pos, Quaternion rot, Vector3? scale = null)
    {
        print("animate to is called at " + Time.realtimeSinceStartup);
        StartCoroutine(AnimatorCoroutine(pos, rot, scale));
    }

    private IEnumerator AnimatorCoroutine(Vector3 pos, Quaternion rot, Vector3? scale = null)
    {
        List<Tween> activeTweens = DOTween.TweensByTarget(transform);

        if (activeTweens != null && activeTweens.Count != 0)
        {
            foreach (Tween t in activeTweens)
            {
                yield return t.WaitForCompletion();
                // This log will happen after the tween has completed
                Debug.Log("Tween completed! " + t.id);
            }
        }

        print("all coroutines are done! ");
        transform.DORotateQuaternion(rot, 0.4f).SetEase(Ease.OutBack);
        transform.DOMove(pos, 0.4f).SetEase(Ease.OutBack);
        if(scale != null)
            transform.DOScale((Vector3)scale, 0.4f).SetEase(Ease.OutElastic);
    }    
    
    public void MoveTo2DBoard(Transform TwoDBoard, Vector3 pos, Quaternion rot, Vector3 scale)
    {

        // We want the axis to be released from the controller before it begins the sequence
        foreach (var obj in GameObject.FindObjectsOfType<WandController>())
        {
            if (obj.IsDragging(this))
            {
                print("on release called from two d " + Time.realtimeSinceStartup);
                OnRelease(obj);
            }
        }

        StartCoroutine(AnimateTo2DBoardCoroutine(TwoDBoard, pos, rot, scale));
    }

    private IEnumerator AnimateTo2DBoardCoroutine(Transform TwoDBoard, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        List<Tween> activeTweens = DOTween.TweensByTarget(transform);
        
        if(activeTweens != null && activeTweens.Count != 0)
        {
            foreach(Tween t in activeTweens)
            {
                yield return t.WaitForCompletion();
                // This log will happen after the tween has completed
                Debug.Log("Tween completed! " + t.id);
            }
        }

        print("all coroutines are done! ");
        Sequence seq = DOTween.Sequence();
        // a.transform.
        seq.Append(transform.DORotate(rot.eulerAngles, 0.1f).SetEase(Ease.OutElastic));

        seq.Append(transform.DOMove(TwoDBoard.transform.position + pos + (TwoDBoard.transform.forward * -0.05f), 0.3f).SetEase(Ease.OutElastic));

        seq.Join(transform.DOScale(new Vector3(transform.localScale.x, transform.localScale.y, 0.00001f), 0.3f).SetEase(Ease.OutElastic));

        seq.AppendCallback(() =>
        {
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            // a 2D Panel will always be inside a dataShelf then! Cube -> 2DPanel -> DataShelf
            // TODO: fix this later in a way that the item is moved with the panel,
            // right now it won't be moved with the parent
            // a.transform.SetParent(transform.parent.parent);
            isOn2DPanel = true;
        });
    }

    public void UpdateAttributeFilters() {
        List<AttributeFilter> temp = new List<AttributeFilter>();
        foreach (var vis in correspondingVisualizations())
        {
            temp.AddRange(vis.AttributeFilters);
        }
        AttributeFilters = temp;
    }

    public void ScaleAxis(float scaleFactor)
    {
        this.axisScaleFactor = scaleFactor;
        transform.localScale *= scaleFactor;
        foreach(var v in correspondingVisualizations())
        {
            v.transform.localScale *= scaleFactor;
        }
    }

    private void activateGhost(WandController grabbingController)
    {
        GameObject ghostClone = Instantiate(ghostCube.gameObject, transform);
        ghostClone.SetActive(true);
        ghostClone.GetComponent<AxisGhost>().OnGrab(grabbingController);
    }

}