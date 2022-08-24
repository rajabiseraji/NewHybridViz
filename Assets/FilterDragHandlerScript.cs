using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilterDragHandlerScript : MonoBehaviour
{
    Transform controllerTransform = null;
    WandController controller = null;
    // Start is called before the first frame update
    FilterBubbleScript parentFilterBubble;
    public int filterAxisId = -1;
    public GameObject highlightGameobject = null;
    public GameObject parentSlider = null;
    bool isCollidingWithController = false;
    
    void Start()
    {
        parentFilterBubble = GetComponentInParent<FilterBubbleScript>();
        Debug.Assert(parentFilterBubble != null, "Parent filter bubble shouldn't be null!");
        Debug.Assert(highlightGameobject != null, "Highlight game object shouldn't be null!");
        Debug.Assert(parentSlider != null, "Parent slider game object shouldn't be null!");
    }

    // Update is called once per frame
    void Update()
    {
        if(controller == null && controllerTransform == null) 
            return;

        if(controller.gripping) {
            float distance = Vector3.Distance(controllerTransform.position, transform.position);
            if(!highlightGameobject.activeSelf)
                highlightGameobject.SetActive(true);
            if(distance > 0.25f) {
                Debug.Log("distance is " + distance);
                Debug.Log("controller gripping is " + controller.gripping);
                Debug.Assert(filterAxisId != -1, "Filter Axis Id is not set");
                parentFilterBubble.removeFilter(filterAxisId, parentSlider);
                controller = null;
                controllerTransform = null;
                isCollidingWithController = false;
                highlightGameobject.SetActive(false);
            }
        }
    }

    void OnTriggerEnter(Collider other) {
        if(other.GetComponent<WandController>() == null)
            return;
        
        // if the collided was controller do this
        var tempController = other.GetComponent<WandController>();
        Debug.Log("controller collided and it's " + tempController.gripping + " gripping");
        
        controller = tempController;
        controllerTransform = other.transform;
        isCollidingWithController = true;

    }

    void OnTriggerExit(Collider other) {
        //if(other.GetComponent<WandController>() == null) {
        //    controller = null;
        //    controllerTransform = null;
        //} else {
        //    var tempController = other.GetComponent<WandController>();
        //    if(!tempController.gripping)  {
        //        controller = null;
        //        controllerTransform = null;
        //        highlightGameobject.SetActive(false);
        //    }
        //}
        if(other.GetComponent<WandController>())
        {
            if(controller != null && controllerTransform.GetInstanceID() == other.transform.GetInstanceID() && !controller.gripping)
            {
                isCollidingWithController = false;
                controller = null;
                controllerTransform = null;
                highlightGameobject.SetActive(false);
            }
        }
        
    }
}
