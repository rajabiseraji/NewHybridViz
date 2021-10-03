using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionManagementScript : MonoBehaviour
{

    public struct RestorableAction
    {
        Axis sourceAxis;
        Vector3 OriginPositin;
        Vector3 TargetPosition;
        Quaternion OriginRotation;
        Quaternion TargetRotation;
        public enum ActionType
        {
            Movement,
            Cloning,
            Destruction,
            Filtering,
            AttirbuteChange
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
            type = ActionType.Movement; // The default type is always movement 
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
    private Queue<RestorableAction> actionStack = new Queue<RestorableAction>();

    void Start()
    {
        EventManager.StartListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisGrabbed);
    }

    private void registerAxisGrabbed(Axis sourceAxis) {
        Debug.Log(sourceAxis.name + " " +  sourceAxis.transform.position);
    }

    void Update()
    {
        
    }
}
