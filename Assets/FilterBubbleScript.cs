using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class FilterBubbleScript : MonoBehaviour
{

    public Visualization parentVisualization;
    public GameObject sliderPrefab;

    public  GameObject spacerprefab; 
    public Transform controlGameobject;
    public GameObject labelGameobject;


    [Tooltip("A list of dropdowns that will be automatically populated with attribute names")]
    public List<Dropdown> AttributeDropdowns = new List<Dropdown>();

    public UnityEngine.UI.Slider PointMinSizeSlider;

    public UnityEngine.UI.Slider PointMaxSizeSlider;

    public ColourPickerMenu colorPickerMenu;

    public Button MinGradientColourButton;

    public Button MaxGradientColourButton;

    public List<Axis> filterAxes = new List<Axis>();
    

    public bool isGlobalFilterBubble = false;

    // public List<AttributeFilter> AttributeFilters = new List<AttributeFilter>();


    // Use this for initialization
    void Start()
    {
        // foreach (var dropdown in AttributeDropdowns)
        // {
        //     dropdown.AddOptions(SceneManager.Instance.dataObject.Identifiers.ToList());
        // }

        // Make it transparent at the beginning
        GetComponentInChildren<CanvasGroup>().alpha = 0f;

        labelGameobject.GetComponent<Text>().text = parentVisualization.name;

        if(GameObject.FindGameObjectsWithTag("Controller").Length != 0) {
            // SteamVR_TrackedController activeController = GameObject.FindGameObjectsWithTag("Controller").FirstOrDefault().GetComponent<SteamVR_TrackedController>();
            GetComponent<ViveMenu>().Controller = GameObject.FindGameObjectsWithTag("Controller")[0].GetComponent<SteamVR_TrackedController>();
        } else {
            Debug.LogWarning("There's no controller!");
        }
        
    }

    void Update() {
        if(GetComponent<ViveMenu>().Controller == null) {
            if(GameObject.FindGameObjectsWithTag("Controller").Length != 0) {
                GetComponent<ViveMenu>().Controller = GameObject.FindGameObjectsWithTag("Controller")[0].GetComponent<SteamVR_TrackedController>();
            }
        } 

        if(GetComponent<ViveMenu>().Camera == null) {
            GetComponent<ViveMenu>().Camera = Camera.main.gameObject;
        }
    }

    public void SetAsGlobalFitlerBubble() { 
        isGlobalFilterBubble = true;
    }

    public void SetAsLocalFitlerBubble() {
        isGlobalFilterBubble = false;
    }

    public void AddNewFilter(List<Axis> axes) {
        foreach (var axis in axes)
        {
            GameObject clonedSpacer = Instantiate(spacerprefab, spacerprefab.transform.position, spacerprefab.transform.rotation, controlGameobject);
            GameObject clonedSlider = Instantiate(sliderPrefab, sliderPrefab.transform.position, sliderPrefab.transform.rotation, controlGameobject);
            // UnityEngine.UI.Slider sliderComponent = clonedSlider.GetComponent<UnityEngine.UI.Slider>();
            clonedSlider.GetComponent<UnityEngine.UI.Slider>().minValue = Mathf.Lerp(axis.AttributeRange.x, axis.AttributeRange.y, axis.MinNormaliser + 0.5f);
            clonedSlider.GetComponent<UnityEngine.UI.Slider>().maxValue = Mathf.Lerp(axis.AttributeRange.x, axis.AttributeRange.y, axis.MaxNormaliser + 0.5f);
            // clonedSlider.GetComponent<UnityEngine.UI.Slider>().minValue = -0.5f;
            // clonedSlider.GetComponent<UnityEngine.UI.Slider>().maxValue = 0.5f;

            clonedSlider.GetComponentInChildren<Text>().text = axis.name;

            clonedSlider.GetComponent<UnityEngine.UI.Slider>().onValueChanged.AddListener(delegate {OnTestSliderChanged(clonedSlider.GetComponent<UnityEngine.UI.Slider>(), axis);});

            filterAxes.Add(axis);
            // Debug.Log("Added one! : " + axis.name);

            // TODO: figure out what to do about duplicate axes

            // Check if it's global or local
            if(isGlobalFilterBubble) {
                bool globalExists = SceneManager.Instance.globalFilters.Any(attrFilter => attrFilter.idx == axis.axisId);


                if(!globalExists) {
                    SceneManager.Instance.globalFilters.Add(new AttributeFilter(axis.axisId, axis.name, 0f, 1f, 0f, 1f, true));
                }
            } else {
                // Also create an Attribute filter and add it to the list of Attribute Filter that we already have
                bool exists = parentVisualization.AttributeFilters.Any(attrFilter => attrFilter.idx == axis.axisId);
                // for now if it already exists we're not going to add it, but later we can just replace it with the new one or add it on top and give it another ID or something ... basically having two filters of the same sort (shouldn't be any problem)

                if(!exists) {
                    parentVisualization.AttributeFilters.Add(new AttributeFilter(axis.axisId, axis.name, 0f, 1f, 0f, 1f));
                }
            }

        }
    }

    public void OnTestSliderChanged(UnityEngine.UI.Slider slider, Axis axisAsFilter)
    {
        // TODO: tell the visualization class that something has been changed and it needs to be updated
        float normalisedValue = SceneManager.Instance.dataObject.normaliseValue(slider.value, slider.minValue, slider.maxValue, 0, 1f);
        // Debug.Log(axisAsFilter.name + "'s value has changed and it's now: " + slider.value);
        // Debug.Log(axisAsFilter.name + "'s source index is: " + axisAsFilter.axisId);
        // Debug.Log(axisAsFilter.name + "'s value has changed and it's normalised value is: " + normalisedValue);

        // We should remember that each view object has the Visualization as its direct parent
        // This could be the way for us to access the properies of its parent

        // For now let's just pass the minFilterValue to the View filtering function
        // (because I don't have any better way of collecting min and max from the sliders now)

        // --------------- GLOBAL FILTERING (not really any more!) --------------- //
        if(isGlobalFilterBubble) {
            int foundGlobalIndex = SceneManager.Instance.globalFilters.FindIndex(attrFilter => attrFilter.idx == axisAsFilter.axisId);
            if(foundGlobalIndex != -1) {
                // For now I'm just changing the minFilter value, later we're gonna go more into details
                SceneManager.Instance.globalFilters[foundGlobalIndex].minFilter = normalisedValue;
            }

            // This triggers it for all of the visualizations
            EventManager.TriggerEvent(ApplicationConfiguration.OnFilterSliderChanged, VisualisationAttributes.Instance.FilterAttribute);
        } else {
            // if it's local
            int foundIndex = parentVisualization.AttributeFilters.FindIndex(attrFilter => attrFilter.idx == axisAsFilter.axisId);
            if(foundIndex != -1) {
                // For now I'm just changing the minFilter value, later we're gonna go more into details
                parentVisualization.AttributeFilters[foundIndex].minFilter = normalisedValue;
            }

            // When called without any params it would simply be the local filtering then! 
            // parentVisualization.DoFilter();

            // We need to trigger an event here that let's us know a filtering thing got changed! 

            EventManager.TriggerEvent(ApplicationConfiguration.OnLocalFilterSliderChanged, parentVisualization.GetInstanceID());
        }


        
    }
}
