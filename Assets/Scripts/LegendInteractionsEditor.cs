using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LegendInteractions))]
public class LegendInteractionsEditor: Editor
{
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        DrawDefaultInspector();

        LegendInteractions legendInteractionsScript = (LegendInteractions)target;

        if(GUILayout.Button("Look At Camera"))
        {
            legendInteractionsScript.lookAtCamera();
        }
    }
}