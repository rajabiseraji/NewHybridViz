using UnityEngine;
using System.Runtime.InteropServices;


public class RaycastExample : MonoBehaviour
{
    public Transform from;
    public Transform to;
    public Transform controller; 
    public GameObject cube;
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);
    public bool setMousePosDirty = true;


    private void Start()
    {
        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = 0.01f * Vector3.one;
        cube.GetComponent<Renderer>().material.color = Color.red;
    }

    void Update()
    {
        if (!from || !to) return;
        Debug.DrawLine(from.position, to.position, Color.red);


        foreach (var uddTexture in GameObject.FindObjectsOfType<uDesktopDuplication.Texture>()) {
            var result = uddTexture.RayCast(from.position, to.position - from.position);
            if (result.hit) {
                Debug.DrawLine(result.position, result.position + result.normal, Color.yellow);
                //Debug.Log("COORD: " + result.coords + ", DESKTOP: " + result.desktopCoord);
                //var isResultPositionInvalid = float.IsNaN(result.position.x) || float.IsNaN(result.position.y) || float.IsNaN(result.position.z);
                
                //if (isResultPositionInvalid)
                //    return;
                if(!float.IsNaN(result.position.x))
                    cube.transform.position = result.position;

                if(!setMousePosDirty)
                {
                    SetCursorPos((int)result.desktopCoord.x, (int)result.desktopCoord.y);
                    //setMousePosDirty = true;
                }

            }
        }
    }
}
