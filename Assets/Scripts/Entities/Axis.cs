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

    [SerializeField] TextMeshPro label;
    [SerializeField] TextMeshPro minimumValueDimensionLabel;
    [SerializeField] TextMeshPro maximumValueDimensionLabel;

    // The Id is useful for ID in the data panels (aka protoytpes   )
    public int axisId;

    // Q: What is this for? 
    // This is active when the axis is on the data shelf and has not been dragged to the main scene
    public bool isPrototype;

    public bool isClonedByCloningWidget = false;

    //temporary hack 

    // these values are used with the setInitOrigin in order to set the initial postiion and rotation of the axes
    // This is mostly useful for setting the data inside the data panels (they call the data panel, prototypes)
    Vector3 originPosition;
    Quaternion originRotation;

    // These are the literal game objects for filtering that are there
    // If you want to change these objectst you can just pass a different one instead of them, just make sure they make sense as max and min knobs
    // TODO: get rid of the knobs for the time being and replace them with the actual filters
    [SerializeField] Transform minFilterObject;
    [SerializeField] Transform maxFilterObject;
    
    [SerializeField] Transform minNormaliserObject;
    [SerializeField] Transform maxNormaliserObject;
    [SerializeField] GameObject cloningWidgetGameObject;

    [SerializeField] Renderer ticksRenderer;

    [Space(10)]

    [SerializeField] UnityEvent OnEntered;
    [SerializeField] UnityEvent OnExited;

    // Q: What is this for? 
    // This is used to handle Axis's collision with other objects including the controller object
    // this is mainly controlled by the AxisAnchor script and class
    public HashSet<Axis> ConnectedAxis = new HashSet<Axis>();

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
   
    SteamVR_TrackedObject trackedObject;
    List<Vector3> tracking = new List<Vector3>();
    
    // This comes from the DimensionRange in DataObject
    // x of this vector2 is min and y of this vector2 is max
    Vector2 AttributeRange;

    // This changes the scale size of the ticks 
    float ticksScaleFactor = 1.0f;

    // ghost properties
    // Q: What does ghost property means? 
    Axis ghostSourceAxis = null;

    // TODO: make the value for each tick more clear! it's now not clear what's the value when the user gets over there! 
    // This is called from the sceneManager script which basically sets up the scene that we have at the beginning
    // TODO: Change the SceneManager scene to get to where I want it to be
    public void Init(DataBinding.DataObject srcData, int idx, bool isPrototype = false)
    {
        SourceIndex = idx;
        axisId = idx;
        name = "axis " + srcData.indexToDimension(idx);

        //  This basically sets the range of the data dimensions that are going to be in the file
        // SrcData is the whole of the data
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
    }

    void Start()
    {

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

        // TODO: turn this cloning into its own method to use with the anchor cloning
        if (isPrototype)
        {
            if (Vector3.Distance(originPosition, transform.position) > 0.25f)
            {
                isPrototype = false;
                GameObject clone = Clone();
                clone.GetComponent<Axis>().OnExited.Invoke();

                // This is the part that we get to do the shaking sequence of the main object
                clone.GetComponent<Axis>().ReturnToOrigin();

                // Only activate the cloning knob when the axis is out of the dataShelf
                // TODO: turn this knob to something else when we move this to a visualization
                cloningWidgetGameObject.SetActive(true);


                SceneManager.Instance.AddAxis(clone.GetComponent<Axis>());
                
                foreach (var obj in GameObject.FindObjectsOfType<WandController>())
                {
                    // It means shaking the controller not the visualization itself
                    if (obj.IsDragging())
                        obj.Shake();
                }
            }
        }
        
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
        Axis axis = clone.GetComponent<Axis>();
        axis.InitOrigin(originPosition, originRotation);
        axis.isClonedByCloningWidget = isClonedByCloningWidget;
        axis.ticksRenderer.material = Instantiate(ticksRenderer.material) as Material;

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
        if (!isTweening)
        {
            transform.parent = controller.transform;
            transform.DOKill();
        }
        GetComponent<Rigidbody>().isKinematic = true;
        isDirty = true;
        return true;
    }

    public void OnRelease(WandController controller)
    {
        transform.parent = null;

        if (!isPrototype)
        {
            // destroy the axis
            // This is the part that controls to see if we're throwing it with some velicity, it needs to be dstoryed
            if (controller.Velocity.magnitude > 0.2f)
            {
                Rigidbody body = GetComponent<Rigidbody>();
                body.isKinematic = false;
                body.useGravity = true;
                body.AddForce(controller.Velocity * -1000);
                gameObject.layer = LayerMask.NameToLayer("TransparentFX");

                transform.DOScale(0.0f, 0.5f).SetEase(Ease.InBack);

                return;
            }
        }
        else
        {
            // return the axis to its position
            ReturnToOrigin();
        }

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

        GetComponent<Rigidbody>().isKinematic = false;
        isDirty = false;
    }

    public void OnDrag(WandController controller)
    {
        isDirty = true;
    }

    public void OnEnter(WandController controller)
    {
        OnEntered.Invoke();
    }

    public void OnExit(WandController controller)
    {
        OnExited.Invoke();
    }

    // This function is responsible for the shaking animation that happens when we get the axis off of the data shelf
    void ReturnToOrigin()
    {
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

    void OnCollisionEnter(Collision collision)
    {
        print(collision.gameObject.name + "  " + collision.contacts[0].ToString());
    }

    void OnCollisionExit(Collision collision)
    {

    }

    // This finds all fo the visualizations that this axis is a part of ... and yes, an axis could be a part of multiple data visualziations
    public List<Visualization> correspondingVisualizations()
    {
        return GameObject.FindObjectsOfType<Visualization>().Where(x => x.axes.Contains(this)).ToList();
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

    public void AnimateTo(Vector3 pos, Quaternion rot)
    {
        transform.DORotateQuaternion(rot, 0.4f).SetEase(Ease.OutBack);
        transform.DOMove(pos, 0.4f).SetEase(Ease.OutBack);        
    }

}