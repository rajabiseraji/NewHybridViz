using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using Staxes;
using System.Linq;
using TMPro;

// a visualization prefab will auto-configure which visuzalization to present depending on the number of attached axes and
// orientation of those axes
public class Visualization : MonoBehaviour, Grabbable, Brushable
{
    [SerializeField] TextMeshPro label;
    // Q: What are the reference Axis? Where is it actually used?
    public struct ReferenceAxis
    {
        public Axis horizontal;
        public Axis vertical;
        public Axis depth;
        public Axis horizontal2;
        public Axis vertical2;
        public Axis depth2;

        public void Clear()
        {
            horizontal = null;
            vertical = null;
            depth = null;
            horizontal2 = null;
            vertical2 = null;
            depth2 = null;
        }
    }

    public List<Axis> axes { get; internal set; }
    public int axesCount { get { return axes.Count; } }

    ReferenceAxis referenceAxis;

    public ReferenceAxis ReferenceAxis1
    {
        get { return referenceAxis; }
    }

    [SerializeField]
    /* It basically adds some empty gameobjects as athe placeholders of multuple different components and the assigns them dynamically in the program */
    GameObject histogramObject;

    [SerializeField]
    GameObject scatterplot2DObject;

    [SerializeField]
    GameObject parallelCoordsObject;

    [SerializeField]
    GameObject scatterplot3DObject;

    [SerializeField]
    GameObject linkedScatterplots;

    //[SerializeField]
    //GameObject sizePanel;

    [SpaceAttribute(10)]

    // Q: What does this do?
    // This is the holder of the all view objects (including the diagram placeholders)
    [SerializeField]
    GameObject viewObjectsRoot;

    // Just a list of all of the placeholder and actual visualization obejcts
    List<GameObject> visualizationObjects = new List<GameObject>();

    bool isBrushing;
    bool isDetailOnDemand;

    bool isDirty;

    GameObject theSPLOMReference = null;

    public GameObject TheSPLOMReference
    {
        get { return theSPLOMReference; }
        set { theSPLOMReference = value; }
    }

    bool isSPLOMElement = false;
    public bool IsSPLOMElement
    {
        get { return isSPLOMElement; }
        set { isSPLOMElement = value; }
    }

    // Is this really usesd in the actual syustsm?
    Vector3 brushPosition = Vector3.zero;

    // object-relative coordinates for the visualisation distortion
    // ftl: Front top left - btl: Back(?) top left
    // Front and back are used for depth I suppose (e.g. in the case of 3D systems and coordinates)
    public Vector3 ftl = new Vector3();
    public Vector3 ftr = new Vector3();
    public Vector3 fbl = new Vector3();
    public Vector3 fbr = new Vector3();

    public Vector3 btl = new Vector3();
    public Vector3 btr = new Vector3();
    public Vector3 bbl = new Vector3();
    public Vector3 bbr = new Vector3();

    //minimum and maximum normalization values on axes handles
    public float minXNormalizer = -1f;
    public float maxXNormalizer = 1f;
    public float minYNormalizer = -1f;
    public float maxYNormalizer = 1f;
    public float minZNormalizer = -1f;
    public float maxZNormalizer = 1f;

    //minimum and maximum filtering values on axes handles
    public float minXFilter = -1f;
    public float maxXFilter = 1f;
    public float minYFilter = -1f;
    public float maxYFilter = 1f;
    public float minZFilter = -1f;
    public float maxZFilter = 1f;

    int HISTOGRAM_BIN_SIZE = 10;

    //int linkingField = 1;

    public enum ViewType
    {
        Histogram,
        //ParallelCoordinates,
        Scatterplot2D,
        Scatterplot3D
    }
    public ViewType viewType;

    List<View> instantiatedViews = new List<View>();

    // TODO: use this position to make the details on demand change position!
    Vector3 detailOnDemandPosition = Vector3.zero;
    Vector3[] histogramPositions;

    // Events when staxes are created
    public delegate void StaxesAction(string[] visualisationType);
    public static event StaxesAction OnStaxesAction;

    // The DoD component that we need to change
    DetailsOnDemand DetailsOnDemandComponent = null;

    // Localized DataObject for each of the visualizations
    public DataBinding.DataObject dataObjectReference;

    public List<AttributeFilter> AttributeFilters = new List<AttributeFilter>();
    private List<AttributeFilter> GlobalFiltersInstance = new List<AttributeFilter>();

    public GameObject filterBubbleGameobject;
    public GameObject filterBubbleButtonGameobject;
    public GameObject filterBubbleCompactGameobject;

    void Awake()
    {
        axes = new List<Axis>();
        visualizationObjects.Add(histogramObject);
        visualizationObjects.Add(scatterplot2DObject);
        visualizationObjects.Add(parallelCoordsObject);
        visualizationObjects.Add(scatterplot3DObject);
        visualizationObjects.Add(linkedScatterplots);
    }

    void Start()
    {   
        //add the tag
        tag = "Visualisation";
        string myName = "";
        foreach (var item in axes)
        {
            myName += item.name + " ";
        }
        name = myName + "visualisation";
        if(label && axesCount > 1) {
            string tmp = name.Replace("axis", "");
            label.text = tmp.Replace("visualisation", "");
        }

        Debug.Assert((filterBubbleGameobject != null), "In Vis: The filter bubble object cannot be null");
        Debug.Assert((filterBubbleButtonGameobject != null), "In Vis: The filter bubble button object cannot be null");
        Debug.Assert((filterBubbleCompactGameobject != null), "In Vis: The filter bubble object cannot be null");

        //listen to menu events
        EventManager.StartListening(ApplicationConfiguration.OnSlideChangePointSize, OnChangePointSize);
        EventManager.StartListening(ApplicationConfiguration.OnSlideChangeMinPointSize, OnChangeMinPointSize);
        EventManager.StartListening(ApplicationConfiguration.OnSlideChangeMaxPointSize, OnChangeMaxPointSize);
        EventManager.StartListening(ApplicationConfiguration.OnColoredAttributeChanged, OnAttributeChanged);
        EventManager.StartListening(ApplicationConfiguration.OnLinkedAttributeChanged, OnAttributeChanged);
        EventManager.StartListening(ApplicationConfiguration.OnScatterplotAttributeChanged, OnAttributeChanged);

        // listen to filtering events
        // this is for global filtering
        EventManager.StartListening(ApplicationConfiguration.OnFilterSliderChanged, OnGlobalFilterChanged);
        // this is for local filtering
        EventManager.StartListening(ApplicationConfiguration.OnLocalFilterSliderChanged, OnLocalFilterChanged);

        
        //ignore raycasts for brushing/details on demand
        GetComponent<SphereCollider>().gameObject.layer = 2;
    }

    void OnDestroy()
    {
        EventManager.StopListening(ApplicationConfiguration.OnSlideChangePointSize, OnChangePointSize);
        EventManager.StopListening(ApplicationConfiguration.OnColoredAttributeChanged, OnAttributeChanged);
        EventManager.StopListening(ApplicationConfiguration.OnLinkedAttributeChanged, OnAttributeChanged);
        EventManager.StopListening(ApplicationConfiguration.OnScatterplotAttributeChanged, OnAttributeChanged); 
        // listen to filtering events
        EventManager.StopListening(ApplicationConfiguration.OnFilterSliderChanged, OnGlobalFilterChanged);
        // this is for local filtering
        EventManager.StopListening(ApplicationConfiguration.OnLocalFilterSliderChanged, OnLocalFilterChanged);
  

        foreach (Axis axis in axes)
        {
            axis.OnFiltered.RemoveListener(Axis_OnFilter);
            axis.OnNormalized.RemoveListener(Axis_OnNormalize);
        }
    }

    // TODO: to be called for the filter bubble area
    private void Axis_OnNormalize(float minNormalizer, float maxNormalizer)
    {
        // precondition 2: be a histogram visualization
        if (viewType == ViewType.Histogram && histogramObject != null)
        {
            //destroy current histogram object

            Mesh mToUpdate = histogramObject.GetComponentInChildren<MeshFilter>().mesh;

            VisualisationFactory.UpdatetHistogramMesh(SceneManager.Instance.dataObject,
            axes[0].axisId,
            (int)HISTOGRAM_BIN_SIZE,
            false,
            1f,
            VisualisationFactory.Instance.histogramMaterial,
            histogramObject.transform,
            axes[0].MinFilter,
            axes[0].MaxFilter,
            minNormalizer,
            maxNormalizer,
            ref mToUpdate,
            AddandSortRange(AttributeFilters, GlobalFiltersInstance));

            histogramObject.GetComponentInChildren<MeshFilter>().mesh = mToUpdate;

            //recalculate one based on the normalized value
        }
    }

    // This is the function assigned to the listener for AxisFiltered event
    // It updates the visualization by updating the min and max filters on an axis
    // We alter the data that is fed to the Update_*_Mesh function to fliter the 
    // data before it even gets processed by that function 
    // This is just for histograms!!
    private void Axis_OnFilter(float minFilter, float maxFilter)
    {
        // precondition 2: be a histogram visualization
        if (viewType == ViewType.Histogram && histogramObject != null)
        {
            //destroy current histogram object

            Mesh mToUpdate = histogramObject.GetComponentInChildren<MeshFilter>().mesh;

            VisualisationFactory.UpdatetHistogramMesh(SceneManager.Instance.dataObject,
            axes[0].axisId,
            (int)HISTOGRAM_BIN_SIZE,
            false,
            1f,
            VisualisationFactory.Instance.histogramMaterial,
            histogramObject.transform,
            minFilter,
            maxFilter,
            axes[0].MinNormaliser,
            axes[0].MaxNormaliser,
            ref mToUpdate,
            AddandSortRange(AttributeFilters, GlobalFiltersInstance));

            histogramObject.GetComponentInChildren<MeshFilter>().mesh = mToUpdate;

        }
    }

    public void DoFilter(int AxisId, float minFilterValue) {
        Debug.Log("I'm filtering with " + AxisId + " and min value of " + minFilterValue);
        instantiatedViews.ForEach(view => {
            // since there's nothing that differentiates the scatterplot and other views, we should find a way to filter them in here!
            if(!view.isParallelCoordsView) 
                view.doFilter(AxisId, minFilterValue);
        });
    }

    public void DoFilter(List<AttributeFilter> filters) {
        Debug.Log("I'm filtering with all filters count of" + filters.Count);
        instantiatedViews.ForEach(view => {
            // since there's nothing that differentiates the scatterplot and other views, we should find a way to filter them in here!
            if(!view.isParallelCoordsView) // TODO: find a way to filter histograms and parallel coords
                view.doFilter(filters);
        });
    }
    public void DoFilter() {
        Debug.Log("I'm filtering with all filters count of" + AttributeFilters.Count);
        instantiatedViews.ForEach(view => {
            // since there's nothing that differentiates the scatterplot and other views, we should find a way to filter them in here!
            if(!view.isParallelCoordsView) // TODO: find a way to filter histograms and parallel coords
                view.doFilter(AttributeFilters);
        });
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // TODO: learn from the animations in this place for the other components
    void OnEnable()
    {
        transform.DOScale(0.0f, 0.35f)
            .From()
            .SetEase(Ease.OutBack);
    }

    void OnDisable()
    {
        transform.DOKill(true);
    }

    public void ShowHistogram(bool hide)
    {
        histogramObject.SetActive(hide);
    }

    // This is the important part of code where gets called whenever we add an axis to an existing  visuaslization
    /* The event is sent through the AXIS class and the listener for the fitlering system is in this class (aka the visualization class) */
    public void AddAxis(Axis axis)
    {
        if (!axes.Contains(axis))
        {
            axes.Add(axis);
            axis.OnFiltered.AddListener(Axis_OnFilter);
            axis.OnNormalized.AddListener(Axis_OnNormalize);
        }
        if (axis != null)
        {
            // We want to update the filters of the visualizations based on what filters each axis has!
            AddNewFilterToFilterBubbles(axis.AttributeFilters);
            UpdateViewType();
            UpdateVisualizations();
        }
    }

    // TODO: Change this so that we can accomodate for the time series and stuff like that too
    // Right now the number of the Axis dictate the type pof the visualization
    public void UpdateViewType()
    {
        ViewType newType;
        switch (axes.Count)
        {
            case 1:
                newType = ViewType.Histogram;
                break;
            case 2:
                newType = ViewType.Scatterplot2D;
                break;
            case 3:
                newType = ViewType.Scatterplot3D;
                break;

            default:
                newType = ViewType.Histogram;
                break;
        }
        if (newType != viewType)
        {
            viewType = newType;
            switch (viewType)
            {
                case ViewType.Histogram:
                    EnableVisualizationObject(histogramObject);
                    break;
                case ViewType.Scatterplot2D:
                    EnableVisualizationObject(scatterplot2DObject);

                    break;
                case ViewType.Scatterplot3D:
                    EnableVisualizationObject(scatterplot3DObject);
                    break;

            }
            // TODO: Figure out what do staxes do!
            // Staxes are just more fancy tuples! that's all! they're in Util/Tuple.cs
            if (OnStaxesAction != null)
                fireOnStaxesEvent("CREATED");
        }
    }

    // TODO: make it more performant by removing the destroy steps and 
    // changing them into; modify steps in runtime
    // This is only called when an attribiute is changed or when a new axis is added
    public void UpdateVisualizations(DataBinding.DataObject dobjs = null)
    {
        foreach (Transform t in histogramObject.transform)
        {
            Destroy(t.gameObject);
        }
        foreach (Transform t in parallelCoordsObject.transform)
        {
            Destroy(t.gameObject);
        }
        foreach (Transform t in scatterplot2DObject.transform)
        {
            Destroy(t.gameObject);
        }
        foreach (Transform t in scatterplot3DObject.transform)
        {
            Destroy(t.gameObject);
        }

        var usedDataObject = dobjs !=  null ? dobjs : SceneManager.Instance.dataObject;

        if (axes.Count == 1)
        {
            // Visualization factory gets the specs for each of the histograms and then spits it out as a Tuple of the created histogram gameobject and the postions of that gameobject!
            Staxes.Tuple<GameObject, Vector3[]> histT = VisualisationFactory.Instance.CreateBarHistogramView(usedDataObject,
                axes[0].axisId,
                (int)HISTOGRAM_BIN_SIZE,
                false,
                1f,
                VisualisationFactory.Instance.histogramMaterial,
                histogramObject.transform,
                axes[0].MinFilter,
                axes[0].MaxFilter,
                axes[0].MinNormaliser,
                axes[0].MaxNormaliser,
                AddandSortRange(AttributeFilters, GlobalFiltersInstance));

            GameObject hist = histT.Item1;
            histogramPositions = histT.Item2;

            hist.transform.SetParent(histogramObject.transform, false);
        }
        else if (axes.Count == 2) //
        {
            Axis axisV = axes[0].IsHorizontal ? axes[1] : axes[0];
            Axis axisH = axes[0].IsHorizontal ? axes[0] : axes[1];

            referenceAxis.Clear();
            referenceAxis.horizontal = axisH;
            referenceAxis.vertical = axisV;

            Staxes.Tuple<GameObject, View> parallelT = VisualisationFactory.Instance.CreateSingle2DView(this,usedDataObject, axes[0].axisId, axes[1].axisId, -1, VisualisationAttributes.Instance.LinkedAttribute,
                MeshTopology.Lines, VisualisationFactory.Instance.linesGraphMaterial, true);
            GameObject parallel = parallelT.Item1;
            parallel.transform.SetParent(parallelCoordsObject.transform, false);
            instantiatedViews.Add(parallelT.Item2);
            parallelT.Item2.setDefaultColor();
            parallelT.Item1.layer = LayerMask.NameToLayer("View");
            parallelT.Item1.tag = "View";
            parallelT.Item1.name += " parallel";
            parallelT.Item2.setColors(VisualisationAttributes.Instance.colors, true);            
            DetailsOnDemandComponent = parallelT.Item1.AddComponent<DetailsOnDemand>();
            DetailsOnDemandComponent.VisualizationReference = this;
            parallelT.Item1.GetComponentInChildren<DetailsOnDemand>().setTransformParent(transform);

            Staxes.Tuple<GameObject, View> scatter2DT = VisualisationFactory.Instance.CreateSingle2DView(this, usedDataObject, axisH.axisId, axisV.axisId, -1, VisualisationAttributes.Instance.LinkedAttribute, MeshTopology.Points,
                VisualisationAttributes.Instance.LinkedAttribute < 0 ? VisualisationFactory.Instance.pointCloudMaterial : VisualisationFactory.Instance.connectedPointLineMaterial);
            GameObject scatter2 = scatter2DT.Item1;

            scatter2.transform.SetParent(scatterplot2DObject.transform, false);
            instantiatedViews.Add(scatter2DT.Item2);
            scatter2DT.Item2.setDefaultColor();
            scatter2DT.Item2.setColors(VisualisationAttributes.Instance.colors, false);
            scatter2DT.Item2.setSizes(VisualisationAttributes.Instance.sizes);
            OnChangePointSize(VisualisationAttributes.Instance.ScatterplotDefaultPointSize);
            OnChangeMinPointSize(VisualisationAttributes.Instance.MinScatterplotPointSize);
            OnChangeMaxPointSize(VisualisationAttributes.Instance.MaxScatterplotPointSize);
            scatter2DT.Item1.layer = LayerMask.NameToLayer("View");
            scatter2DT.Item1.tag = "View";
            scatter2DT.Item1.name += " scatterplot2D";
            DetailsOnDemandComponent = scatter2DT.Item1.AddComponent<DetailsOnDemand>();
            DetailsOnDemandComponent.VisualizationReference = this;
            scatter2DT.Item1.GetComponentInChildren<DetailsOnDemand>().setTransformParent(transform);

            //scatter2DT.Item1.AddComponent<BrushingAndLinking>();
        }
        else if (axes.Count == 3)
        {
            Vector3 CameraFwd = Camera.main.transform.forward;
            CameraFwd.y = 0f;

            Axis axisV = axes.FirstOrDefault(x => x.IsVertical);
            var horizontals = axes.Where(x => x != axisV).ToList();
            Axis h0 = horizontals[0];
            Axis h1 = horizontals[1];

            Axis depth = null;
            Axis horizontal = null;

            float dothp0fwd = Vector3.Dot(CameraFwd, h0.transform.up);
            if (dothp0fwd > 0.5f || dothp0fwd < -0.5f)
            {
                depth = h0;
                horizontal = h1;
            }
            else
            {
                depth = h1;
                horizontal = h0;
            }

            referenceAxis.Clear();
            referenceAxis.horizontal = horizontal;
            referenceAxis.vertical = axisV;
            referenceAxis.depth = depth;

            if (horizontal != null && axisV != null && depth != null)
            {

                Staxes.Tuple<GameObject, View> scatter3DT = VisualisationFactory.Instance.CreateSingle2DView(this, usedDataObject,
                    referenceAxis.horizontal.axisId, referenceAxis.vertical.axisId, referenceAxis.depth.axisId, VisualisationAttributes.Instance.LinkedAttribute, MeshTopology.Points,
                    VisualisationAttributes.Instance.LinkedAttribute < 0 ? VisualisationFactory.Instance.pointCloudMaterial : VisualisationFactory.Instance.connectedPointLineMaterial, false);

                GameObject scatter = scatter3DT.Item1;
                scatter.transform.SetParent(scatterplot3DObject.transform, false);
                instantiatedViews.Add(scatter3DT.Item2);
                scatter3DT.Item2.setDefaultColor();
                scatter3DT.Item2.setColors(VisualisationAttributes.Instance.colors, false);
                scatter3DT.Item2.setSizes(VisualisationAttributes.Instance.sizes);
                OnChangePointSize(VisualisationAttributes.Instance.ScatterplotDefaultPointSize);
                OnChangeMinPointSize(VisualisationAttributes.Instance.MinScatterplotPointSize);
                OnChangeMaxPointSize(VisualisationAttributes.Instance.MaxScatterplotPointSize);
                scatter3DT.Item1.tag = "View";
                scatter3DT.Item1.name += " scatterplot3D";
                DetailsOnDemandComponent = scatter3DT.Item1.AddComponent<DetailsOnDemand>();
                scatter3DT.Item1.GetComponentInChildren<DetailsOnDemand>().setTransformParent(transform);
                DetailsOnDemandComponent.VisualizationReference = this;

                //TODO: erase
                //scatter3DT.Item2.updateSizeChannel(1, usedDataObject.getDimension(1));
            }
        }
        else if (axes.Count == 4)
        {
            Axis axisV1 = axes[0].IsHorizontal ? axes[1] : axes[0];
            Axis axisH1 = axes[0].IsHorizontal ? axes[0] : axes[1];

            Axis axisV2 = axes[2].IsHorizontal ? axes[3] : axes[2];
            Axis axisH2 = axes[2].IsHorizontal ? axes[2] : axes[3];

            referenceAxis.Clear();
            referenceAxis.vertical = axisV1;
            referenceAxis.horizontal = axisH1;
            referenceAxis.vertical2 = axisV2;
            referenceAxis.horizontal2 = axisH2;

            //create the linked visualisation
            var linkedView = VisualisationFactory.Instance.CreateLinked2DScatterplotsViews(this, usedDataObject,
                axisH1.axisId, axisV1.axisId, axisH2.axisId, axisV2.axisId,
                VisualisationFactory.Instance.linkedViewsMaterial);
            linkedView.Item1.transform.SetParent(linkedScatterplots.transform, false);
            linkedView.Item2.setColors(VisualisationAttributes.Instance.colors, true);
            linkedView.Item1.tag = "View";
            linkedView.Item1.name += " linkedView";

        }
    }

    void CalculateCorners1(Axis axisA, Axis axisB, Axis axisC, ref Vector3 ftl, ref Vector3 ftr, ref Vector3 fbl, ref Vector3 fbr)
    {
        ftl = axisA.transform.TransformPoint(Vector3.up * 0.5f);
        ftr = axisB.transform.TransformPoint(Vector3.up * 0.5f);
        fbl = axisA.transform.TransformPoint(Vector3.down * 0.5f);
        fbr = axisB.transform.TransformPoint(Vector3.down * 0.5f);
    }

    void CalculateCorners2(Axis axisA, Axis axisB, Axis axisC, ref Vector3 ftl, ref Vector3 ftr, ref Vector3 fbl, ref Vector3 fbr)
    {
        Vector3 up = axisA.transform.TransformVector(Vector3.up * 0.5f);
        Vector3 down = axisA.transform.TransformVector(Vector3.down * 0.5f);

        ftl = up + axisB.transform.TransformVector(Vector3.down * 0.5f);
        ftr = up + axisB.transform.TransformVector(Vector3.up * 0.5f);
        fbl = down + axisB.transform.TransformVector(Vector3.down * 0.5f);
        fbr = down + axisB.transform.TransformVector(Vector3.up * 0.5f);
    }

    void CalculateCorners4(Axis axisA, Axis axisB, Axis axisC, ref Vector3 ftl, ref Vector3 ftr, ref Vector3 fbl, ref Vector3 fbr, ref Vector3 btl, ref Vector3 btr, ref Vector3 bbl, ref Vector3 bbr)
    {
        Vector3 up = axisA.transform.TransformVector(Vector3.up * 0.5f);
        Vector3 down = axisA.transform.TransformVector(Vector3.down * 0.5f);
        Vector3 forward = axisC.transform.TransformVector(Vector3.down * 0.5f);
        Vector3 back = axisC.transform.TransformVector(Vector3.up * 0.5f);

        ftl = up + axisB.transform.TransformVector(Vector3.down * 0.5f) + forward;
        ftr = up + axisB.transform.TransformVector(Vector3.up * 0.5f) + forward;
        fbl = down + axisB.transform.TransformVector(Vector3.down * 0.5f) + forward;
        fbr = down + axisB.transform.TransformVector(Vector3.up * 0.5f) + forward;

        btl = up + axisB.transform.TransformVector(Vector3.down * 0.5f) + back;
        btr = up + axisB.transform.TransformVector(Vector3.up * 0.5f) + back;
        bbl = down + axisB.transform.TransformVector(Vector3.down * 0.5f) + back;
        bbr = down + axisB.transform.TransformVector(Vector3.up * 0.5f) + back;
    }

    void CalculateCorners3(Axis axisA, Axis axisB, Axis axisC, ref Vector3 ftl, ref Vector3 ftr, ref Vector3 fbl, ref Vector3 fbr)
    {
        ftl = axisA.transform.TransformPoint(Vector3.up * 0.5f);
        ftr = axisB.transform.TransformPoint(new Vector3(12, -0.5f, 0));
        fbl = axisA.transform.TransformPoint(Vector3.down * 0.5f);
        fbr = axisB.transform.TransformPoint(Vector3.down * 0.5f);
    }

    // returns the extents (bounds) of the axis in world coordinates
    List<Vector3> Extents(params Axis[] axes)
    {
        List<Vector3> results = new List<Vector3>();

        foreach (var a in axes)
        {
            float distA = axes.Where(x => x != a)
                              .SelectMany(x => x.Points())
                              .Aggregate(0.0f, (acc, x) => acc + Vector3.Distance(x, a.Points()[0]));

            float distB = axes.Where(x => x != a)
                              .SelectMany(x => x.Points())
                              .Aggregate(0.0f, (acc, x) => acc + Vector3.Distance(x, a.Points()[1]));

            if (distA > distB)
            {
                results.Add(a.Points()[0]);
            }
            else
            {
                results.Add(a.Points()[1]);
            }
        }
        return results;
    }

    // Kinda everything about the visualizations are handled here 
    void LateUpdate()
    {

         CheckFilterBubble();

        // Check to see if the visualization is falling down! 
        // If they are, then just get rid of them and destroy the whole thing
        if(transform.position.y < -30f) {
            foreach (var axis in axes)
            {
                Destroy(axis);
            }
            Destroy(gameObject);
        }

        UpdateViewType();
        if(label && !string.IsNullOrEmpty(label.text) && axesCount > 1) {
            string lab = "";
            foreach (var item in axes)
            {
                lab += item.name + " ";
            }
            lab = lab.Replace("axis", " ");
            lab = lab.Replace("visualisation", "");
            label.text = lab;
        }

        switch (viewType)
        {
            case ViewType.Histogram:
                {
                    if (axes.Count > 0)
                    {
                        Vector3 pos = axes[0].transform.position;
                        pos += axes[0].transform.TransformDirection(Vector3.right * 0.1f);
                        transform.position = pos;
                        transform.rotation = axes[0].transform.rotation;

                        Vector3 up = axes[0].transform.TransformPoint(Vector3.up * 0.5f);
                        Vector3 down = axes[0].transform.TransformPoint(Vector3.down * 0.5f);

                        ftl = up;
                        ftr = up;
                        fbl = down;
                        fbr = down;

                        btl = up;
                        btr = up;
                        bbl = down;
                        bbr = down;

                        minYNormalizer = axes[0].MinNormaliser;
                        maxYNormalizer = axes[0].MaxNormaliser;

                        minYFilter = axes[0].MinFilter;
                        maxYFilter = axes[0].MaxFilter;

                    }
                }
                break;

            case ViewType.Scatterplot2D:
                {
                    tag = "Scatterplot2D";
                    Axis axisV = referenceAxis.vertical;
                    Axis axisH = referenceAxis.horizontal;
                    if (axisV != null && axisH != null)
                    {
                        Vector3 center = (axisV.transform.position + axisH.transform.position) / 2;
                        Vector3 axisAForward = Vector3.Cross(center - axisV.transform.position, axisV.Up);
                        Vector3 axisBForward = Vector3.Cross(axisH.transform.position - center, axisH.Up);
                        Vector3 visforward = (axisAForward + axisBForward) * 0.5f;
                        Vector3 visup = (axisV.Up + axisH.Up) * 0.5f;

                        List<Vector3> extents = Extents(axisV, axisH);

                        transform.position = extents.Aggregate(Vector3.zero, (acc, x) => acc + x) / extents.Count;
                        // Gets rid of the annoying message for now
                        // TODO: fix it for real!
                        if(visforward != Vector3.zero && axisV.Up != Vector3.zero)
                            transform.rotation = Quaternion.LookRotation(visforward, axisV.Up);

                        // get the corners of the visualization in space
                        CalculateCorners2(axisV, axisH, null, ref ftl, ref ftr, ref fbl, ref fbr);

                        ftl = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((ftl)));
                        ftr = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((ftr)));
                        fbl = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((fbl)));
                        fbr = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((fbr)));

                        btl = ftl;
                        btr = ftr;
                        bbl = fbl;
                        bbr = fbr;

                        // This is used to transform the different rendering materials of the system
                        foreach (var r in scatterplot2DObject.GetComponentsInChildren<Renderer>())
                        {
                            r.material.SetVector("_ftl", scatterplot2DObject.transform.InverseTransformPoint(ftl));
                            r.material.SetVector("_ftr", scatterplot2DObject.transform.InverseTransformPoint(ftr));
                            r.material.SetVector("_fbl", scatterplot2DObject.transform.InverseTransformPoint(fbl));
                            r.material.SetVector("_fbr", scatterplot2DObject.transform.InverseTransformPoint(fbr));

                            r.material.SetVector("_btl", scatterplot2DObject.transform.InverseTransformPoint(btl));
                            r.material.SetVector("_btr", scatterplot2DObject.transform.InverseTransformPoint(btr));
                            r.material.SetVector("_bbl", scatterplot2DObject.transform.InverseTransformPoint(bbl));
                            r.material.SetVector("_bbr", scatterplot2DObject.transform.InverseTransformPoint(bbr));

                            r.material.SetFloat("_MinX", axisH.MinFilter);
                            r.material.SetFloat("_MaxX", axisH.MaxFilter);
                            r.material.SetFloat("_MinY", axisV.MinFilter);
                            r.material.SetFloat("_MaxY", axisV.MaxFilter);
                            r.material.SetFloat("_MinZ", -1f);
                            r.material.SetFloat("_MaxZ", 1f);

                            r.material.SetFloat("_MinNormX", axisH.MinNormaliser);
                            r.material.SetFloat("_MaxNormX", axisH.MaxNormaliser);
                            r.material.SetFloat("_MinNormY", axisV.MinNormaliser);
                            r.material.SetFloat("_MaxNormY", axisV.MaxNormaliser);
                            r.material.SetFloat("_MinNormZ", -1f);
                            r.material.SetFloat("_MaxNormZ", 1f);

                            minXNormalizer = axisH.MinNormaliser;
                            maxXNormalizer = axisH.MaxNormaliser;
                            minYNormalizer = axisV.MinNormaliser;
                            maxYNormalizer = axisV.MaxNormaliser;

                            minXFilter = axisH.MinFilter;
                            maxXFilter = axisH.MaxFilter;
                            minYFilter = axisV.MinFilter;
                            maxYFilter = axisV.MaxFilter;

                        }
                    }
                }
                break;
            case ViewType.Scatterplot3D:
                {
                    tag = "Scatterplot3D";
                    Axis axisV = referenceAxis.vertical;
                    Axis axisH = referenceAxis.horizontal;
                    Axis axisD = referenceAxis.depth;

                    if (axisV != null && axisH != null && axisD != null)
                    {
                        Vector3 center = (axisV.transform.position + axisH.transform.position + axisD.transform.position) / 3;
                        Vector3 axisAForward = Vector3.Cross(center - axisV.transform.position, axisV.Up);
                        Vector3 axisBForward = Vector3.Cross(axisH.transform.position - center, axisH.Up);
                        Vector3 visforward = (axisAForward + axisBForward) * 0.5f;
                        Vector3 visup = (axisV.Up + axisH.Up) * 0.5f;

                        List<Vector3> extents = Extents(axisV, axisH);

                        Vector3 targetPos = extents.Aggregate(Vector3.zero, (acc, x) => acc + x) / extents.Count;
                        float direction = 1.0f;
                        if (axisD.transform.InverseTransformPoint(targetPos).y < 0)
                        {
                            direction = -1.0f;
                        }
                        transform.position = targetPos + axisD.transform.TransformVector(Vector3.down * 0.5f * direction);
                        transform.rotation = Quaternion.LookRotation(visforward, axisV.Up);

                        // get the corners of the visualization in space
                        CalculateCorners4(axisV, axisH, axisD, ref ftl, ref ftr, ref fbl, ref fbr, ref btl, ref btr, ref bbl, ref bbr);

                        ftl = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((ftl)));
                        ftr = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((ftr)));
                        fbl = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((fbl)));
                        fbr = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((fbr)));

                        btl = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((btl)));
                        btr = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((btr)));
                        bbl = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((bbl)));
                        bbr = scatterplot2DObject.transform.TransformPoint(scatterplot2DObject.transform.InverseTransformVector((bbr)));

                        foreach (var r in scatterplot3DObject.GetComponentsInChildren<Renderer>())
                        {
                            r.material.SetVector("_ftl", scatterplot3DObject.transform.InverseTransformPoint(ftl));
                            r.material.SetVector("_ftr", scatterplot3DObject.transform.InverseTransformPoint(ftr));
                            r.material.SetVector("_fbl", scatterplot3DObject.transform.InverseTransformPoint(fbl));
                            r.material.SetVector("_fbr", scatterplot3DObject.transform.InverseTransformPoint(fbr));

                            r.material.SetVector("_btl", scatterplot3DObject.transform.InverseTransformPoint(btl));
                            r.material.SetVector("_btr", scatterplot3DObject.transform.InverseTransformPoint(btr));
                            r.material.SetVector("_bbl", scatterplot3DObject.transform.InverseTransformPoint(bbl));
                            r.material.SetVector("_bbr", scatterplot3DObject.transform.InverseTransformPoint(bbr));

                            r.material.SetFloat("_MinX", axisH.MinFilter);
                            r.material.SetFloat("_MaxX", axisH.MaxFilter);
                            r.material.SetFloat("_MinY", axisV.MinFilter);
                            r.material.SetFloat("_MaxY", axisV.MaxFilter);
                            r.material.SetFloat("_MinZ", axisD.MinFilter);
                            r.material.SetFloat("_MaxZ", axisD.MaxFilter);

                            r.material.SetFloat("_MinNormX", axisH.MinNormaliser);
                            r.material.SetFloat("_MaxNormX", axisH.MaxNormaliser);
                            r.material.SetFloat("_MinNormY", axisV.MinNormaliser);
                            r.material.SetFloat("_MaxNormY", axisV.MaxNormaliser);
                            r.material.SetFloat("_MinNormZ", axisD.MinNormaliser);
                            r.material.SetFloat("_MaxNormZ", axisD.MaxNormaliser);

                            minXNormalizer = axisH.MinNormaliser;
                            maxXNormalizer = axisH.MaxNormaliser;
                            minYNormalizer = axisV.MinNormaliser;
                            maxYNormalizer = axisV.MaxNormaliser;
                            minZNormalizer = axisD.MinNormaliser;
                            maxZNormalizer = axisD.MaxNormaliser;

                            minXFilter = axisH.MinFilter;
                            maxXFilter = axisH.MaxFilter;
                            minYFilter = axisV.MinFilter;
                            maxYFilter = axisV.MaxFilter;
                            minZFilter = axisD.MinFilter;
                            maxZFilter = axisD.MaxFilter;
                        }
                    }
                }
                break;
            default:
                break;
        }

        //handle brushing and linking
        // TODO: enable this in the system for the link and brush
        if (isBrushing)
        {
            //1- Tell the linking script not to update this visualisation
            //pull the vertices of the visualisation and apply brush
            //View scatterplot = instantiatedViews.Find(p => p.Name.Contains("scatterplot"));

            switch (viewType)
            {
                case ViewType.Histogram:
                    break;
                case ViewType.Scatterplot2D:

                    Vector3[] verticesS2d = scatterplot2DObject.GetComponentInChildren<MeshFilter>().mesh.vertices;
                    BrushingAndLinking.updateBrushedIndices(BrushingAndLinking.BrushIndicesPointScatterplot(
                        verticesS2d,
                        BrushingAndLinking.brushPosition,
                        BrushingAndLinking.brushSize / 2f,
                        scatterplot2DObject.transform.InverseTransformVector(ftl),
                        scatterplot2DObject.transform.InverseTransformVector(ftr),
                        scatterplot2DObject.transform.InverseTransformVector(fbl),
                        scatterplot2DObject.transform.InverseTransformVector(fbr),
                        scatterplot2DObject.transform.InverseTransformVector(btl),
                        scatterplot2DObject.transform.InverseTransformVector(btr),
                        scatterplot2DObject.transform.InverseTransformVector(bbl),
                        scatterplot2DObject.transform.InverseTransformVector(bbr),
                        scatterplot2DObject.transform, false),
                        false);

                    break;
                case ViewType.Scatterplot3D:
                    Vector3[] verticesS3d = scatterplot3DObject.GetComponentInChildren<MeshFilter>().mesh.vertices;
                    BrushingAndLinking.updateBrushedIndices(BrushingAndLinking.BrushIndicesPointScatterplot(
                        verticesS3d,
                        BrushingAndLinking.brushPosition,
                        BrushingAndLinking.brushSize / 2f,
                        scatterplot3DObject.transform.InverseTransformVector(ftl),
                        scatterplot3DObject.transform.InverseTransformVector(ftr),
                        scatterplot3DObject.transform.InverseTransformVector(fbl),
                        scatterplot3DObject.transform.InverseTransformVector(fbr),
                        scatterplot3DObject.transform.InverseTransformVector(btl),
                        scatterplot3DObject.transform.InverseTransformVector(btr),
                        scatterplot3DObject.transform.InverseTransformVector(bbl),
                        scatterplot3DObject.transform.InverseTransformVector(bbr),
                        scatterplot3DObject.transform, true), false);
                    break;
                default:
                    break;
            }
        }
    }

    // Called every frame
    private void CheckFilterBubble() {
        // This is of course very bad for the performance! so maybe do something about it! 
        // TODO: performance fix in here
        // Check if the parent visualization's axes are on the proto then just hide the whole thing at the beginning
        
            
        // if(axes.Any(axis => axis.isPrototype || axis.parentIsMoving)) {
        if(axes.Any(axis => axis.transform.parent != null && axis.transform.parent.tag == "DataShelfPanel")) {
            // axes.ForEach(axis => {
            //     Debug.Log("in viz " + name + " with axis " + axis.name);
            // });
            filterBubbleButtonGameobject.SetActive(false);
            filterBubbleGameobject.SetActive(false);
            filterBubbleCompactGameobject.SetActive(false);
        } else {
            filterBubbleButtonGameobject.SetActive(true);
            filterBubbleGameobject.SetActive(true);
            filterBubbleCompactGameobject.SetActive(true);
        }
    }

    public GameObject GetVisualizationObject(ViewType viewtype)
    {
        switch (viewType)
        {
            case ViewType.Histogram:
                return histogramObject;
            case ViewType.Scatterplot2D:
                return scatterplot2DObject;
            case ViewType.Scatterplot3D:
                return scatterplot3DObject;
        }
        return null;
    }

    public void AxesSnapToGrid(Axis stillAxis, Axis snapTo)
    {
        //get orientation of the still axis
        if (stillAxis.IsVertical)
        {
            //get right vector
            Vector3 right = stillAxis.transform.right;
            snapTo.transform.position = stillAxis.MinPosition + snapTo.Up / 2f;
            snapTo.transform.up = right;
        }
        else if (stillAxis.IsHorizontal)
        {
            //get right vector
            Vector3 up = stillAxis.transform.up;
            snapTo.transform.position = stillAxis.MinPosition + snapTo.Up / 2f;
            snapTo.transform.up = up;
        }
    }

    public Color[] getColorBuffer()
    {
        Color[] colorBuffer = null;
        switch (viewType)
        {
            case ViewType.Histogram:
                colorBuffer = null;
                break;

            case ViewType.Scatterplot2D:
                colorBuffer = scatterplot2DObject.GetComponentInChildren<MeshFilter>().mesh.colors;
                break;
            case ViewType.Scatterplot3D:
                colorBuffer = scatterplot3DObject.GetComponentInChildren<MeshFilter>().mesh.colors;
                break;
        }
        return colorBuffer;

    }

    // This does the act of normalization (the thing that the gray knob does)
    // Makes sure that the points in the system have a value between -0.5 and 0.5 (probably)
    // 
    // Just for linked visualizations
    public Vector3?[] normaliser(Vector3?[] points)
    {
        switch (viewType)
        {
            case ViewType.Histogram:

                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i] != null)
                    {
                        Vector3 normalisedPosition =
                            new Vector3(points[i].Value.x,
                                UtilMath.normaliseValue(points[i].Value.y, axes[0].MinNormaliser, axes[0].MaxNormaliser, -0.5f, 0.5f),
                                points[i].Value.z);

                        if (normalisedPosition.y < -0.5 ||
                         normalisedPosition.y > 0.5)
                        {
                            points[i] = null;
                        }
                        else
                        {
                            points[i] = normalisedPosition;
                        }
                    }
                }
                break;
            case ViewType.Scatterplot2D:
                Axis axisV2D = referenceAxis.vertical;
                Axis axisH2D = referenceAxis.horizontal;

                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i] != null)
                    {
                        Vector3 normalisedPosition =
                            new Vector3(UtilMath.normaliseValue(points[i].Value.x, axisH2D.MinNormaliser, axisH2D.MaxNormaliser, -0.5f, 0.5f),
                                UtilMath.normaliseValue(points[i].Value.y, axisV2D.MinNormaliser, axisV2D.MaxNormaliser, -0.5f, 0.5f),
                                points[i].Value.z);

                        if (normalisedPosition.x < -0.5 ||
                         normalisedPosition.x > 0.5 ||
                         normalisedPosition.y < -0.5 ||
                         normalisedPosition.y > 0.5)
                        {
                            points[i] = null;
                        }
                        else
                        {
                            points[i] = normalisedPosition;
                        }
                    }
                }

                break;
            case ViewType.Scatterplot3D:
                Axis axisH3D = referenceAxis.horizontal;
                Axis axisV3D = referenceAxis.vertical;
                Axis axisD3D = referenceAxis.depth;

                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i] != null)
                    {
                        Vector3 normalisedPosition =
                            new Vector3(UtilMath.normaliseValue(points[i].Value.x, axisH3D.MinNormaliser, axisH3D.MaxNormaliser, -0.5f, 0.5f),
                                UtilMath.normaliseValue(points[i].Value.y, axisV3D.MinNormaliser, axisV3D.MaxNormaliser, -0.5f, 0.5f),
                                UtilMath.normaliseValue(points[i].Value.z, axisD3D.MinNormaliser, axisD3D.MaxNormaliser, -0.5f, 0.5f));

                        if (normalisedPosition.x < -0.5 ||
                         normalisedPosition.x > 0.5 ||
                         normalisedPosition.y < -0.5 ||
                         normalisedPosition.y > 0.5 ||
                            normalisedPosition.z < -0.5 ||
                            normalisedPosition.z > 0.5
                         )
                        {
                            points[i] = null;
                        }
                        else
                        {
                            points[i] = normalisedPosition;
                        }
                    }
                }
                break;
            default:
                break;
        }
        return points;
    }

    // This seesms to be doing the actual act of filtering the points (on a scatterplot)
    // Just for linked visualizations
    public Vector3?[] filter(Vector3?[] points)
    {
        switch (viewType)
        {
            case ViewType.Histogram:
                float minFilter = axes[0].MinFilter;
                float maxFilter = axes[0].MaxFilter;

                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i].Value.y < minFilter || points[i].Value.y > maxFilter)
                    {
                        points[i] = null;
                    }
                }
                break;
            case ViewType.Scatterplot2D:
                Axis axisV2D = referenceAxis.vertical;
                Axis axisH2D = referenceAxis.horizontal;

                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i].Value.y < axisV2D.MinFilter ||
                        points[i].Value.y > axisV2D.MaxFilter ||
                        points[i].Value.x < axisH2D.MinFilter ||
                        points[i].Value.x > axisH2D.MaxFilter)
                    {
                        points[i] = null;
                    }
                }

                break;
            case ViewType.Scatterplot3D:
                Axis axisV3D = referenceAxis.vertical;
                Axis axisH3D = referenceAxis.horizontal;
                Axis axisD3D = referenceAxis.depth;
                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i].Value.y < axisV3D.MinFilter ||
                        points[i].Value.y > axisV3D.MaxFilter ||
                        points[i].Value.x < axisH3D.MinFilter ||
                        points[i].Value.x > axisH3D.MaxFilter ||
                        points[i].Value.z < axisD3D.MinFilter ||
                        points[i].Value.z > axisD3D.MaxFilter)
                    {
                        points[i] = null;
                    }
                }
                break;
        }

        return points;
    }

    // Gives you the points of a visualization this one means the points that are in the mesh array of the whole thing
    // Just for linked visualizations
    public Vector3?[] GetPoints()
    {
        Vector3?[] points = null;

        switch (viewType)
        {
            case ViewType.Histogram:
                {
                    float[] x = SceneManager.Instance.dataObject.getDimension(axes[0].axisId);
                    points = new Vector3?[x.Length];

                    for (int i = 0; i < x.Length; i++)
                    {
                        Vector3 pointToConvert = Vector3.zero;
                        pointToConvert.y += (x[i] - 0.5f);
                        points[i] = pointToConvert;
                    }
                }
                break;

            case ViewType.Scatterplot2D:
                {
                    Mesh mesh = scatterplot2DObject.GetComponentInChildren<MeshFilter>().mesh;
                    points = new Vector3?[mesh.vertices.Length];
                    Array.Copy(mesh.vertices, points, points.Length);
                }
                break;
            case ViewType.Scatterplot3D:
                {
                    Mesh mesh = scatterplot3DObject.GetComponentInChildren<MeshFilter>().mesh;
                    points = new Vector3?[mesh.vertices.Length];
                    Array.Copy(mesh.vertices, points, points.Length);
                }
                break;
            default:
                break;
        }

        return points;
    }

    // This one seems to be doing some kind of projection of the mesh vertices to the 1D axis (whichever is the reference axis it seems)
    // Just for linked visualizations
    public Vector3?[] get1DAxisCoordinates(int index)
    {
        Vector3?[] points = null;
        if (viewType == ViewType.Scatterplot2D)
        {
            if (index <= (axes.Count - 1))
            {
                if (referenceAxis.vertical == axes[index])
                {
                    float[] x = SceneManager.Instance.dataObject.getDimension(axes[index].axisId);
                    points = new Vector3?[x.Length];
                    for (int i = 0; i < x.Length; i++)
                    {
                        Vector3 pointToConvert = Vector3.zero;
                        pointToConvert.y += (x[i] - 0.5f);
                        pointToConvert.x = -0.5f;
                        points[i] = pointToConvert;

                    }
                }
                else
                {
                    float[] y = SceneManager.Instance.dataObject.getDimension(axes[index].axisId);
                    points = new Vector3?[y.Length];
                    for (int i = 0; i < y.Length; i++)
                    {
                        Vector3 pointToConvert = Vector3.zero;
                        pointToConvert.x += (y[i] - 0.5f);
                        pointToConvert.y = -0.5f;
                        points[i] = pointToConvert;

                    }
                }
                Vector3?[] filterPoints = normaliser(filter(points));

            }
        }
        else if (viewType == ViewType.Scatterplot3D)
        {
            if (index <= (axes.Count - 1))
            {
                if (referenceAxis.vertical == axes[index])
                {
                    float[] x = SceneManager.Instance.dataObject.getDimension(axes[index].axisId);
                    points = new Vector3?[x.Length];
                    for (int i = 0; i < x.Length; i++)
                    {
                        Vector3 pointToConvert = Vector3.zero;
                        pointToConvert.y += (x[i] - 0.5f);
                        pointToConvert.x = -0.5f;
                        pointToConvert.z = -0.5f;
                        points[i] = pointToConvert;

                    }
                }
                else if (referenceAxis.horizontal == axes[index])
                {
                    float[] y = SceneManager.Instance.dataObject.getDimension(axes[index].axisId);
                    points = new Vector3?[y.Length];
                    for (int i = 0; i < y.Length; i++)
                    {
                        Vector3 pointToConvert = Vector3.zero;
                        pointToConvert.x += (y[i] - 0.5f);
                        pointToConvert.y = -0.5f;
                        pointToConvert.z = -0.5f;
                        points[i] = pointToConvert;

                    }
                }
                else if (referenceAxis.depth == axes[index])
                {
                    float[] z = SceneManager.Instance.dataObject.getDimension(axes[index].axisId);
                    points = new Vector3?[z.Length];
                    for (int i = 0; i < z.Length; i++)
                    {
                        Vector3 pointToConvert = Vector3.zero;
                        pointToConvert.z += (z[i] - 0.5f);
                        pointToConvert.y = -0.5f;
                        pointToConvert.x = -0.5f;
                        points[i] = pointToConvert;

                    }
                }
                Vector3?[] filterPoints = normaliser(filter(points));

            }
        }
        return points;
    }

    // TODO: the brushing seems non-existent now! 
    void SwapToBrushing()
    {
        switch (viewType)
        {
            case ViewType.Histogram:
                break;

            case ViewType.Scatterplot2D:

                MeshRenderer mr = scatterplot2DObject.GetComponentInChildren<MeshRenderer>();
                mr.material = VisualisationFactory.Instance.pointCloudMaterialBrush;

                break;
            case ViewType.Scatterplot3D:

                break;
            default:
                break;
        }
    }

    void SwapToNotBrushing()
    {
        switch (viewType)
        {
            case ViewType.Histogram:
                break;

            case ViewType.Scatterplot2D:

                MeshFilter mf = scatterplot2DObject.GetComponentInChildren<MeshFilter>();
                mf.gameObject.GetComponent<MeshRenderer>().material = VisualisationFactory.Instance.pointCloudMaterial;

                break;
            case ViewType.Scatterplot3D:

                break;
            default:
                break;
        }
    }

    void EnableVisualizationObject(GameObject vis)
    {
        foreach (var v in visualizationObjects)
        {
            v.SetActive(v == vis);
        }
    }

    #region Interaction
    /// <summary>
    /// listens to slider change values for point size value
    /// </summary>
    /// <param name="pointSize"></param>
    private void OnChangePointSize(float pointSize)
    {
        /* The floats of _Size and min and maxSize in the mesh material are the ones responsible for the actual point size change */
        switch (viewType)
        {
            case ViewType.Histogram:
                break;
            case ViewType.Scatterplot2D:
                foreach (var r in scatterplot2DObject.GetComponentsInChildren<Renderer>())
                {
                    r.material.SetFloat("_Size", pointSize);
                    r.material.SetFloat("_MinSize", VisualisationAttributes.Instance.MinScatterplotPointSize);
                    r.material.SetFloat("_MaxSize", VisualisationAttributes.Instance.MaxScatterplotPointSize);
                }
                break;
            case ViewType.Scatterplot3D:
                foreach (var r in scatterplot3DObject.GetComponentsInChildren<Renderer>())
                {
                    r.material.SetFloat("_Size", pointSize);
                    r.material.SetFloat("_MinSize", VisualisationAttributes.Instance.MinScatterplotPointSize);
                    r.material.SetFloat("_MaxSize", VisualisationAttributes.Instance.MaxScatterplotPointSize);
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// listens to slider change values for min point size value
    /// </summary>
    /// <param name="pointSize"></param>
    private void OnChangeMinPointSize(float pointSize)
    {
        switch (viewType)
        {
            case ViewType.Histogram:
                break;
            case ViewType.Scatterplot2D:
                foreach (var r in scatterplot2DObject.GetComponentsInChildren<Renderer>())
                {
                    r.material.SetFloat("_MinSize", pointSize);
                }
                break;
            case ViewType.Scatterplot3D:
                foreach (var r in scatterplot3DObject.GetComponentsInChildren<Renderer>())
                {
                    r.material.SetFloat("_MinSize", pointSize);
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// listens to slider change values for min point size value
    /// </summary>
    /// <param name="pointSize"></param>
    private void OnChangeMaxPointSize(float pointSize)
    {
        switch (viewType)
        {
            case ViewType.Histogram:
                break;
            case ViewType.Scatterplot2D:
                foreach (var r in scatterplot2DObject.GetComponentsInChildren<Renderer>())
                {
                    r.material.SetFloat("_MaxSize", pointSize);
                }
                break;
            case ViewType.Scatterplot3D:
                foreach (var r in scatterplot3DObject.GetComponentsInChildren<Renderer>())
                {
                    r.material.SetFloat("_MaxSize", pointSize);
                }
                break;
            default:
                break;
        }
    }

    private void OnAttributeChanged(float idx)
    {  
        UpdateVisualizations();
    }

    // This one will be called from the filterBubble button to make the intractivity from that side
    public void AddNewFilterToFilterBubbles(List<Axis> axes) {
        filterBubbleGameobject.GetComponent<FilterBubbleScript>().AddNewFilter(axes);
        updateAxesAttributeFilters();
    }

    // This one will be called from the Axes and the visualization
    public void AddNewFilterToFilterBubbles(List<AttributeFilter> filters) {
        filterBubbleGameobject.GetComponent<FilterBubbleScript>().AddNewFilter(filters);
        updateAxesAttributeFilters();
    }

    private void OnGlobalFilterChanged(float filterAxisId) {
        Debug.Log("On global filter changed + " + AttributeFilters.Count);
        Debug.Log("On global filter changed + " + filterAxisId);
        
        GlobalFiltersInstance = SceneManager.Instance.globalFilters;



        // We should also update the normalizers and then call the onAxisNormalize to take care of the thing!

        // The way to do it is that: 
        // For more than Axes count of 1 - we put the code in the DoFilter so that it invokes the axis.onNormalize for each of the involved axes

        // for histograms we can do it in UpdateVisualization
        // Basically call the set min and set max of the only involved axis 
        filterAndNormalise(axes);
        if(axes.Count == 1) {
            // UpdateVisualizations();
        }
        else {
            DoFilter(AddandSortRange(AttributeFilters, GlobalFiltersInstance));
        }
        updateAxesAttributeFilters();
    }
    private void OnLocalFilterChanged(float visualizationId)
    {  
        // We should also update the normalizers and then call the onAxisNormalize to take care of the thing!
        if((int)visualizationId != GetInstanceID())
            return;
        Debug.Log("OnlocalfilterChaned + " + AttributeFilters.Count);
        filterAndNormalise(axes);
        
        if(axes.Count == 1) {
            // UpdateVisualizations();
        }
        else {
            DoFilter(AttributeFilters);
        }
        updateAxesAttributeFilters();
    }

    private void updateAxesAttributeFilters(){
        foreach (var axis in axes)
        {
            axis.UpdateAttributeFilters();
            // TODO: this is going to be problematic if our axis is part of more than one visualization!
        }
    }

    private void filterAndNormalise(List<Axis> axes) {
        foreach (var axis in axes)
        {
            float[] filteredValues = SceneManager.Instance.dataObject.getFilteredCol(SceneManager.Instance.dataObject.DataArray, axis.axisId, AddandSortRange(AttributeFilters, GlobalFiltersInstance));

            // if empty
            if(!filteredValues.Any())
                continue;

            float minVal = SceneManager.Instance.dataObject.DimensionsRange[axis.axisId].x;
            float maxVal = SceneManager.Instance.dataObject.DimensionsRange[axis.axisId].y;

            float newMinNormaliser = UtilMath.normaliseValue(filteredValues.Min(), 0, 1f, -0.505f, 0.505f);
            float newMaxNormaliser = UtilMath.normaliseValue(filteredValues.Max(), 0, 1f, -0.505f, 0.505f);
            if(newMaxNormaliser - newMinNormaliser < 0.005f)
                newMaxNormaliser = newMinNormaliser + 0.005f;
            // Debug.Log("min VAL value was: " + minVal);
            // Debug.Log("max VAL normaliser value was: " + maxVal);
            // Debug.Log("filtered.min: " + filteredValues.Min());
            // Debug.Log("filtered.max: " + filteredValues.Max());
            // Debug.Log("newmin norm: " + newMinNormaliser);
            // Debug.Log("newmax norm: " + newMaxNormaliser);
            // Debug.Log("Prev min normaliser value was: " + axis.MinNormaliser);
            // Debug.Log("Prev max normaliser value was: " + axis.MaxNormaliser);
            axis.SetMinNormalizer(newMinNormaliser);
            axis.SetMaxNormalizer(newMaxNormaliser);
            // Debug.Log("NEW min normaliser value was: " + axis.MinNormaliser);
            // Debug.Log("NEW MAX normaliser value was: " + axis.MaxNormaliser);
        }
    }

    private List<AttributeFilter> AddandSortRange(List<AttributeFilter> src, List<AttributeFilter> toBeAdded) {
        var newList = new List<AttributeFilter>(src); 
        newList.AddRange(toBeAdded);

        // sort the filters so that the global filters are first!
        // this way we don't need to change anything since the filters are AND filters and we're done!
        newList.Sort((a, b) => {
            if(a.isGlobal && !b.isGlobal)
                return 1;
            else if(!a.isGlobal && b.isGlobal)
                return -1;
            
            return 0;
        });

        return newList;
    }

    public float[] getFilteredDimensionForIndexSearch(int dimIndex) {
        return SceneManager.Instance.dataObject.getFilteredDimensionForIndexSearch(dimIndex, AddandSortRange(AttributeFilters, GlobalFiltersInstance));
    }

    // this seems to be an incomplete event handler for the brushing thing!  we should get to use it! 
    // TODO: make it work!
    public void OnBrush(WandController controller, Vector3 hitPoint, bool is3D)
    {
        // Debug.Log("I'm brushing this thing :" + hitPoint + "is 3d is" + is3D);
        isBrushing = true;
        // swapToBrushing();
        BrushingAndLinking.isBrushing = isBrushing;

        BrushingAndLinking.brushPosition = hitPoint;
    }

    public void OnBrushRelease(WandController controller)
    {
        isBrushing = false;
        BrushingAndLinking.isBrushing = isBrushing;

        //  swapToNotBrushing();
    }

    public bool OnGrab(WandController controller)
    {
        if (theSPLOMReference == null)
        {
            // Debug.Log("splom grabbed inside if");
            // Debug.Log(theSPLOMReference);
            foreach (var axis in axes)
            {
                controller.PropergateOnGrab(axis.gameObject);
            }
        }
        else
        {
            Debug.Log("splom grabbed inside ELSE");
            Debug.Log(theSPLOMReference);
            // EventManager.TriggerVisualizationEvent(ApplicationConfiguration.OnVisualizationGrabbed, this);
            controller.PropergateOnGrab(theSPLOMReference.gameObject);
        }

        return false;
    }

    public void OnRelease(WandController controller)
    {
        // EventManager.TriggerVisualizationEvent(ApplicationConfiguration.OnVisualizationReleased, this);
        if (OnStaxesAction != null)
            fireOnStaxesEvent("RELEASED");
        isDirty = true;
    }

    public void OnDrag(WandController controller)
    { }

    public void OnEnter(WandController controller)
    {
        foreach (var axis in axes)
        {
            axis.OnEnter(controller);
        }
    }

    public void OnExit(WandController controller)
    {
        foreach (var axis in axes)
        {
            axis.OnExit(controller);
        }
    }

    // TODO: fix the visualization of this part
    public void OnDetailOnDemand(WandController controller, Vector3 worldPosition, Vector3 localPosition, bool is3D)
    {
        if (worldPosition != null)
        {
            isDetailOnDemand = true;
            if (DetailsOnDemandComponent != null)
            {
                DetailsOnDemandComponent.setPointerPosition(worldPosition);
                DetailsOnDemandComponent.setLocalPointerPosition(localPosition);
                if (is3D) DetailsOnDemandComponent.OnDetailOnDemand3D(); else DetailsOnDemandComponent.OnDetailOnDemand2D();
            }
            detailOnDemandPosition = worldPosition;// sphereWandPostion;
        }
        else
        {
            isDetailOnDemand = false;
            DetailsOnDemandComponent.OnDetailOnDemandEnd();
        }
    }

    public void OnDetailOnDemandRelease(WandController controller)
    {
        isDetailOnDemand = false;

        /////////////////////////////////////////TEMP TEMP TEMP TEMP TEMP///////////////////////////////
        ////////////////////////////////////////TODO: uncomment after fixing redo///////////////////////////////
        
        DetailsOnDemandComponent.OnDetailOnDemandEnd();

                /////////////////////////////////////////TEMP TEMP TEMP TEMP TEMP///////////////////////////////
        ////////////////////////////////////////TODO: uncomment after fixing redo///////////////////////////////

    }

    string[] memory = new string[5];

    // this seems to be there for Staxes event handling
    // TODO: figure this thing out!
    void fireOnStaxesEvent(string eventType)
    {
        //fire event for dragging staxes here
        string axeslabels = "";
        foreach (var item in axes)
        {
            axeslabels += item.name + "-";
        }
        axeslabels = axeslabels.Remove(axeslabels.Length - 1);

        string[] actions = new string[] {
                    viewType.ToString(),
                    axeslabels,
                    eventType,
                    UtilMath.printPositionCSV(transform.position,4),
                    UtilMath.printRotationCSV(transform.rotation,4)
                 };
        //if changing visualisation, declare that we have deleted the previous 
        if (memory[1] == actions[1] && memory[2] == actions[2])
        {
            memory[2] = "DELETED";
            OnStaxesAction(memory);
        }

        OnStaxesAction(actions);
        memory = actions;
    }

    #endregion

    List<Visualization> collidedVisualizations = new List<Visualization>();

    public bool IsHidden
    {
        get { return !viewObjectsRoot.activeSelf; }
        set { viewObjectsRoot.SetActive(!value); }
    }

    // TODO: use this for the filtering and attribute assignment! 
    void OnTriggerEnter(Collider other)
    {
        Visualization vis = other.GetComponent<Visualization>();
        if (vis != null && !collidedVisualizations.Contains(vis))
        {
            collidedVisualizations.Add(vis);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Visualization vis = other.GetComponent<Visualization>();
        if (collidedVisualizations.Contains(vis))
        {
            collidedVisualizations.Remove(vis);
        }
    }

    public int GetPriority()
    {
        return 20;
    }

    public void OnDetailOnDemand(WandController controller, Vector3 position, Vector3 localPosition)
    {
    }

    // TODO: put the code in the filterBubbleButton in this component so that we can handle the hiding of a visualization from here
    // public Sequence hideVisualizatoin() {
        
    // }
}