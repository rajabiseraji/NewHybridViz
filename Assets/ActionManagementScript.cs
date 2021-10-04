using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ActionManagementScript : MonoBehaviour
{
    private static readonly int MAX_HISTORY_SIZE = 5;

    public struct RestorableAction
    {
        public Axis sourceAxis;
        public Vector3 OriginPositin;
        public Vector3 TargetPosition;
        public Quaternion OriginRotation;
        public Quaternion TargetRotation;

        // This is for only when we have a visualization movement
        public List<Axis> involvedAxes;
        public enum ActionType
        {
            MOVE,
            MOVE_VISUALIZATION,
            CLONE,
            DESTORY,
            FILTER,
            ATTRIBUTE_CHANGE
        }

        public ActionType type;

        public RestorableAction(ActionType type, Axis sourceAxis, Vector3 OriginPositin, Vector3 TargetPosition, Quaternion OriginRotation, Quaternion TargetRotation) {
            this.type = type;
            this.sourceAxis = sourceAxis;
            this.OriginPositin = OriginPositin;
            this.TargetPosition = TargetPosition;
            this.OriginRotation = OriginRotation;
            this.TargetRotation = TargetRotation;
            this.involvedAxes = new List<Axis>();
        }

        public void clear() {
            sourceAxis = null;
            OriginPositin = Vector3.zero;
            TargetPosition = Vector3.zero;
            OriginRotation = Quaternion.identity;
            TargetRotation = Quaternion.identity;
            type = ActionType.MOVE; // The default type is always movement 
        }

        public override string ToString() {
            return System.String.Format("sourceAxis: {0} type: {1} OriginPositin: {2} TargetPosition: {3} OriginRotation: {4} TargetRotation: {5}", sourceAxis, type, OriginPositin, TargetPosition, OriginRotation, TargetRotation);
        }

        
    }
    /* 
    1- Define an action class that has the attributes of an action:
        We should start with an action queue that is like 5 members long (or more??)
        Two approaches: 
            1- We can manage everything from this script for all of the axes
            2- Or we can put a state stack in each axis and request a callback to that (how about that??)

        Approach 1:
        Things we need:
            1- Action Queue (a class)

    Possible actions in the scene: 
        1- Cloning of axes (from proto and from the cloning knob)
        2- Moving the axes 
        3- Moving the visualizations (it could be the same thing as moving the axes cuz it propagates)
        4- Filtering (aka moving the knobs on the axis normalizer and axis max and min knobs)
        5- Throwing out (aka throwing to the floor and destroying the axis)
        6- Attribute Change
    
    
     */
    private Stack<RestorableAction> actionStack = new Stack<RestorableAction>();
    private Stack<RestorableAction> redoStack = new Stack<RestorableAction>();

    // This the one we use to hold the transform of the start of the movement 
    private Vector3 tempPosition;
    private Quaternion tempRotation;

    void Start()
    {
        EventManager.StartListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisGrabbed);
        EventManager.StartListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisReleased);
        EventManager.StartListeningToAxisEvent(ApplicationConfiguration.OnVisualizationGrabbed, registerVisualizationGrabbed);
        EventManager.StartListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerVisualizationReleased);
        EventManager.StartListeningToAxisEvent(ApplicationConfiguration.OnAxisCloned, registerCloningAction);
    }

    private void registerCloningAction(Axis sourceAxis) {
        // Debug.Log(sourceAxis.name + " CLONED! " +  sourceAxis.transform.position);
        RestorableAction newAction = new RestorableAction(RestorableAction.ActionType.CLONE, sourceAxis, sourceAxis.transform.position, sourceAxis.transform.position, sourceAxis.transform.rotation, sourceAxis.transform.rotation);

        // First check if the action stack is not at max capacity
        EnsureStackCapacity(actionStack);

        // After clearing out the max cap, push the new action in there
        // Debug.Log("New action is : " + newAction);
        actionStack.Push(newAction);
    }
    private void registerAxisGrabbed(Axis sourceAxis) {
        tempPosition = sourceAxis.transform.position;
        tempRotation = sourceAxis.transform.rotation;

        // Debug.Log(sourceAxis.name + " Grabbed " +  tempPosition.x);
        // Debug.Log(sourceAxis.name + " Grabbed " +  tempPosition.y);
        // Debug.Log(sourceAxis.name + " Grabbed " +  tempPosition.z);
    }
    private void registerAxisReleased(Axis sourceAxis) {
        // Debug.Log(sourceAxis.name + " Released " +  sourceAxis.transform.position.x);
        // Debug.Log(sourceAxis.name + " Released " +  sourceAxis.transform.position.y);
        Debug.Log(sourceAxis.name + " Released " +  sourceAxis.transform.position.z);
        
        
        // Do something different when we're moving a whole visualization

        RestorableAction newAction = new RestorableAction(RestorableAction.ActionType.MOVE, sourceAxis, tempPosition, sourceAxis.transform.position, tempRotation, sourceAxis.transform.rotation);

        // First check if the action stack is not at max capacity
        EnsureStackCapacity(actionStack);

        // After clearing out the max cap, push the new action in there
        Debug.Log("New action is : " + newAction);
        actionStack.Push(newAction);
    }

    private void registerVisualizationGrabbed(Axis sourceAxis) {
        Debug.Log(sourceAxis.name + " VIS GRABBED " +  sourceAxis.transform.position);
        
        
        // Do something different when we're moving a whole visualization

        RestorableAction newAction = new RestorableAction(RestorableAction.ActionType.MOVE_VISUALIZATION, sourceAxis, tempPosition, sourceAxis.transform.position, tempRotation, sourceAxis.transform.rotation);

        

        // First check if the action stack is not at max capacity
        EnsureStackCapacity(actionStack);

        // After clearing out the max cap, push the new action in there
        Debug.Log("New action is : " + newAction);
        actionStack.Push(newAction);

    }

    private void registerVisualizationReleased(Axis sourceAxis) {
        return;
    }

    public void UndoAction() {
        if(actionStack.Count > 0) {
            // If redo stack was full then make some room by pushing out the earliest item
            EnsureStackCapacity(redoStack);
            RestorableAction poppedAction = actionStack.Pop(); 
            // Debug.Log("Action is undo: " + poppedAction);

            // If it's moving one axis from one place to the other
            if(poppedAction.type == RestorableAction.ActionType.MOVE) {
                poppedAction.sourceAxis.AnimateTo(poppedAction.OriginPositin, poppedAction.OriginRotation);
            } else if (poppedAction.type == RestorableAction.ActionType.CLONE) {
                Rigidbody body = poppedAction.sourceAxis.GetComponent<Rigidbody>();
                body.isKinematic = false;
                body.useGravity = true;
                body.AddForce(Vector3.up * -1000);
                poppedAction.sourceAxis.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
                poppedAction.sourceAxis.transform.DOScale(0.0f, 0.5f).SetEase(Ease.InBack);
            }

            redoStack.Push(poppedAction);
        } else {
            Debug.Log("Nothing to Undo!");
        }
    }
    public void RedoAction() {
        if(redoStack.Count > 0) {
            // If action stack was full then make some room by pushing out the earliest item
            EnsureStackCapacity(actionStack);
            RestorableAction poppedAction = redoStack.Pop(); 
            // Debug.Log("Action is REDO: " + poppedAction);

            // If it's moving one axis from one place to the other
            if(poppedAction.type == RestorableAction.ActionType.MOVE) {
                poppedAction.sourceAxis.AnimateTo(poppedAction.TargetPosition, poppedAction.TargetRotation);
            }

            actionStack.Push(poppedAction);
        } else {
            Debug.Log("Nothing to Redo!");
        }
    }

    private void EnsureStackCapacity(Stack<RestorableAction> targetStack) {
        if(targetStack.Count == MAX_HISTORY_SIZE) {
            // pop the latest in the stsck
            Stack<RestorableAction> temp = new Stack<RestorableAction>();
            for (int i = 0; i < MAX_HISTORY_SIZE; i++)
            {
                temp.Push(targetStack.Pop());
            }
            temp.Pop(); // Discard the last item of the temp which is the earliest targetStack entry
            for (int i = 0; i < MAX_HISTORY_SIZE - 1 ; i++)
            {
                targetStack.Push(temp.Pop());
            }
        }
    }

    void Update()
    {
        
    }

    public void OnLeftPadClicked() {
        // Debug.Log("Left was clicked!");
        UndoAction();
    }
    public void OnRightPadClicked() {
        // Debug.Log("Right was clicked!");
        RedoAction();
    }

    void OnDestroy() {
        EventManager.StopListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisGrabbed);
        EventManager.StopListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisReleased);
    }
}
