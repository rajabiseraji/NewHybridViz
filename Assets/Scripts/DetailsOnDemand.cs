using Staxes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DetailsOnDemand : MonoBehaviour
{

    Vector3 Center = Vector3.zero;
    Vector3 pointerPosition;
    Vector3 localPointerPosition;

    int maxDetails = 1;
    GameObject labelDetails;
    GameObject textMesh;

    Vector3 tl = Vector3.zero;
    Vector3 tr = Vector3.zero;
    Vector3 bl = Vector3.zero;
    Vector3 br = Vector3.zero;

    string xDimension = "";
    string yDimension = "";
    string zDimension = "";

    Vector3 poitionInWorld = Vector3.one;
    System.Tuple<Vector3, Vector3> tuplePCPWorld;
    System.Tuple<Vector3, Vector3> tuplePCPData;

    Transform parentTransform;
    public LineRenderer leaderInformation;

    Visualization visualizationReference = null;

    public Visualization VisualizationReference
    {
        get { return visualizationReference; }
        set { visualizationReference = value; }
    }

    bool isParallelView = false;

    public bool IsParallelView
    {
        get
        {
            return isParallelView;
        }

        set
        {
            isParallelView = value;
        }
    }

    public void setCorners(Vector3 _tl, Vector3 _tr, Vector3 _bl, Vector3 _br)
    {
        tl = _tl;
        tr = _tr;
        bl = _bl;
        br = _br;
    }

    public void setDataPCP(System.Tuple<Vector3, Vector3> _tuplePCPData)
    {
        tuplePCPData = _tuplePCPData;
    }

    public void setTuplePCPWorld(System.Tuple<Vector3, Vector3> tupleV3PCP)
    {
        tuplePCPWorld = tupleV3PCP;
    }

    public void setPositionInWorldScatterplot(Vector3 p)
    {
        poitionInWorld = p;
    }

    public void setCenter(Vector3 center)
    {
        Center = center;

    }

    public void setPointerPosition(Vector3 _pointerPosition)
    {
        pointerPosition = _pointerPosition;
    }

    public void setLocalPointerPosition(Vector3 _localPointerPosition)
    {
        localPointerPosition = _localPointerPosition;
    }

    private void Awake()
    {
        // Q: why this local scale??
        tempTransformObject = new GameObject("Brush Transform");
        tempTransformObject.transform.parent = transform;
        tempTransformObject.transform.localPosition = Vector3.zero;
        tempTransformObject.transform.localScale = new Vector3(Axis.AXIS_ROD_LENGTH, Axis.AXIS_ROD_LENGTH, Axis.AXIS_ROD_LENGTH) / 2;
    }

    void Start()
    {
        labelDetails = GameObject.CreatePrimitive(PrimitiveType.Quad);
        labelDetails.transform.localScale = new Vector3(0.04f, 0.01f, 1f);
        labelDetails.transform.parent = transform;

        Material m = new Material(Shader.Find("Standard"));
        m.SetFloat("_Mode", 2);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        m.color = new Color(1.0f, 1.0f, 1.0f, 0.7f);
        m.name = "MyCustomMaterial";

        labelDetails.GetComponent<MeshRenderer>().material = m;

        textMesh = new GameObject();
        textMesh.transform.parent = transform;
        textMesh.transform.localScale = Vector3.one / 100f;
        textMesh.AddComponent<TextMesh>();
        textMesh.GetComponent<TextMesh>().color = Color.black;
        textMesh.GetComponent<TextMesh>().fontSize = 25;

        string[] dimensionVisualisation = transform.name.Split('-');

        xDimension = dimensionVisualisation[0];
        yDimension = dimensionVisualisation[1];

        leaderInformation = gameObject.AddComponent<LineRenderer>();

        leaderInformation.material = new Material(Shader.Find("ColorPicker/SolidColor"));
        //leaderInformation.widthMultiplier = 0.0015f;
        leaderInformation.positionCount = 2;
        leaderInformation.useWorldSpace = true;
        leaderInformation.widthCurve = AnimationCurve.Linear(0, 0.0015f, 1, 0.0015f);

        // A simple 2 color gradient with a fixed alpha of 1.0f.
        float alpha = 1.0f;
        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.blue, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
            );

        //leaderInformation.colorGradient = gradient;
        //labelDetails[0].transform.position = 2f * (Vector3.one);
    }
    
    float precisionSearch = 10E-6f;

    // takes a dimension name and an index of a datapoint, and returns a string value for that dimension 
    string StringValFromDataObj(DataBinding.DataObject dataObj, string dimensionName, int index)
    {
        float xval = dataObj.getOriginalDimension(dimensionName)[index];
        string xvalstr = xval.ToString();
        if (dataObj.TypeDimensionDictionary1[dataObj.dimensionToIndex(dimensionName)] == "string")
        {
            xvalstr = dataObj.TextualDimensions[xval];
        };
        return xvalstr;
    }

    GameObject cube;

    public void OnDetailOnDemand2D()
    {
        textMesh.SetActive(true);
        labelDetails.SetActive(true);

        // Dimensions range is the range of the data in original data values! 
        // 

        Vector2 rangeX = SceneManager.Instance.dataObject.DimensionsRange[SceneManager.Instance.dataObject.dimensionToIndex(xDimension)];
        Vector2 rangeY = SceneManager.Instance.dataObject.DimensionsRange[SceneManager.Instance.dataObject.dimensionToIndex(yDimension)];
        string values = "";

        if (pointerPosition != null && labelDetails != null && textMesh != null)
        {
            // register action for logger
            DataLogger.Instance.LogActionData("DoD2D", visualizationReference.gameObject, gameObject);


            if (!isParallelView)
            {
                textMesh.transform.position = (pointerPosition);

                // This is because they've set the local scale to 0.26..f
                // these valuse will be between -0.5 and 0.5
                // local pointer position is somewhere between -0.11 and 0.11


                // IMPORANT POINT
                // The points that are drawin in the vertex shaders are from -0.45 to 0.45

                float xMinNormaliser = visualizationReference.ReferenceAxis1.horizontal.MinNormaliser;
                float xMaxNormaliser = visualizationReference.ReferenceAxis1.horizontal.MaxNormaliser;
                float yMinNormaliser = visualizationReference.ReferenceAxis1.vertical.MinNormaliser;
                float yMaxNormaliser = visualizationReference.ReferenceAxis1.vertical.MaxNormaliser;

                float XnewScale = 1/ (xMaxNormaliser - xMinNormaliser); 
                float YnewScale = 1 / (yMaxNormaliser - yMinNormaliser);


                var origParentLocalScale = parentTransform.localScale;
                parentTransform.localScale = Vector3.one;
                var localPoint = parentTransform.InverseTransformPoint(pointerPosition);
                parentTransform.localScale = origParentLocalScale;

                // we don't even touch this hitpoint2D, it's as good as possible
                // it gives us a number [data[i].min, data[i].max] which means [-0.5, 0.5]
                // TODO: make this also dynamic, so that we can change it if we want
                Vector2 hitpoint2D = new Vector2(
                    localPoint.x / BrushingAndLinking._scale.x,
                    localPoint.y / BrushingAndLinking._scale.y);

                //List<float> distances = new List<float>();

                // We need to map the min of FilteredColX to min of localPosition (which is about 0.11f) and the do the same of the max of it too.
                // The same for Y too

                // All the filtered values are between 0 and 1 too
                float[] filteredXcol = visualizationReference.getFilteredDimensionForIndexSearch(SceneManager.Instance.dataObject.dimensionToIndex(xDimension));
                float[] filteredYcol = visualizationReference.getFilteredDimensionForIndexSearch(SceneManager.Instance.dataObject.dimensionToIndex(yDimension));

                // if 1 means we should ignore, if not then we can use
                float[] isFiltered = visualizationReference.getFirstScatterplotView().getFilterChannelData();

                



                float currentShortestDistance = 900;

                int index = 0;

                //float prevDistance = 900f;
                for (int i = 0; i < filteredXcol.Length; i++)
                {
                    float SHIFT_FORWARD_VALUE = Math.Abs(BrushingAndLinking.PREV_AXIS_MIN_NORM);

                    var dataXDistanceWithNewMin = filteredXcol[i] - BrushingAndLinking.shift(xMinNormaliser, SHIFT_FORWARD_VALUE);
                    var dataYDistanceWithNewMin = filteredYcol[i] - BrushingAndLinking.shift(yMinNormaliser, SHIFT_FORWARD_VALUE); ;

                    var dataXScale = (BrushingAndLinking.PREV_AXIS_MAX_NORM - BrushingAndLinking.PREV_AXIS_MIN_NORM) / (xMaxNormaliser - xMinNormaliser);
                    var dataYScale = (BrushingAndLinking.PREV_AXIS_MAX_NORM - BrushingAndLinking.PREV_AXIS_MIN_NORM) / (yMaxNormaliser - yMinNormaliser);


                    Vector2 ScaledDataPoint = new Vector2(
                        dataXDistanceWithNewMin * dataXScale,
                        dataYDistanceWithNewMin * dataYScale
                    ) - (SHIFT_FORWARD_VALUE * Vector2.one);


                    //distances.Add(Vector2.Distance(ScaledDataPoint, hitpoint2D));
                    var distance = Vector2.Distance(ScaledDataPoint, hitpoint2D);
                    if (distance < currentShortestDistance)
                    {
                        currentShortestDistance = distance;
                        index = i;
                    }


                }
                //int index = distances.FindIndex(d => d < distances.Min() + precisionSearch && d > distances.Min() - precisionSearch);
                

                var dataObj = SceneManager.Instance.dataObject;

                string xvalstr = StringValFromDataObj(dataObj, xDimension, index);
                string yvalstr = StringValFromDataObj(dataObj, yDimension, index);

                values = string.Format(@"{0}:{1} {2} {3}:{4}",
                    xDimension,
                    xvalstr,
                    Environment.NewLine,
                    yDimension,
                    yvalstr);

               

                //var normalisedX = UtilMath.normaliseValue(filteredXcol[index], xMinNormaliser, xMaxNormaliser, -0.5f, 0.5f);
                
                //// filteredXcol[index] * XnewScale;
                //var normalisedY = UtilMath.normaliseValue(filteredYcol[index], yMinNormaliser, yMaxNormaliser, -0.5f, 0.5f);
                

                //var test = String.Format("Normalised x is: {0} \n Before X was: {1} Normalised x is: {2} \n Before Y was: {3}", normalisedX, SceneManager.Instance.dataObject.getDimension(xDimension)[index]-0.5f, normalisedY, SceneManager.Instance.dataObject.getDimension(yDimension)[index] - 0.5f);

                // Debug.Log(test);
                // Debug.Log(xMinNormaliser);
                // Debug.Log(xMaxNormaliser);
                // Debug.Log(XnewScale);
                // Debug.Log(yMinNormaliser);
                // Debug.Log(localPointerPosition.x);
                // Debug.Log(localPointerPosition.y);

                Vector3 worldSpacePoint = transform.TransformPoint(localPointerPosition.x,localPointerPosition.y, 0f);

                // get the vertex that pertains to the found index
                // this index is between -.5 and 0.5
                var foundVertex = visualizationReference.getScatterplot2DGameobject().GetComponentInChildren<MeshFilter>().mesh.vertices[index];

                // this just works flawlessly for points that have not been filtered and scaled
                var localPointForVertex = new Vector2(
                    foundVertex.x * (BrushingAndLinking._scale.x - 0.0266091f),
                    foundVertex.y * (BrushingAndLinking._scale.y - 0.0266091f)
                );

                parentTransform.localScale = Vector3.one;
                var worldPointForVertex = parentTransform.TransformPoint(localPointForVertex);
                parentTransform.localScale = origParentLocalScale;

                ///////////////////////// For when we have scaling
                ///

                // this will get us a point between -.5 and 0.5
                var newVertexPointForFiltering = new Vector2(
                    BrushingAndLinking.ScaleDataPoint(foundVertex.x, xMinNormaliser, xMaxNormaliser),
                    BrushingAndLinking.ScaleDataPoint(foundVertex.y, yMinNormaliser, yMaxNormaliser)
                );

                var localPointForVertexForFiltering = new Vector2(
                    newVertexPointForFiltering.x * (BrushingAndLinking._scale.x - 0.0266091f),
                    newVertexPointForFiltering.y * (BrushingAndLinking._scale.y - 0.0266091f)
                );

                parentTransform.localScale = Vector3.one;
                var worldPointForVertexForFiltering = parentTransform.TransformPoint(localPointForVertexForFiltering);
                parentTransform.localScale = origParentLocalScale;



                if (cube == null)
                {
                    cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.localScale = Vector3.one * 0.005f;
                    cube.transform.parent = transform;
                    cube.GetComponent<Renderer>().material.color = Color.red;
                }
                cube.transform.localPosition = localPointForVertexForFiltering;

                leaderInformation.SetPosition(0, pointerPosition);
                    leaderInformation.SetPosition(1, worldPointForVertexForFiltering);
                    leaderInformation.widthCurve = AnimationCurve.Linear(0, 0.0015f, 1, 0.0015f);

            }
            else
            {
                textMesh.transform.position = (pointerPosition);
                //get the axis from the PCP
                values = "PCP";
                // details on demand for PCP
                if (tuplePCPData.Item1.x < 0f)
                {
                    values = "PCP";// UtilMath.normaliseValue(tuplePCPWorld.Item1.x, -0.5f, 0.5f, rangeX.x, rangeX.y).ToString();
                }
            }

            textMesh.GetComponentInChildren<TextMesh>().text = values;
            textMesh.transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up);

            labelDetails.transform.Translate(labelDetails.transform.localScale.x / 2f + 0.5f,
                                                  -labelDetails.transform.localScale.y / 2f + 0.5f, 0.005f);
        }
    }
    
    GameObject tempTransformObject = null;
    GameObject dod3DCube = null;

    public void OnDetailOnDemand3D()
    {
        textMesh.SetActive(true);
        string[] dimensionVisualisation = transform.name.Split('-');

        xDimension = dimensionVisualisation[0];
        yDimension = dimensionVisualisation[1];
        zDimension = dimensionVisualisation[2];
        zDimension = zDimension.Split(' ')[0];

        Vector2 rangeX = SceneManager.Instance.dataObject.DimensionsRange[SceneManager.Instance.dataObject.dimensionToIndex(xDimension)];
        Vector2 rangeY = SceneManager.Instance.dataObject.DimensionsRange[SceneManager.Instance.dataObject.dimensionToIndex(yDimension)];
        Vector2 rangeZ = SceneManager.Instance.dataObject.DimensionsRange[SceneManager.Instance.dataObject.dimensionToIndex(zDimension)];

        labelDetails.SetActive(true);
        
        if (pointerPosition != null && labelDetails != null && textMesh != null)
        {

            // register action for logger
            DataLogger.Instance.LogActionData("DoD3D", visualizationReference.gameObject, gameObject);


            if (!isParallelView)
            {
                textMesh.transform.position = (pointerPosition) + (textMesh.transform.right * 0.05f);

                string values = "";

                float xMinNormaliser = visualizationReference.ReferenceAxis1.horizontal.MinNormaliser;
                float xMaxNormaliser = visualizationReference.ReferenceAxis1.horizontal.MaxNormaliser;
                float yMinNormaliser = visualizationReference.ReferenceAxis1.vertical.MinNormaliser;
                float yMaxNormaliser = visualizationReference.ReferenceAxis1.vertical.MaxNormaliser;
                float zMinNormaliser = visualizationReference.ReferenceAxis1.depth.MinNormaliser;
                float zMaxNormaliser = visualizationReference.ReferenceAxis1.depth.MaxNormaliser;

                // create a transform for the visualisation space
                var vup = visualizationReference.fbl - visualizationReference.ftl;
                var right = visualizationReference.fbr - visualizationReference.fbl;

                right.Normalize();
                vup.Normalize();
                vup = -vup;

                var cp = Vector3.Cross(right, vup);

                var forward = visualizationReference.fbl - visualizationReference.bbl;

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

                // this is always between -1 , 1
                Vector3 positionInLocal3DSP = vt.InverseTransformPoint(pointerPosition);

                float x = (positionInLocal3DSP.x) / 2;
                float y = (positionInLocal3DSP.y) / 2;
                float z = (positionInLocal3DSP.z) / 2;

                if (isFlipped)
                {
                    z = -1 * z;
                }

                //find the closest point in the list 
                Vector3 pointerPosition3D = new Vector3(x, y, z);


                List<float> distances = new List<float>();

                float minDistance = float.MaxValue;

                // we should pass isFiltered and also the raw dimensions to the compute shader
                // then ask it to find the distance of our point with all the points in the array and 
                // put in another array, then sort that array and give us the top member
                // 

                // these are between 0 and 1 and for our calcs they should be between -.5 and 0.5
                float[] filteredXcol = visualizationReference.getFilteredDimensionForIndexSearch(SceneManager.Instance.dataObject.dimensionToIndex(xDimension));
                float[] filteredYcol = visualizationReference.getFilteredDimensionForIndexSearch(SceneManager.Instance.dataObject.dimensionToIndex(yDimension));
                float[] filteredZcol = visualizationReference.getFilteredDimensionForIndexSearch(SceneManager.Instance.dataObject.dimensionToIndex(zDimension));

                float currentShortestDistance = 900;
                int index = 0;

                for (int i = 0; i < filteredXcol.Length; i++)
                {
                    
                    Vector3 scaledDataPosition = new Vector3(
                        BrushingAndLinking.ScaleDataPoint(filteredXcol[i] - 0.5f, xMinNormaliser, xMaxNormaliser),
                        BrushingAndLinking.ScaleDataPoint(filteredYcol[i] - 0.5f, yMinNormaliser, yMaxNormaliser),
                        BrushingAndLinking.ScaleDataPoint(filteredZcol[i] - 0.5f, zMinNormaliser, zMaxNormaliser)
                    );

                    //distances.Add(Vector3.SqrMagnitude(pointerPosition3D - scaledDataPosition));
                    var distance = Vector3.SqrMagnitude(pointerPosition3D - scaledDataPosition);
                    if (distance < currentShortestDistance)
                    {
                        currentShortestDistance = distance;
                        index = i;
                    }
                }

                //int index = distances.FindIndex(d => d < distances.Min() + precisionSearch && d > distances.Min() - precisionSearch);

                var dataObj = SceneManager.Instance.dataObject;

                string xvalstr = StringValFromDataObj(dataObj, xDimension, index);
                string yvalstr = StringValFromDataObj(dataObj, yDimension, index);
                string zvalstr = StringValFromDataObj(dataObj, zDimension, index);

                values = string.Format(@"{0}:{1} {2} {3}:{4} {5} {6}:{7}",
                    xDimension, xvalstr,
                    Environment.NewLine,
                    yDimension, yvalstr,
                    Environment.NewLine,
                    zDimension, zvalstr);

                // this is between -.5 and 0.5
                var foundVertex = visualizationReference.getScatterplot3DGameobject().GetComponentInChildren<MeshFilter>().mesh.vertices[index];

                //if (isFlipped)
                //{
                //    foundVertex.z = -1 * foundVertex.z;
                //}


                Vector3 newVertexPointForFiltering = new Vector3(
                    BrushingAndLinking.ScaleDataPoint(foundVertex.x, xMinNormaliser, xMaxNormaliser),
                    BrushingAndLinking.ScaleDataPoint(foundVertex.y, yMinNormaliser, yMaxNormaliser),
                    BrushingAndLinking.ScaleDataPoint(foundVertex.z, zMinNormaliser, zMaxNormaliser)
                );

                if (isFlipped)
                {
                    newVertexPointForFiltering.z = -1 * newVertexPointForFiltering.z;
                }

                var localPointForVertexForFiltering = new Vector3(
                    newVertexPointForFiltering.x * (2f - 0.2f),
                    newVertexPointForFiltering.y * (2f - 0.2f),
                    newVertexPointForFiltering.z * (2f - 0.2f)
                );


                Vector3 worldPointForVertexForFiltering = vt.TransformPoint(localPointForVertexForFiltering);

                if (dod3DCube == null)
                {
                    dod3DCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dod3DCube.transform.localScale = Vector3.one * 0.005f;
                    dod3DCube.transform.parent = vt;
                    dod3DCube.GetComponent<Renderer>().material.color = Color.yellow;
                }
                dod3DCube.transform.localPosition = localPointForVertexForFiltering;

                leaderInformation.SetPosition(0, pointerPosition);
                leaderInformation.SetPosition(1, worldPointForVertexForFiltering);
                leaderInformation.widthCurve = AnimationCurve.Linear(0, 0.0015f, 1, 0.0015f);

                textMesh.GetComponentInChildren<TextMesh>().text = values;
                textMesh.transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
            Vector3.up);// Camera.main.transform.rotation * Vector3.up);

            }
            else
            {
                textMesh.transform.position =
                        labelDetails.transform.position =
                        tuplePCPWorld.Item1 - (tuplePCPWorld.Item1 - Center);

                // details on demand for PCP
                if (tuplePCPData.Item1.x < 0f)
                {
                    float valueLeft = UtilMath.normaliseValue(tuplePCPWorld.Item1.x, -0.5f, 0.5f, rangeX.x, rangeX.y);
                }
            }

            labelDetails.transform.Translate(
                (labelDetails.transform.localScale.x / 2f) ,
                (-labelDetails.transform.localScale.y / 2f ), 
                0.005f);
        }
    }

    public object getValueFromDimension(float value, int dimension)
    {
        if (SceneManager.Instance.dataObject.TypeDimensionDictionary1[dimension] == "string")
        {
            Vector2 range = SceneManager.Instance.dataObject.DimensionsRange[dimension];
            float lerpedValue = Mathf.Lerp(range.x, range.y, value);
            float closest = UtilMath.ClosestTo(SceneManager.Instance.dataObject.TextualDimensions.Keys.ToList(), lerpedValue);
            return SceneManager.Instance.dataObject.TextualDimensions[closest].ToString();
        }
        else
            return SceneManager.Instance.dataObject.getOriginalValue(value, dimension);
    }

    internal void OnDetailOnDemandEnd()
    {
        textMesh.SetActive(false);
        labelDetails.SetActive(false);

        leaderInformation.SetPosition(0, Vector3.zero);
        leaderInformation.SetPosition(1, Vector3.zero);
    }

    Vector3 transformPointToVisualisation(Vector3 point)
    {
        return new Vector3(UtilMath.normaliseValue(point.x, -0.5f, 0.5f, br.x, bl.x),
                           UtilMath.normaliseValue(point.y, -0.5f, 0.5f, bl.y, tl.y),
                           UtilMath.normaliseValue(point.z, -0.5f, 0.5f, bl.z, br.z));
    }

    internal void setTransformParent(Transform transform)
    {
        parentTransform = transform;
    }


}
