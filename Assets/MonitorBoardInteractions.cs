using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MonitorBoardInteractions : MonoBehaviour, Grabbable
{

    public Visualization localCollidedVisualization;
    public int GetPriority()
    {
        // We want a low priority so that the controller doesn't grab this
        return 1;
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
        if (other.GetComponent<Visualization>())
        {
            Visualization collidedVisualization = other.GetComponent<Visualization>();
            List<Axis> axisList = collidedVisualization.axes;
            if (!this.localCollidedVisualization)
            {
                this.localCollidedVisualization = collidedVisualization;
            } else if (this.localCollidedVisualization.GetInstanceID() == collidedVisualization.GetInstanceID())
            {
                //Debug.Log("they are equal");
                //Debug.Log("collided Axis instance ID is " + collidedAxis.GetInstanceID());
                //Debug.Log("axis instance ID is " + a.GetInstanceID());
                return;
            }

            //Debug.Log("they are NOOOOOOT equal");
            // Find the point of entrance
            Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((collidedVisualization.transform.position - transform.position), transform.forward);

            Vector3 dirForRaycast = (transform.position + projectedDistanceOnPlane) - collidedVisualization.transform.position;
            var result = GetComponent<uDesktopDuplication.Texture>().RayCast(collidedVisualization.transform.position, dirForRaycast);

            Sequence seq = DOTween.Sequence();
            seq.Append(other.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
            // test
            foreach (var axis in axisList)
            {
                seq.Join(axis.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
            }
            seq.AppendCallback(() => {

                // The visualization destroyer thingy will take care of the axes too
                other.GetComponent<Visualization>().DestroyVisualization();
            });

            if (result.hit)
            {
                print("I've hit somethig");
                print(result.desktopCoord.x);
                print(result.desktopCoord.y);
                WebSocketMsg msg;
                if (collidedVisualization.axes.Count == 1)
                {
                    Axis XAxis = collidedVisualization.axes[0].IsHorizontal ? collidedVisualization.axes[0] : null;
                    Axis YAxis = !collidedVisualization.axes[0].IsHorizontal ? collidedVisualization.axes[0] : null;
                    msg = new WebSocketMsg(1, 
                        result.desktopCoord, 
                        collidedVisualization.axes.Count,
                        XAxis,
                        YAxis,
                        null,
                        collidedVisualization.name);
                } else
                {
                    msg = new WebSocketMsg(1, 
                        result.desktopCoord, 
                        collidedVisualization.axes.Count,
                        collidedVisualization.ReferenceAxis1.horizontal,
                        collidedVisualization.ReferenceAxis1.vertical,
                        collidedVisualization.ReferenceAxis1.depth,
                        collidedVisualization.name);
                }
                GameObject.FindGameObjectWithTag("WebSocketManager").GetComponent<WsClient>().SendMsgToDesktop(msg);
            }

        }
        // }
    }


}
