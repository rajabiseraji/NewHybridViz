using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class FollowMenuScript : MonoBehaviour, Grabbable
{
    public GameObject VRCamera;
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
                // a.isPrototype = false;

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
            // Making sure they're not going to be copied in the move
            foreach (Transform child in DataShelfPanel.transform)
            {
                if(child.GetComponent<Axis>()) {
                    child.GetComponent<Axis>().isPrototype = false;
                }
            }

            // Setting up the sequences
            Sequence seq = DOTween.Sequence();
            seq.Append(DataShelfPanel.transform.DORotate(Camera.main.transform.rotation.eulerAngles, 0.5f, RotateMode.Fast).SetEase(Ease.OutSine));
            seq.Join(DataShelfPanel.transform.DOMove(Camera.main.transform.position + (Camera.main.transform.forward * -0.6f) + (Camera.main.transform.up * 4f) , 0.5f).SetEase(Ease.InOutElastic));
            // seq.Join(DataShelfPanel.transform.DOLookAt(transform.position, 0.4f).SetEase(Ease.OutSine));
            // Making sure the dataPanel deactivates 
            seq.AppendCallback(() => DataShelfPanel.SetActive(false));
        } else {
            // Making sure the data panel activates 
            DataShelfPanel.SetActive(true);

            // Sequence and animation stuff
            Sequence seq = DOTween.Sequence();
            seq.Append(DataShelfPanel.transform.DORotate(Camera.main.transform.rotation.eulerAngles, 0.5f, RotateMode.Fast).SetEase(Ease.OutSine));
            seq.Join(DataShelfPanel.transform.DOMove(Camera.main.transform.position + (Camera.main.transform.forward * 0.5f) + (Camera.main.transform.up * -0.8f), 0.7f).SetEase(Ease.OutElastic));

            // Activate the datashelf panel and make sure the axes are set back to the proto mode (clonable mode)
            seq.AppendCallback(() => {
                DataShelfPanel.SetActive(true);
                foreach (Transform child in DataShelfPanel.transform)
                {
                    if(child.GetComponent<Axis>()) {
                        Axis childAxis = child.GetComponent<Axis>();
                        childAxis.InitOrigin(childAxis.transform.position, childAxis.transform.rotation);
                        childAxis.isPrototype = true;
                    }
                }
            });
        }
    }

     public void ProximityEnter()
    {
        transform.DOKill(true);
        transform.DOScale(rescaled, 0.35f).SetEase(Ease.OutBack);
    }

    public void ProximityExit()
    {
        transform.DOKill(true);
        transform.DOScale(initialScale, 0.25f);
    }

    // Update is called once per frame
    void Update()
    {
        // tHIS IS JUST A test I guess
        if (FollowCamera)
        { // Code for the menu to follow the camera.	
            Vector3 v = VRCamera.transform.position - transform.position;
            v.z = 0.0f;
            v.x = 0.0f;
            transform.LookAt(VRCamera.transform.position - v);
            transform.Rotate(0, 180, 0);
        }
    }
}
