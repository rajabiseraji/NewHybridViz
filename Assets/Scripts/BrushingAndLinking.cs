using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Staxes;

public class BrushingAndLinking : MonoBehaviour, UIComponent
{


    public static Vector3[] brushedIndexes;// = new List<float>();
    public static Vector3[] brushedIndexesParallelPlot;

    public static bool isBrushing = false;
    Color linkingColor = Color.red;
    public static float brushSize = 0.1f;
    // This the brush position in the world coords
    public static Vector3 brushPosition = Vector3.zero;


    // Use this for initialization
    void Start()
    {
        //brushedTexture = new RenderTexture(Screen.width, Screen.height, 24);

        //brushedTexture.Create();

        //VisualisationFactory.Instance.pointCloudMaterial.SetFloat("_data_size", brushedTexture.width);

    }

    // Update is called once per frame
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

    }

    void Update()
    {
        if (isBrushing)
        {
            GameObject[] views = GameObject.FindGameObjectsWithTag("View");
            Debug.Log("in the update and I'm brushing!");
            //link the brush to all other visualisations
            for (int i = 0; i < views.Length; i++)// (var item in activeViews)
            {
                try
                {
                    Mesh m = views[i].GetComponent<MeshFilter>().mesh;

                    if (brushedIndexes != null)
                    {
                        if (m.vertexCount < brushedIndexes.Length)
                        {
                            //we brushed a parallel coordinates so we need to reduce by 2 the brushed indices
                            Debug.Log("Brushing first condition ");
                            List<Vector3> brushScatter = new List<Vector3>();
                            for (int k = 0; k < brushedIndexes.Length; k += 2)
                                brushScatter.Add(brushedIndexes[k]);

                            Vector3[] meshNormals = m.normals;
                            for (int p = 0; p < meshNormals.Length; p++)
                            {
                               meshNormals[p] = new Vector3(brushScatter[p].x ,m.normals[p].y, m.normals[p].z); 
                            }
                            //Array.Resize(ref brushedIndexes, m.vertexCount);
                            m.normals = meshNormals;

                        }
                        else if (m.vertexCount > brushedIndexes.Length)
                        {
                            //we brushed a 2D scatterplot we need to make twice bigger
                            //List<Vector3> brushParrallel = new List<Vector3>();
                            //Color[] linkedVisuColor = m.colors;

                            //for (int k = 0; k < brushedIndexes.Length; k += 1)
                            //{
                            //    brushParrallel.Add(brushedIndexes[k]);
                            //    brushParrallel.Add(brushedIndexes[k]);
                            //}

                            ////Vector3[] copyParallelIndices = new Vector3[brushedIndexes.Length * 2];
                            ////brushedIndexes.CopyTo(copyParallelIndices, 0);
                            ////brushedIndexes.CopyTo(copyParallelIndices, brushedIndexes.Length - 1);

                            //m.normals = brushParrallel.ToArray();
                            print("updating a PCP");
                        }

                        else
                        {
                            Vector3[] meshNormals = m.normals;
                            for (int p = 0; p < meshNormals.Length; p++)
                            {
                               meshNormals[p] = new Vector3(brushedIndexes[p].x ,m.normals[p].y, m.normals[p].z); 
                            }
                            Debug.Log("Hey I just brushed ");
                            //we are brushing and linking same visualisation types
                            m.normals = meshNormals;
                        }
                    }

                }
                catch (MissingReferenceException)
                {
                    Debug.Log("exception component");
                }
                //                item.GetComponentInChildren<MeshFilter>().mesh = m;
            }
        }

        //if (brushedIndexes != null)
        //{
        //    GameObject[] linkedvizs = GameObject.FindGameObjectsWithTag("LinkedVisualisation");

        //    foreach (var item in linkedvizs)
        //    {
        //        Mesh m = item.GetComponent<MeshFilter>().mesh;

        //        List<Vector3> brushParrallel = new List<Vector3>();
        //        Color[] linkedVisuColor = m.colors;

        //        for (int k = 0; k < brushedIndexes.Length; k += 1)
        //        {
        //            brushParrallel.Add(brushedIndexes[k]);
        //            brushParrallel.Add(brushedIndexes[k]);
        //        }

        //        if (m.normals.Length == brushParrallel.Count)
        //            m.normals = brushParrallel.ToArray();
        //    }
        //}

    }



    public static void updateBrushedIndices(Vector3[] brushed, bool isParallelPlot)
    {

        // if (isParallelPlot)
        //   brushedIndexesParallelPlot = brushed;
        //else
        brushedIndexes = brushed;
    }


    // ==============================================================================================
    // ==============================================================================================
    // ==============================================================================================
    // ==============================================================================================
    // ========================== CPU FUNCTIONS FOR BRUSHING /////////////////
    // ==============================================================================================
    // ==============================================================================================
    // ==============================================================================================
    // ==============================================================================================
    static GameObject cube = null;
    static GameObject cubeYellow = null;
    


    /// <summary>
    /// returns a list of brushed indexes for the data points within distance
    /// </summary> 
    /// <param name="data"></param>
    /// <param name="point"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static Vector3[] BrushIndicesPointScatterplot(
        Vector3[] data,
        Vector3 worldCoordsPoint,
        float distance,
        Vector4 _ftl,
        Vector4 _ftr,
        Vector4 _fbl,
        Vector4 _fbr,
        Vector4 _btl,
        Vector4 _btr,
        Vector4 _bbl,
        Vector4 _bbr,
        Transform parentTransform, Visualization parentVis, bool is3D)
    {
        // Visualization parentVis = parentTransform.GetComponentInParent<Visualization>();
        Debug.Assert(parentVis != null, "Parent visualization cannot be null!");

        GameObject tempTransformObject = null;

        // Q: why this local scale??
        tempTransformObject = new GameObject("Brush Transform");
        tempTransformObject.transform.parent = parentTransform;
        tempTransformObject.transform.localPosition = Vector3.zero;
        tempTransformObject.transform.localScale = new Vector3(Axis.AXIS_ROD_LENGTH, Axis.AXIS_ROD_LENGTH, Axis.AXIS_ROD_LENGTH) / 2;
        
        Vector3[] brushedIndices = new Vector3[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            //if (Vector3.Distance(transformPointToVisualisation(data[i], bl, br, tl), point) < distance)
            //            if (Vector3.Distance(GetVertexWorldPosition(data[i], parentTransform), point) < distance)

            if (is3D)
            {
                float xMinNormaliser = parentVis.ReferenceAxis1.horizontal.MinNormaliser;
                float xMaxNormaliser = parentVis.ReferenceAxis1.horizontal.MaxNormaliser;
                float yMinNormaliser = parentVis.ReferenceAxis1.vertical.MinNormaliser;
                float yMaxNormaliser = parentVis.ReferenceAxis1.vertical.MaxNormaliser;
                float zMinNormaliser = parentVis.ReferenceAxis1.depth.MinNormaliser;
                float zMaxNormaliser = parentVis.ReferenceAxis1.depth.MaxNormaliser;

                // create a transform for the visualisation space
                var vup = parentVis.fbl - parentVis.ftl;
                var right = parentVis.fbr - parentVis.fbl;

                right.Normalize();
                vup.Normalize();
                vup = -vup;

                var cp = Vector3.Cross(right, vup);

                var forward = parentVis.fbl - parentVis.bbl;

                bool isFlipped = false;

                if (Vector3.Dot(cp, forward) > 0)
                {
                    isFlipped = true;
                    forward = forward.normalized;
                }
                else
                {
                    forward = -forward.normalized;
                }

                Transform vt = tempTransformObject.transform;
                vt.rotation = Quaternion.LookRotation(forward, vup);

                Vector3 positionInLocal3DSP = vt.InverseTransformPoint(worldCoordsPoint);

                float x = (positionInLocal3DSP.x + 1) / 2;
                float y = (positionInLocal3DSP.y + 1) / 2;
                float z = (positionInLocal3DSP.z + 1) / 2;

                if (isFlipped)
                {
                    z = 1 - z;
                }

                //find the closest point in the list
                Vector3 pointerPosition3D = new Vector3(x, y, z);
                float localDistance = Vector3.SqrMagnitude(pointerPosition3D - data[i]);
            //     float localDistance = Vector3.Distance(ObjectToWorldDistort3d(data[i], parentTransform,
            //    _ftl,
            //    _ftr,
            //    _fbl,
            //    _fbr,
            //    _btl,
            //    _btr,
            //    _bbl,
            //    _bbr), worldCoordsPoint);

                // Debug.Log("I'm in brush 3d and the local distance is " + localDistance);
                if (localDistance < distance) {
                    brushedIndices[i] = new Vector3(1f, 0f, 0f);
                    

                    // Do the 3D stuff here 
                    // TODO
                    Debug.Log("Haven'nt written the code yet!");
               }
                    //brushedIndexes.Add(i); 
                else
                    brushedIndices[i] = new Vector3(0f, 0f, 0f);
            }
            else
            {   
                // Before doing the inverse transform, make sure the scales are right!

                float xMinNormaliser = parentVis.ReferenceAxis1.horizontal.MinNormaliser;
                float xMaxNormaliser = parentVis.ReferenceAxis1.horizontal.MaxNormaliser;
                float yMinNormaliser = parentVis.ReferenceAxis1.vertical.MinNormaliser;
                float yMaxNormaliser = parentVis.ReferenceAxis1.vertical.MaxNormaliser;

                // New zero is the shift value
                float XnewScale = (xMaxNormaliser - xMinNormaliser); 
                float Xdispalcement = Math.Abs(xMinNormaliser + (XnewScale/2f));
                float YnewScale = (yMaxNormaliser - yMinNormaliser);
                float Ydispalcement = Math.Abs(yMinNormaliser + (YnewScale/2f));
                
                var origParentLocalScale = parentTransform.localScale;
                parentTransform.localScale = Vector3.one;
                var localPoint = parentTransform.InverseTransformPoint(worldCoordsPoint);
                parentTransform.localScale = origParentLocalScale;

                Vector2 hitpoint2D = new Vector2(
                    localPoint.x / _scale.x, 
                    localPoint.y / _scale.y);
                // normally the data[i] members should be between -0.5 and 0.5 (local position)
                // We chose 2 as the multiplying factor cuz it's gonna map -0.5 to -1
                Vector2 ScaledDataPoint = new Vector2(
                    (data[i].x+Xdispalcement)*(1/XnewScale),
                    (data[i].y+Ydispalcement)*(1/YnewScale)
                );
                const float PREV_MIN_NORM = -0.5f;  
                const float PREV_MAX_NORM = 0.5f;  
                Vector2 dataModifiedPoint = new Vector2(data[i].x, data[i].y);
                var d = Vector2.Distance(ScaledDataPoint, hitpoint2D);
                
                if (d < distance) {
                    brushedIndices[i] = new Vector3(1f, 0f, 0f);
                }
                    //brushedIndexes.Add(i); 
                else {
                    brushedIndices[i] = new Vector3(0f, 0f, 0f);
                    // Debug.Log("in else the brushed is " + i + " data is "+ data[i] + " d os "+ d);
                }
            }
        }
        return brushedIndices;
    }

    
    public static Vector3[] BrushedIindicesPointParallelPlot(
        Vector3[] data,
        Vector3 point,
        float distance,
        Vector4 _ftl,
        Vector4 _ftr,
        Vector4 _fbl,
        Vector4 _fbr,
        Vector4 _btl,
        Vector4 _btr,
        Vector4 _bbl,
        Vector4 _bbr,
        Transform parentTransform)
    {
        // Vector3[] brushedIndices = new Vector3[data.Length / 2];
        Vector3[] brushedIndices = new Vector3[data.Length];

        //1st pass 
        for (int i = 0; i < data.Length; i += 2)
        {
            {
                if (Vector3.Distance(GetVertexWorldPosition(data[i], parentTransform), point) < distance)
                //if (Vector3.Distance(ObjectToWorldDistort(data[i], parentTransform,
                //    _ftl,
                //    _ftr,
                //    _fbl,
                //    _fbr,
                //    _btl,
                //    _btr,
                //    _bbl,
                //    _bbr
                //    ), point) < distance)
                {
                    brushedIndices[i] = new Vector3(1f, 0f, 0f);
                    brushedIndices[i + 1] = new Vector3(1f, 0f, 0f);
                }
                else if (Vector3.Distance(GetVertexWorldPosition(data[i + 1], parentTransform), point) < distance)
                //else if (Vector3.Distance(ObjectToWorldDistort(data[i + 1], parentTransform,
                //    _ftl,
                //    _ftr,
                //    _fbl,
                //    _fbr,
                //    _btl,
                //    _btr,
                //    _bbl,
                //    _bbr
                //    ), point) < distance)
                {
                    brushedIndices[i] = new Vector3(1f, 0f, 0f);
                    brushedIndices[i + 1] = new Vector3(1f, 0f, 0f);
                }
                else
                {
                    brushedIndices[i] = new Vector3(0f, 0f, 0f);
                    brushedIndices[i + 1] = new Vector3(0f, 0f, 0f);
                }
            }
        }
        return brushedIndices;
    }

    public static Staxes.Tuple<Vector3, Vector3> DetailOnDemandPCP(Vector3[] data, Vector3 point, float distance, Vector3 bl, Vector3 br, Vector3 tl, Transform parentTransform)
    {
        float[] distances = new float[data.Length];

        for (int i = 0; i < data.Length; i += 2)
        {
            distances[i] = Vector3.Distance(GetVertexWorldPosition(data[i], parentTransform), point);
        }
        float minDist = distances.Min();
        int index = Array.IndexOf(distances, minDist);
        if (index % 2 == 0)
            return new Staxes.Tuple<Vector3, Vector3>(data[index], data[index + 1]);
        else
            return new Staxes.Tuple<Vector3, Vector3>(data[index], data[index - 1]);
    }

    public static Vector3 DetailOnDemandScatterplots(Vector3[] data, Vector3 point, float distance,
        Vector4 _ftl,
        Vector4 _ftr,
        Vector4 _fbl,
        Vector4 _fbr,
        Vector4 _btl,
        Vector4 _btr,
        Vector4 _bbl,
        Vector4 _bbr,
        Transform parentTransform, bool is3D)
    {
        float[] distances = new float[data.Length];

        for (int i = 0; i < data.Length; i++)
        {

            if (is3D)
            {
                distances[i] = (Vector3.Distance(ObjectToWorldDistort3d(data[i], parentTransform,
               _ftl,
               _ftr,
               _fbl,
               _fbr,
               _btl,
               _btr,
               _bbl,
               _bbr), point));
            }
            else
            {
                distances[i] = (Vector3.Distance(ObjectToWorldDistort(data[i], parentTransform,
              _ftl,
              _ftr,
              _fbl,
              _fbr,
              _btl,
              _btr,
              _bbl,
              _bbr), point));
            }

          //  distances[i] = Vector3.Distance(GetVertexWorldPosition(data[i], parentTransform), point);
        }
        float minDist = distances.Min();
        int index = Array.IndexOf(distances, minDist);
        return data[index];
    }

    public static Vector3[] DetailsOnDemand(Vector3[] data, Vector3 point, float distance, Vector3 bl, Vector3 br, Vector3 tl, Transform parentTransform)
    {
        Vector3[] detailData = new Vector3[data.Length];
        float[] distances = new float[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            distances[i] = Vector3.Distance(GetVertexWorldPosition(data[i], parentTransform), point);

            if (Vector3.Distance(GetVertexWorldPosition(data[i], parentTransform), point) < distance)
            //brushedIndexes.Add(i); 
            {
                detailData[i] = data[i];
            }
        }
        return detailData;
    }

    public static List<float> BrushDataBox(Vector3[] data, Vector3 topLeft, Vector3 bottomRight)
    {
        List<float> brushedIndexes = new List<float>();

        for (int i = 0; i < data.Length; i++)
        {
            if (data[i].x > topLeft.x && data[i].x < bottomRight.x
                && data[i].y < topLeft.y && data[i].y > bottomRight.y
                && data[i].z > topLeft.z && data[i].z < bottomRight.z)
                brushedIndexes.Add(i);
        }
        return brushedIndexes;
    }

    //this is really bad, needs to calculate it properly and send the value through the getvertexworldposition function....
    public static Vector3 _scale = new Vector3(0.2660912f, 0.2660912f, 0.2660912f);

    public static Vector3 GetVertexWorldPosition(Vector3 vertex, Transform owner)
    {
        //return owner.localToWorldMatrix.MultiplyPoint3x4(vertex);
        return owner.TransformPoint(Vector3.Scale(vertex, _scale));

    }

    public static Vector3 transformPointToVisualisation(Vector3 point, Vector3 bl, Vector3 br, Vector3 tl)
    {
        return new Vector3(UtilMath.normaliseValue(point.x, -0.5f, 0.5f, br.x, bl.x),
                           UtilMath.normaliseValue(point.y, -0.5f, 0.5f, bl.y, tl.y),
                           UtilMath.normaliseValue(point.z, -0.5f, 0.5f, bl.z, br.z));

    }

    public static Vector3 ObjectToWorldDistort3d(Vector3 pos, Transform _transform,
        Vector4 _ftl,
        Vector4 _ftr,
        Vector4 _fbl,
        Vector4 _fbr,
        Vector4 _btl,
        Vector4 _btr,
        Vector4 _bbl,
        Vector4 _bbr)
    {
        float u = (pos.x + 0.5f);
        float v = 1f - (pos.y + 0.5f);
        float w = pos.z + 0.5f;
        Matrix4x4 M = _transform.localToWorldMatrix;

        Vector3 o = _ftl * (1f - u) * (1f - v) * (1f - w) + _ftr * u * (1f - v) * (1f - w) + _fbl * (1f - u) * v * (1f - w) + _fbr * u * v * (1f - w) +
                    _btl * (1f - u) * (1f - v) * w + _btr * u * (1f - v) * w + _bbl * (1f - u) * v * w + _bbr * u * v * w;

        return (M * new Vector4(o.x, o.y, o.z, 1.0f));
    }

    public static Vector3 ObjectToWorldDistort(Vector3 pos,
        Transform _transform,
        Vector4 _ftl,
        Vector4 _ftr,
        Vector4 _fbl,
        Vector4 _fbr,
        Vector4 _btl,
        Vector4 _btr,
        Vector4 _bbl,
        Vector4 _bbr)
    {
        float u = (pos.x + 0.5f);
        float v = 1f - (pos.y + 0.5f);
        Vector3 o = _ftl * (1f - u) * (1f - v) + _ftr * u * (1f - v) + _fbl * (1f - u) * v + _fbr * u * v;
        return (_transform.localToWorldMatrix * new Vector4(o.x, o.y, o.z, 1.0f));
    }

    public void OnComponentValueChange(float value)
    {
        brushSize = Mathf.Abs(value / 2f);
        // Debug.Log("brush " + brushSize);
    }
}
