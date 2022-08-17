using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PromptCollisionHandler : MonoBehaviour
{
    public Transform textTransform = null;
    public Visualization parentVisualization = null;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Assert(textTransform != null, "in prompt: text traansform shouldn't be null");
        Debug.Assert(parentVisualization != null, "in prompt: parentVisualization shouldn't be null");

        hideDropHint();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Visualization collidedVis = other.GetComponent<Visualization>();
        if (collidedVis && collidedVis.viewType == Visualization.ViewType.Histogram)
        {
            if (parentVisualization.viewType == Visualization.ViewType.Histogram)
                return;

            print("I've hit a histo or an Axis! " + other.name);

            // because a histogram has only one Axis
            collidedVis.axes[0].setCollidedVisualizationForPrompt(parentVisualization);

            showDropHint();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        Visualization collidedVis = other.GetComponent<Visualization>();
        if (collidedVis && collidedVis.viewType == Visualization.ViewType.Histogram)
        {
            if (parentVisualization.viewType == Visualization.ViewType.Histogram)
                return;

            print("I've EXITED a histo or an Axis! " + other.name);

            // because a histogram has only one Axis
            collidedVis.axes[0].setCollidedVisualizationForPrompt(null);

            hideDropHint();
        }
    }

    private void showDropHint()
    {
        GetComponent<CanvasRenderer>().SetAlpha(1f);
        textTransform.gameObject.SetActive(true);
    }
    private void hideDropHint()
    {
        GetComponent<CanvasRenderer>().SetAlpha(0f);
        textTransform.gameObject.SetActive(false);
    }
}
