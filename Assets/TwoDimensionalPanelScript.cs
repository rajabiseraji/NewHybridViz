using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TwoDimensionalPanelScript : MonoBehaviour, Grabbable
{
    public static float COLLISION_DISTANCE_BOUNDARY = 0.35f;

    public GameObject dropHint;
    public GameObject panelHighlight;

    Mesh mesh;
    Vector3[] vertices;

    public List<Axis> ConnectedAxes = new List<Axis>();

    private Vector3 MyPrevPosition;

    public bool AmIMoving = false;
    public bool isControllerAttachedMenu = false;

    private bool hasCollidedWithAxis = false;
    private Axis collidedAxis = null;

    void Start()
    {
        Debug.Assert(dropHint != null, "Drop hint game object shouldn't be null!");
        Debug.Assert(panelHighlight != null, "Drop hint game object shouldn't be null!");

        MyPrevPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
        if(hasCollidedWithAxis && collidedAxis != null)
        {
            // if the controller that was grabbing the axis has let go while the axis is inside
            // the 2D plane, then add the axis to the plane
            if(collidedAxis.grabbingController == null || (collidedAxis.grabbingController != null && !collidedAxis.grabbingController.gripping))
            {
                addCollidedAxisToBoard();
                // after adding, just reset everything
                // this one waits for a frame so that all of the effects to take place and then resets everything
                //StartCoroutine(resetCollidersCoroutine());
                hasCollidedWithAxis = false;
                collidedAxis = null;
                // hide the dropping hint after the axis got added to the board
                hideDropHint();
            }
        }


        for (int i = 0; i < ConnectedAxes.Count; i++)
        {
            var axis = ConnectedAxes[i];
            var AxisRigidBody = axis.GetComponent<Rigidbody>();
            float distanceAlongPlaneNormal = Vector3.Project(axis.transform.position - transform.position, transform.forward).magnitude;
            if (distanceAlongPlaneNormal > COLLISION_DISTANCE_BOUNDARY) {
                print("removing axis " + axis.name);
                ConnectedAxes.RemoveAt(i);

            } else {
                // For all the existing axes, just freeze their movement to two degrees
                if(AxisRigidBody.constraints == RigidbodyConstraints.FreezeAll){
                    AxisRigidBody.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ;
                }
            }
        }

        // Check if the parent dataShelf is moving
        if(MyPrevPosition.Equals(transform.position)) {
            AmIMoving = false;
        } else 
            AmIMoving = true;
        // TODO: implement a check that removes the axis that its Z (in planes coords) distance with our plane is more than 0.25f 

        MyPrevPosition = transform.position;
    }

    //private IEnumerator resetCollidersCoroutine()
    //{
    //    yield return new WaitForSeconds(0.5f);

    //    hasCollidedWithAxis = false;
    //    collidedAxis = null;
    //    // hide the dropping hint after the axis got added to the board
    //    hideDropHint();
    //}

    void OnTriggerEnter(Collider other) {
        print("on trigger enter called with " + other.name + " at " + Time.realtimeSinceStartup);
        Axis a = other.GetComponent<Axis>();
        if (a != null) {
            //List<Axis> visAxes = other.GetComponent<Visualization>().axes;
            if(ConnectedAxes.Contains(a))
            {
                print("axis is already on the panel " + a.name);
                return;
            }

            // set the collided panel
            if(!hasCollidedWithAxis)
            {
                hasCollidedWithAxis = true;
                collidedAxis = a;
                showDropHint();

                // this is to handle axis drops to 2D panel that are run from a script 
                // and not the controller
                if(a.grabbingController == null)
                {
                    addCollidedAxisToBoard(a);
                    hasCollidedWithAxis = false;
                    collidedAxis = null;
                    hideDropHint();
                }
            }

        }
    }

    private void OnTriggerExit(Collider other)
    {
        Axis a = other.GetComponent<Axis>();
        if(a != null && hasCollidedWithAxis && collidedAxis.GetInstanceID() == a.GetInstanceID())
        {
            // means that the same axis is leaving the plane 
            collidedAxis = null;
            hasCollidedWithAxis = false;
            hideDropHint();
        }
    }

    private void addCollidedAxisToBoard(Axis a = null)
    {
        Axis collidedAxis = a != null ? a : this.collidedAxis;

        if(collidedAxis == null)
        {
            print("Colided axis shouldn't be null");
            return;
        }

        // Find the point of entrance
        Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((collidedAxis.transform.position - transform.position), transform.forward);

        // Handling rotation mappings
        Quaternion aBeforeRotation = collidedAxis.transform.rotation;
        bool isParallelWithPanel = Vector3.Dot(collidedAxis.transform.forward, transform.forward) > 0;

        float rotationAroundXAngleOffset = Vector3.Dot(collidedAxis.transform.up, transform.up) > 0 ? 0 : (isParallelWithPanel ? 0 : 180f);
        float rotationAroundYAngleOffset = Vector3.Dot(collidedAxis.transform.right, transform.right) > 0 ? 0 : (isParallelWithPanel ? 0 : 180f);

        aBeforeRotation.eulerAngles = new Vector3(rotationAroundXAngleOffset + transform.eulerAngles.x, rotationAroundYAngleOffset + transform.eulerAngles.y, aBeforeRotation.eulerAngles.z);

        print("adding " + collidedAxis.name + " to board");
        // just ask the axis class to handle it from now on
        collidedAxis.MoveTo2DBoard(transform, projectedDistanceOnPlane, aBeforeRotation, new Vector3(collidedAxis.transform.localScale.x, collidedAxis.transform.localScale.y, 0.00001f));

        ConnectedAxes.Add(collidedAxis);

    } 

    public void clearConnectedAxisList()
    {
        ConnectedAxes.Clear();
    }

    private void toggleChildAxesClonig(bool onOff) {
        foreach (var axis in GameObject.FindGameObjectWithTag("DataShelfPanel").GetComponentsInChildren<Axis>())
        {
            axis.isPrototype = onOff;
        }
    }

    public int GetPriority()
    {
        // lower priority than almost everything in the world
        return 1;
    }

    public bool OnGrab(WandController controller)
    {
        if(!transform.parent.parent) {
            transform.parent.parent = controller.transform;
            return false;
        } //if it's not a datashelf and just a simple 2D Panel

        toggleChildAxesClonig(false);
        transform.parent.parent.parent = controller.transform; // Cube -> 2DPanel -> DataShelfPanel
        return true;
    }

    public void OnRelease(WandController controller)
    {
        if(transform.parent.parent == controller.transform) {
            transform.parent.parent = null;
            return;
        } //in case it's not a dataShelf

        transform.parent.parent.parent = null;
        toggleChildAxesClonig(false);
        StartCoroutine("DoToggleBackChildAxis");
    }

    IEnumerator DoToggleBackChildAxis() {
        yield return new WaitUntil(() => AmIMoving == false);
        toggleChildAxesClonig(true);
    }

    public void OnDrag(WandController controller)
    {
    }

    public void OnEnter(WandController controller)
    {
    }

    public void OnExit(WandController controller)
    {
    }

    private void showDropHint()
    {
        dropHint.SetActive(true);
        panelHighlight.SetActive(true);
    }

    private void hideDropHint()
    {
        dropHint.SetActive(false);
        panelHighlight.SetActive(false);
    }
}
