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
            // Debug.Log("hit the panel");
            // Debug.Log(a.transform.name);
            // Rotation and position change stuff

            Sequence seq = DOTween.Sequence();
            seq.Append(a.transform.DORotateQuaternion(transform.rotation, 0.7f).SetEase(Ease.OutElastic));

            seq.Join(a.transform.DOMove(transform.position + (transform.forward * 0.05f), 0.7f).SetEase(Ease.OutElastic));

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
