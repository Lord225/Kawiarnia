using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using System.IO;
using System.Xml.Schema;
using System.Linq;


[CustomEditor(typeof(SceneLoaderController))]
public class SceneLoaderControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SceneLoaderController loader = (SceneLoaderController)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Load Scene"))
        {
            loader.loadScene();
        }
    }
}


[Serializable]
public class SceneDescription
{
    public string name;
    public float sizeX;
    public float sizeY;
    public DeployableObject floorObject; //walkable surface (root for navmesh)
    public List<DeployableObject> objectsOnScreen; 

    // rotation and scale can be implemented further down the road?
    public List<Vector3> doorsPositions;
    public List<Vector3> barsPositions;
    public List<Vector3> tablesPositions;
}

[RequireComponent(typeof(ModelLoader))]
public class SceneLoaderController : MonoBehaviour
{
    // abs paths works too, if no abs path it will add project dir so sceneDescription\scene1 is enought.
    public string sceneDescriptonPath = string.Empty;

    public SceneDescription sceneDescription;

    public ModelLoader modelLoader;

    public GameObject doorObject;
    public GameObject barObject;
    public GameObject tableObject;


    public void loadScene()
    {
        this.loadScene(sceneDescriptonPath);
    }

    public void loadScene(string path)
    {
        // getting back to clean state
        CleanupScene();

        // load file from disc
        sceneDescription = loadFromJson(sceneDescriptonPath);

        Debug.Log($"Loaded description of scene {sceneDescription.name}");

        // load objects
        
        // lets assume it will load everyting as child to this script.

        // loadGlobalSettings();

        prepereScene();

    }

    private SceneDescription loadFromJson(string path)
    {
        if (!(Path.GetFileName(path).EndsWith(".json") || Path.GetFileName(path).EndsWith(".jsonc")))
        {
            // Find json in path
            string[] files = Directory.GetFiles(path, "*.json");
            if (files.Length > 0)
            {
                path = files[0];
            }
            else
            {
                Debug.LogError("No JSON file found in the specified directory.");
                return null;
            }
        }

        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist: " + path);
            return null;
        }

        var file = File.ReadAllText(path);
        
        var scene = JsonUtility.FromJson<SceneDescription>(file);

        return scene;
    }

    private void prepereScene()
    {
        // create parent object for all OOS (Objects On Screen) for better categorization
        Transform OOSParent = new GameObject("OOS").transform;
        OOSParent.position = Vector3.zero;
        OOSParent.parent = transform;

        // prepere prefabs from loaded objects and place them.
        Debug.Log("Placing " + sceneDescription.objectsOnScreen.Count + " objects");
        if (sceneDescription.objectsOnScreen.Count != 0)
        {
            modelLoader.InitialzeModels(sceneDescription.objectsOnScreen, OOSParent.transform);
        }

        // place door prefabs and other mandatory stuff
        Transform doorsParent = new GameObject("Doors").transform;
        doorsParent.position = Vector3.zero;
        doorsParent.parent = transform;

        // instantinate doors at given postions
        Debug.Log("Placing " + sceneDescription.doorsPositions.Count + " doors");
        modelLoader.InitializeGameObjects(doorObject, sceneDescription.doorsPositions, doorsParent);

        Transform barsParent = new GameObject("Bars").transform;
        barsParent.position = Vector3.zero;
        barsParent.parent = transform;

        // instantinate bars at given postions
        Debug.Log("Placing " + sceneDescription.barsPositions.Count + " bars");
        modelLoader.InitializeGameObjects(barObject, sceneDescription.barsPositions, barsParent);

        Transform tablesParent = new GameObject("Tables").transform;
        tablesParent.position = Vector3.zero;
        tablesParent.parent = transform;

        // instantinate tables at given postions
        Debug.Log("Placing " + sceneDescription.tablesPositions.Count + " tables");
        modelLoader.InitializeGameObjects(tableObject, sceneDescription.tablesPositions, tablesParent);

        // build navigation on map
    }

    // Remove remants of last loaded scene if any
    private void CleanupScene()
    {
        // Every object on scene is a child of this one so we just need to destroy them all    
        for (int i = this.transform.childCount; i > 0; --i)
        {
            // Destroy doesn't work in editor mode thus second version
            if (!Application.isEditor)
            {
                Destroy(this.transform.GetChild(0).gameObject);
            }
            else
            {
                DestroyImmediate(this.transform.GetChild(0).gameObject);
            }
        }
    }
}
