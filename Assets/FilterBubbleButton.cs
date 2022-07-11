using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public class FilterBubbleButton : MonoBehaviour, Grabbable
{
    public static Color OG_COMPACT_MENU_COLOR = new Color(0.07843138f, 0.07843138f, 0.07843138f, 0.9411765f);


    public GameObject filterBubbleGameobject;
    public CanvasGroup filterBubbleMenuCanvas;
    public GameObject filterBubbleCompactGameobject;

    public Text filterTextsGameobject;

    public Visualization visReference; 
    public CanvasGroup filterBubbleCompactMenuCanvas;

    [SerializeField]
    UnityEvent OnEntered;

    [SerializeField]
    UnityEvent OnExited;

    public Vector3 initialScale;
    Vector3 rescaled = Vector3.one;

    public bool isGlobalFilterBubble = false;

    public bool hasCollidedWithVis = false;
    public Visualization collidedVis = null;

    private WandController visGrabbingController = null;
    private string currentFilterText = "";



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
            Debug.Assert((filterBubbleMenuCanvas != null), "The filter bubble menu canvas cannot be null");
            Debug.Assert((filterBubbleCompactMenuCanvas != null), "The filter bubble compact menu canvas cannot be null");
            //filterBubbleMenuCanvas = filterBubbleGameobject.transform.Find("ScatterplotMenu").GetComponent<CanvasGroup>();
            //filterBubbleCompactMenuCanvas = filterBubbleCompactGameobject.transform.Find("ScatterplotMenu").GetComponent<CanvasGroup>();
        }
    }

    public int GetPriority()
    {
        return 1; // I should work with this to have the lowest priority
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

        ExpandFilterBubble();
    }

    void OnTriggerEnter(Collider other) {
        // if the entered one is a visualization or an axis
        // Even the histograms are always visualizations so listen for visualization collapse and not axis! 
        if(other.GetComponent<Visualization>()) {
            Debug.Log("I've collided with an visualization/axis with the name of: " + other.name);

            // just set a flag here that says we've collided with a visualization 
            hasCollidedWithVis = true;
            collidedVis = other.GetComponent<Visualization>();
            visGrabbingController = collidedVis.axes[0].grabbingController;

            
            showDropFilterHint();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(hasCollidedWithVis && collidedVis != null)
        {
            var exitingVis = other.GetComponent<Visualization>();
            if (exitingVis != null && exitingVis.GetInstanceID() == collidedVis.GetInstanceID())
            {
                // means that if the same visualization that entered is exitting now
                // reset everything
                hasCollidedWithVis = false;
                collidedVis = null;

                OnExited.Invoke();
                hideDropFilterHint();
                CollapseFilterBubble();
            }
        }
    }

    public void OnExit(WandController controller)
    {
        // regardless of what the controller is carrying, if we exit the filter area, reset everything
        hasCollidedWithVis = false;
        collidedVis = null;

        OnExited.Invoke();
        hideDropFilterHint();
        CollapseFilterBubble();
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

    public bool OnGrab(WandController controller)
    {
        return false; // it's not supposed to be draggable, so return false for this
    }

    public void OnRelease(WandController controller)
    {
        // nothing here to implement
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

    void Update()
    {
        if(hasCollidedWithVis && visGrabbingController != null &&  collidedVis != null)
        {
            if(!visGrabbingController.gripping)
            {
                // if the controller that is holding the visualization released the trigger
                // while inside the filter bubble area, add the filters 
                hideDropFilterHint();
                TurnVisIntoFilters(collidedVis);
            }
        }        
    }

    private void TurnVisIntoFilters(Visualization vis)
    {
        // find out the involved axis in the visualization
        List<Axis> involvedAxes;
        involvedAxes = vis.axes;

        // never add the prototypes to the filters 
        if (involvedAxes.Any(axis => axis.isPrototype))
            return;

        // Here we're making sure that both the axes and the visualization will hide after hitting one another
        Sequence seq = DOTween.Sequence();
        seq.Append(vis.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
        foreach (var axis in involvedAxes)
        {
            seq.Join(axis.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
        }
        seq.AppendCallback(() => {

            foreach (var axis in involvedAxes)
            {
                axis.gameObject.SetActive(false);
            }
            vis.gameObject.SetActive(false);
            visReference.AddNewFilterToFilterBubbles(involvedAxes);

            changeCompactFilterText();
        });
    }

    private void ExpandFilterBubble()
    {
        if (filterBubbleGameobject && filterBubbleCompactGameobject && filterBubbleCompactMenuCanvas && filterBubbleMenuCanvas)
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(filterBubbleCompactMenuCanvas.DOFade(0f, 0.5f).SetEase(Ease.OutSine));
            seq.Join(filterBubbleMenuCanvas.DOFade(1f, 0.5f).SetEase(Ease.InSine));
        }
    }

    private void CollapseFilterBubble()
    {
        if (filterBubbleGameobject && filterBubbleCompactGameobject && filterBubbleCompactMenuCanvas && filterBubbleMenuCanvas)
        {
            changeCompactFilterText();
            Sequence seq = DOTween.Sequence();
            seq.Append(filterBubbleMenuCanvas.DOFade(0f, 0.5f).SetEase(Ease.OutSine));
            seq.Join(filterBubbleCompactMenuCanvas.DOFade(1f, 0.5f).SetEase(Ease.InSine));
        }
    }

    public void changeCompactFilterText() {
        List<AttributeFilter> localFilters;

        if(isGlobalFilterBubble)
            localFilters = SceneManager.Instance.globalFilters;
        else
            localFilters = visReference.AttributeFilters;

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
        currentFilterText = filterText;
        filterTextsGameobject.text = filterText;
    }

    private void showDropFilterHint()
    {
        filterBubbleCompactMenuCanvas.gameObject.GetComponent<Image>().color = Color.green;
        filterTextsGameobject.text = "DROP VIS HERE TO APPLY AS FILTER";
    }

    private void hideDropFilterHint()
    {
        filterBubbleCompactMenuCanvas.gameObject.GetComponent<Image>().color = OG_COMPACT_MENU_COLOR;
        filterTextsGameobject.text = currentFilterText;
    }
}
