using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class AxisGhost : MonoBehaviour, Grabbable
{
    public TextMeshPro label;
    public Axis parentAxis = null;
    public WandController grabbingController = null;
    public bool isCloned = false;

    private Vector3 parentOrigininalPosition = Vector3.zero;
    public int GetPriority()
    {
        return 1; // the priority should be higher than an Axis
    }

    public void OnDrag(WandController controller)
    {
        //print("From ghost I'm being dragged! ");
    }

    public void OnEnter(WandController controller)
    {
        print("From ghost Im entered");
    }

    public void OnExit(WandController controller)
    {
        print("From ghost I'm exited! ");
    }

    public bool OnGrab(WandController controller)
    {
             
        print("From ghost I'm grabbed! ");
        grabbingController = controller;
        transform.parent = controller.transform;
        return true;
    }

    public void OnRelease(WandController controller)
    {
        print("From ghost I'm released!");
        transform.parent = null;
        grabbingController = null;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, Axis.AXIS_ROD_WIDTH);



        // make a clone of the parent axis in here now
        //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //cube.transform.position = transform.position;
        //cube.transform.localScale = transform.localScale;
        //cube.transform.rotation = transform.rotation;
        //cube.GetComponent<Renderer>().material.color = Color.red;
        makeNewAxis();

        // after making a clone, destroy the whole ghost
        Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        print("hey! booooo I'm a ghost!");
        parentOrigininalPosition = parentAxis.transform.position;
    }

    private void OnEnable()
    {
        print("ghost is enabled now");
        transform.parent = null;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, Axis.AXIS_ROD_WIDTH / 2f);
        GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.3f);
        label.text = parentAxis.label.text;
    }

    // Update is called once per frame
    void Update()
    {
        // check if it was released before getting to the treshold
        // the reason I'm doing this is that WandController doesn't automatically 
        // call the OnRelease for objects that it hasn't entered 
        // (and the reason for that is how we create the ghost using Instantiate)
        if(grabbingController != null && !grabbingController.gripping) {
            // return the ghost to its origin and then destroy it 
            returnToOrigin();
        }


        if(!isCloned && Vector3.Distance(transform.position, parentOrigininalPosition) > TwoDimensionalPanelScript.COLLISION_DISTANCE_BOUNDARY)
        {
            Debug.Assert(grabbingController != null, "Grabbing controller cannot be null! ");
            isCloned = true;
            OnRelease(grabbingController);
        }
    }

    private void returnToOrigin()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DORotate(parentAxis.transform.eulerAngles, 0.7f, RotateMode.Fast).SetEase(Ease.OutSine));
        seq.Join(transform.DOMove(parentOrigininalPosition, 0.7f).SetEase(Ease.OutElastic));
        seq.Join(GetComponent<Renderer>().material.DOFade(0, 0.7f).SetEase(Ease.OutSine));
        seq.AppendCallback(() => Destroy(gameObject));
    }

    private void makeNewAxis()
    {
        GameObject clone = parentAxis.Clone(transform.position, transform.rotation);
        clone.transform.DOScaleZ(Axis.AXIS_ROD_WIDTH/2, 0.4f).SetEase(Ease.OutElastic);
        clone.GetComponent<Axis>().cloningWidgetGameObject.SetActive(true);
        SceneManager.Instance.AddAxis(clone.GetComponent<Axis>());
    }

    public void changeColor()
    {
        //GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 0.5f);
    }

    private void OnDestroy()
    {
        grabbingController = null;
        parentAxis = null;
        isCloned = true;
    }
}
