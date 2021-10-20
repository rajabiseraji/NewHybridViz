﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class FilterBubbleScript : MonoBehaviour
{

    public Visualization parentVisualization;
    public GameObject sliderPrefab; 
    public Transform controlGameobject;


    [Tooltip("A list of dropdowns that will be automatically populated with attribute names")]
    public List<Dropdown> AttributeDropdowns = new List<Dropdown>();

    public UnityEngine.UI.Slider PointMinSizeSlider;

    public UnityEngine.UI.Slider PointMaxSizeSlider;

    public ColourPickerMenu colorPickerMenu;

    public Button MinGradientColourButton;

    public Button MaxGradientColourButton;


    // Use this for initialization
    void Start()
    {
        foreach (var dropdown in AttributeDropdowns)
        {
            dropdown.AddOptions(SceneManager.Instance.dataObject.Identifiers.ToList());
        }

        UpdateGradientButtons();

        colorPickerMenu.OnHidePicker = OnHidePicker;
        colorPickerMenu.OnShowPicker = OnShowPicker;

        // Make it transparent at the beginning
        GetComponentInChildren<CanvasGroup>().alpha = 0f;

        GetComponent<ViveMenu>().Controller = GameObject.FindGameObjectsWithTag("Controller")[0].GetComponent<SteamVR_TrackedController>();

        
    }

    void Update() {
        if(GetComponent<ViveMenu>().Controller == null) {
            GetComponent<ViveMenu>().Controller = GameObject.FindGameObjectsWithTag("Controller")[0].GetComponent<SteamVR_TrackedController>();
        }
    }

    public void AddNewFilter(List<Axis> axes) {
        foreach (var axis in axes)
        {
            GameObject clonedSlider = Instantiate(sliderPrefab, sliderPrefab.transform.position, sliderPrefab.transform.rotation, controlGameobject);
            UnityEngine.UI.Slider sliderComponent = clonedSlider.GetComponent<UnityEngine.UI.Slider>();
            sliderComponent.minValue = axis.MinFilter;
            sliderComponent.maxValue = axis.MaxFilter;
            clonedSlider.GetComponentInChildren<Text>().text = axis.name;
            Debug.Log("Added one! : " + axis.name);
        }
    }

    public void OnTestSliderChanged(float value)
    {
        Debug.Log("My value has changed and it's now: " + value);
    }
    public void OnLinkAttributeChanged(int idx)
    {
        VisualisationAttributes.Instance.LinkedAttribute = idx - 1;

        EventManager.TriggerEvent(ApplicationConfiguration.OnLinkedAttributeChanged, VisualisationAttributes.Instance.LinkedAttribute);

    }
    
    public void OnPointSizeChanged(float value)
    {
        VisualisationAttributes.Instance.ScatterplotDefaultPointSize = value;
        EventManager.TriggerEvent(ApplicationConfiguration.OnSlideChangePointSize, value);
    }

    public void OnPointSizeMinChanged(float value)
    {
        EventManager.TriggerEvent(ApplicationConfiguration.OnSlideChangeMinPointSize, value);
    }

    public void OnPointSizeMaxChanged(float value)
    {
        EventManager.TriggerEvent(ApplicationConfiguration.OnSlideChangeMaxPointSize, value);
    }

    public void OnPointSizeAttributeChanged(int idx)
    {
        VisualisationAttributes.Instance.SizeAttribute = idx - 1;

        PointMaxSizeSlider.interactable = idx != 0;
        PointMinSizeSlider.interactable = idx != 0;

        if (VisualisationAttributes.Instance.SizeAttribute >= 0)
        {
            VisualisationAttributes.Instance.sizes = SceneManager.Instance.dataObject.getDimension(VisualisationAttributes.Instance.SizeAttribute);
        }
        else
        {
            VisualisationAttributes.Instance.sizes = Enumerable.Range(0, SceneManager.Instance.dataObject.DataPoints).Select(_ => 1f).ToArray();
        }

        EventManager.TriggerEvent(ApplicationConfiguration.OnScatterplotAttributeChanged, VisualisationAttributes.Instance.SizeAttribute);
    }

    public void OnLinkTransparencyChanged(float value)
    {
        VisualisationAttributes.Instance.LinkTransparency = value;
        EventManager.TriggerEvent(ApplicationConfiguration.OnLinkedTransparencyChanged, value);
    }

    //
    // Colours
    //
    
    public void OnColorAttributeChanged(int idx)
    {
        VisualisationAttributes.Instance.ColoredAttribute = idx;
        UpdateColorMapping();
    }

    public void OnHidePicker()
    {
        foreach (Transform tr in transform)
        {
            if (tr.GetComponent<CanvasGroup>() != null)
            {
                tr.GetComponent<CanvasGroup>().interactable = true;
            }
        }
    }

    public void OnShowPicker()
    {
        foreach (Transform tr in transform)
        {
            if (tr.GetComponent<CanvasGroup>() != null)
            {
                tr.GetComponent<CanvasGroup>().interactable = false;
            }
        }
    }

    public void OnSetMinGradientColor()
    {
        colorPickerMenu.OnColourChanged = _ =>
        {
            VisualisationAttributes.Instance.MinGradientColor = _;
            UpdateGradientButtons();
            UpdateColorMapping();
        };
        colorPickerMenu.ShowColorPicker(VisualisationAttributes.Instance.MinGradientColor);
    }

    public void OnSetMaxGradientColor()
    {
        colorPickerMenu.OnColourChanged = _ =>
        {
            VisualisationAttributes.Instance.MaxGradientColor = _;
            UpdateGradientButtons();
            UpdateColorMapping();
        };
        colorPickerMenu.ShowColorPicker(VisualisationAttributes.Instance.MaxGradientColor);
    }

    public void OnCategoricalColoring()
    {
        VisualisationAttributes.Instance.IsGradientColor = false;
        UpdateColorMapping();
    }

    public void OnGradientColoring()
    {
        VisualisationAttributes.Instance.IsGradientColor = true;
        UpdateColorMapping();
    }

    void UpdateGradientButtons()
    {
        var cols = MinGradientColourButton.colors;
        Color c = new Color(VisualisationAttributes.Instance.MinGradientColor.r, VisualisationAttributes.Instance.MinGradientColor.g, VisualisationAttributes.Instance.MinGradientColor.b);
        cols.normalColor = c;
        cols.highlightedColor = c;
        cols.pressedColor = c;
        c.a = 0.5f;
        cols.disabledColor = c;
        MinGradientColourButton.colors = cols;

        cols = MaxGradientColourButton.colors;
        c = new Color(VisualisationAttributes.Instance.MaxGradientColor.r, VisualisationAttributes.Instance.MaxGradientColor.g, VisualisationAttributes.Instance.MaxGradientColor.b);
        cols.normalColor = c;
        cols.highlightedColor = c;
        cols.pressedColor = c;
        c.a = 0.5f;
        cols.disabledColor = c;
        MaxGradientColourButton.colors = cols;
    }

    void UpdateColorMapping()
    {

        if (VisualisationAttributes.Instance.IsGradientColor)
        {
            VisualisationAttributes.Instance.colors = VisualisationAttributes.getContinuousColors(VisualisationAttributes.Instance.MinGradientColor, VisualisationAttributes.Instance.MaxGradientColor, SceneManager.Instance.dataObject.getDimension(VisualisationAttributes.Instance.ColoredAttribute));
        }
        else
        {

            List<float> categories = SceneManager.Instance.dataObject.getNumberOfCategories(VisualisationAttributes.Instance.ColoredAttribute);
            int nbCategories = categories.Count;
            Color[] palette = Colors.generateColorPalette(nbCategories);

            Dictionary<float, Color> indexCategoryToColor = new Dictionary<float, Color>();
            for (int i = 0; i < categories.Count; i++)
            {
                indexCategoryToColor.Add(categories[i], palette[i]);
            }

            VisualisationAttributes.Instance.colors = Colors.mapColorPalette(SceneManager.Instance.dataObject.getDimension(VisualisationAttributes.Instance.ColoredAttribute), indexCategoryToColor);
        }
        EventManager.TriggerEvent(ApplicationConfiguration.OnColoredAttributeChanged, VisualisationAttributes.Instance.ColoredAttribute);

    }
}
