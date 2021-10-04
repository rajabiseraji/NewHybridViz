using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    /// <summary>
    /// TODO :Make this class generic and specialize for each type of message
    /// </summary>
    /// 
    public class UnityEventFloat : UnityEvent<float>
    {

    }
    public class UnityEventAxis : UnityEvent<Axis>
    {

    }
    public class UnityEventVisualization : UnityEvent<Visualization>
    {

    }


    private Dictionary<string, UnityEventFloat> eventDictionary;
    private Dictionary<string, UnityEventAxis> axisEventDictionary;
    private Dictionary<string, UnityEventVisualization> visualizationEventDictionary;

    private static EventManager eventManager;

   
    public static EventManager instance
    {
        get
        {
            if (!eventManager)
            {
                eventManager = FindObjectOfType(typeof(EventManager)) as EventManager;

                if (!eventManager)
                {
                    Debug.LogError("There needs to be one active EventManger script on a GameObject in your scene.");
                }
                else
                {
                    eventManager.Init();
                }
            }

            return eventManager;
        }
    }

    void Init()
    {
        if (eventDictionary == null)
        {
            eventDictionary = new Dictionary<string, UnityEventFloat>();
        }
        if (axisEventDictionary == null)
        {
            axisEventDictionary = new Dictionary<string, UnityEventAxis>();
        }
        if (visualizationEventDictionary == null)
        {
            visualizationEventDictionary = new Dictionary<string, UnityEventVisualization>();
        }
    }

    /* 
    
    ------------------- FOR FLOAT EVENTS ---------------------
    
     */

    public static void StartListening(string eventName, UnityAction<float> listener)
    {
        UnityEventFloat thisEvent = null;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.AddListener(listener);
        }
        else
        {
            thisEvent = new UnityEventFloat();
            thisEvent.AddListener(listener);
            instance.eventDictionary.Add(eventName, thisEvent);
        }
    }

    public static void StopListening(string eventName, UnityAction<float> listener)
    {
        if (eventManager == null) return;
        UnityEventFloat thisEvent = null;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.RemoveListener(listener);
        }
    }
    
    public static void TriggerEvent(string eventName, float value)
    {
        UnityEventFloat thisEvent = null;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.Invoke(value);
        }
    }


    /* 
    
    ------------------- FOR AXIS EVENTS ---------------------
    
     */
    public static void StartListeningToAxisEvent(string eventName, UnityAction<Axis> listener)
    {
        UnityEventAxis thisEvent = null;
        if (instance.axisEventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.AddListener(listener);
        }
        else
        {
            thisEvent = new UnityEventAxis();
            thisEvent.AddListener(listener);
            instance.axisEventDictionary.Add(eventName, thisEvent);
        }
    }

    public static void StopListeningToAxisEvent(string eventName, UnityAction<Axis> listener)
    {
        if (eventManager == null) return;
        UnityEventAxis thisEvent = null;
        if (instance.axisEventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.RemoveListener(listener);
        }
    }
    
    public static void TriggerAxisEvent(string eventName, Axis value)
    {
        UnityEventAxis thisEvent = null;
        if (instance.axisEventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.Invoke(value);
        }
    }
    /* 
    
    ------------------- FOR VISUALIZATION EVENTS ---------------------
    
     */
    public static void StartListeningToVisuailzationEvent(string eventName, UnityAction<Visualization> listener)
    {
        UnityEventVisualization thisEvent = null;
        if (instance.visualizationEventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.AddListener(listener);
        }
        else
        {
            thisEvent = new UnityEventVisualization();
            thisEvent.AddListener(listener);
            instance.visualizationEventDictionary.Add(eventName, thisEvent);
        }
    }

    public static void StopListeningToVisualizationEvent(string eventName, UnityAction<Visualization> listener)
    {
        if (eventManager == null) return;
        UnityEventVisualization thisEvent = null;
        if (instance.visualizationEventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.RemoveListener(listener);
        }
    }
    
    public static void TriggerVisualizationEvent(string eventName, Visualization value)
    {
        UnityEventVisualization thisEvent = null;
        if (instance.visualizationEventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.Invoke(value);
        }
    }
}