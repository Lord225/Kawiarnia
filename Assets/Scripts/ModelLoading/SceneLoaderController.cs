using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using System.IO;
using System.Xml.Schema;
using System.Linq;
using JetBrains.Annotations;
using Unity.AI.Navigation;
using System.Xml.Xsl;
using Unity.VisualScripting;

//TODO
// * Floor should be an child of object Loder (it is not)
// * Prefab's models (bars, doors,...) should be external obj files from blender (one mesh)


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

        if (GUILayout.Button("Clear"))
        {
            loader.cleanupScene();
        }

        if (GUILayout.Button("Save Scene"))
        {
            loader.saveScene();
        }
    }
}

[Serializable]
public class Transformation
{
    public float px = 0;
    public float py = 0;
    public float pz = 0;

    public float rx = 0;
    public float ry = 0;
    public float rz = 0;

    public float sx = 1;
    public float sy = 1;
    public float sz = 1;

    public string id = string.Empty;

    public static Transformation fromTransform(Transform other)
    {
        var result = new Transformation();

        result.pos = other.position;
        result.rot = other.rotation;
        result.scale = other.localScale;

        result.id = other.name;

        return result;
    }

    public Vector3 pos
    {
        get
        {
            return new Vector3(px, py, pz);
        }
        set
        {
            this.px = value.x;
            this.py = value.y;
            this.pz = value.z;
        }
    }

    public Quaternion rot
    {
        get
        {
            return Quaternion.Euler(rx, ry, rz);
        }
        set
        {
            this.rx = value.eulerAngles.x;
            this.ry = value.eulerAngles.y;
            this.rz = value.eulerAngles.z;
        }
    }

    public Vector3 scale
    {
        get
        {
            return new Vector3(sx, sy, sz);
        }
        set
        {
            this.sx = value.x;
            this.sy = value.y;
            this.sz = value.z;
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
    public List<Transformation> doorsPositions;
    public List<Transformation> barsPositions;
    public List<Transformation> tablesPositions;
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
        cleanupScene();

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

        Transform floorParent = new GameObject("Floor").transform;

        modelLoader.InitialzeModels(new List<DeployableObject> { sceneDescription.floorObject }, floorParent, sceneDescriptonPath);

        // prepere prefabs from loaded objects and place them.
        Debug.Log("Placing " + sceneDescription.objectsOnScreen.Count + " objects");

        modelLoader.InitialzeModels(sceneDescription.objectsOnScreen, OOSParent.transform, sceneDescriptonPath);
        

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

    public void saveScene()
    {
        saveSceneToJson("");
    }

    private string localPath(string absolutePath)
    {
        var projectPath = sceneDescriptonPath;

        // strip projectPath from absolutePath
        if (absolutePath.StartsWith(projectPath))
        {
            var local = absolutePath.Substring(projectPath.Length);
            // strip from / on start (any amount)
            while (local.StartsWith("/") || local.StartsWith("\\"))
            {
                local = local.Substring(1);
            }

            return local;
        }
        else
        {
            return absolutePath;
        }
    }

    private void saveSceneToJson(string path)
    {
        // serialize all objects into SceneDescription and then save that as json

        // update scene desciption based on scene state
        // get all children of Doors
        // TODO GetComponentInChildren retruns also parent object so we need to skip this one first

        var doors = GameObject.Find("Doors").GetComponentsInChildren<Transform>();
        var bars = GameObject.Find("Bars").GetComponentsInChildren<Transform>();
        var tables = GameObject.Find("Tables").GetComponentsInChildren<Transform>();

        sceneDescription.doorsPositions = doors.Select(x => Transformation.fromTransform(x)).ToList();
        sceneDescription.barsPositions = bars.Select(x => Transformation.fromTransform(x)).ToList();
        sceneDescription.tablesPositions = tables.Select(x => Transformation.fromTransform(x)).ToList();

        var objectsOnScreen = GameObject.Find("OOS").GetComponentsInChildren<DeployableObjectPath>();

        sceneDescription.objectsOnScreen = objectsOnScreen.Select(x => new DeployableObject
        {
            path = localPath(x.path),
            pos = x.GetComponent<Transform>().position,
            rot = x.GetComponent<Transform>().rotation,
            scale = x.GetComponent<Transform>().localScale,
            id = x.name,
        }).ToList();


        // save to json
        string json = JsonUtility.ToJson(sceneDescription, true);

        Debug.Log(json);
        
        // File.WriteAllText(path, json);
    }

    // Remove remants of last loaded scene if any
    public void cleanupScene()
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
