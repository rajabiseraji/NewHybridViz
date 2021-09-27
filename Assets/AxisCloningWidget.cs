using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class AxisCloningWidget : MonoBehaviour, Grabbable
{   
    /* This is the widget in charge of cloning a whole visualization */
    [SerializeField]
    float axisOffset = 2.0f;

    [SerializeField]
    UnityEvent OnEntered;

    [SerializeField]
    UnityEvent OnExited;

    Axis parentAxis;

    public Vector3 initialScale;
    Vector3 rescaled = Vector3.one;

    // Use this for initialization
    void Start () {
        parentAxis = GetComponentInParent<Axis>();
        //initialScale = transform.localScale;
        rescaled = initialScale;
        rescaled.x *= 2f;
        rescaled.y *= 2f;
        rescaled.z *= 2f;
    }
	
	// Update is called once per frame
	void Update () { }

    public void OnEnter(WandController controller)
    {
        OnEntered.Invoke();
    }

    public void OnExit(WandController controller)
    {
        OnExited.Invoke();
    }

    public bool OnGrab(WandController controller)
    {
        // First we set the isClonable of the axis to true!
        /* set the originPosition for all of those involved axes to their current position
            then set the isProto of all of them to true and see what happens
            TODO: we should then flip the switch on the original visualization so that it doesn't get cloned every time! 

            TODO: we need to have something that shows that a visualization has became dirty and the origin of it is no longer the origin that there is on the data shelf
         */

        bool parentIsInVisualization = false;
        List<Visualization> lv = parentAxis.correspondingVisualizations();
        foreach (var visu in lv)
        {  
            // step 1
            foreach (var axis in visu.axes)
            {
                axis.InitOrigin(axis.transform.position, axis.transform.rotation);
                axis.isPrototype = true;
                axis.isClonedByCloningWidget = true;
            }
            // 
            parentIsInVisualization = true;
            visu.OnGrab(controller);
        }
        if (parentIsInVisualization)
            return false;
        else 
            return parentAxis.OnGrab(controller); 
        
        // If we want to clone even an axis, then we need to enable axis cloning for the parent axis, too
        // Which means that we shouldn't just call the OnGrab thing, we should instead do the whole cloning thing
    }

    public void OnRelease(WandController controller)
    {
        bool parentIsInVisualization = false;
        List<Visualization> lv = parentAxis.correspondingVisualizations();
        foreach (var visu in lv)
        {
            parentIsInVisualization = true;
            visu.OnRelease(controller);
        }
        if (!parentIsInVisualization) 
            parentAxis.OnRelease(controller); 
    }

    public void OnDrag(WandController controller)
    {

        Debug.Log("Ive been dragged! Im the cloning thing");
        
        // float offset = parentAxis.CalculateLinearMapping(controller.transform);
        // Vector3 axisOffset = new Vector3(transform.localPosition.x, 0, 0);
        // transform.localPosition = Vector3.Lerp(new Vector3(transform.localPosition.x, -0.5f, 0),
        //                                        new Vector3(transform.localPosition.x, 0.5f, 0), 
        //                                   offset);
        // parentAxis.isDirty = true;
    }
	
    public int GetPriority()
    {
        return 10;
    }

    public void ProximityEnter()
    {
        transform.DOKill(true);
        // transform.DOLocalMoveX(-axisOffset, 0.35f).SetEase(Ease.OutBack);
        transform.DOScale(rescaled, 0.35f).SetEase(Ease.OutBack);
    }

    public void ProximityExit()
    {
        transform.DOKill(true);
        // transform.DOLocalMoveX(0, 0.25f);
        transform.DOScale(initialScale, 0.25f);
    }
}
