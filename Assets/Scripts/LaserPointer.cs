using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LaserPointer : MonoBehaviour
{
    public float m_defaultLength = 5.0f;
    public GameObject m_dot;
    public VRInputModule m_vrInputModule;

    private LineRenderer m_lineRenderer = null;


    private void Awake()
    {
        m_lineRenderer = GetComponent<LineRenderer>(); 
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLine();  
    }

    private void UpdateLine()
    {

        // use the normal distance from the inoput moduel
        PointerEventData data = m_vrInputModule.getData();
        float targetLength = data.pointerCurrentRaycast.distance == 0 ? m_defaultLength : data.pointerCurrentRaycast.distance;


        // Raycast
        RaycastHit hit = CreateRaycast(targetLength);

        //Default in here
        Vector3 endPosition = transform.position + (transform.forward * targetLength);
        // or check on the collider
        if (hit.collider != null)
            endPosition = hit.point;


        // set the positiion of the dor
        m_dot.transform.position = endPosition;

        //set the position of the line rendere
        m_lineRenderer.SetPosition(0, transform.position);
        m_lineRenderer.SetPosition(1, endPosition);
    }

    private RaycastHit CreateRaycast(float length)
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);
        Physics.Raycast(ray, out hit, m_defaultLength);

        return hit;
    }
}
