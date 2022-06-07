using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonitorBoardInteractions : MonoBehaviour, Grabbable
{
    public int GetPriority()
    {
        return 200;
    }

    public void OnDrag(WandController controller)
    {

    }

    public void OnEnter(WandController controller)
    {
        Debug.Log("Hey i have enetered the monitor!");
    }

    public void OnExit(WandController controller)
    {

        //throw new System.NotImplementedException();
    }

    public bool OnGrab(WandController controller)
    {
        Debug.Log("Hey i have grabbed the thing now the monitor!");
        return false;
        //throw new System.NotImplementedException();
    }

    public void OnRelease(WandController controller)
    {
        //throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
