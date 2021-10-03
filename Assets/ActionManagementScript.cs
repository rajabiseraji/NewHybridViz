using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionManagementScript : MonoBehaviour
{
    private static readonly int MAX_HISTORY_SIZE = 5;

    public struct RestorableAction
    {
        Axis sourceAxis;
        Vector3 OriginPositin;
        Vector3 TargetPosition;
        Quaternion OriginRotation;
        Quaternion TargetRotation;
        public enum ActionType
        {
            MOVE,
            CLONE,
            DESTORY,
            FILTER,
            ATTRIBUTE_CHANGE
        }

        ActionType type;

        public RestorableAction(ActionType type, Axis sourceAxis, Vector3 OriginPositin, Vector3 TargetPosition, Quaternion OriginRotation, Quaternion TargetRotation) {
            this.type = type;
            this.sourceAxis = sourceAxis;
            this.OriginPositin = OriginPositin;
            this.TargetPosition = TargetPosition;
            this.OriginRotation = OriginRotation;
            this.TargetRotation = TargetRotation;
        }

        public void clear() {
            sourceAxis = null;
            OriginPositin = Vector3.zero;
            TargetPosition = Vector3.zero;
            OriginRotation = Quaternion.identity;
            TargetRotation = Quaternion.identity;
            type = ActionType.MOVE; // The default type is always movement 
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
    private Transform tempTransform;

    void Start()
    {
        EventManager.StartListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisGrabbed);
        EventManager.StartListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisReleased);
    }

    private void registerAxisGrabbed(Axis sourceAxis) {
        Debug.Log(sourceAxis.name + " Grabbed " +  sourceAxis.transform.position);
        tempTransform = sourceAxis.transform;
    }
    private void registerAxisReleased(Axis sourceAxis) {
        Debug.Log(sourceAxis.name + " Released " +  sourceAxis.transform.position);
        RestorableAction newAction = new RestorableAction(RestorableAction.ActionType.MOVE, sourceAxis, tempTransform.position, sourceAxis.transform.position, tempTransform.rotation, sourceAxis.transform.rotation);

        // First check if the action stack is not at max capacity
        if(actionStack.Count == MAX_HISTORY_SIZE) {
            // pop the latest in the stsck
            Stack<RestorableAction> temp = new Stack<RestorableAction>();
            for (int i = 0; i < MAX_HISTORY_SIZE; i++)
            {
                temp.Push(actionStack.Pop());
            }
            temp.Pop(); // Discard the last item of the temp which is the earliest ActionStack entry
            for (int i = 0; i < MAX_HISTORY_SIZE - 1 ; i++)
            {
                actionStack.Push(temp.Pop());
            }
        }

        // After clearing out the max cap, push the new action in there
        Debug.Log("New action is : " + newAction);
        actionStack.Push(newAction);
    }

    public void UndoAction() {
        if(actionStack.Count > 0) {
            // If redo stack was full then make some room by pushing out the earliest item
            if(redoStack.Count == MAX_HISTORY_SIZE) {
                // pop the latest in the stsck
                Stack<RestorableAction> temp = new Stack<RestorableAction>();
                for (int i = 0; i < MAX_HISTORY_SIZE; i++)
                {
                    temp.Push(redoStack.Pop());
                }
                temp.Pop(); // Discard the last item of the temp which is the earliest ActionStack entry
                for (int i = 0; i < MAX_HISTORY_SIZE - 1 ; i++)
                {
                    redoStack.Push(temp.Pop());
                }
            }

            Debug.Log("Action is undo: " + actionStack.Peek());
            redoStack.Push(actionStack.Pop());
        } else {
            Debug.Log("Nothing to Undo!");
        }
    }
    public void RedoAction() {
        if(redoStack.Count > 0) {
            // If action stack was full then make some room by pushing out the earliest item
            if(actionStack.Count == MAX_HISTORY_SIZE) {
                // pop the latest in the stsck
                Stack<RestorableAction> temp = new Stack<RestorableAction>();
                for (int i = 0; i < MAX_HISTORY_SIZE; i++)
                {
                    temp.Push(actionStack.Pop());
                }
                temp.Pop(); // Discard the last item of the temp which is the earliest ActionStack entry
                for (int i = 0; i < MAX_HISTORY_SIZE - 1 ; i++)
                {
                    actionStack.Push(temp.Pop());
                }
            }

            Debug.Log("Action is REDO: " + redoStack.Peek());
            actionStack.Push(redoStack.Pop());
        } else {
            Debug.Log("Nothing to Redo!");
        }
    }

    void Update()
    {
        
    }

    public void OnLeftPadClicked() {
        Debug.Log("Left was clicked!");
        UndoAction();
    }
    public void OnRightPadClicked() {
        Debug.Log("Right was clicked!");
        RedoAction();
    }

    void OnDestroy() {
        EventManager.StopListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisGrabbed);
        EventManager.StopListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisReleased);
    }
}
