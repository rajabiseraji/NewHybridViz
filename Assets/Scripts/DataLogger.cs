using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.IO;
using System.Linq;


public class DataLogger: MonoBehaviour 
{
    public struct ObjectInfo
    {
        public string ObjectName;
        public string OriginalObjectOwner;
        public string ObjectOwner;
        public string ObjectID;
        public string DistanceFromRightController;
        public string DistanceFromLeftController;
    }

    [Serializable]
    public struct Work
    {
        public string workName;
        public string workDescription;
    }

    static DataLogger _instance;
    public static DataLogger Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<DataLogger>()); }
    }

    [SerializeField]
    public float timeBetweenLogs = 0.050f;
    [SerializeField]
    public string filePath = "Assets/Resources/Logs/";
    [SerializeField]
    public int groupID;
    [SerializeField]
    public int participantID;
    [SerializeField]
    public List<Work> tasks;

    public int taskID;

    public bool isMasterLogger = true;
    public bool isLoggingPlayerData = true;

    private StreamWriter playerStreamWriter;
    private StreamWriter actionsStreamWriter;
    private StreamWriter objectStreamWriter;
    private StreamWriter codapStreamWriter;

    private bool isLogging = false;
    private float time;
    private string format = "F4";

    private double startTime;
    public Transform headset;
    public Transform leftController;
    public Transform rightController;
    public WandController leftWand;
    public WandController rightWand;
    private Transform observer;
    //private VRTK_ControllerEvents leftControllerEvents;
    //private VRTK_ControllerEvents rightControllerEvents;
    private Vector3 headPos;
    private Quaternion headRot;
    private Vector3 leftPos;
    private Quaternion leftRot;
    private Vector3 rightPos;
    private Quaternion rightRot;
    private RaycastHit gazeRaycast;
    private RaycastHit leftRaycast;
    private RaycastHit rightRaycast;
    private bool gazeHit;
    private bool leftHit;
    private bool rightHit;
    private ObjectInfo gazeInfo;
    private ObjectInfo leftInfo;
    private ObjectInfo rightInfo;
    private ObjectInfo rightGrabbedObjectInfo;
    private ObjectInfo leftGrabbedObjectInfo;

    public void StartLogging()
    {
        if (isMasterLogger)
        {
            StartLogging(groupID, taskID, tasks[taskID].workName, tasks[taskID].workDescription);
        }
    }

    private void StartLogging(int groupID, int taskID, string taskName, string taskDescription)
    {
        isLogging = true;

        this.groupID = groupID;
        this.taskID = taskID;

        if (!filePath.EndsWith("\\"))
            filePath += "\\";

        if (isMasterLogger)
        {
            // Objects
            string path = string.Format("{0}Group{1}_Task{2}_ObjectData.txt", filePath, groupID, tasks[taskID].workName);
            objectStreamWriter = new StreamWriter(path, true);

            /**
             
             SceneManager.Instance.globalFilters.Count(),
                    JsonUtility.ToJson(SceneManager.Instance.globalFilters),
                    vis.axesCount,
                    vis.ReferenceAxis1.horizontal,
                    vis.ReferenceAxis1.vertical,
                    vis.ReferenceAxis1.horizontal2,
                    vis.ReferenceAxis1.vertical2,
                    vis.ReferenceAxis1.depth2,
                    vis.viewType


            timestamp,
                    "Axis",
                    axis.name,
                    axis.axisId,
                    axis.GetInstanceID().ToString(),
                    axis.transform.position.x.ToString(format),
                    axis.transform.position.y.ToString(format),
                    axis.transform.position.z.ToString(format),
                    axis.transform.rotation.x.ToString(format),
                    axis.transform.rotation.y.ToString(format),
                    axis.transform.rotation.z.ToString(format),
                    axis.transform.rotation.w.ToString(format),
                    axis.AttributeFilters.Count(),
                    JsonUtility.ToJson(axis.AttributeFilters),
                    axis.correspondingVisColorAxisId,
                    axis.correspondingVisSizeAxisId,
                    axis.MinNormaliser,
                    axis.MaxNormaliser
             
             */

            // Write header for object data
            objectStreamWriter.WriteLine(
                "Timestamp" + 
                "\tObjectType" + 
                "\tObjectGameName" + 
                "\tObjectId" + 
                "\tObjectGameInstanceId" + 
                "\tPosition.x" + 
                "\tPosition.y" + 
                "\tPosition.z" + 
                "\tRotation.x" + 
                "\tRotation.y" + 
                "\tRotation.z" + 
                "\tRotation.w" + 
                "\tlocalFiltersCount" + 
                "\tlocalFiltersJson" + 
                "\tcolorAxisId" +
                "\tsizeAxisId" + 
                "\taxisOnlyMinNormaliser" + 
                "\taxisOnlyMaxNormaliser" + 
                "\tvisOnlyGlobalFilterCount" + 
                "\tvisOnlyGlobalFilterJson" + 
                "\tvisOnlyAxesCount" + 
                "\tvisOnlyHorizontalAxis" + 
                "\tvisOnlyVerticalAxis" + 
                "\tvisOnlyDepthAxis" + 
                "\tvisOnlyHorizontal2Axis" + 
                "\tvisOnlyVertical2Axis" + 
                "\tvisOnlyDepth2Axis" + 
                "\tvisOnlyViewType"
            );

            // Save references of logged entities
            //dashboards = FindObjectsOfType<Panel>().ToList();
            //if (FindObjectOfType<KeyboardAndMouseAvatar>() != null)
            //    observer = FindObjectOfType<KeyboardAndMouseAvatar>().transform;
            //markers = FindObjectsOfType<MarkerScript>().ToList();
            //erasers = FindObjectsOfType<EraserScript>().ToList();

            // Annotations
            path = string.Format("{0}Group{1}_Task{2}_AnnotationData.txt", filePath, groupID, tasks[taskID].workName);
            //annotationsStreamWriter = new StreamWriter(path, true);

            // Write header for annotation data
            //annotationsStreamWriter.WriteLine("Timestamp\tOriginalOwner\tOwner\tPositions\tParentChart");

        }
        if (!isMasterLogger || (isMasterLogger && isLoggingPlayerData))
        {
            string path = string.Format("{0}Group{1}_Task{2}_Participant{3}_PlayerData.txt", filePath, groupID, tasks[taskID].workName, participantID);
            playerStreamWriter = new StreamWriter(path, true);

            // Write header for player data
            playerStreamWriter.WriteLine(
                "Timestamp\t" +
                "HeadPosition.x\tHeadPosition.y\tHeadPosition.z\t" +
                "HeadRotation.x\tHeadRotation.y\tHeadRotation.z\tHeadRotation.w\t" +
                "LeftPosition.x\tLeftPosition.y\tLeftPosition.z\t" +
                "LeftRotation.x\tLeftRotation.y\tLeftRotation.z\tLeftRotation.w\t" +
                "LeftTrigger\tLeftGrip\tLeftTouchpad\tLeftTouchpadAngle\t" +
                "RightPosition.x\tRightPosition.y\tRightPosition.z\t" +
                "RightRotation.x\tRightRotation.y\tRightRotation.z\tRightRotation.w\t" +
                "RightTrigger\tRightGrip\tRightTouchpad\tRightTouchpadAngle\t" +
                "GazeObject\tGazeObjectOriginalOwner\tGazeObjectOwner\tGazeObjectID\t" +
                "LeftPointObject\tLeftPointObjectOriginalOwner\tLeftPointObjectOwner\t" + 
                "LeftPointObjectID\tLeftPointObjectDistanceFromLeftController\t" +
                "LeftGrabbedObject\tLeftGrabbedObjectOriginalOwner\tLeftGrabbedObjectOwner\t" +
                "LeftGrabbedObjectID\tLeftGrabbedObjectDistanceFromLeftController\t" +
                "RightPointObject\tRightPointObjectOriginalOwner\tRightPointObjectOwner\t" +
                "RightPointObjectID\tRightPointObjectDistanceFromRightController" +
                "RightGrabbedObject\tRightGrabbedObjectOriginalOwner" +
                "RightGrabbedObjectOwner\tRightGrabbedObjectID" +
                "RightGrabbedObjectDistanceFromRightController"
            );

            path = string.Format("{0}Group{1}_Task{2}_Participant{3}_ActionData.txt", filePath, groupID, tasks[taskID].workName, participantID);
            actionsStreamWriter = new StreamWriter(path, true);

            /**
             * Time.realtimeSinceStartup.ToString("F3"),
                actionName,
                sourceObj.GetType(),
                sourceObj.name,
                sourceObj.GetInstanceID(),
                targetObj.GetType(),
                targetObj.name,
                targetObj.GetInstanceID()
             * 
             * **/

            // Write header for action data
            actionsStreamWriter.WriteLine("Timestamp\tActionName\tActionSourceObj\tActionSourceObjName\tActionSourceObjID\tActionTargetObj\tActionTargetObjName\tActionTargetObjID");


            ////////////////////// CODAP logging
            ///
            path = string.Format("{0}Group{1}_Task{2}_Participant{3}_CodapData.txt", filePath, groupID, tasks[taskID].workName, participantID);
            codapStreamWriter = new StreamWriter(path, true);

            codapStreamWriter.WriteLine("Timestamp\tcodapJsonLoad");

        }

        //startTime = info.SentServerTime;
        //textMesh.text = string.Format("<b>{0}</b>\n{1}", taskName, taskDescription.Replace("\\n", "\n"));

        startEventListening();

        Debug.Log("Logging started");

    }

    private void startEventListening()
    {
        //EventManager.StartListening("ControllerGrab", (float v) => LogActionData();
    }


    public void StopLogging()
    {
        isLogging = false;
        time = 0;

        if (isMasterLogger)
        {
            objectStreamWriter.Close();
            //annotationsStreamWriter.Close();
        }
        if (!isMasterLogger || (isMasterLogger && isLoggingPlayerData))
        {
            playerStreamWriter.Close();
            actionsStreamWriter.Close();
        }

        codapStreamWriter.Close();
        //textMesh.text = "";

        stopEventListening();

        Debug.Log("Logging stopped");
    }

    private void stopEventListening()
    {
        //EventManager.StopListeningToAxisEvent(ApplicationConfiguration.OnAxisGrabbed, registerAxisGrabbed);
    }

    private void FixedUpdate()
    {
        if (isLogging)
        {
            time += Time.fixedDeltaTime;

            if (timeBetweenLogs <= time)
            {
                time = 0f;

                LogPlayerData();

                //photonView.RPC("LogPlayerData", RpcTarget.AllViaServer);

                //string t = (PhotonNetwork.Time - startTime).ToString("F3");

                // this is a very heavy one, we can choose not to do it! it's mostly useful for reconstructing the scene
                LogObjectData(Time.realtimeSinceStartup.ToString());
                //LogAnnotationData(t);
            }
        }
    }

    private void LogPlayerData()
    {
        if (isLogging && playerStreamWriter != null)
        {
            headPos = headset.position;
            headRot = headset.rotation;
            leftPos = leftController.position;
            leftRot = leftController.rotation;
            rightPos = rightController.position;
            rightRot = rightController.rotation;

            // I need to find a better way to log this kind of data
            gazeHit = Physics.Raycast(headset.position, headset.forward, out gazeRaycast);
            leftHit = Physics.Raycast(leftController.position, leftController.forward, out leftRaycast);
            rightHit = Physics.Raycast(rightController.position, rightController.forward, out rightRaycast);

            gazeInfo = GetRaycastInfo(gazeHit, gazeRaycast, gazeInfo);
            leftInfo = GetRaycastInfo(leftHit, leftRaycast, leftInfo);
            rightInfo = GetRaycastInfo(rightHit, rightRaycast, rightInfo);

            rightGrabbedObjectInfo = GetGrabbedObjectByController(rightWand, rightGrabbedObjectInfo);
            leftGrabbedObjectInfo = GetGrabbedObjectByController(leftWand, leftGrabbedObjectInfo);

            playerStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}\t{20}\t{21}\t{22}\t{23}\t{24}\t{25}\t{26}\t{27}\t{28}\t{29}" +
                "\t{30}\t{31}\t{32}\t{33}\t{34}\t{35}\t{36}\t{37}\t{38}\t{39}\t{40}\t{41}\t{42}\t{43}" +
                "\t{44}\t{45}\t{46}\t{47}\t{48}\t{49}\t{50}\t{51}\t{52}\t{53}",
                Time.realtimeSinceStartup,
                // Head position
                headPos.x.ToString(format),
                headPos.y.ToString(format),
                headPos.z.ToString(format),
                // Head rotation
                headRot.x.ToString(format),
                headRot.y.ToString(format),
                headRot.z.ToString(format),
                headRot.w.ToString(format),
                // Left controller position
                leftPos.x.ToString(format),
                leftPos.y.ToString(format),
                leftPos.z.ToString(format),
                // Left controller rotation
                leftRot.x.ToString(format),
                leftRot.y.ToString(format),
                leftRot.z.ToString(format),
                leftRot.w.ToString(format),
                // Left controller events
                leftWand.gripping,
                "false",
                leftWand.padPressDown,
                leftWand.padPressUp,
                // Right controller position
                rightPos.x.ToString(format),
                rightPos.y.ToString(format),
                rightPos.z.ToString(format),
                // Right controller rotation
                rightRot.x.ToString(format),
                rightRot.y.ToString(format),
                rightRot.z.ToString(format),
                rightRot.w.ToString(format),
                // Right controller events
                rightWand.gripping,
                "false",
                rightWand.padPressDown,
                rightWand.padPressUp,
                // Raycasting
                gazeInfo.ObjectName,
                gazeInfo.OriginalObjectOwner,
                gazeInfo.ObjectOwner,
                gazeInfo.ObjectID,
                leftInfo.ObjectName,
                leftInfo.OriginalObjectOwner,
                leftInfo.ObjectOwner,
                leftInfo.ObjectID,
                leftInfo.DistanceFromLeftController,
                leftGrabbedObjectInfo.ObjectName,
                leftGrabbedObjectInfo.OriginalObjectOwner,
                leftGrabbedObjectInfo.ObjectOwner,
                leftGrabbedObjectInfo.ObjectID,
                leftGrabbedObjectInfo.DistanceFromLeftController,
                rightInfo.ObjectName,
                rightInfo.OriginalObjectOwner,
                rightInfo.ObjectOwner,
                rightInfo.ObjectID,
                rightInfo.DistanceFromRightController,
                rightGrabbedObjectInfo.ObjectName,
                rightGrabbedObjectInfo.OriginalObjectOwner,
                rightGrabbedObjectInfo.ObjectOwner,
                rightGrabbedObjectInfo.ObjectID,
                rightGrabbedObjectInfo.DistanceFromRightController
            );

            playerStreamWriter.Flush();
        }
    }

    private void LogObjectData(string timestamp)
    {
        // here are the important bits for me to measure: 
        /**
         * Axis: 
         *  pos
         *  rotation
         *  parentVisualization1
         *  parentVisualization2
         *  name
         *  axisId
         *  minFilter
         *  maxFilter
         *  minNormalizer
         *  maxNormalizer
         *  AttributeFilters[]
         *      {
         *          filterAxisId,
         *          min,
         *          max,
         *          localOrGlobal
         *      }
         *  VizColorAxisId
         *  VizSizeAxisId
         *  uniqueId (like a hash or something)
         *  
         * Visualization
         *  pos
         *  rotation
         *  xDimAxisId
         *  yDimAxisId
         *  zDimAxisId
         *  sizeDimAxisId
         *  colorDimAxisId
         *  AttributeFilters[]
         *      {
         *          filterAxisId,
         *          min,
         *          max,
         *          localOrGlobal
         *      }
         * uniqueID (hash)
         * 
         * Datashelf
         *  pos
         *  rotation
         *  
         * MonitorPanel is going to be fixed so we don't need that
         * 
         * **/

        if (isLogging && objectStreamWriter != null)
        {
            foreach (var axis in SceneManager.Instance.sceneAxes)
            {
                objectStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}",
                    timestamp,
                    "Axis",
                    axis.name,
                    axis.axisId,
                    axis.GetInstanceID().ToString(),
                    axis.transform.position.x.ToString(format),
                    axis.transform.position.y.ToString(format),
                    axis.transform.position.z.ToString(format),
                    axis.transform.rotation.x.ToString(format),
                    axis.transform.rotation.y.ToString(format),
                    axis.transform.rotation.z.ToString(format),
                    axis.transform.rotation.w.ToString(format),
                    axis.AttributeFilters.Count(),
                    JsonUtility.ToJson(axis.AttributeFilters),
                    axis.correspondingVisColorAxisId,
                    axis.correspondingVisSizeAxisId,
                    axis.MinNormaliser,
                    axis.MaxNormaliser
                );

            }


            foreach (var vis in ImAxesRecognizer.GetVisList())
            {
                objectStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}\t{20}\t{21}\t{22}\t{23}\t{24}\t{25}\t{26}",
                    timestamp,
                    "Chart",
                    vis.name,
                    vis.GenerateUniqueIDForVis(),
                    vis.GetInstanceID().ToString(),
                    vis.transform.position.x.ToString(format),
                    vis.transform.position.y.ToString(format),
                    vis.transform.position.z.ToString(format),
                    vis.transform.rotation.x.ToString(format),
                    vis.transform.rotation.y.ToString(format),
                    vis.transform.rotation.z.ToString(format),
                    vis.transform.rotation.w.ToString(format),
                    vis.AttributeFilters.Count(),
                    JsonUtility.ToJson(vis.AttributeFilters),
                    vis.visualizationColorAxisId,
                    vis.visualizationSizeAxisId,
                    "",
                    "",
                    SceneManager.Instance.globalFilters.Count(),
                    JsonUtility.ToJson(SceneManager.Instance.globalFilters),
                    vis.axesCount,
                    vis.ReferenceAxis1.horizontal,
                    vis.ReferenceAxis1.vertical,
                    vis.ReferenceAxis1.horizontal2,
                    vis.ReferenceAxis1.vertical2,
                    vis.ReferenceAxis1.depth2,
                    vis.viewType
                );

            }


            objectStreamWriter.Flush();
        }
    }

    private ObjectInfo GetGrabbedObjectByController(WandController controller, ObjectInfo info)
    {
        if (!controller.gripping)
            return new ObjectInfo();

        GameObject grabbedObject = controller.getDraggingGameobject();

        if(!grabbedObject)
            return new ObjectInfo();

        info.DistanceFromLeftController = Vector3.Distance(leftController.position, grabbedObject.transform.position).ToString();
        info.DistanceFromRightController = Vector3.Distance(rightController.position, grabbedObject.transform.position).ToString();

        Axis axis = grabbedObject.GetComponent<Axis>();
        if (axis == null) axis = grabbedObject.GetComponentInParent<Axis>();
        if (axis != null)
        {
            info.ObjectName = "Axis";
            info.OriginalObjectOwner = "1";
            info.ObjectOwner = "1";
            info.ObjectID = axis.axisId + " " + axis.GetInstanceID().ToString();

            return info;
        }


        Visualization chart = grabbedObject.GetComponent<Visualization>();
        if (chart == null) chart = grabbedObject.GetComponentInParent<Visualization>();
        if (chart != null)
        {
            info.ObjectName = "Chart";
            info.OriginalObjectOwner = "1";
            info.ObjectOwner = "1";
            info.ObjectID = chart.name + chart.GenerateUniqueIDForVis().GetHashCode().ToString();

            return info;
        }


        MonitorBoardInteractions monitor = grabbedObject.GetComponent<MonitorBoardInteractions>();
        if (monitor == null) monitor = grabbedObject.GetComponentInParent<MonitorBoardInteractions>();
        if (monitor != null)
        {
            info.ObjectName = "Monitor";
            info.OriginalObjectOwner = "1";
            info.ObjectOwner = "1";
            info.ObjectID = monitor.GetInstanceID().ToString();

            return info;
        }

        return new ObjectInfo();

    }

    private ObjectInfo GetRaycastInfo(bool isHit, RaycastHit hit, ObjectInfo info)
    {
        if (!isHit)
            return new ObjectInfo();

        GameObject hitObj = hit.collider.gameObject;

        info.DistanceFromLeftController = Vector3.Distance(leftController.position, hitObj.transform.position).ToString();
        info.DistanceFromRightController = Vector3.Distance(rightController.position, hitObj.transform.position).ToString();

        //OvrAvatar ovrAvatar = hitObj.GetComponent<OvrAvatar>();
        //if (ovrAvatar == null) ovrAvatar = hitObj.GetComponentInParent<OvrAvatar>();
        //if (ovrAvatar != null)
        //{
        //    info.ObjectName = "Player Avatar";
        //    info.OriginalObjectOwner = photonToParticipantDictionary[ovrAvatar.GetComponent<PhotonView>().Owner.ActorNumber].ToString();
        //    info.ObjectOwner = info.OriginalObjectOwner;
        //    info.ObjectID = "";

        //    return info;
        //}

        //Min_Max_Slider.MinMaxSlider slider = hitObj.GetComponent<Min_Max_Slider.MinMaxSlider>();
        //if (slider == null) slider = hitObj.GetComponentInParent<Min_Max_Slider.MinMaxSlider>();
        //if (slider != null)
        //{
        //    var sliderParentVis = slider.GetComponentInParent<FilterBubbleScript>().parentVisualization;
        //    info.ObjectName = "Slider";
        //    info.OriginalObjectOwner = "1";
        //    info.ObjectOwner = "1";
        //    info.ObjectID = sliderParentVis.name + sliderParentVis.GenerateUniqueIDForVis().GetHashCode().ToString() + slider.GetInstanceID();

        //    return info;
        //}

        LegendInteractions legend = hitObj.GetComponent<LegendInteractions>();
        if (legend == null) legend = hitObj.GetComponentInParent<LegendInteractions>();
        if (legend != null)
        {
            var legendParentVis = legend.GetComponentInParent<Visualization>();
            info.ObjectName = "Legend";
            info.OriginalObjectOwner = "1";
            info.ObjectOwner = "1";
            info.ObjectID = legendParentVis.name + legendParentVis.GenerateUniqueIDForVis().GetHashCode().ToString() + legendParentVis.GetInstanceID();

            return info;
        }

        FilterBubbleButton filterBubble = hitObj.GetComponent<FilterBubbleButton>();
        if (filterBubble == null) filterBubble = hitObj.GetComponentInParent<FilterBubbleButton>();
        if (filterBubble != null)
        {
            var objID = filterBubble.visReference != null ? filterBubble.visReference.name + filterBubble.visReference.GenerateUniqueIDForVis().GetHashCode().ToString() + filterBubble.GetInstanceID() : "global";

            info.ObjectName = "Filter Bubble";
            info.OriginalObjectOwner = "1";
            info.ObjectOwner = "1";
            info.ObjectID = objID;

            return info;
        }

        Axis axis = hitObj.GetComponent<Axis>();
        if (axis == null) axis = hitObj.GetComponentInParent<Axis>();
        if (axis != null)
        {
            info.ObjectName = "Axis";
            info.OriginalObjectOwner = "1";
            info.ObjectOwner = "1";
            info.ObjectID = axis.axisId + " " + axis.GetInstanceID().ToString();

            return info;
        }


        Visualization chart = hitObj.GetComponent<Visualization>();
        if (chart == null) chart = hitObj.GetComponentInParent<Visualization>();
        if (chart != null)
        {
            info.ObjectName = "Chart";
            info.OriginalObjectOwner = "1";
            info.ObjectOwner = "1";
            info.ObjectID = chart.name + chart.GenerateUniqueIDForVis().GetHashCode().ToString();

            return info;
        }


        MonitorBoardInteractions monitor = hitObj.GetComponent<MonitorBoardInteractions>();
        if (monitor == null) monitor = hitObj.GetComponentInParent<MonitorBoardInteractions>();
        if (monitor != null)
        {
            info.ObjectName = "Monitor";
            info.OriginalObjectOwner = "1";
            info.ObjectOwner = "1";
            info.ObjectID = monitor.GetInstanceID().ToString();

            return info;
        }




        return new ObjectInfo();
    }

    bool isDodActive = false;
    bool isBrushingActive = false;
    public void LogActionData(string actionName, GameObject sourceObj = null, GameObject targetObj = null)
    {
        if (isLogging && actionsStreamWriter != null)
        {
            var targetObjType = "";
            var targetObjName = "";
            var targetObjInstanceId = "";
            if(targetObj != null)
            {
                targetObjType = targetObj.GetType().ToString();
                targetObjName = targetObj.name;
                targetObjInstanceId = targetObj.GetInstanceID().ToString();
            }

            #region Details on Demand Logging
            if (actionName == "DoD2D" || actionName == "DoD3D")
            {
                // This is for when we start with DoD
               if(!isDodActive)
                {
                    isDodActive = true;

                    actionsStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                        Time.realtimeSinceStartup.ToString("F3"),
                        actionName + "Started",
                        sourceObj.GetType(),
                        sourceObj.name,
                        sourceObj.GetInstanceID(),
                        targetObjType,
                        targetObjName,
                        targetObjInstanceId
                    );

                    actionsStreamWriter.Flush();
                } else
                {
                    return;
                }
            } else if(actionName == "DoDEnd")
            {
                // this is for writing the stuff when we're finished with DoD
                if(isDodActive)
                {
                    actionsStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                        Time.realtimeSinceStartup.ToString("F3"),
                        actionName,
                        sourceObj.GetType(),
                        sourceObj.name,
                        sourceObj.GetInstanceID(),
                        targetObjType,
                        targetObjName,
                        targetObjInstanceId
                    );

                    actionsStreamWriter.Flush();

                    isDodActive = false;

                    return;
                }
            }
            #endregion


            #region Brushing Logging
            if (actionName == "Brush")
            {
                // This is for when we start with DoD
                if (!isBrushingActive)
                {
                    isBrushingActive = true;

                    actionsStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                        Time.realtimeSinceStartup.ToString("F3"),
                        actionName + "Started",
                        sourceObj.GetType(),
                        sourceObj.name,
                        sourceObj.GetInstanceID(),
                        targetObjType,
                        targetObjName,
                        targetObjInstanceId
                    );

                    actionsStreamWriter.Flush();
                }
                else
                {
                    return;
                }
            }
            else if(actionName == "BrushEnd")
            {
                // this is for writing the stuff when we're finished with DoD
                if (isBrushingActive)
                {
                    actionsStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                        Time.realtimeSinceStartup.ToString("F3"),
                        actionName,
                        sourceObj.GetType(),
                        sourceObj.name,
                        sourceObj.GetInstanceID(),
                        targetObjType,
                        targetObjName,
                        targetObjInstanceId
                    );

                    actionsStreamWriter.Flush();

                    isDodActive = false;

                    return;
                }
            }
            #endregion

            actionsStreamWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                Time.realtimeSinceStartup.ToString("F3"),
                actionName,
                sourceObj.GetType(),
                sourceObj.name,
                sourceObj.GetInstanceID(),
                targetObjType,
                targetObjName,
                targetObjInstanceId
            );

            actionsStreamWriter.Flush();
        }
    }


    public void LogCodapData(string codapLoad)
    {
        if (isLogging && codapStreamWriter != null)
        {
            print("logging Codap stuff");
            codapStreamWriter.WriteLine("{0}\t{1}", Time.realtimeSinceStartup.ToString("F3"), codapLoad);

            codapStreamWriter.Flush();
        }
    }
    public bool IsLogging()
    {
        return isLogging;
    }
}

