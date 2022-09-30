using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class SceneManager : MonoBehaviour
{

    const float AXIS_X_PADDING = 0.3f;
    const float AXIS_Y_PADDING = -3.5f;
    const int AXES_PER_ROW = 5;

    public const float AXIS_SCALE_FACTOR = 0.5f;

    // TODO: We need to change this function in order to create our scene and its corresponding objects

    public bool isLinkedEnabled = true;
    public bool is2DBoardInteractionEnabled = true;
    //public bool isMonitorCubesEnabled = true;

    public List<Axis> sceneAxes { get; internal set; }

    public DataBinding.DataObject dataObject;

    public class OnAxisAddedEvent : UnityEvent<Axis> { }
    public OnAxisAddedEvent OnAxisAdded = new OnAxisAddedEvent();
    public class OnAxisRemovedEvent : UnityEvent<Axis> { }
    public OnAxisRemovedEvent OnAxisRemoved = new OnAxisRemovedEvent();

    [SerializeField]
    public GameObject axisPrefab;
    
    public GameObject mainCamera;

    [Header("Data Source")]

    [SerializeField]
    TextAsset sourceData;

    [SerializeField]
    DataObjectMetadata metadata;

    [SerializeField]
    GameObject TwoDPanel;
    [SerializeField]
    GameObject GlobalFilterPanel;

    private List<GameObject> monitorBoards;

    [SerializeField]
    public List<AttributeFilter> globalFilters;

    public int toBeActivatedXAxisId = -1; 
    public int toBeActivatedYAxisId = -1; 
    public int toBeActivatedZAxisId = -1; 
    public int toBeActivatedColorAxisId = -1; 
    public int toBeActivatedSizeAxisId = -1;

    public bool extrusionWasEmpty = false;

    static SceneManager _instance;
    public static SceneManager Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<SceneManager>()); }
    }

    public List<int> selectedDataAttributesIds = new List<int>();
    public Transform dataShelfPanel;
    public Transform AxisPlaceholderObject;

    public int[] brushedIndexes = new int[0];

    public ComponentListItem[] componentList = new ComponentListItem[0];

    public string toBeLoggedData = "";

    public MonitorBoardInteractions mainMonitor;

    void Start()
    {
        // Init the monitorboards list 
        //GameObject.FindGameObjectsWithTag("MonitorBoard");
        if (GameObject.FindGameObjectsWithTag("MonitorBoard").Count() != 0)
        {
            mainMonitor = GameObject.FindGameObjectsWithTag("MonitorBoard")[0].GetComponent<MonitorBoardInteractions>();
            // print("found the monitor");
        }
        else
            // print("didn't find the monitor");

        Debug.Assert(AxisPlaceholderObject != null, "Axis Placeholder shouldnb't be null");
        Debug.Assert(dataShelfPanel != null, "Data Shelf panel shouldnb't be null");

        AxisPlaceholderObject.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 0);

        // TODO: enable it later
        //dataShelfPanel = GameObject.FindGameObjectWithTag("DataShelfPanel").transform;

        // find the DataShelf panel and set it in a way that it's in front of the camera
        putThingsInFrontofCamera();


        // Vector3 dataShelfZDirection = 

        sceneAxes = new List<Axis>();
        dataObject = new DataBinding.DataObject(sourceData.text, metadata);

        // setup default visual settings

        VisualisationAttributes.Instance.sizes = Enumerable.Range(0, SceneManager.Instance.dataObject.DataPoints).Select(_ => 1f).ToArray();

        List<float> categories = SceneManager.Instance.dataObject.getNumberOfCategories(VisualisationAttributes.Instance.ColoredAttribute);
        int nbCategories = categories.Count;
        Color[] palette = Colors.generateColorPalette(nbCategories);

        Dictionary<float, Color> indexCategoryToColor = new Dictionary<float, Color>();
        for (int i = 0; i < categories.Count; i++)
        {
            indexCategoryToColor.Add(categories[i], palette[i]);
        }

        VisualisationAttributes.Instance.colors = Colors.mapColorPalette(SceneManager.Instance.dataObject.getDimension(VisualisationAttributes.Instance.ColoredAttribute), indexCategoryToColor);



        // uncomment this when we want to create data panels that contain all of the data
        //makeAllDataAttributeAxes();
        
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            putThingsInFrontofCamera();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CreateScatterplot();
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            AddNewFilterToFilterBubbles();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            placeDesktopMonitors();
        } 
        if (Input.GetKeyDown(KeyCode.S))
        {
            ScaleThings();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            makeAllDataAttributeAxes();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeAxisColor(sceneAxes.Last());
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            int[] list = { 1, 2, 3, 4, 6, 7, 9 };
            print(list);
            setSelectedDataAttributeIds(list.ToList<int>());
        }

        //////////////////////// 
        /// this is because we cannot call stuff directly from Websocket 
        /// onMessage thread, so I just set a flag, the better way to do 
        /// this would have been through a simple event system kind of thing.
        /// Unity limit with main thread
       
        if(brushedIndexes.Length > 0)
        {
            // print("in scenemanager: asking for brushing");
            BrushingAndLinking.ApplyDesktopBrushing(brushedIndexes);
            brushedIndexes = new int[0];
        }

        handleComponentListChange();

        handleCodapLoggingData();

        if(isDebugging && testAxis != null && testScatter == null)
        {
            var vises = testAxis.correspondingVisualizations();
            if (vises == null || vises.Count() == 0)
                return;
            
            testScatter = testAxis.correspondingVisualizations()[0];
        }

    }

    private void handleComponentListChange()
    {
        if (componentList.Length > 0)
        {
            // print("in scenemanager: asking for components");

            // here we should call the monitorboard interaction thingy to then create the cubes!
            if (mainMonitor == null)
            {
                mainMonitor = GameObject.FindGameObjectsWithTag("MonitorBoard")[0].GetComponent<MonitorBoardInteractions>();
            }

            // Ask the main monitor to read the component list and draw them all 
            mainMonitor.ParseComponentListIntoCubes(componentList);


            componentList = new ComponentListItem[0];
        }
    }

    private void handleCodapLoggingData()
    {
        // we can alternatively handle this by queuing the data in a list, not just a single string!

        if (toBeLoggedData != "")
        {
            // print("in scenemanger: logging data for codap");
            DataLogger.Instance.LogCodapData(toBeLoggedData);

            toBeLoggedData = "";
        }
    }

    void makeAllDataAttributeAxes()
    {

        // create the axes
        for (int i = 0; i < dataObject.Identifiers.Length; ++i)
        {
            Debug.Log(i);
            // Vector3 v = new Vector3(1.352134f - (i % 7) * 0.35f, 1.506231f - (i / 7) / 2f, 0f);// -0.4875801f);
            Vector3 v = AxisPlaceholderObject.position;
            v += ((i % AXES_PER_ROW) * AXIS_X_PADDING) * dataShelfPanel.right;   
            v += (((i / AXES_PER_ROW) / AXIS_Y_PADDING)) * dataShelfPanel.up;
            //v += dataShelfPanel.forward * -0.015f;

            GameObject obj = (GameObject)Instantiate(axisPrefab, v, dataShelfPanel.rotation, dataShelfPanel);

            Axis axis = obj.GetComponent<Axis>();
            axis.Init(dataObject, i, true, AXIS_SCALE_FACTOR);
            axis.InitOrigin(v, obj.transform.rotation);
            axis.initOriginalParent(dataShelfPanel);
            axis.tag = "Axis";

            AddAxis(axis);
        }
    }

    void setSelectedDataAttributeIds(List<int> newAttributeIdsList)
    {
        this.selectedDataAttributesIds = newAttributeIdsList;

        // destroy all of the axes in the 2D panel thing
        var prototypeAxes = sceneAxes.Where(a => a.isPrototype);
        var protoVises = new List<Visualization>();
        foreach(Axis a in prototypeAxes)
        {
            protoVises.AddRange(a.correspondingVisualizations());
        }
        for(int i = 0; i < protoVises.Count; ++i)
        {
            protoVises[i].DestroyVisualization();
        }

        // section to 

        TwoDimensionalPanelScript panel = (TwoDimensionalPanelScript)GameObject.FindObjectOfType(typeof(TwoDimensionalPanelScript));

        // set the interaction to true no matter what for this: 
        panel.isInteractionEnbabled = true;

        panel.clearConnectedAxisList();

        // make all of those attributes again
        makeSelectedDataAttributeAxes();
        //putThingsInFrontofCamera();

        // After things are done, set the interaction to whatever the flag is with a delay

        StartCoroutine(setPanelInteractionFlag(panel)); //
        StartCoroutine("orderThingscoroutine"); //

    }

    System.Collections.IEnumerator setPanelInteractionFlag(TwoDimensionalPanelScript panel)
    {
        yield return new WaitForSeconds(2f);

        panel.isInteractionEnbabled = this.is2DBoardInteractionEnabled;

    }
    
    void makeSelectedDataAttributeAxes()
    {
        // create the axes
        for (int i = 0; i < selectedDataAttributesIds.Count; ++i)
        {

            Vector3 v = AxisPlaceholderObject.position;
            v += ((i % AXES_PER_ROW) * AXIS_X_PADDING) * dataShelfPanel.right;
            v += (((i / AXES_PER_ROW) / AXIS_Y_PADDING)) * dataShelfPanel.up;
            //v += dataShelfPanel.forward * -0.1f;

            GameObject obj = (GameObject)Instantiate(axisPrefab, v, dataShelfPanel.rotation, dataShelfPanel);
            //obj.transform.localScale = obj.transform.localScale * 0.5f;
            // obj.transform.position = v;
            Axis axis = obj.GetComponent<Axis>();
            axis.Init(dataObject, selectedDataAttributesIds[i], true, AXIS_SCALE_FACTOR);
            axis.InitOrigin(v, obj.transform.rotation);
            axis.initOriginalParent(dataShelfPanel);
            axis.tag = "Axis";

            AddAxis(axis);
        }
    }

    public void putThingsInFrontofCamera()
    {
        // print("putting things in front of the camera");
        Vector3 cameraAngles = mainCamera.transform.eulerAngles;
        dataShelfPanel.rotation = Quaternion.Euler(0, cameraAngles.y, 0);

        if (Vector3.Dot(mainCamera.transform.forward, dataShelfPanel.transform.forward) < 0)
        {
            dataShelfPanel.Rotate(dataShelfPanel.up, 180f);
        }


        dataShelfPanel.position = mainCamera.transform.position + (mainCamera.transform.forward * 0.5f);
        dataShelfPanel.Translate(Vector3.up * -0.4f);

        dataShelfPanel.rotation = Quaternion.Euler(30f, cameraAngles.y, 0);
        //dataShelfPanel.Rotate(dataShelfPanel.right, -30f);
        //dataShelfPanel.position += (mainCamera.transform.up * -0.5f);

        // Get the global filters in front of the camera too
        //GlobalFilterPanel.GetComponentInChildren<FilterBubbleScript>().SetAsGlobalFitlerBubble();
        //Transform mainCam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        //GlobalFilterPanel.transform.rotation = mainCam.rotation;
        //// GlobalFilterPanel.transform.position = mainCam.position;
        //GlobalFilterPanel.transform.position = mainCam.position + (mainCam.right * -4.4f) + (mainCam.forward * -1.85f) + (mainCam.up * -1.2f);


    }

    private System.Collections.IEnumerator orderThingscoroutine()
    {
        yield return new WaitForSeconds(1f);

        placeDesktopMonitors();
        putThingsInFrontofCamera();
    }

    public void placeDesktopMonitors()
    {
        Transform multiMonitorSet = GameObject.FindGameObjectWithTag("MonitorBoard").transform;

        Vector3 cameraAngles = mainCamera.transform.eulerAngles;
        multiMonitorSet.rotation = Quaternion.Euler(0, cameraAngles.y, 0);
        //multiMonitorSet.Rotate(mainCamera.transform.up, cameraAngles.z);


        if (Vector3.Dot(mainCamera.transform.forward, multiMonitorSet.transform.forward) < 0)
        {
            multiMonitorSet.Rotate(multiMonitorSet.up, 180f);
        }


        multiMonitorSet.position = mainCamera.transform.position + (mainCamera.transform.forward * 0.7f);


    }

    public void AddAxis(Axis axis)
    {
        sceneAxes.Add(axis);
        OnAxisAdded.Invoke(axis);
    }
    public void RemoveAxis(Axis axis)
    {
        sceneAxes.Remove(axis);
        OnAxisRemoved.Invoke(axis);
    }

    public void ChangeAxisColor(Axis axis)
    {
        axis.correspondingVisualizations()[0].setVisualizationColors(axis);
    }


    Color[] GetColorMapping(int coloredAttribute)
    {

        if (VisualisationAttributes.Instance.IsGradientColor)
        {
            /*VisualisationAttributes.Instance.colors =*/ return VisualisationAttributes.getContinuousColors(VisualisationAttributes.Instance.MinGradientColor, VisualisationAttributes.Instance.MaxGradientColor, SceneManager.Instance.dataObject.getDimension(coloredAttribute));
        }
        else
        {

            List<float> categories = SceneManager.Instance.dataObject.getNumberOfCategories(coloredAttribute);
            int nbCategories = categories.Count;
            Color[] palette = Colors.generateColorPalette(nbCategories);

            Dictionary<float, Color> indexCategoryToColor = new Dictionary<float, Color>();
            for (int i = 0; i < categories.Count; i++)
            {
                indexCategoryToColor.Add(categories[i], palette[i]);
            }

            /*VisualisationAttributes.Instance.colors =*/
            return Colors.mapColorPalette(SceneManager.Instance.dataObject.getDimension(coloredAttribute), indexCategoryToColor);
        }
        //EventManager.TriggerEvent(ApplicationConfiguration.OnColoredAttributeChanged, VisualisationAttributes.Instance.ColoredAttribute);

    }

    //
    // Debug functions
    //

    void ScaleThings()
    {
        foreach(Axis a in sceneAxes)
        {
            a.ScaleAxis(0.5f);
        }
    }

    void CreateSPLOMS()
    {
        print("creating the splom");
        Axis[] axes = (Axis[])GameObject.FindObjectsOfType(typeof(Axis));

        GameObject a = axes[0].gameObject;// GameObject.Find("axis horesepower");
        a.transform.position = new Vector3(0f, 1.383f, 0.388f);

        Quaternion qt = new Quaternion();
        qt.eulerAngles = new Vector3(-90f, 180f, 0f);
        a.transform.rotation = qt;

        GameObject b = axes[1].gameObject;
        b.transform.position = new Vector3(0f, 1.506231f, 0.2461f);

        GameObject d = axes[2].gameObject;
        d.transform.position = new Vector3(0.1485f, 1.4145f, 0.2747f);
        qt.eulerAngles = new Vector3(0f, 180f, 90f);
        d.transform.rotation = qt;
    }

    void CreateLSPLOM()
    {
        Quaternion qt = new Quaternion();
        qt.eulerAngles = new Vector3(0f, 180f, 90f);

        GameObject a = GameObject.Find("axis horesepower");
        a.transform.position = new Vector3(0.1018f, 1.369f, -1.3629f);
        a.transform.rotation = qt;

        GameObject b = GameObject.Find("axis weight");
        b.transform.position = new Vector3(-0.04786599f, 1.506231f, -1.356f);

        GameObject c = GameObject.Find("axis mpg");
        c.transform.position = new Vector3(-0.045f, 1.768f, -1.357f);

        GameObject d = GameObject.Find("axis name");
        d.transform.position = new Vector3(-0.047f, 2.03f, -1.354f);

        qt.eulerAngles = new Vector3(0f, 180f, 90f);

        GameObject e = GameObject.Find("axis displacement");
        e.transform.position = new Vector3(0.37f, 1.378f, -1.37f);
        e.transform.rotation = qt;

    }

    void CreateSPLOMCenter()
    {
        Quaternion qt = new Quaternion();
        qt.eulerAngles = new Vector3(0f, 180f, -90f);

        GameObject c = GameObject.Find("axis mpg");
        c.transform.position = new Vector3(1.3173f, 1.7632f, -0.941f);

        GameObject d = GameObject.Find("axis weight");

        d.transform.position = new Vector3(1.173f, 1.6389f, -0.9362f);
        d.transform.rotation = qt;

        //        Quaternion qt = new Quaternion();

        GameObject e = GameObject.Find("axis displacement");

        e.transform.position = new Vector3(0.8942f, 1.64f, -0.938f);
        e.transform.rotation = qt;
    }

    void CreateSPLOMSWithU()
    {
        Axis[] axes = (Axis[])GameObject.FindObjectsOfType(typeof(Axis));
        for (int i = 2; i < 8; ++i)
        {
            axes[i].transform.Translate(i % 2 * (axes[i].transform.localScale.x * 10f), i % 2 * (-axes[i].transform.localScale.x * 6.5f), 1f);
            axes[i].transform.Rotate(0f, 0f, i % 2 * (90f));
        }

        GameObject a = GameObject.Find("axis mpg");
        a.transform.position = new Vector3(0.236f, 1.506231f, -1.486f);
    }

    public Transform scatterPlaceholder;
    public Visualization testScatter = null;
    public Axis testAxis = null;
    public bool isDebugging = true;
    
    //[Range(-0.5f, 0.5f)]
    public float minFilter = -0.5f;
    //[Range(-0.5f, 0.5f)]
    public float maxFilter = 0.5f;

    List<AttributeFilter> debugFilters = new List<AttributeFilter>();

    public void CreateScatterplot()
    {
        toBeActivatedXAxisId = 4; //weight
        toBeActivatedYAxisId = 6; // model

        Vector3 pos = new Vector3(
            3.548397f,
            -2.848239f,
            -0.6445103f
        );

        scatterPlaceholder.position = Camera.main.transform.position + Camera.main.transform.forward * 0.2f;

        CreateChart(Vector3.zero, Quaternion.identity, Vector3.forward, Vector3.right, Vector3.up, scatterPlaceholder);
    } 
    
    public void CreateScatterplot3D()
    {
        toBeActivatedXAxisId = 4; //weight
        toBeActivatedYAxisId = 6; // model
        toBeActivatedZAxisId = 2; // model

        Vector3 pos = new Vector3(
            3.548397f,
            -2.848239f,
            -0.6445103f
        );

        scatterPlaceholder.position = Camera.main.transform.position + Camera.main.transform.forward * 0.2f;

        CreateChart(Vector3.zero, Quaternion.identity, Vector3.forward, Vector3.right, Vector3.up, scatterPlaceholder);


        int facingSign = Vector3.Dot(mainCamera.transform.forward, scatterPlaceholder.forward) > 0 ? 1 : -1;

        //scatterPlaceholder.Translate(scatterPlaceholder.transform.right * Axis.AXIS_ROD_LENGTH * 0.5f * facingSign); 
        scatterPlaceholder.Translate(scatterPlaceholder.transform.up * Axis.AXIS_ROD_LENGTH * AXIS_SCALE_FACTOR * -0.5f * facingSign);
        scatterPlaceholder.Translate(scatterPlaceholder.transform.forward * Axis.AXIS_ROD_LENGTH * AXIS_SCALE_FACTOR * -0.5f * facingSign);

        scatterPlaceholder.Rotate(scatterPlaceholder.transform.right, -90f);
        
        CreateHistogram(toBeActivatedZAxisId, scatterPlaceholder.position, scatterPlaceholder.rotation);
        //if (facingSign == -1)
    }

    public void AddNewFilterToFilterBubbles()
    {
         
        debugFilters.Add(new AttributeFilter(3, "Result", 0.2f, 0.5f, -0.5f, 0.5f)); // horserpower;
        minFilter = debugFilters[0].minFilter;
        maxFilter = debugFilters[0].maxFilter;

        if (testScatter != null)
            testScatter.AddNewFilterToFilterBubbles(debugFilters);

        Debug.Log("In Scene manager: just added a new filter");
    }

    public void ChangeDebugFilterValue()
    {
        debugFilters[0].minFilter = minFilter;
        debugFilters[0].maxFilter = maxFilter;

        int foundIndex = testScatter.AttributeFilters.FindIndex(attrFilter => attrFilter.idx == debugFilters[0].idx);
        if (foundIndex != -1)
        {
            // For now I'm just changing the minFilter value, later we're gonna go more into details
            testScatter.AttributeFilters[foundIndex].minFilter = debugFilters[0].minFilter;
            testScatter.AttributeFilters[foundIndex].maxFilter = debugFilters[0].maxFilter;
        }

        EventManager.TriggerEvent(ApplicationConfiguration.OnLocalFilterSliderChanged, testScatter.GetInstanceID());

    }

    //[ExecuteInEditMode]
    //public void printShit(float val)
    //{
    //    Debug.Log("value is changing");
    //    Debug.Log(val);
    //}

    // We will use this function to both create normal histograms, and also create two-d scatterplots
    public void CreateChart(
        Vector3 XAxisplacementPosition, 
        Quaternion XAxisplacementRotation, 
        Vector3 XAxisForwardVector, 
        Vector3 XAxisRightVector,
        Vector3 xAxisUpVector,
        Transform dotCube
        )
    {
        Transform originalDotCubeTransform = Instantiate(dotCube, dotCube.position, dotCube.rotation);
        originalDotCubeTransform.GetComponent<Renderer>().material.color = Color.yellow;

        int facingSign = Vector3.Dot(mainCamera.transform.forward, dotCube.forward) > 0 ? 1 : -1;

        /// before doing anything, just update the Axis.Axis_rod length and other ones by calling the scaling function thingy
        //



        ///////////////////////// VERY IMPORTANT NOTE ///////////////////////
        /// THE ANGLES OF THE EXTRUDED OBJECT IS DIRECTY RELATED TO THE ANGLE
        /// BETWEEN THE FORWARD VECTOR OF CAMERA, WORLD, AND ALSO DOTCUBE
        /// IN A SITUATION WHEN WE FACE AWAY FROM THE LIGHTHOUSE (AKA NEGATIVE 
        /// COORDS), THE SCRIPT WORKS FINE. 
        /// I DON'T HAVE THE TIME TO FIX IT JUST YET, SOME OTHER TIME I'LL COME 
        /// AND FIGURE IT OUT
        ///////////////////////////////////////////////////////////////////////

        if (toBeActivatedYAxisId != -1)
        {

            dotCube.Translate(dotCube.transform.right * Axis.AXIS_ROD_LENGTH * AXIS_SCALE_FACTOR * -0.5f * facingSign);
            if(facingSign == -1)
                dotCube.Rotate(dotCube.transform.up, -180f);

            //Quaternion yAxisRotation = XAxisplacementRotation * Quaternion.AngleAxis(-180f, xAxisUpVector);
            ////yAxisRotation *= Quaternion.AngleAxis(90f, XAxisForwardVector);

            //// If we had already created an XAxis, then make the YAxis a little bit to the right of where we release the controller
            //Vector3 yAxisPosition = toBeActivatedXAxisId != -1 ? (Axis.AXIS_ROD_LENGTH * 0.5f * XAxisRightVector) + XAxisplacementPosition : XAxisplacementPosition;

            // This will create Y Axis
            if(!isDebugging)
                CreateHistogram(toBeActivatedYAxisId, dotCube.position, dotCube.rotation);
            else 
                testAxis = CreateTestHistogram(toBeActivatedYAxisId, dotCube.position, dotCube.rotation);

        }


        if (toBeActivatedXAxisId != -1)
        {
            originalDotCubeTransform.Translate(originalDotCubeTransform.up * Axis.AXIS_ROD_LENGTH * AXIS_SCALE_FACTOR * -0.5f);
            originalDotCubeTransform.Rotate(originalDotCubeTransform.forward, -90f * facingSign);
            //originalDotCubeTransform.Rotate(originalDotCubeTransform., 90f);
            //originalDotCubeTransform.Rotate(originalDotCubeTransform.forward, 90f);


            // if the YAxis has also been called for activation, we need to move the X axis a little bit to the bottom so that it can make a 2D scatterplot
            //Vector3 xAxisPosition = toBeActivatedYAxisId != -1 ? (Axis.AXIS_ROD_LENGTH * -0.5f * xAxisUpVector) + XAxisplacementPosition : XAxisplacementPosition;

            //Quaternion xAxisRotation = XAxisplacementRotation * Quaternion.AngleAxis(-90f, XAxisForwardVector);

            //xAxisRotation *= Quaternion.AngleAxis(180f, xAxisUpVector);
            //xAxisRotation *= Quaternion.AngleAxis(180f, XAxisRightVector);

            //// This will create X Axis

            //// We need to move XAxis to the right by 1.5 * AXIS_ROD_LENGTH 
            //// this is to prevent flipping
            //xAxisPosition = toBeActivatedYAxisId != -1 ? xAxisPosition + (Axis.AXIS_ROD_LENGTH * 1f * XAxisRightVector) : xAxisPosition;

            CreateHistogram(toBeActivatedXAxisId ,originalDotCubeTransform.position, originalDotCubeTransform.rotation);
            
        }

        

        // in the end again reset these vars so that we can create visualizations again ...>
        // <... after this call
        toBeActivatedYAxisId = -1;
        toBeActivatedXAxisId = -1;

        //GameObject.Destroy(dotCube.gameObject);
        dotCube.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0);
        GameObject.Destroy(originalDotCubeTransform.gameObject);

    }

    // This function is called when we START pulling from the screen 
    public void SetYToBeCreatedAxis(string axisName)
    {

        // I wanted to find the index of the string that is the Axis name from the identifiers in dataobject

        // then create the Axis and add it to the Axis list

        int foundIndex = dataObject.dimensionToIndex(axisName);
        if(foundIndex == -1)
        {
            Debug.Log("No such identifier found!");
            return;
        }
        Debug.Log("The found index is " + foundIndex);
        toBeActivatedYAxisId = foundIndex;
    }
    public void SetXToBeCreatedAxis(string axisName)
    {

        // I wanted to find the index of the string that is the Axis name from the identifiers in dataobject

        // then create the Axis and add it to the Axis list

        int foundIndex = dataObject.dimensionToIndex(axisName);
        if(foundIndex == -1)
        {
            Debug.Log("No such identifier found!");
            return;
        }
        Debug.Log("The found index is " + foundIndex);
        toBeActivatedXAxisId = foundIndex;
    }

    public void setBrushedIndexes(int[] indexes)
    {
        brushedIndexes = indexes;
    }

    

    public void setComponetList(ComponentListItem[] componentList)
    {
        // Massive problem! we shouldn't call this function from the wsThread, it doesn't run, 

        // ////////////////////////////////// TODO ////////////////////////////
        // we need to seomthig like we did with the brushing thingy
        //print("hey I'm here and I found the monitor count : " + GameObject.FindGameObjectsWithTag("MonitorBoard").Count());
        this.componentList = componentList;
    }

    public void setToBeLoggedCodapData(string codapLoad)
    {
        this.toBeLoggedData = codapLoad;
    }

    public void setMonitorBoard(MonitorBoardInteractions monitor)
    {
        this.mainMonitor = monitor;
    }

    // This function is called when we are releasing the trigger after an extrusion event
    public void CreateHistogram(int toBeActivatedAxisId, Vector3 placementPosition, Quaternion placementRotation)
    {

        if (toBeActivatedAxisId == -1)
        {
            Debug.Log("There's no active axis in the scenemanager's cache!");
            return;
        }

        //dataObject.Identifiers.ToList<string>()
        GameObject obj = (GameObject)Instantiate(axisPrefab, placementPosition, placementRotation);
        // obj.transform.position = v;
        Axis axis = obj.GetComponent<Axis>();
        axis.Init(dataObject, toBeActivatedAxisId, false, AXIS_SCALE_FACTOR);
        axis.InitOrigin(placementPosition, placementRotation);
        //axis.initOriginalParent(dataShelfPanel);
        axis.tag = "Axis";

        AddAxis(axis);
    }

    public Axis CreateTestHistogram(int toBeActivatedAxisId, Vector3 placementPosition, Quaternion placementRotation)
    {

        if (toBeActivatedAxisId == -1)
        {
            Debug.Log("There's no active axis in the scenemanager's cache!");
            return null;
        }

        //dataObject.Identifiers.ToList<string>()
        GameObject obj = (GameObject)Instantiate(axisPrefab, placementPosition, placementRotation);
        // obj.transform.position = v;
        Axis axis = obj.GetComponent<Axis>();
        axis.Init(dataObject, toBeActivatedAxisId, false, AXIS_SCALE_FACTOR);
        axis.InitOrigin(placementPosition, placementRotation);
        //axis.initOriginalParent(dataShelfPanel);
        axis.tag = "Axis";

        AddAxis(axis);
        return axis;
    }
}
