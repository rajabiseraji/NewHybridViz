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
            if(AxisRigidBody.constraints == RigidbodyConstraints.FreezeAll){
                AxisRigidBody.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionZ;
            }
        }
    }

    void OnTriggerEnter(Collider other) {
        if(other.GetComponent<Axis>()) {
            Axis a = other.GetComponent<Axis>();
            // Debug.Log("hit the panel");
            // Debug.Log(a.transform.name);
            // Rotation and position change stuff
            // TODO: replace with Tweening
            a.transform.rotation = Quaternion.FromToRotation(Vector3.right, transform.right);
            a.transform.rotation = Quaternion.FromToRotation(Vector3.up, transform.up);
            a.transform.rotation = Quaternion.FromToRotation(Vector3.forward, transform.forward);
            a.transform.position = transform.position + (transform.forward * 0.05f);
            a.transform.localScale = new Vector3(a.transform.localScale.x, a.transform.localScale.y, 0.00001f);

            a.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            ConnectedAxes.Add(a);
        }
    }



}
