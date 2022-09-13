using System;
using UnityEngine;
using WebSocketSharp;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
public class WsClient : MonoBehaviour
{
    WebSocket ws;
    string jsonString;

    public delegate void UpdateComponentListAction(ComponentListItem[] componentList);
    public static event UpdateComponentListAction OnUpdateComponentListAction;
    private void Start()
    {
        
        

        ws = new WebSocket("ws://localhost:7071/ws?name=unity");
        ws.Connect();
        Debug.Log("I'm receiving msgs from websocket");
        ws.OnMessage += WebSocketOnMessage;

    }

    private void WebSocketOnMessage(object sender, WebSocketSharp.MessageEventArgs e)
    {
        Debug.Log("Message Received from " + ((WebSocket)sender).Url + ", Data : " + e.Data);
        WebSocketMsg receivedMsg = JsonUtility.FromJson<WebSocketMsg>(e.Data);
        Debug.Log(receivedMsg.sender);
        if (receivedMsg.sender == "codap")
        {
            // this is the msg that codap sends when it wants to send us data about the thing we're extruding 
            // from desktop to VR
            if(receivedMsg.typeOfMessage == "CODAPINFO")
            {

                if((receivedMsg.x == -1 && receivedMsg.y == -1) || receivedMsg.text == "NOT_FOUND")
                {
                    // this means that there was nothing in the place that we put the controller

                    // TODO: the correct way to this is to handle it using event system and then 
                    // listening for that event in the MonitorBoardInteraction script
                    // however, for now we just set a flag in the SceneManager script to set this up
                    SceneManager.Instance.extrusionWasEmpty = true;
                    print("we didn't find anything under the extrusion, just set the variable in SceneManager");

                    return;

                }
                // This is a message that is sent to tell XR about the axes that we want to be extruded

                // TODO
                // We should check for the type of the msg here, checking if it's a highlight, extrusion or what
                Debug.Log("we want something to be extruded!");
                string xAxisName = receivedMsg.xAxisName;
                string yAxisName = receivedMsg.yAxisName;
                print("X axis here is " + xAxisName);
                print("Y Axis here is " + yAxisName);
                if(yAxisName != "")
                    SceneManager.Instance.SetYToBeCreatedAxis(yAxisName);
                if(xAxisName != "")
                    SceneManager.Instance.SetXToBeCreatedAxis(xAxisName);

                SceneManager.Instance.extrusionWasEmpty = false;

            } else if (receivedMsg.typeOfMessage == "BRUSHTOVR")
            {
                //if(receivedMsg.indexes != 0)
                //{
                    //print("I have received one with everything " + JsonUtility.ToJson(receivedMsg));
                    // let's first find someone that has the Brushing and linking class, then call the apply method from there
                    //BrushingAndLinking.ApplyDesktopBrushing(receivedMsg.indexes);
                    SceneManager.Instance.setBrushedIndexes(receivedMsg.indexes);
                //}
            } else if (receivedMsg.typeOfMessage == "COMPONENTLISTUPDATE")
            {
                print("I got a component msg that is " + JsonUtility.ToJson(receivedMsg.componentList));
                SceneManager.Instance.setComponetList(receivedMsg.componentList);
            } else if (receivedMsg.typeOfMessage == "CODAPLOGGING")
            {
                print("I'm logging for codap");
                // call the data logger to output the result of the action from codap's side
                //DataLogger.Instance.LogCodapData(e.Data);
                SceneManager.Instance.setToBeLoggedCodapData(e.Data);
                //print(e.Data);
            }
        }
    }

    private void Update()
    {
        if (ws == null)
        {
            return;
        }
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    ws.Send(jsonString);
        //}

        //if(HasMouseMoved())
        //{
        //    MouseEvent myObject = new MouseEvent();
        //    myObject.x = (int)Input.mousePosition.x;
        //    myObject.y = (int)Input.mousePosition.y;
        //    jsonString = JsonUtility.ToJson(myObject);
        //    ws.Send(jsonString);
        //}
    }

    private void OnDestroy()
    {
        ws.Close();
    }

    bool HasMouseMoved()
    {
        //I feel dirty even doing this 
        return (Input.GetAxis("Mouse X") != 0) || (Input.GetAxis("Mouse Y") != 0);
    }

    public void SendMsgToDesktop(WebSocketMsg message)
    {
        if(ws != null)
        {
            jsonString = JsonUtility.ToJson(message);
            print("in WSClient: " + jsonString);
            // ws.send works in Sync, we can do it with SendAsync too. That one gets a "completed" arg that is a delegate to do sth when the thing is complete
            ws.Send(jsonString);
        }
    }

    public void SendBrushingMsgToDesktop(int id, Vector3[] brushedIndexes)
    {
        print("brushing shit to dekstop!");
        var msg = new WebSocketMsg(id, "brushing stuff", "BRUSHTODESKTOP", brushedIndexes);
        SendMsgToDesktop(msg);
    }    
    
    public void SendBrushingMsgToDesktop(int id, int[] brushedIndexes)
    {
        print("brushing shit to dekstop!");
        var msg = new WebSocketMsg(id, "brushing stuff", "BRUSHTODESKTOP", brushedIndexes);
        SendMsgToDesktop(msg);
    }
}



[Serializable]
public class MouseEvent
{
    public int x;
    public int y;
}

[Serializable]
public class CodapPosition
{
    public int x;
    public int y;
    public int endX;
    public int endY;
}

[Serializable]
public class ComponentListItem
{
    public int id;
    public CodapPosition position;
    public string xAttributeName;
    public string yAttributeName;
    public string legendAttributeName;
}

[Serializable]
public class WebSocketMsg
{
    public int id;
    public int x;
    public int y;
    public int numberOfAxes = 0;
    public string xAxisName;
    public string yAxisName;
    public string zAxisName;
    public string text;
    public string typeOfMessage;
    public string sender;
    public int[] indexes = new int[0];
    public ComponentListItem[] componentList = new ComponentListItem[0];

    public WebSocketMsg(int id, 
        Vector2 desktopPosition, 
        int numberOfAxes, 
        Axis xAxis, 
        Axis yAxis, 
        Axis zAxis, 
        string text,
        string typeOfMessage // CREATE or EXTRUDE 
        )
    {

        this.id = id;
        this.x = (int)desktopPosition.x;
        this.y = (int)desktopPosition.y;
        this.text = text;
        this.typeOfMessage = typeOfMessage;
        this.numberOfAxes = numberOfAxes;
        this.xAxisName = xAxis != null ? SceneManager.Instance.dataObject.Identifiers[xAxis.axisId] : "";
        this.yAxisName = yAxis != null ? SceneManager.Instance.dataObject.Identifiers[yAxis.axisId] : "";
        this.zAxisName = zAxis != null ? SceneManager.Instance.dataObject.Identifiers[zAxis.axisId] : "";
    }

    public WebSocketMsg
        (
            int id,
            Vector2 desktopPosition,
            string typeOfMessage,
            string text
        )
    {
        this.id = id;
        this.typeOfMessage = typeOfMessage;
        this.text = text;
        this.x = (int)desktopPosition.x;
        this.y = (int)desktopPosition.y;
    }

    public WebSocketMsg
        (
            int id,
            string text,
            string typeOfMessage, // BRUSH  
            Vector3[] brushedIndexes
        )
    {
        this.id = id;
        this.text = text;
        this.typeOfMessage = typeOfMessage;

        // convert brushed indexes into normal indexes
        var indexList = new List<int>();
        for(int i = 0; i < brushedIndexes.Length; i++)
        {
            if (brushedIndexes[i].x == 1f)
                indexList.Add(i);
        }

        indexes = indexList.ToArray();
    }
    
    public WebSocketMsg
        (
            int id,
            string text,
            string typeOfMessage, // BRUSH  
            int[] brushedIndexes
        )
    {
        this.id = id;
        this.text = text;
        this.typeOfMessage = typeOfMessage;

        // convert brushed indexes into normal indexes
        var indexList = new List<int>();
        for (int i = 0; i < brushedIndexes.Length; i++)
        {
            if (brushedIndexes[i] == 1)
                indexList.Add(i);
        }

        indexes = indexList.ToArray();
    }
}
