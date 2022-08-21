using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LegendInteractions : MonoBehaviour
{
    public Camera uiCamera = null;
    public GameObject colorLegendGameObject = null;
    public GameObject sizeLegendGameObject = null;

    private bool colorLegendActive = false;
    private bool sizeLegendActive = false;

    public Visualization parentVis = null;
    // Start is called before the first frame update

    public bool shouldLookAtCamra = true;
    public Transform VRCameraTransform;
    void Start()
    {
        uiCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
        VRCameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;


        if (uiCamera)
            GetComponent<Canvas>().worldCamera = uiCamera;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Assert(colorLegendGameObject != null, "Color Legend Cannot be null");
        Debug.Assert(colorLegendGameObject != null, "Color Legend Cannot be null");
        Debug.Assert(parentVis != null, "Parent Vis of Legened Cannot be null");
        
        Debug.Assert(uiCamera != null, "UI Camera Cannot be null");
        Debug.Assert(VRCameraTransform != null, "VR Camera for Legened Cannot be null");

        if(!uiCamera)
        {
            uiCamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();

            if (uiCamera)
                GetComponent<Canvas>().worldCamera = uiCamera;
        }


        if (!colorLegendActive && !sizeLegendActive)
            gameObject.SetActive(false);


        if(!VRCameraTransform)
            VRCameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;

        if (parentVis.viewType == Visualization.ViewType.Scatterplot3D && shouldLookAtCamra && VRCameraTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(VRCameraTransform.forward, VRCameraTransform.up);
        }
    }

    public void RemoveClicked()
    {
        print("remove has been clicked!");
    }

    public void updateColorLegend(int axisId, Color[] colors)
    {
        // if Axis is null means that we're unsetting
        if(axisId == -1)
        {
            removeColorLegend();
        } else
        {
            string axisName = SceneManager.Instance.dataObject.Identifiers[axisId];
            showColorLegend(axisName, colors);
        }
    }

    public void updateSizeLegend(int axisId, float[] sizes)
    {
        // if Axis is null means that we're unsetting
        if (axisId == -1)
        {
            removeSizeLegend();
        }
        else
        {
            string axisName = SceneManager.Instance.dataObject.Identifiers[axisId];
            showSizeLegend(axisName, sizes);
        }
    }

    private void showColorLegend(string axisName, Color[] colors)
    {
        colorLegendActive = true;
        colorLegendGameObject.SetActive(true);
        colorLegendGameObject.GetComponentInChildren<Text>().text = "Color: " + axisName;
    }
    private void removeColorLegend()
    {
        colorLegendActive = false;
        colorLegendGameObject.SetActive(false);
        colorLegendGameObject.GetComponentInChildren<Text>().text = "Color:";
    }
    private void showSizeLegend(string axisName, float[] sizes)
    {
        sizeLegendActive = true;
        sizeLegendGameObject.SetActive(true);
        sizeLegendGameObject.GetComponentInChildren<Text>().text = "Size: " + axisName;
    }
    private void removeSizeLegend()
    {
        sizeLegendActive = false;
        sizeLegendGameObject.SetActive(false);
        sizeLegendGameObject.GetComponentInChildren<Text>().text = "Size:";
    }


    public void lookAtCamera()
    {
        shouldLookAtCamra = true;
        VRCameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;
    }

}
