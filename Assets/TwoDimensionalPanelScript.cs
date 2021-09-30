using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TwoDimensionalPanelScript : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;

    public HashSet<Axis> ConnectedAxes = new HashSet<Axis>();
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;

        transform.parent.rotation = Camera.main.transform.rotation;
        transform.parent.Rotate(0, -90f, 0, Space.Self);
        transform.parent.position = Camera.main.transform.position + (Camera.main.transform.right * 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var axis in ConnectedAxes)
        {   
            var AxisRigidBody = axis.GetComponent<Rigidbody>();
            if(Vector3.Project(axis.transform.position - transform.position, transform.forward).magnitude > 0.25f) {
                ConnectedAxes.Remove(axis);
            } else {
                if(AxisRigidBody.constraints == RigidbodyConstraints.FreezeAll){
                    AxisRigidBody.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ;
                }
            }
        }

        // TODO: implement a check that removes the axis that its Z (in planes coords) distance with our plane is more than 0.25f 
    }

    void OnTriggerEnter(Collider other) {
        if(other.GetComponent<Axis>()) {
            Axis a = other.GetComponent<Axis>();

            // Find the point of entrance
            Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((a.transform.position - transform.position), transform.forward);
            
            Quaternion aBeforeRotation = a.transform.rotation;
            aBeforeRotation.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, aBeforeRotation.eulerAngles.z);
            // Rotation and position change stuff
            Sequence seq = DOTween.Sequence();
            // a.transform.
            seq.Append(a.transform.DORotateQuaternion(aBeforeRotation, 0.7f).SetEase(Ease.OutElastic));

            seq.Join(a.transform.DOMove(transform.position + projectedDistanceOnPlane + (transform.forward * 0.05f), 0.7f).SetEase(Ease.OutElastic));

            seq.Join(a.transform.DOScale(new Vector3(a.transform.localScale.x, a.transform.localScale.y, 0.00001f), 0.7f).SetEase(Ease.OutElastic));

            seq.AppendCallback(() => {
                a.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                a.transform.SetParent(transform);
                a.isOn2DPanel = true;
            });

            ConnectedAxes.Add(a);
        }
    }



}
