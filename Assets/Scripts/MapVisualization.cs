using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapVisualization : MonoBehaviour
{
    //[SerializeField]
    AbstractMap _map;
    public Visualization _viz;
    //   public Visualisation HospitalViz;
    //public CSVDataSource mySourceData;


    //private  variables 
    private string xExtremeAxis;
    private string zExtremeAxis;
    private string center;
    private Vector3 velocity = Vector3.zero;

    private DataBinding.DataObject mySourceData;


    void Start()
    {

        Mercator mProj = new Mercator();

        // obtaining the  maximum and minimum latitiude and longitude from the graph 

        if (_viz == null)
        {
            _viz = this.gameObject.GetComponent<Visualization>();
            mySourceData = SceneManager.Instance.dataObject;
        }
        int x_data_dim =  _viz.ReferenceAxis1.horizontal.axisId;
        int z_data_dim = _viz.ReferenceAxis1.depth.axisId;


        float maxlongitute = float.Parse(mySourceData.getOriginalValue(mySourceData.DimensionsRange[x_data_dim].y, x_data_dim) + "");
        float maxlatitude = float.Parse(mySourceData.getOriginalValue(mySourceData.DimensionsRange[z_data_dim].y, z_data_dim) + "");
        float minlongitute = float.Parse(mySourceData.getOriginalValue(mySourceData.DimensionsRange[x_data_dim].x, x_data_dim) + "");
        float minlatitude = float.Parse(mySourceData.getOriginalValue(mySourceData.DimensionsRange[z_data_dim].x, z_data_dim) + "");


        //GeoCoordinate 
        float[] topLeft = mProj.latLonToMeters(minlatitude, minlongitute);
        float[] topright = mProj.latLonToMeters(minlatitude, maxlongitute);
        float[] bottomLeft = mProj.latLonToMeters(maxlatitude, minlongitute);
        float[] bottomRight = mProj.latLonToMeters(maxlatitude, maxlongitute);


        Vector2d centerMap = new Vector2d(minlatitude + (maxlatitude - minlatitude) / 2.0, minlongitute + (maxlongitute - minlongitute) / 2.0);

        float leftRightDistance = this.distance(topLeft[0], topLeft[1], topright[0], topright[1]);
        float topBottomDistance = this.distance(topLeft[0], topLeft[1], bottomLeft[0], bottomLeft[1]);

        float maxdist = Mathf.Max(leftRightDistance, topBottomDistance);

        float pixelDist = 3 * 256;

        int lastgoodZoom = 0;

        for (int i = 0; i < 17; i++)
        {
            float realSize = 256 * (maxdist / 40000000) * Mathf.Pow(2, i);
            if (realSize < pixelDist)
            {
                lastgoodZoom = i;
            }
        }
        Debug.Log("Appropriate Zoom level: " + lastgoodZoom);
        //_map.ResetMap();

        GameObject mapGo = new GameObject("Map_GO");
        mapGo.transform.parent = this.transform;
        mapGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        _map = mapGo.AddComponent<AbstractMap>();

        _map.Initialize(centerMap, lastgoodZoom);

        // calculating the coordinates for the x-axis end point, z-axis end point  and the center
        xExtremeAxis = maxlatitude + "," + minlongitute;
        zExtremeAxis = minlatitude + "," + maxlongitute;
        center = minlatitude + "," + minlongitute;

        Debug.Log("ExtremeValue: " + center + " - " + xExtremeAxis + " - " + zExtremeAxis);

        // coverting the coordinates into geolocation 
        Vector3 xExtremeAxisGeo = _map.GeoToWorldPosition(Conversions.StringToLatLon(xExtremeAxis), true);
        Vector3 zExtremeAxisGeo = _map.GeoToWorldPosition(Conversions.StringToLatLon(zExtremeAxis), true);
        Vector3 centerGeo = _map.GeoToWorldPosition(Conversions.StringToLatLon(center), true);

        // Assigning the position to the visulization by making center of the visulization fixed  

        Vector3 mapPos = _map.transform.position;
        _viz.transform.position = centerGeo;
        _map.transform.position = mapPos;
        // calculating the length of x and z axis for width and depth of the graph respectively  
        var width = (centerGeo - zExtremeAxisGeo).magnitude;
        var Depth = (centerGeo - xExtremeAxisGeo).magnitude;
        ////////////////// TODO: add these back in later
        //_viz.width = width;
        //_viz.depth = Depth;
        //_map.
        //  height of the graph
        // when z-axis is not defined 
        //if (!_viz.ReferenceAxis1.depth)
        //    _viz.height = _map.Options.locationOptions.zoom / 5;

        //}
        //else // when z-axis is defined 
        //{
        //    _viz.height = _viz.zDimension.maxScale;

        //}


        //_viz.gameObject.GetComponent<ScatterplotVisualisation>().UpdateVisualisationAxes(AbstractVisualisation.PropertyType.Scaling);

        // function for map update 
        _map.OnUpdated += delegate
        {
            UpdateMap();

        };

    }


    public void UpdateMap()
    {
        Debug.Log("Map Updated");
        // update the geolocation of the graph according to change in the Map
        Vector3 xExtremeAxisGeo = _map.GeoToWorldPosition(Conversions.StringToLatLon(xExtremeAxis), true);
        Vector3 zExtremeAxisGeo = _map.GeoToWorldPosition(Conversions.StringToLatLon(zExtremeAxis), true);
        Vector3 centerGeo = _map.GeoToWorldPosition(Conversions.StringToLatLon(center), true);

        var width = (centerGeo - zExtremeAxisGeo).magnitude;
        var Depth = (centerGeo - xExtremeAxisGeo).magnitude;

        //_viz.width = width;
        //_viz.depth = Depth;
        Vector3 mapPos = _map.transform.position;
        _viz.transform.position = centerGeo;
        _map.transform.position = mapPos;
        //_viz.visualisationReference.updateViewProperties(AbstractVisualisation.PropertyType.Scaling);
        //   ScatterplotVisualisation svis = (ScatterplotVisualisation) (_viz);
        //_viz.gameObject.GetComponent<ScatterplotVisualisation>().UpdateVisualisationAxes(AbstractVisualisation.PropertyType.Scaling);

    }


    private void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            UpdateMap();
        }
    }

    private float distance(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt(Mathf.Pow(x2 - x1, 2) + Mathf.Pow(y2 - y1, 2));
    }
}
