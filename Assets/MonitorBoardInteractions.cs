using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MonitorBoardInteractions : MonoBehaviour, Grabbable
{

    public Visualization collidedAxis;
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
        if (other.GetComponent<Visualization>())
        {
            Visualization a = other.GetComponent<Visualization>();
            List<Axis> axisList = a.axes;
            if (!collidedAxis)
            {
                collidedAxis = a;
            } else if (collidedAxis.GetInstanceID() == a.GetInstanceID())
            {
                Debug.Log("they are equal");
                Debug.Log("collided Axis instance ID is " + collidedAxis.GetInstanceID());
                Debug.Log("axis instance ID is " + a.GetInstanceID());
                return;
            }

            Debug.Log("they are NOOOOOOT equal");
            // Find the point of entrance
            //Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((collision.contacts[0].point - transform.position), transform.forward);
            Vector3 projectedDistanceOnPlane = Vector3.ProjectOnPlane((a.transform.position - transform.position), transform.forward);

            Vector3 dirForRaycast = (transform.position + projectedDistanceOnPlane) - a.transform.position;
            var result = GetComponent<uDesktopDuplication.Texture>().RayCast(a.transform.position, dirForRaycast);

            Sequence seq = DOTween.Sequence();
            seq.Append(other.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
            // test
            foreach (var axis in axisList)
            {
                seq.Join(axis.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
            }
            seq.AppendCallback(() => {

                // The visualization destroyer thingy will take care of the axes too
               
                int axesNewCount = axisList.Count;
                for (int i = 0; i < axesNewCount; i++)
                {
                    SceneManager.Instance.sceneAxes.Remove(axisList[i]);
                    axisList.Remove(axisList[i]);
                    DestroyImmediate(axisList[i].gameObject);
                }
                axisList.Clear();
                other.gameObject.SetActive(false);
                DestroyImmediate(other.gameObject);

                //foreach (var axis in axisList)
                //{
                //    axis.gameObject.SetActive(false);
                //    SceneManager.Instance.sceneAxes.Remove(axis);
                //    Destroy(axis.gameObject);
                //}
                //other.gameObject.SetActive(false);
                //Destroy(other.gameObject);
            });
            // Rotation and position change stuff

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
