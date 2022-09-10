using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

[CustomEditor(typeof(BrushingAndLinking))]
public class BrushingAndLinkingEditor: Editor
{
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        DrawDefaultInspector();

        BrushingAndLinking brushingAndLinkingScript = (BrushingAndLinking)target;

        if(GUILayout.Button("Generate Random Indexes"))
        {
            //BrushingAndLinking.BrushVisualization(generateRandomDataIndexes());
            brushingAndLinkingScript.doManualBrushing(generateIntRandomDataIndexes());
        }
    }

    public Vector3[] generateRandomDataIndexes()
    {
        var rand = new System.Random();
        Vector3[] randomIndexes = new Vector3[SceneManager.Instance.dataObject.DataPoints];
        for(int i = 0; i < SceneManager.Instance.dataObject.DataPoints; i++)
        {
            if(i == rand.Next(0, SceneManager.Instance.dataObject.DataPoints))
            {
                randomIndexes[i] = new Vector3(1f, 0, 0);
            } else
            {
                randomIndexes[i] = new Vector3(0, 0, 0);
            }
        }
        return randomIndexes;
    }
    
    public int[] generateIntRandomDataIndexes()
    {
        var rand = new System.Random();
        int[] randomIndexes = new int[SceneManager.Instance.dataObject.DataPoints];
        int[] randomForSelection = new int[30];

        for (int k = 0; k < randomForSelection.Length; k++)
        {
            randomForSelection[k] = rand.Next(0, SceneManager.Instance.dataObject.DataPoints);
        }
        for(int i = 0; i < SceneManager.Instance.dataObject.DataPoints; i++)
        {

            if(randomForSelection.Any(x => x == i))
            {
                randomIndexes[i] = 1;
            } else
            {
                randomIndexes[i] = 0;
            }
        }
        return randomIndexes;
    }
}