using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Pointer : MonoBehaviour
{
    [SerializeField] private float defaultLength = 5.0f;
    [SerializeField] private GameObject dot = null;
    public WandController parentController = null;
    public bool enableRaycastObjectManipulation = false;
    public bool enableRaycastFilterBubbleManipulation = true;

    private Collider currentCollider = null;
    private FilterBubbleButton currentCollidedFilterBubble = null;

    public Camera Camera { get; private set; } = null;

    private LineRenderer lineRenderer = null;
    public VRInputModule inputModule = null;

    private void Awake()
    {
        Camera = GetComponent<Camera>();
        Camera.enabled = false;

        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        // current.currentInputModule does not work
        inputModule = EventSystem.current.gameObject.GetComponent<VRInputModule>();
    }

    private void Update()
    {
        Debug.Assert(inputModule != null, "the input module should not be zero!");
        Debug.Assert(parentController != null, "Parent controller cannot be null!");
        UpdateLine();
    }

    private void UpdateLine()
    {
        // Use default or distance
        PointerEventData data = inputModule.Data;
        RaycastHit hit = CreateRaycast();

        dot.GetComponent<Renderer>().material.color = Color.blue;

        // If nothing is hit, set do default length
        float colliderDistance = hit.distance == 0 ? defaultLength : hit.distance;
        float canvasDistance = data.pointerCurrentRaycast.distance == 0 ? defaultLength : data.pointerCurrentRaycast.distance;

        // Get the closest one
        float targetLength = Mathf.Min(colliderDistance, canvasDistance);

        // Default
        Vector3 endPosition = transform.position + (transform.forward * targetLength);

        // Set position of the dot
        dot.transform.position = endPosition;

        // Set linerenderer
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, endPosition);

        // Here's a check for when we're brushing
        // in that case just hide the dot object for a bit
        if(hit.distance != 0 && hit.collider.gameObject.layer == LayerMask.NameToLayer("Brushable"))
        {
            dot.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 0);
        }

        handleRemoteObjectManipulation(hit);
    }

    private void handleRemoteObjectManipulation(RaycastHit hit)
    {

        if (!enableRaycastObjectManipulation && !enableRaycastFilterBubbleManipulation)
            return;
        else if(enableRaycastFilterBubbleManipulation)
        {
            if (hit.collider)
            {
                //print("handling dot raycast with " + hit.collider.name);
                handleRaycastFilterBubbleManipulation(hit.collider.GetComponent<FilterBubbleButton>());
            }
            return;
        }

        if (hit.collider != null) // if something was hit
        {
            if (currentCollider != null && hit.collider.GetInstanceID() != currentCollider.GetInstanceID())
                parentController.OnTriggerExit(currentCollider);

            if (currentCollider != null && hit.collider.GetInstanceID() == currentCollider.GetInstanceID())
            {
                // if you hit the same object just don't do anything!
                return;
            }

            //print("dot has collided with " + hit.collider.name);
            parentController.OnTriggerEnter(hit.collider);
            currentCollider = hit.collider;
        }

        if (hit.collider == null)
        {
            if (currentCollider != null)
            {
                parentController.OnTriggerExit(currentCollider);
                //print("dot has exited from " + currentCollider.name);
            }

            currentCollider = null;
        }
    }

    private void handleRaycastFilterBubbleManipulation(FilterBubbleButton newCollidedFilterBubble)
    {
        if (newCollidedFilterBubble != null) // if something was hit
        {
            //print("dot just hit a filter bubble");
            if (currentCollidedFilterBubble != null && newCollidedFilterBubble.GetInstanceID() != currentCollidedFilterBubble.GetInstanceID())
                currentCollidedFilterBubble.handlePointerCollisionExit(transform);

            if (currentCollidedFilterBubble != null && newCollidedFilterBubble.GetInstanceID() == currentCollidedFilterBubble.GetInstanceID())
            {
                // if you hit the same object just don't do anything!
                return;
            }

            //print("dot has collided with " + hit.collider.name);
            newCollidedFilterBubble.handlePointerCollisionEnter(transform);
            currentCollidedFilterBubble = newCollidedFilterBubble;
        }

        if (newCollidedFilterBubble == null)
        {
            if (currentCollidedFilterBubble != null)
            {
                currentCollidedFilterBubble.handlePointerCollisionExit(transform);
                //print("dot has exited from " + currentCollidedFilterBubble.name);
            }

            currentCollidedFilterBubble = null;
        }
    }

    private RaycastHit CreateRaycast()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);
        Physics.Raycast(ray, out hit, defaultLength);

        return hit;
    }
}
