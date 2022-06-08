using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MonitorBoardInteractions : MonoBehaviour, Grabbable
{
    public int GetPriority()
    {
        return 200;
    }

    public void OnDrag(WandController controller)
    {

    }

    public void OnEnter(WandController controller)
    {
        Debug.Log("Hey i have enetered the monitor!");
    }

    public void OnExit(WandController controller)
    {

        //throw new System.NotImplementedException();
    }

    public bool OnGrab(WandController controller)
    {
        Debug.Log("Hey i have grabbed the thing now the monitor!");
        return false;
        //throw new System.NotImplementedException();
    }

    public void OnRelease(WandController controller)
    {
        //throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {

        // in order to user the uddTexture.Raycast function
        // we need a to (Transform) and a from (Transform) to make a raycast
        
        // TODO: make this later
        // for now I'm just gonna detect a collision and that's it! 



        // if(!DOTween.IsTweening(this.transform)) {
        if (other.GetComponent<Axis>())
        {
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
                if (obj.IsDragging(a))
                {
                    a.OnRelease(obj);
                }
            }

            Vector3 dirForRaycast = projectedDistanceOnPlane + (transform.forward * 0.05f);

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
                //a.isOn2DPanel = true;
            });

            var result = GetComponent<uDesktopDuplication.Texture>().RayCast(a.transform.position, dirForRaycast);
            if (result.hit)
            {
                print("I've hit somethig");
                print(result.desktopCoord.x);
                print(result.desktopCoord.y);
                WebSocketMsg msg = new WebSocketMsg(1, result.desktopCoord, "some text");
                GameObject.FindGameObjectWithTag("WebSocketManager").GetComponent<WsClient>().SendMsgToDesktop(msg);
            }

        }
        // }
    }


}
