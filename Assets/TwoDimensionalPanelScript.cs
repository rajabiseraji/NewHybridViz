using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TwoDimensionalPanelScript : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;

    public List<Axis> ConnectedAxes = new List<Axis>();
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

        // TODO: implement a check that removes the axis that its Z (in planes coords) distance with our plane is more than 0.25f 
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
                // Rotation and position change stuff
                Sequence seq = DOTween.Sequence();
                // a.transform.
                seq.Append(a.transform.DORotate(aBeforeRotation.eulerAngles, 0.1f).SetEase(Ease.OutElastic));

                seq.Append(a.transform.DOMove(transform.position + projectedDistanceOnPlane + (transform.forward * 0.05f), 0.3f).SetEase(Ease.OutElastic));

                seq.Join(a.transform.DOScale(new Vector3(a.transform.localScale.x, a.transform.localScale.y, 0.00001f), 0.3f).SetEase(Ease.OutElastic));

                seq.AppendCallback(() => {
                    a.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    a.transform.SetParent(transform);
                    a.isOn2DPanel = true;
                });

                ConnectedAxes.Add(a);
            }
        // }
    }



}
