using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Valve.VR;

public class VRInputModule : BaseInputModule
{

    public Camera m_camera;

    public SteamVR_Input_Sources m_targetSource;
    public SteamVR_Action_Boolean m_clickAction;
        
    private GameObject m_currentGameObject = null;
    private PointerEventData m_data = null;

    protected override void Awake()
    {
        base.Awake();

        m_data = new PointerEventData(eventSystem);
    }


    public override void Process()
    {
        // reset data
        //m_data.Reset();


        // set camera
        m_data.position = new Vector2(m_camera.pixelWidth / 2, m_camera.pixelHeight / 2);

        // raycast
        eventSystem.RaycastAll(m_data, m_RaycastResultCache);
        m_data.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        m_currentGameObject = m_data.pointerCurrentRaycast.gameObject;

        // Clear the raycast
        m_RaycastResultCache.Clear();

        // Hover state handling here
        HandlePointerExitAndEnter(m_data, m_currentGameObject);

        // press

        if (m_clickAction.GetStateDown(m_targetSource))
            ProcessPress(m_data);
        
        // release
        if (m_clickAction.GetStateUp(m_targetSource))
            ProcessRelease(m_data);



        /*
         I was here trying to match and find what game objects to pass to handle dragging
         */
        //if the thing was dragging something
        if (m_data.pointerDrag != null)
        {
            print("hey there's a drag thingy " + m_data.pointerDrag.name + " and current name is " + m_currentGameObject.name);
            ExecuteEvents.Execute(m_currentGameObject, m_data, ExecuteEvents.dragHandler);
        }

        /*
         We might want some sort of a select handler for this too. Just if we were using dropdowns and all
         */



    }

    public PointerEventData getData()
    {
        return m_data;
    }

    private void ProcessPress(PointerEventData data)
    {
        // set the raycast
        data.pointerPressRaycast = data.pointerCurrentRaycast;

        // check if we're hitting somethign and then get it
        // here we can check for anything that has a  pointer event handler 
        GameObject newPointerPress = ExecuteEvents.ExecuteHierarchy(m_currentGameObject, data, ExecuteEvents.pointerDownHandler);

        // if no down handler, get a click handler 
        if(newPointerPress == null)
        {
            newPointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(m_currentGameObject);
        }
        // set data
        data.pressPosition = data.position;
        data.pointerPress = newPointerPress;
        data.rawPointerPress = m_currentGameObject;


        ExecuteEvents.Execute(m_currentGameObject, data, ExecuteEvents.beginDragHandler);
        data.pointerDrag = m_currentGameObject;
        m_data = data;
    }

    private void ProcessRelease(PointerEventData data)
    {
        // here we first might want to check and see if there are any things being dragged
        if(data.pointerDrag != null)
        {
            m_currentGameObject = data.pointerDrag;
            ExecuteEvents.Execute(m_currentGameObject, data, ExecuteEvents.endDragHandler);
            if(data.pointerDrag != null)
                ExecuteEvents.ExecuteHierarchy(data.pointerDrag, data, ExecuteEvents.dropHandler);

            data.pointerDrag = null;
            m_currentGameObject = null;
        }


        // execute the pointer up event
        ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);

        // check for a click handler 
        GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(m_currentGameObject);

        // Check to see if the one is the prev one
        if(data.pointerPress == pointerUpHandler)
        {
            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerClickHandler);
        }

        // clear the selected gameobject
        eventSystem.SetSelectedGameObject(null);

        // reset the data
        data.pressPosition = Vector2.zero;
        data.pointerPress = null;
        data.rawPointerPress = null;
        m_data = data;

        m_data.Reset();
    }
}
