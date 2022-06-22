using System;
using UnityEngine;
using WebSocketSharp;
public class WsClient : MonoBehaviour
{
    WebSocket ws;
    string jsonString;
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
            if(receivedMsg.typeOfMessage == "CODAPINFO")
            {
                // This is a message that is sent to tell XR about the axes that we want to be extruded
                Debug.Log("we want something to be extruded!");
                string xAxisName = receivedMsg.xAxisName;
                string yAxisName = receivedMsg.yAxisName;
                print("X axis here is " + xAxisName);
                print("Y Axis here is " + yAxisName);
                if(yAxisName != "")
                    SceneManager.Instance.SetYToBeCreatedAxis(yAxisName);
                if(xAxisName != "")
                    SceneManager.Instance.SetXToBeCreatedAxis(xAxisName);
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
            ws.Send(jsonString);
        }
    }
}



[Serializable]
public class MouseEvent
{
    public int x;
    public int y;
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
}
