# Immersive Analytics in Hybrid Environments
This is a visual analytics application for immersive environmetns (AR/VR/XR). 

You can find the technical report of this project in the link below:
[Link to the technical report](https://vault.sfu.ca/index.php/s/yi9RlftzE0W06cB)

## Installation instructions
This application is a Unity project. In order to run it, you need [Unity 2018.4.29f1](https://unity3d.com/get-unity/download/archive)

## Hardware and compatibility

All of our VR capabilities have been tested on **HTC Vive, HTC Vive Pro, and HTC Vive Cosmos**. Our system's AR and mixed reality features have been developed and tested on **Varjo XR-3 headset**. To ensure the best experience, we encourage the users to use one of the mentioned headsets on a Windows PC with at least 6GB of GDDR5 Graphics memory, 8GB of DDR4 RAM, and a four-core CPU. It should be noted that our system is best experienced on Varjo XR-3.

## Launching the application
You can load your custom data set in form of a CSV file that 
  * **is clean! meaning no empty line, or a line with incorrect data values or risk seeing parsing exceptions**
  * **does not exceed 65,534 entries**

Your dataset file should look like this:

![snippetdata](https://user-images.githubusercontent.com/11532065/36827716-5ea25eb2-1d69-11e8-8da4-f073c88d3923.PNG)

Each column corresponds to a dimension of the data. 


Once in the project, open the Main Scene or ViveScene. There is a SceneManager Unity gameobject in the hierarchy. Click this object [1] and in the inspector window, you can drag and drop your CSV/TSV file into the *Source Data* field [2]. You can also create a *metadata* file for your dataset and drag and drop it into the *Metadata* field.

![datasource](https://user-images.githubusercontent.com/11532065/36767569-28938eb6-1c8f-11e8-8aa5-984aab9202a7.PNG)

### Runing the app in the Unity editor
Once you have attached a clean CSV/TSV file to the SceneManager (and optionnally a metadata file), you can run the application in the editor by simply clicking the play button. Make sure your HTC Vive headset and controllers are connected and that Steam VR is running. 

