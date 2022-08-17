using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PromptCollisionHandler : MonoBehaviour
{
    public enum PromptType {
        Color, 
        Size
    }

    public Transform textTransform = null;
    public Visualization parentVisualization = null;
    public PromptType promptType;

    // this is a reference to the other prompt of this visualization, if that prompt was active, then this one should stay deactivated
    public GameObject OtherPromptGameObject = null;

    // This flag shows whether or not the prompt is showing, it's just to make detection of it easier for the other prompt component
    public bool promptVisibilityStatus = false;

    void Start()
    {
        Debug.Assert(textTransform != null, "in prompt: text traansform shouldn't be null");
        Debug.Assert(parentVisualization != null, "in prompt: parentVisualization shouldn't be null");
        Debug.Assert(OtherPromptGameObject != null, "in prompt: OtherPromptComponent shouldn't be null");

        hideDropHint();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        // If the other prompt is active, then just don't do anything!
        // TODO: change it with a more robust mechanism
        if (OtherPromptGameObject.GetComponent<PromptCollisionHandler>().promptVisibilityStatus)
            return;


        Visualization collidedVis = other.GetComponent<Visualization>();
        if (collidedVis && collidedVis.viewType == Visualization.ViewType.Histogram)
        {
            if (parentVisualization.viewType == Visualization.ViewType.Histogram)
                return;

            print("I've hit a histo or an Axis! " + other.name);

            // because a histogram has only one Axis
            collidedVis.axes[0].setCollidedVisualizationForPrompt(parentVisualization, promptType);

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
            collidedVis.axes[0].setCollidedVisualizationForPrompt(null, promptType);

            hideDropHint();
        }
    }

    private void showDropHint()
    {
        GetComponent<CanvasRenderer>().SetAlpha(1f);
        textTransform.gameObject.SetActive(true);
        promptVisibilityStatus = true;
    }
    private void hideDropHint()
    {
        GetComponent<CanvasRenderer>().SetAlpha(0f);
        textTransform.gameObject.SetActive(false);
        promptVisibilityStatus = false;
    }
}
