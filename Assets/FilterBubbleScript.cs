using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using DG.Tweening;

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

    // We can use this to later recreate the axis upon filter removal
    public struct AxisAndVizes {
        public Axis axis;
        public Visualization[] vizes;

        public AxisAndVizes(Axis axis, Visualization[] vizes) {
            this.axis = axis;
            this.vizes = vizes;
        }
        public AxisAndVizes(int axisId, Transform originTransform) {
            GameObject obj = (GameObject)Instantiate(SceneManager.Instance.axisPrefab, originTransform.position, originTransform.rotation);
            obj.transform.localScale = Vector3.zero;
            // obj.transform.position = v;
            Axis axis = obj.GetComponent<Axis>();
            axis.Init(SceneManager.Instance.dataObject, axisId, false, SceneManager.AXIS_SCALE_FACTOR);
            axis.InitOrigin(originTransform.position, originTransform.rotation);
            axis.tag = "Axis";
            
            foreach (var vis in axis.correspondingVisualizations())
            {
                vis.gameObject.SetActive(false);
            }
            obj.SetActive(false);

            this.axis = axis;
            this.vizes = axis.correspondingVisualizations().ToArray();
        }

    };
    public List<AxisAndVizes> filterAxes = new List<AxisAndVizes>();
    

    public bool isGlobalFilterBubble = false;

    public FilterBubbleButton FilterBubblebuttonGameobject = null;

    // public List<AttributeFilter> AttributeFilters = new List<AttributeFilter>();


    // Use this for initialization
    void Start()
    {
        Debug.Assert(FilterBubblebuttonGameobject != null, "Filter bubble button object is not assigned");

        GetComponentInChildren<CanvasGroup>().alpha = 0f;
        
        if(!isGlobalFilterBubble)
            labelGameobject.GetComponent<Text>().text = parentVisualization.name;

        if(GameObject.FindGameObjectsWithTag("Controller").Length != 0) {
            // SteamVR_TrackedController activeController = GameObject.FindGameObjectsWithTag("Controller").FirstOrDefault().GetComponent<SteamVR_TrackedController>();
            //GetComponent<ViveMenu>().Controller = GameObject.FindGameObjectsWithTag("Controller")[0].GetComponent<SteamVR_TrackedController>();
        } else {
            Debug.LogWarning("There's no controller!");
        }
        
    }

    void Update() {
        //if(GetComponent<ViveMenu>().Controller == null) {
        //    if(GameObject.FindGameObjectsWithTag("Controller").Length != 0) {
        //        GetComponent<ViveMenu>().Controller = GameObject.FindGameObjectsWithTag("Controller")[0].GetComponent<SteamVR_TrackedController>();
        //    }
        //} 

        //if(GetComponent<ViveMenu>().Camera == null) {
        //    GetComponent<ViveMenu>().Camera = Camera.main.gameObject;
        //}
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
            int axisId = axis.axisId;
            string axisName = SceneManager.Instance.dataObject.Identifiers[axisId];
            float axisAttributeRangeMin = axis.AttributeRange.x;
            float axisAttributeRangeMax = axis.AttributeRange.y;
            var axisMinNormaliser = axis.MinNormaliser;
            var axisMaxNormaliser = axis.MaxNormaliser;


            // destory the axis and its visualization just in case!
            // this would destroy it without the need to manually go and kill it
            axis.transform.Translate(new Vector3(0, -10000.0f, 0));

            //GameObject.Destroy(axis);


             // Check if it's global or local
            if(isGlobalFilterBubble) {
                bool globalExists = SceneManager.Instance.globalFilters.Any(attrFilter => attrFilter.idx == axisId);

                if(globalExists) 
                    continue;

                SceneManager.Instance.globalFilters.Add(new AttributeFilter(axisId, axisName, -0.5f, 0.5f, 0f, 1f, true));
                
            } else {
                // Also create an Attribute filter and add it to the list of Attribute Filter that we already have
                bool exists = parentVisualization.AttributeFilters.Any(attrFilter => attrFilter.idx == axisId);
                // for now if it already exists we're not going to add it, but later we can just replace it with the new one or add it on top and give it another ID or something ... basically having two filters of the same sort (shouldn't be any problem)

                if(exists) 
                    continue;

                parentVisualization.AttributeFilters.Add(new AttributeFilter(axisId, axisName, -0.5f, 0.5f, 0f, 1f));
            }


            // if(!filterAxes.Any(item => item.axisId == axisId)) {
            filterAxes.Add(new AxisAndVizes(axisId, transform));
            // }
            

            sliderPrefab.SetActive(false);
            GameObject clonedSpacer = Instantiate(spacerprefab, spacerprefab.transform.position, spacerprefab.transform.rotation, controlGameobject);
            GameObject clonedSlider = Instantiate(sliderPrefab, sliderPrefab.transform.position, sliderPrefab.transform.rotation, controlGameobject);
            clonedSlider.SetActive(true);
            // UnityEngine.UI.Slider sliderComponent = clonedSlider.GetComponent<UnityEngine.UI.Slider>();
            float minLimit = Mathf.Lerp(axisAttributeRangeMin, axisAttributeRangeMax, axisMinNormaliser + 0.5f);
            float maxLimit = Mathf.Lerp(axisAttributeRangeMin, axisAttributeRangeMax, axisMaxNormaliser + 0.5f);

            // Debug.Log("I'm adding axis + " + axis.name + " and slider is: " + clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>());
            // Debug.Log("I'm adding axis + " + axis.name + " and MIN LIMIT is: " + minLimit);
            // Debug.Log("I'm adding axis + " + axis.name + " and MAX LIMIT is: " + maxLimit);

            var typ = SceneManager.Instance.dataObject.TypeDimensionDictionary1[axis.axisId];

            clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>().SetLimits(minLimit, maxLimit);
            clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>().hasCustomText = (typ == "string");
            clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>().SetValues(minLimit, maxLimit);
            // clonedSlider.GetComponent<UnityEngine.UI.Slider>().minValue = -0.5f;
            // clonedSlider.GetComponent<UnityEngine.UI.Slider>().maxValue = 0.5f;

            clonedSlider.GetComponentInChildren<Text>().text = SceneManager.Instance.dataObject.Identifiers[axisId];



            // The min max slider has a minValue and maxValue as the slider parts
            clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>().onValueChanged.AddListener(delegate {OnTestSliderChanged(clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>(), axisId);});

            clonedSlider.GetComponentInChildren<FilterDragHandlerScript>().filterAxisId = axisId;

            // Debug.Log("Added one! : " + axis.name);

            // TODO: figure out what to do about duplicate axes

        }
    }

    public void AddNewFilter(List<AttributeFilter> newFilters) {
        foreach (var filter in newFilters)
        {
            var dobj = SceneManager.Instance.dataObject;
            // Check if it's global or local
            if(isGlobalFilterBubble) {
                bool globalExists = SceneManager.Instance.globalFilters.Any(attrFilter => attrFilter.idx == filter.idx);

                if(globalExists)
                    continue; 

                SceneManager.Instance.globalFilters.Add(new AttributeFilter(filter.idx, dobj.Identifiers[filter.idx], -0.5f, 0.5f, 0f, 1f, true));
                
            } else {
                // Also create an Attribute filter and add it to the list of Attribute Filter that we already have
                bool exists = parentVisualization.AttributeFilters.Any(attrFilter => attrFilter.idx == filter.idx);
                // for now if it already exists we're not going to add it, but later we can just replace it with the new one or add it on top and give it another ID or something ... basically having two filters of the same sort (shouldn't be any problem)

                if(exists) 
                    continue;

                parentVisualization.AttributeFilters.Add(new AttributeFilter(filter.idx, dobj.Identifiers[filter.idx], filter.minFilter, filter.maxFilter, 0f, 1f));
                
            }

            // if(!filterAxes.Any(item => item.axis.axisId == axis.axisId)) {
                filterAxes.Add(new AxisAndVizes(filter.idx, transform));
            // }

            sliderPrefab.SetActive(false);
            GameObject clonedSpacer = Instantiate(spacerprefab, spacerprefab.transform.position, spacerprefab.transform.rotation, controlGameobject);
            GameObject clonedSlider = Instantiate(sliderPrefab, sliderPrefab.transform.position, sliderPrefab.transform.rotation, controlGameobject);
            clonedSlider.SetActive(true);
            // UnityEngine.UI.Slider sliderComponent = clonedSlider.GetComponent<UnityEngine.UI.Slider>();
            
            float minLimit = Mathf.Lerp(dobj.DimensionsRange[filter.idx].x, dobj.DimensionsRange[filter.idx].y, 0f);
            float maxLimit = Mathf.Lerp(dobj.DimensionsRange[filter.idx].x, dobj.DimensionsRange[filter.idx].y, 1f);

            // Debug.Log("I'm adding axis + " + axis.name + " and slider is: " + clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>());
            // Debug.Log("I'm adding axis + " + axis.name + " and MIN LIMIT is: " + minLimit);
            // Debug.Log("I'm adding axis + " + axis.name + " and MAX LIMIT is: " + maxLimit);
            
            var minFilterValue = UtilMath.normaliseValue(filter.minFilter, -0.5f, 0.5f, minLimit, maxLimit);
            var maxFilterValue = UtilMath.normaliseValue(filter.maxFilter, -0.5f, 0.5f, minLimit, maxLimit);

            clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>().SetLimits(minLimit, maxLimit);
            clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>().SetValues(minFilterValue, maxFilterValue);
            // clonedSlider.GetComponent<UnityEngine.UI.Slider>().minValue = -0.5f;
            // clonedSlider.GetComponent<UnityEngine.UI.Slider>().maxValue = 0.5f;

            clonedSlider.GetComponentInChildren<Text>().text = dobj.Identifiers[filter.idx];

            // The min max slider has a minValue and maxValue as the slider parts
            clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>().onValueChanged.AddListener(delegate {OnTestSliderChanged(clonedSlider.GetComponent<Min_Max_Slider.MinMaxSlider>(), filter.idx);});

            clonedSlider.GetComponentInChildren<FilterDragHandlerScript>().filterAxisId = filter.idx;

            
            // Debug.Log("Added one! : " + axis.name);

            // TODO: figure out what to do about duplicate axes

        }

        // this is to update the text in case it hasn't beed updated yet!
        FilterBubblebuttonGameobject.changeCompactFilterText();
    }

    public void removeFilter(int axisId, GameObject sliderToRemove) {
        Debug.Log("I'm called to remove the filter! hurray! :D ");

        // destory the slider component
        var rebornAxisIndex = filterAxes.FindIndex(axisAndViz => axisAndViz.axis.axisId == axisId);
        Axis rebornAxis = filterAxes[rebornAxisIndex].axis;
        var correspondingVises = filterAxes[rebornAxisIndex].vizes;
        filterAxes.RemoveAt(rebornAxisIndex);

        rebornAxis.transform.position = sliderToRemove.transform.position;
        Vector3 finalAxisPosition = rebornAxis.transform.position + (sliderToRemove.transform.forward * -0.2f);
        
        GameObject.Destroy(sliderToRemove);
        if(isGlobalFilterBubble) {
            var filterToRemove = SceneManager.Instance.globalFilters.SingleOrDefault(attrFilter => attrFilter.idx == axisId);


            if(filterToRemove != null) {
                SceneManager.Instance.globalFilters.Remove(filterToRemove);
            }

            EventManager.TriggerEvent(ApplicationConfiguration.OnFilterSliderChanged, axisId);
        } else {
            // Also create an Attribute filter and add it to the list of Attribute Filter that we already have
            var filterToRemove = parentVisualization.AttributeFilters.SingleOrDefault(attrFilter => attrFilter.idx == axisId);


            if(filterToRemove != null) {
                parentVisualization.AttributeFilters.Remove(filterToRemove);
            }

            EventManager.TriggerEvent(ApplicationConfiguration.OnLocalFilterSliderChanged, parentVisualization.GetInstanceID());
        }

        FilterBubblebuttonGameobject.changeCompactFilterText();

        // Call axis init and init origin when we're done with axis removal

        rebornAxis.gameObject.SetActive(true);
        foreach(var viz in correspondingVises) {
            Debug.Log("I'm activating viz " + viz.name);
            viz.gameObject.SetActive(true);
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(rebornAxis.transform.DOMove(finalAxisPosition, 0.5f).SetEase(Ease.OutElastic));
        seq.Join(rebornAxis.transform.DOScale(new Vector3(0.02059407f, 0.2660912f, 0.02059407f), 0.5f).SetEase(Ease.OutElastic));
        seq.AppendCallback(() => {
            if(!SceneManager.Instance.sceneAxes.Contains(rebornAxis))
                SceneManager.Instance.AddAxis(rebornAxis);
        });
        foreach (var visualisation in correspondingVises)
        {
            seq.Join(visualisation.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutElastic));
        }

        // register action for logger
        DataLogger.Instance.LogActionData("FilterRemoved", parentVisualization.gameObject);
    }

    public void OnTestSliderChanged(Min_Max_Slider.MinMaxSlider slider, int axisAsFilterId)
    {
        /* VERY IMPORTANT */
        /* VERY IMPORTANT */
        /* VERY IMPORTANT */

        /* THE FILTERS IN THE SYSTEM ARE ORIGINALLY BETWEEN -.5 AND .5 AS IT'S THERE IN THE 
            AXIS RANGE WIDGET!
         */

        /* VERY IMPORTANT */
        /* VERY IMPORTANT */
        /* VERY IMPORTANT */

        var AttributeRange = SceneManager.Instance.dataObject.DimensionsRange[axisAsFilterId];
        float minValue = Mathf.Lerp(AttributeRange.x, AttributeRange.y, slider.GetPercentageValues()[0]);
        float maxValue = Mathf.Lerp(AttributeRange.x, AttributeRange.y, slider.GetPercentageValues()[1]);

        float nearestMinValue = UtilMath.ClosestTo(SceneManager.Instance.dataObject.TextualDimensions.Keys.ToList(), minValue);
        float nearestMaxValue = UtilMath.ClosestTo(SceneManager.Instance.dataObject.TextualDimensions.Keys.ToList(), maxValue);

        string minimumValueDimensionLabeltext = SceneManager.Instance.dataObject.TextualDimensions[nearestMinValue].ToString();
        string maximumValueDimensionLabeltext = SceneManager.Instance.dataObject.TextualDimensions[nearestMaxValue].ToString();

        slider.UpdateText(minimumValueDimensionLabeltext, maximumValueDimensionLabeltext);



        // TODO: tell the visualization class that something has been changed and it needs to be updated
        float normalisedMinValue = SceneManager.Instance.dataObject.normaliseValue(slider.GetPercentageValues()[0], 0, 1f, -0.5f, 0.5f);
        float normalisedMaxValue = SceneManager.Instance.dataObject.normaliseValue(slider.GetPercentageValues()[1], 0, 1f, -0.5f, 0.5f);
         Debug.Log(axisAsFilterId + "'s min percentage is " + slider.GetPercentageValues()[0]);
        Debug.Log(axisAsFilterId + "'s max percentage is: " + slider.GetPercentageValues()[1]);
        // Debug.Log(axisAsFilterId + "'s value has changed and it's min is now: " + slider.minValue);
        // Debug.Log(axisAsFilterId + "'s value has changed and it's max is now: " + slider.maxValue);
        // Debug.Log(axisAsFilterId + "'s source index is: " + axisAsFilter.axisId);
         Debug.Log(axisAsFilterId + "'s value has changed and it's normalised min value is: " + normalisedMinValue);
        Debug.Log(axisAsFilterId + "'s value has changed and it's normalised max value is: " + normalisedMaxValue);
        // Debug.Log(axisAsFilter.name + "'s value has changed and AXIS normaliseR min value is: " + axisAsFilter.MinNormaliser);
        // Debug.Log(axisAsFilter.name + "'s value has changed and AXIS normaliseR max value is: " + axisAsFilter.MaxNormaliser);

        // We should remember that each view object has the Visualization as its direct parent
        // This could be the way for us to access the properies of its parent

        // For now let's just pass the minFilterValue to the View filtering function
        // (because I don't have any better way of collecting min and max from the sliders now)

        // --------------- GLOBAL FILTERING (not really any more!) --------------- //
        if(isGlobalFilterBubble) {
            int foundGlobalIndex = SceneManager.Instance.globalFilters.FindIndex(attrFilter => attrFilter.idx == axisAsFilterId);
            if(foundGlobalIndex != -1) {
                // For now I'm just changing the minFilter value, later we're gonna go more into details
                SceneManager.Instance.globalFilters[foundGlobalIndex].minFilter = normalisedMinValue;
                SceneManager.Instance.globalFilters[foundGlobalIndex].maxFilter = normalisedMaxValue;
            }

            // This triggers it for all of the visualizations
            EventManager.TriggerEvent(ApplicationConfiguration.OnFilterSliderChanged, axisAsFilterId);
        } else {
            // if it's local
            int foundIndex = parentVisualization.AttributeFilters.FindIndex(attrFilter => attrFilter.idx == axisAsFilterId);
            if(foundIndex != -1) {
                // For now I'm just changing the minFilter value, later we're gonna go more into details
                parentVisualization.AttributeFilters[foundIndex].minFilter = normalisedMinValue;
                parentVisualization.AttributeFilters[foundIndex].maxFilter = normalisedMaxValue;
            }

            // When called without any params it would simply be the local filtering then! 
            // parentVisualization.DoFilter();

            // We need to trigger an event here that let's us know a filtering thing got changed! 

            EventManager.TriggerEvent(ApplicationConfiguration.OnLocalFilterSliderChanged, parentVisualization.GetInstanceID());
        }


        
    }
}
