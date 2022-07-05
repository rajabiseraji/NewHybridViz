using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TwoDimensionalPanelScript : MonoBehaviour, Grabbable
{
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
            if(Vector3.Project(axis.transform.position - transform.position, transform.forward).magnitude > 0.25f) {
                ConnectedAxes.RemoveAt(i);
            } else {
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
        // if(!DOTween.IsTweening(this.transform)) {
            if(other.GetComponent<Axis>()) {
                Axis a = other.GetComponent<Axis>();

                // Find the point of entrance
                Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((a.transform.position - transform.position), transform.forward);
                
                // Handling rotation mappings
                Quaternion aBeforeRotation = a.transform.rotation;
                bool isParallelWithPanel = Vector3.Dot(a.transform.forward, transform.forward) > 0;

                float rotationAroundXAngleOffset = Vector3.Dot(a.transform.up, transform.up) > 0 ? 0 : (isParallelWithPanel ? 0 : 180f);
                float rotationAroundYAngleOffset = Vector3.Dot(a.transform.right, transform.right) > 0 ? 0 : (isParallelWithPanel ? 0 : 180f);
            
                aBeforeRotation.eulerAngles = new Vector3(rotationAroundXAngleOffset + transform.eulerAngles.x, rotationAroundYAngleOffset + transform.eulerAngles.y, aBeforeRotation.eulerAngles.z);
                // We want the axis to be released from the controller before it begins the sequence
                foreach (var obj in GameObject.FindObjectsOfType<WandController>())
                {
                    // It means shaking the controller not the visualization itself
                    if (obj.IsDragging(a)) {
                        a.OnRelease(obj);
                    }
                }

                // Rotation and position change stuff
                Sequence seq = DOTween.Sequence();
                // a.transform.
                seq.Append(a.transform.DORotate(aBeforeRotation.eulerAngles, 0.1f).SetEase(Ease.OutElastic));

                seq.Append(a.transform.DOMove(transform.position + projectedDistanceOnPlane + (transform.forward * 0.05f), 0.3f).SetEase(Ease.OutElastic));

                seq.Join(a.transform.DOScale(new Vector3(a.transform.localScale.x, a.transform.localScale.y, 0.00001f), 0.3f).SetEase(Ease.OutElastic));

                seq.AppendCallback(() => {
                    a.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    // a 2D Panel will always be inside a dataShelf then! Cube -> 2DPanel -> DataShelf
                    // TODO: fix this later in a way that the item is moved with the panel,
                    // right now it won't be moved with the parent
                    // a.transform.SetParent(transform.parent.parent);
                    a.isOn2DPanel = true;
                });

                ConnectedAxes.Add(a);
            }
        // }
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
