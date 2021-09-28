using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class FollowMenuScript : MonoBehaviour, Grabbable
{
    public GameObject camera;
    public GameObject DataShelfPanel;
    public bool FollowCamera = true;

    [SerializeField]
    UnityEvent OnEntered;

    [SerializeField]
    UnityEvent OnExited;

    public Vector3 initialScale;
    Vector3 rescaled = Vector3.one;

    // Use this for initialization
    void Start () {
        //initialScale = transform.localScale;
        rescaled = initialScale;
        rescaled.x *= 2f;
        rescaled.y *= 2f;
        rescaled.z *= 2f;
    }

    public int GetPriority()
    {
        return 100; // I should work with this to have the highest priority
    }

    public void OnDrag(WandController controller)
    {
        // I don't think we should do anything about the dragging of this thing
    }

    public void OnEnter(WandController controller)
    {
        OnEntered.Invoke();
        foreach (Transform child in DataShelfPanel.transform)
        {
            if(child.GetComponent<Axis>()) {
                Axis a = child.GetComponent<Axis>(); 
                a.isPrototype = false;

                // This is to make sure that the view parts of the visualizations also move with the axes to the data shelf panel
                foreach (var visu in a.correspondingVisualizations())
                {
                    visu.transform.SetParent(DataShelfPanel.transform);
                }
            }
        }
    }

    public void OnExit(WandController controller)
    {
        OnExited.Invoke();
        foreach (Transform child in transform)
        {
            if(child.GetComponent<Axis>()) {
                child.GetComponent<Axis>().isPrototype = true;
            }
        }
    }

    public bool OnGrab(WandController controller)
    {
        // We should simply act as the ontouch of a button and show the childs of it which are the data shelf items
        // it needs to be a toggle situation - clicked: on/off

        // if this doesn't return a true value, the OnRelease won't work
        return true; // it's not supposed to be draggable, so return false for this
    }

    public void OnRelease(WandController controller)
    {
       
        if(DataShelfPanel.activeSelf) {
            Sequence seq = DOTween.Sequence();
            seq.Append(DataShelfPanel.transform.DORotate(transform.rotation.eulerAngles, 1f, RotateMode.Fast).SetEase(Ease.OutSine));
            seq.Join(DataShelfPanel.transform.DOMove(transform.position, 1f).SetEase(Ease.OutElastic));
            // seq.AppendCallback(() => DataShelfPanel.SetActive(false));
            DataShelfPanel.SetActive(false);
        } else {
            DataShelfPanel.SetActive(true);
            Sequence seq = DOTween.Sequence();

            seq.Append(DataShelfPanel.transform.DORotate(Camera.main.transform.rotation.eulerAngles, 1f, RotateMode.Fast).SetEase(Ease.OutSine));

            seq.Join(DataShelfPanel.transform.DOMove(Camera.main.transform.position + (Camera.main.transform.forward * 2f), 0.7f).SetEase(Ease.OutElastic));

            seq.AppendCallback(() => {
                DataShelfPanel.SetActive(true);
                foreach (Transform child in transform)
                {
                    if(child.GetComponent<Axis>()) {
                        child.GetComponent<Axis>().isPrototype = true;
                    }
                }
            });
        }
    }

     public void ProximityEnter()
    {
        transform.DOKill(true);
        // transform.DOLocalMoveX(-axisOffset, 0.35f).SetEase(Ease.OutBack);
        transform.DOScale(rescaled, 0.35f).SetEase(Ease.OutBack);
    }

    public void ProximityExit()
    {
        transform.DOKill(true);
        // transform.DOLocalMoveX(0, 0.25f);
        transform.DOScale(initialScale, 0.25f);
    }

    // Update is called once per frame
    void Update()
    {
        // tHIS IS JUST A test I guess
        if (FollowCamera)
        { // Code for the menu to follow the camera.	
            Vector3 v = camera.transform.position - transform.position;
            v.z = 0.0f;
            v.x = 0.25f;
            transform.LookAt(camera.transform.position - v);
            transform.Rotate(0, 180, 0);
        }
    }
}
