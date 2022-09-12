using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

[CustomEditor(typeof(SceneManager))]
public class SceneManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        SceneManager target = (SceneManager)base.target;

        DrawDefaultInspector();

        float minSliderValue = EditorGUILayout.Slider(target.minFilter, -0.5f, 0.5f);
        float maxSliderValue = EditorGUILayout.Slider(target.maxFilter, -0.5f, 0.5f);
        
        if(minSliderValue != target.minFilter)
        {
            target.minFilter = minSliderValue;
            //target.printShit(minSliderValue);
            target.ChangeDebugFilterValue();
        }

        if (maxSliderValue != target.maxFilter)
        {
            target.maxFilter = maxSliderValue;
            //target.printShit(minSliderValue);
            target.ChangeDebugFilterValue();
        }

        if (GUILayout.Button("Make Scatterplot"))
        {
            target.CreateScatterplot();
        }
        
        if (GUILayout.Button("Make Scatterplot 3D"))
        {
            target.CreateScatterplot3D();
        }

        if (GUILayout.Button("Add Filter Debug"))
        {
            target.AddNewFilterToFilterBubbles();
        }

        if (GUILayout.Button("Generate Random Indexes"))
        {
            //BrushingAndLinking.BrushVisualization(generateRandomDataIndexes());
            BrushingAndLinking.doManualBrushing(generateIntRandomDataIndexes());
        }
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
        for (int i = 0; i < SceneManager.Instance.dataObject.DataPoints; i++)
        {

            if (randomForSelection.Any(x => x == i))
            {
                randomIndexes[i] = 1;
            }
            else
            {
                randomIndexes[i] = 0;
            }
        }
        return randomIndexes;
    }

}