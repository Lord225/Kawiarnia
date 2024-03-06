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
    public List<TransformOrigin> doorsPositions;
    public List<TransformOrigin> barsPositions;
}

public class SceneLoaderController : MonoBehaviour
{
    // abs paths works too, if no abs path it will add project dir so sceneDescription\scene1 is enought.
    public string sceneDescriptonPath = string.Empty;


    public void loadScene()
    {
        this.loadScene(sceneDescriptonPath);
    }

    public void loadScene(string path)
    {
        // load file from disc
        var scene = loadFromJson(sceneDescriptonPath);

        Debug.Log($"Loaded description of scene {scene.name}");

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
        // prepere prefabs from loaded objects and place them.
        
        // place door prefabs and other mandatory stuff
        
        // build navigation on map
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
