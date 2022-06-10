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
        Debug.Log("im doing some shit");
        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Message Received from " + ((WebSocket)sender).Url + ", Data : " + e.Data);
            //Debug.Log(JsonUtility.FromJson<MyClass>(e.Data).playerName);
        };


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
    int numberOfAxes = 0;
    public string xAxisName;
    public string yAxisName;
    public string zAxisName;
    public string text;

    public WebSocketMsg(int id, 
        Vector2 desktopPosition, 
        int numberOfAxes, 
        Axis xAxis, 
        Axis yAxis, 
        Axis zAxis, 
        string text
        )
    {
        this.id = id;
        this.x = (int)desktopPosition.x;
        this.y = (int)desktopPosition.y;
        this.text = text;
        this.numberOfAxes = numberOfAxes;
        this.xAxisName = xAxis != null ? SceneManager.Instance.dataObject.Identifiers[xAxis.axisId] : "";
        this.yAxisName = yAxis != null ? SceneManager.Instance.dataObject.Identifiers[yAxis.axisId] : "";
        this.zAxisName = zAxis != null ? SceneManager.Instance.dataObject.Identifiers[zAxis.axisId] : "";
    }
}
