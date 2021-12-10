using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public class FilterBubbleButton : MonoBehaviour, Grabbable
{

    public GameObject filterBubbleGameobject;
    private CanvasGroup filterBubbleMenuCanvas;
    public GameObject filterBubbleCompactGameobject;

    public Text filterTextsGameobject;

    public Visualization visReference; 
    private CanvasGroup filterBubbleCompactMenuCanvas;

    [SerializeField]
    UnityEvent OnEntered;

    [SerializeField]
    UnityEvent OnExited;

    public Vector3 initialScale;
    Vector3 rescaled = Vector3.one;

    public bool isGlobalFilterBubble = false;

    // Use this for initialization
    void Start () {
        //initialScale = transform.localScale;
        rescaled = initialScale;
        rescaled.x *= 2f;
        rescaled.y *= 2f;
        rescaled.z *= 2f;

        Debug.Assert((filterTextsGameobject != null), "The fitler text ref object cannot be null");

        if(!isGlobalFilterBubble)
            Debug.Assert((visReference != null), "The visualisation ref object cannot be null");

        Debug.Assert((filterBubbleGameobject != null), "The filter bubble object cannot be null");
        Debug.Assert((filterBubbleCompactGameobject != null), "The filter bubble object cannot be null");
        if(filterBubbleCompactGameobject && filterBubbleGameobject) {
            filterBubbleMenuCanvas = filterBubbleGameobject.transform.Find("ScatterplotMenu").GetComponent<CanvasGroup>();
            filterBubbleCompactMenuCanvas = filterBubbleCompactGameobject.transform.Find("ScatterplotMenu").GetComponent<CanvasGroup>();
        }
    }

    public int GetPriority()
    {
        return 100; // I should work with this to have the highest priority
    }

    public void OnDrag(WandController controller)
    {
        // I don't think we should do anything about the dragging of this thing
    }

    public void OnEnter(WandController controller)
    {
        OnEntered.Invoke();
        // Make sure that we have some fitlers to display before actually doing it! 
        if(!isGlobalFilterBubble) {
            if(visReference.AttributeFilters == null || visReference.AttributeFilters.Count == 0) 
                return;
        } else {
            if(SceneManager.Instance.globalFilters == null || SceneManager.Instance.globalFilters.Count == 0) 
                return;
        }

        if(filterBubbleGameobject && filterBubbleCompactGameobject && filterBubbleCompactMenuCanvas && filterBubbleMenuCanvas) {
            Sequence seq = DOTween.Sequence();
            seq.Append(filterBubbleCompactMenuCanvas.DOFade(0f, 0.5f).SetEase(Ease.OutSine));
            seq.Join(filterBubbleMenuCanvas.DOFade(1f, 0.5f).SetEase(Ease.InSine));
        }
    }

    void OnTriggerEnter(Collider other) {
        // if the entered one is a visualization or an axis
        // Even the histograms are always visualizations so listen for visualization collapse and not axis! 
        if(other.GetComponent<Visualization>()) {
            Debug.Log("I've collided with an axis with the name of: " + other.name);

            // find out the involved axis in the visualization
            List<Axis> involvedAxes;
            // if(other.GetComponent<Visualization>()) {
            involvedAxes = other.GetComponent<Visualization>().axes;
            if(involvedAxes.Any(axis => axis.isPrototype))
                return;

            
            // Here we're making sure that both the axes and the visualization will hide after hitting one another
            Sequence seq = DOTween.Sequence();
            seq.Append(other.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
            foreach (var axis in involvedAxes)
            {
              seq.Join(axis.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
            }
            seq.AppendCallback(() => {
                other.gameObject.SetActive(false);

                foreach (var axis in involvedAxes)
                {
                    axis.gameObject.SetActive(false);
                }
                filterBubbleGameobject.GetComponent<FilterBubbleScript>().AddNewFilter(involvedAxes);
               
                if(!isGlobalFilterBubble)
                    changeCompactFilterText(visReference.AttributeFilters);
                else 
                    changeCompactFilterText(SceneManager.Instance.globalFilters);
            });
        }
    }

    public void OnExit(WandController controller)
    {
        OnExited.Invoke();
        if(filterBubbleGameobject && filterBubbleCompactGameobject && filterBubbleCompactMenuCanvas && filterBubbleMenuCanvas) {
            if(!isGlobalFilterBubble)
                changeCompactFilterText(visReference.AttributeFilters);
            else 
                changeCompactFilterText(SceneManager.Instance.globalFilters);
            Sequence seq = DOTween.Sequence();
            seq.Append(filterBubbleMenuCanvas.DOFade(0f, 0.5f).SetEase(Ease.OutSine));
            seq.Join(filterBubbleCompactMenuCanvas.DOFade(1f, 0.5f).SetEase(Ease.InSine));
        }
    }

    public bool OnGrab(WandController controller)
    {
        // We should simply act as the ontouch of a button and show the childs of it which are the data shelf items
        // it needs to be a toggle situation - clicked: on/off

        // if this doesn't return a true value, the OnRelease won't work
        return false; // it's not supposed to be draggable, so return false for this
    }

    public void OnRelease(WandController controller)
    {

    }

     public void ProximityEnter()
    {
        // transform.DOKill(true);
        // transform.DOScale(rescaled, 0.35f).SetEase(Ease.OutBack);
    }

    public void ProximityExit()
    {
        // transform.DOKill(true);
        // transform.DOScale(initialScale, 0.25f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void changeCompactFilterText(List<AttributeFilter> filters) {
        var localFilters = isGlobalFilterBubble ? SceneManager.Instance.globalFilters : filters;
        string filterText = ""; 

        
        for (int i = 0; i < localFilters.Count; i++)
        {
            var filter = localFilters[i];
            string minimumValueDimensionLabel = "";
            string maximumValueDimensionLabel = "";
            Vector2 AttributeRange = SceneManager.Instance.dataObject.DimensionsRange[filter.idx];
            if(i > 1) {
                filterText = filterText + ", ...";
                break;
            }

            string type = SceneManager.Instance.dataObject.TypeDimensionDictionary1[filter.idx];

            if (type == "float")
            {
                minimumValueDimensionLabel = Mathf.Lerp(AttributeRange.x, AttributeRange.y, filter.minFilter + 0.5f).ToString("0");
                maximumValueDimensionLabel = Mathf.Lerp(AttributeRange.x, AttributeRange.y, filter.maxFilter + 0.5f).ToString("0");
            }

            else if (type == "string")
            {
                float minValue = Mathf.Lerp(AttributeRange.x, AttributeRange.y, filter.minFilter + 0.5f);
                float maxValue = Mathf.Lerp(AttributeRange.x, AttributeRange.y, filter.maxFilter + 0.5f);

                float nearestMinValue = UtilMath.ClosestTo(SceneManager.Instance.dataObject.TextualDimensions.Keys.ToList(), minValue);
                float nearestMaxValue = UtilMath.ClosestTo(SceneManager.Instance.dataObject.TextualDimensions.Keys.ToList(), maxValue);

                minimumValueDimensionLabel = SceneManager.Instance.dataObject.TextualDimensions[nearestMinValue].ToString();
                maximumValueDimensionLabel = SceneManager.Instance.dataObject.TextualDimensions[nearestMaxValue].ToString();
            }

            filterText = filterText + SceneManager.Instance.dataObject.indexToDimension(filter.idx) + ": [" + minimumValueDimensionLabel + "-" + maximumValueDimensionLabel + "]" + (i < 1 ? "\n " : "");
        }
        filterTextsGameobject.text = filterText;
    }
}
