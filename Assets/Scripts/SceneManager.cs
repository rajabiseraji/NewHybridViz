﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class SceneManager : MonoBehaviour
{
    // TODO: We need to change this function in order to create our scene and its corresponding objects
    
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

    static SceneManager _instance;
    public static SceneManager Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<SceneManager>()); }
    }


    void Start()
    {
        // Init the monitorboards list 
         //GameObject.FindGameObjectsWithTag("MonitorBoard");


        Transform dataShelfPanel = GameObject.FindGameObjectWithTag("DataShelfPanel").transform;

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

        // create the axis

        for (int i = 0; i < dataObject.Identifiers.Length; ++i)
        {
            // Vector3 v = new Vector3(1.352134f - (i % 7) * 0.35f, 1.506231f - (i / 7) / 2f, 0f);// -0.4875801f);
            Vector3 v = dataShelfPanel.position + ((1.352134f - (i % 7) * 0.35f) * dataShelfPanel.right) + ((1f - (i / 7) / 2f) * dataShelfPanel.up) + (dataShelfPanel.forward * 1);
            GameObject obj = (GameObject)Instantiate(axisPrefab, v, dataShelfPanel.rotation, dataShelfPanel);
            // obj.transform.position = v;
            Axis axis = obj.GetComponent<Axis>();
            axis.Init(dataObject, i, true);
            axis.InitOrigin(v, obj.transform.rotation);
            axis.initOriginalParent(dataShelfPanel);
            axis.tag = "Axis";

            AddAxis(axis);
        }

        // CreateSPLOMS();

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            putThingsInFrontofCamera();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            placeDesktopMonitors();
        }
    }

    public void putThingsInFrontofCamera()
    {

        Transform dataShelfPanel = GameObject.FindGameObjectWithTag("DataShelfPanel").transform;


        dataShelfPanel.rotation = mainCamera.transform.rotation;
        dataShelfPanel.position = mainCamera.transform.position + (mainCamera.transform.forward * 1f);
        dataShelfPanel.position = mainCamera.transform.position + (mainCamera.transform.up * -0.8f);

        // this is the code to get something in front of a camera
        // top left = width:  2.10 - height: 0.5 
        TwoDPanel.transform.rotation = mainCamera.transform.rotation;
        TwoDPanel.transform.position = mainCamera.transform.position;
        TwoDPanel.transform.position += (mainCamera.transform.forward * 1f) + (mainCamera.transform.up * -0.1f) + (mainCamera.transform.right * 0.4f);
        TwoDPanel.transform.parent = dataShelfPanel;
        TwoDPanel.transform.localScale = new Vector3(2.7f, 1.1f, 0.0001f);

        // Get the global filters in front of the camera too
        GlobalFilterPanel.GetComponentInChildren<FilterBubbleScript>().SetAsGlobalFitlerBubble();
        Transform mainCam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        GlobalFilterPanel.transform.rotation = mainCam.rotation;
        // GlobalFilterPanel.transform.position = mainCam.position;
        GlobalFilterPanel.transform.position = mainCam.position + (mainCam.right * -4.4f) + (mainCam.forward * -1.85f) + (mainCam.up * -1.2f);


    }

    public void placeDesktopMonitors()
    {
        Transform multiMonitorSet = GameObject.FindGameObjectWithTag("MultiMonitor").transform;

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

    //
    // Debug functions
    //

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


    public void Create2DScatterplot(
        Vector3 XAxisplacementPosition, 
        Quaternion XAxisplacementRotation, 
        Vector3 XAxisForwardVector, 
        Vector3 XAxisRightVector,
        Vector3 xAxisUpVector)
    {
        if(toBeActivatedXAxisId != -1)
        {
            XAxisplacementRotation *= Quaternion.AngleAxis(-90f, XAxisForwardVector);
            XAxisplacementRotation *= Quaternion.AngleAxis(180f, xAxisUpVector);
            // This will create X Axis
            CreateHistogram(toBeActivatedXAxisId ,XAxisplacementPosition, XAxisplacementRotation);
            toBeActivatedXAxisId = -1;
        }

        if(toBeActivatedYAxisId != -1)
        {
            Quaternion yAxisRotation = XAxisplacementRotation * Quaternion.AngleAxis(90f, xAxisUpVector);
            //yAxisRotation *= Quaternion.AngleAxis(-90f, XAxisForwardVector);

            //Vector3 yAxisPosition = (Axis.AXIS_ROD_LENGTH * -1.5f * XAxisForwardVector) + XAxisplacementPosition; 

            // This will create Y Axis
            CreateHistogram(toBeActivatedYAxisId ,XAxisplacementPosition, yAxisRotation);
            toBeActivatedYAxisId = -1;
        }


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
        axis.Init(dataObject, toBeActivatedAxisId, false);
        axis.InitOrigin(placementPosition, placementRotation);
        //axis.initOriginalParent(dataShelfPanel);
        axis.tag = "Axis";

        AddAxis(axis);
    }
}
