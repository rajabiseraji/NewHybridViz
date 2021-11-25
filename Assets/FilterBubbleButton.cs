using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class FilterBubbleButton : MonoBehaviour, Grabbable
{

    public GameObject filterBubbleGameobject;
    private CanvasGroup filterBubbleMenuCanvas;
    public GameObject filterBubbleCompactGameobject;
    private CanvasGroup filterBubbleCompactMenuCanvas;

    [SerializeField]
    UnityEvent OnEntered;

    [SerializeField]
    UnityEvent OnExited;

    public Vector3 initialScale;
    Vector3 rescaled = Vector3.one;

    // Use this for initialization
    void Start () {
        //initialScale = transform.localScale;
        rescaled = initialScale;
        rescaled.x *= 2f;
        rescaled.y *= 2f;
        rescaled.z *= 2f;

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
            // } 
            // else {
            //     involvedAxes = new List<Axis>();
            //     involvedAxes.Add(other.GetComponent<Axis>());
            // }

            // TODO:Call a function from the FilterBubbleScript to make the sliders (or toggles and whatnot)

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
            });
        }
    }

    public void OnExit(WandController controller)
    {
        OnExited.Invoke();
        if(filterBubbleGameobject && filterBubbleCompactGameobject && filterBubbleCompactMenuCanvas && filterBubbleMenuCanvas) {
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
}
