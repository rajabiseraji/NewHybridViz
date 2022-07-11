using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TwoDimensionalPanelScript : MonoBehaviour, Grabbable
{
    public static float COLLISION_DISTANCE_BOUNDARY = 0.35f;

    Mesh mesh;
    Vector3[] vertices;

    public List<Axis> ConnectedAxes = new List<Axis>();

    private Vector3 MyPrevPosition;

    public bool AmIMoving = false;
    public bool isControllerAttachedMenu = false;
    void Start()
    {
        MyPrevPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // here check to see if there are any new axes added to the children and then add then to ConnectedAxes in case they're not added on their own 
        


        for (int i = 0; i < ConnectedAxes.Count; i++)
        {
            var axis = ConnectedAxes[i];
            var AxisRigidBody = axis.GetComponent<Rigidbody>();
            float distanceAlongPlaneNormal = Vector3.Project(axis.transform.position - transform.position, transform.forward).magnitude;
            if (distanceAlongPlaneNormal > COLLISION_DISTANCE_BOUNDARY) {
                print("removing axis " + axis.name);


                // TODO: here's the part that we need to tell the axis to change form into a 3D object again
                //something like axis.Getbackto3DShape()
                //axis.MoveOutOf2DBoard();

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

    void OnTriggerEnter(Collider other) {
            if(other.GetComponent<Axis>()) {
                Axis a = other.GetComponent<Axis>();
                if(ConnectedAxes.Contains(a))
                {
                    print("axis is already on the panel " + a.name);
                    return;
                }

                // Find the point of entrance
                Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((a.transform.position - transform.position), transform.forward);
                
                // Handling rotation mappings
                Quaternion aBeforeRotation = a.transform.rotation;
                bool isParallelWithPanel = Vector3.Dot(a.transform.forward, transform.forward) > 0;

                float rotationAroundXAngleOffset = Vector3.Dot(a.transform.up, transform.up) > 0 ? 0 : (isParallelWithPanel ? 0 : 180f);
                float rotationAroundYAngleOffset = Vector3.Dot(a.transform.right, transform.right) > 0 ? 0 : (isParallelWithPanel ? 0 : 180f);
            
                aBeforeRotation.eulerAngles = new Vector3(rotationAroundXAngleOffset + transform.eulerAngles.x, rotationAroundYAngleOffset + transform.eulerAngles.y, aBeforeRotation.eulerAngles.z);

                // just ask the axis class to handle it from now on
                a.MoveTo2DBoard(transform, projectedDistanceOnPlane, aBeforeRotation, new Vector3(a.transform.localScale.x, a.transform.localScale.y, 0.00001f));
                
                ConnectedAxes.Add(a);

            }
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
}
