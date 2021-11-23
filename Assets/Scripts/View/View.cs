using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class View
{
    bool UNITY_GAME_OBJECTS_MODE = false;
    List<GameObject> visualObjects = new List<GameObject>();

    public Visualization visualizationReference; 

    public enum VIEW_DIMENSION { X, Y, Z, LINKING_FIELD };

    private Mesh myMesh;

    public Mesh MyMesh
    {
        get { return myMesh; }
        set { myMesh = value; }
    }

    private MeshTopology myMeshTopolgy;

    private List<Vector3> positions = new List<Vector3>();

    public bool isParallelCoordsView = false;

    public View(MeshTopology type, string viewName, Visualization visRef)
    {
        visualizationReference = visRef;
        myMeshTopolgy = type;
        myMesh = new Mesh();
        name = viewName;
    }

    private string name = "";

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    private float[] brushedIndexes;

    public float[] BrushedIndexes
    {
        get { return brushedIndexes; }
        set { brushedIndexes = value; }
    }

    RenderTexture myRenderTexture;

    public void initialiseDataView(int numberOfPoints, GameObject parent)
    {
        for (int i = 0; i < numberOfPoints; i++)
        {
            positions.Add(new Vector3());

            if (UNITY_GAME_OBJECTS_MODE)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.parent = parent.transform;
                visualObjects.Add(go);
            }
        }

        //Debug.Log("Created " + numberOfPoints +" data points");
    }

    
    public void setDataDimension(float[] dat, VIEW_DIMENSION dimension)
    {
        float minValue = dat.Min();
        float maxValue = dat.Max();

        for (int i = 0; i < dat.Length; i++)
        {
            Vector3 p = positions[i];

            switch (dimension)
            {
                case VIEW_DIMENSION.X:
                    p.x = UtilMath.normaliseValue(dat[i], minValue, maxValue, -0.5f, 0.5f);
                    break;
                case VIEW_DIMENSION.Y:
                    p.y = UtilMath.normaliseValue(dat[i], minValue, maxValue, -0.5f, 0.5f);
                    break;
                case VIEW_DIMENSION.Z:
                    p.z = UtilMath.normaliseValue(dat[i], minValue, maxValue, -0.5f, 0.5f);
                    break;
            }
            positions[i] = p;
        }

    }


    public void doFilter(int filterAttributeIndex, float minFilterValue) {
        // foreach (var viewElement in viewList)
        // {

        // Filter attribute index is the value of the index that is in the Axis.SourceIndex that is being used to filter this data (which it self comes from DataObject.index)
        // the number of items in it are the number of items in each column of data!!
        
        // HERE WE NEED TO FIND THE NUMBER OF ALL THE POINTS OF DATA THAT ARE BEING SHOWN IN THIS VISUALIZATION AND FILTER THEM OUT (NUM(COL a) * NUM(COL b) * NUM(COL c) ) 
        float[] isFiltered = new float[SceneManager.Instance.dataObject.DataArray.Count];


        Debug.Log("In DOfILTER with ARRAY: The position count is: " + isFiltered.Length);
        for (int i = 0; i < SceneManager.Instance.dataObject.NbDimensions; i++)
        {
            // foreach (AttributeFilter attrFilter in visualisationReference.attributeFilters)
            // {
                if (filterAttributeIndex == i)
                {
                    float[] col = SceneManager.Instance.dataObject.GetCol(SceneManager.Instance.dataObject.DataArray, i);
                    // float minFilteringValue = UtilMath.normaliseValue(attrFilter.minFilter, 0f, 1f, attrFilter.minScale, attrFilter.maxScale);
                    // float maxFilteringValue = UtilMath.normaliseValue(attrFilter.maxFilter, 0f, 1f, attrFilter.minScale, attrFilter.maxScale);

                    // Col[j] is equivalent to DataArray[j][another_index]

                    for (int j = 0; j < isFiltered.Length; j++)
                    {
                        // Debug.Log("BEGIN: I'm filtering " + SceneManager.Instance.dataObject.Identifiers[i] + "that has the value of " + SceneManager.Instance.dataObject.DataArray[j][i]);

                        isFiltered[j] = (col[j] < minFilterValue || col[j] > 1.0f) ? 1.0f : isFiltered[j];

                        //  Debug.Log("END: I'm filtering " + SceneManager.Instance.dataObject.Identifiers[i] + "that has the value of " + SceneManager.Instance.dataObject.DataArray[j][i]);
                    }
                }
            // }
        }
        updateFilterChannel(isFiltered);
    }
    public void doFilter(List<AttributeFilter> filters) {
        float[] isFiltered = new float[SceneManager.Instance.dataObject.DataArray.Count];


        Debug.Log("In DOfILTER: The position count is: " + isFiltered.Length);
        for (int i = 0; i < SceneManager.Instance.dataObject.NbDimensions; i++)
        {
            foreach (AttributeFilter attrFilter in filters)
            {
                // Take care to change this when we want to use a localized data source
                if (attrFilter.idx == i)
                {
                    // Debug.Log("Now filtering with index of "+ attrFilter.idx + " : " + SceneManager.Instance.dataObject.Identifiers[i]);

                    float[] col = SceneManager.Instance.dataObject.GetCol(SceneManager.Instance.dataObject.DataArray, i);
                    // float minFilteringValue = UtilMath.normaliseValue(attrFilter.minFilter, 0f, 1f, attrFilter.minScale, attrFilter.maxScale);
                    // float maxFilteringValue = UtilMath.normaliseValue(attrFilter.maxFilter, 0f, 1f, attrFilter.minScale, attrFilter.maxScale);

                    // Col[j] is equivalent to DataArray[j][another_index]

                    for (int j = 0; j < isFiltered.Length; j++)
                    {
                        // Debug.Log("BEGIN: I'm filtering " + SceneManager.Instance.dataObject.Identifiers[i] + " that has the value of " + col[j]);

                        isFiltered[j] = (col[j] < attrFilter.minFilter || col[j] > attrFilter.maxFilter) ? 1.0f : isFiltered[j];

                        //  Debug.Log("END: I'm filtering " + SceneManager.Instance.dataObject.Identifiers[i] + " that has the value of " + SceneManager.Instance.dataObject.DataArray[j][i]);
                    }
                }
            }
        }
        updateFilterChannel(isFiltered);
    }

    public void setDefaultColor()
    {
        Color[] colors = new Color[myMesh.vertices.Length];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;
        myMesh.colors = colors;
    }

    public void updateView(float[] linking)
    {
        //if (UNITY_GAME_OBJECTS_MODE)
        //    updateGameObjects(0.05f);
        //else 
        if (linking == null)
            updateMeshPositions(null);
        else //create the lines
        {
            updateMeshPositions(linking);
        }
    }

    int[] createIndicesScatterPlot(int numberOfPoints)
    {
        int[] indices = new int[numberOfPoints];

        for (int i = 0; i < numberOfPoints; i++)
        {
            indices[i] = i;
        }
        return indices;
    }

    int[] createIndicesLines(float[] linkingField)
    {
        List<int> indices = new List<int>();

        if (linkingField != null)
        {
            for (int i = 0; i < linkingField.Length - 1; i++)
            {
                //Debug.Log(linkingField[i] + "    - - - -        " + linkingField[i + 1]);
                if (linkingField[i] == linkingField[i + 1])
                {
                    indices.Add(i);
                    indices.Add(i + 1);
                }
            }
        }
        else
        {
            // pairwise lines
            for (int i = 0; i < positions.Count - 1; i += 2)
            {
                indices.Add(i);
                indices.Add(i + 1);
            }
        }

        //foreach (int item in indices)
        //{
        //    Debug.Log(item);
        //}
        return indices.ToArray();
    }

    private void updateGameObjects(float scale)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject go = visualObjects[i];
            go.transform.localScale = new Vector3(scale, scale, scale);
            go.transform.localPosition = positions[i];
            go.GetComponent<Renderer>().material.color = Color.black;
        }
    }

    private void updateMeshPositions(float[] linkingField)
    {
        switch (myMeshTopolgy)
        {
            case MeshTopology.LineStrip:
                myMesh.vertices = positions.ToArray();
                myMesh.SetIndices(createIndicesLines(linkingField), MeshTopology.LineStrip, 0);
                break;
            case MeshTopology.Lines:
                myMesh.vertices = positions.ToArray();
                myMesh.SetIndices(createIndicesLines(linkingField), MeshTopology.Lines, 0);
                break;
            case MeshTopology.Points:
                myMesh.vertices = positions.ToArray();
                myMesh.SetIndices(createIndicesScatterPlot(positions.Count), MeshTopology.Points, 0);
                //updateVertexIndices();
                break;
            case MeshTopology.Quads:
                break;
            case MeshTopology.Triangles:
                break;
            default:
                break;
        }

        updateVertexIndices(0);
        
    }

    /// <summary>
    /// use the normal to store the vertex index
    /// </summary>
    private void updateVertexIndices(int channel)
    {

        Vector3[] norms = new Vector3[positions.Count];
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 v = Vector3.zero;
            v[channel] = (float)i;
            norms[i] = v;
        }
        myMesh.normals = norms;
        Debug.Log("In" + visualizationReference.name + " updateVertexIndices now with meshnormal length of: " + myMesh.normals.Length + "vertex count of " + myMesh.vertexCount);
    }

    private void updateSizeChannel(int channel, float[] normalisedValueDimension)
    {
        Vector3[] myMeshNormals = myMesh.normals;

        Debug.Log("In " + visualizationReference.name + "updateSize now with meshnormal length of: " + myMeshNormals.Length + "vertex count of " + myMesh.vertexCount);

        if (myMeshNormals == null) myMeshNormals = new Vector3[myMesh.vertexCount];
        for(int i=0; i<myMeshNormals.Length;i++)
        {
            Vector3 v = myMeshNormals[i];
            v[channel] = normalisedValueDimension[i];
            myMeshNormals[i] = v;
        }
        myMesh.normals= myMeshNormals;

        Debug.Log("In" + visualizationReference.name + " updateSize END now with meshnormal length of: " + myMesh.normals.Length + "vertex count of " + myMesh.vertexCount);
    }

    private void updateFilterChannel(float[] filteredData) {
        // We're using the third channel of norms to hold the filtered data array
        const int CHANNEL = 2; 
        Vector3[] myMeshNormals = myMesh.normals;

        Debug.Log("In" + visualizationReference.name + " updateFilterChannel now with meshnormal length of: " + myMesh.normals.Length + "vertex count of " + myMesh.vertexCount);

        if (myMeshNormals == null) myMeshNormals = new Vector3[myMesh.vertexCount];
        for(int i=0; i<myMeshNormals.Length;i++)
        {
            Vector3 v = myMeshNormals[i];
            v[CHANNEL] = filteredData[i];
            myMeshNormals[i] = v;
        }
        myMesh.normals= myMeshNormals;
        Debug.Log("FROM updateFilterChannel: I'm putting the filtered data in here! ");
    }

    public void debugVector3List(List<Vector3> list)
    {
        foreach (Vector3 p in list)
        {
            Debug.Log(p.ToString());
        }
    }

    public void debugVector3List(Vector3[] vector3)
    {
        foreach (Vector3 p in vector3)
        {
            Debug.Log(p.ToString());
        }
    }

    public void mapColorContinuous(float[] dat, Color fromColor, Color toColor)
    {
        List<Color> myColors = new List<Color>();
        for(int i=0;i<dat.Length;i++)
        {
            myColors.Add(Color.Lerp(fromColor, toColor, dat[i]));
        }
        //Debug.Log("vertices count: " + myMesh.vertices.Length + " colors count: " + myColors.Count);
        myMesh.colors = myColors.ToArray();
    }

    public void mapColorCategory(float[] dat, Color[] palette, bool isPCP)
    {
        Color[] colorSet = new Color[dat.Length];
        int cat =0;
        colorSet[0] = palette[cat];
        for(int i=1; i<dat.Length; i++)
        {
            if (dat[i] == dat[i - 1]) colorSet[i] = palette[cat];
            else {
                cat++;
                Debug.Log(cat);
                colorSet[i] = palette[cat];
            }
        }
        setColors(colorSet, isPCP);
    }

    public void setSizes(float[] sizes)
    {
        updateSizeChannel(1, sizes);
    }
    
    public void setColors(Color[] colors, bool isPCP)
    {
        if (colors != null)
        {
            if (/*myMeshTopolgy == MeshTopology.Lines &&*/ isPCP)
            {
                List<Color> colorsLine = new List<Color>();

                for (int i = 0; i < colors.Length; i += 1)
                {
                    colorsLine.Add(colors[i]);
                    colorsLine.Add(colors[i]);
                }
                myMesh.colors = colorsLine.ToArray();

            }
            else
            {
                myMesh.colors = colors;
            }
        }
    }

}