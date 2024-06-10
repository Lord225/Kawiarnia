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
using static UnityEditor.PlayerSettings;

//TODO
// * Floor should be an child of object Loder (it is not)
// * Prefab's models (bars, doors,...) should be external obj files from blender (one mesh)


[CustomEditor(typeof(SceneLoaderController))]
public class SceneLoaderControllerEditor : Editor
{
    string basePath = @"sceneDescription\";
    // TODO to persist this path it should be taken from loader!
    [SerializeField] string pathToSave = string.Empty;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SceneLoaderController loader = (SceneLoaderController)target;

        GUILayout.Space(10);

        if (loader.loadPath != string.Empty && pathToSave == string.Empty)
        {
            pathToSave = loader.loadPath;
        }

        basePath = GUILayout.TextField(basePath);
        if (GUILayout.Button("Load Scene"))
        {
            pathToSave = EditorUtility.OpenFolderPanel("Choose folder with scene description", basePath, "");

            if (pathToSave == "") return;

            loader.loadScene(pathToSave);
        }

        if (GUILayout.Button("Clear"))
        {
            loader.cleanupScene();
        }

        if (GUILayout.Button("Build Navigation"))
        {
            loader.buildNavigation();
        }

        pathToSave = GUILayout.TextField(pathToSave);
        if (GUILayout.Button("Save Scene"))
        {
            loader.saveScene(pathToSave);
        }

        if (GUILayout.Button("Save as"))
        {
            var path = EditorUtility.SaveFolderPanel("Choose folder to save scene", basePath, "");

            if (path == "") return;

            pathToSave = path;

            loader.saveScene(pathToSave);
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
    public Vector2 boundBoxX;
    public Vector2 boundBoxZ;
    public DeployableObject floorObject; //walkable surface (root for navmesh)
    public List<DeployableObject> objectsOnScreen; 

    // rotation and scale can be implemented further down the road?
    public List<Transformation> doorsPositions;
    public List<Transformation> barsPositions;
    public List<Transformation> tablesPositions;
    public List<Transformation> decorationsPositions;
}

[RequireComponent(typeof(ModelLoader))]
public class SceneLoaderController : MonoBehaviour
{
    public string loadPath = string.Empty;
    public SceneDescription sceneDescription;

    public ModelLoader modelLoader;

    public GameObject doorObject;
    public GameObject barObject;
    public GameObject tableObject;
    public GameObject decorationsObject;

    public Vector2 boundBoxX
    {
        get
        {
            return sceneDescription.boundBoxX;
        }
    }

    public Vector2 boundBoxZ
    {
        get
        {
            return sceneDescription.boundBoxZ;
        }
    }   




    public void loadScene(string path)
    {
        // getting back to clean state
        cleanupScene();

        // load file from disc
        sceneDescription = loadFromJson(path);

        Debug.Log($"Loaded description of scene {sceneDescription.name}");

        // load objects
        
        // lets assume it will load everyting as child to this script.

        // loadGlobalSettings();

        prepereScene(path);

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

    private void prepereScene(string path)
    {
        // create parent object for all OOS (Objects On Screen) for better categorization
        Transform OOSParent = new GameObject("OOS").transform;
        OOSParent.position = Vector3.zero;
        OOSParent.parent = transform;

        GameObject floor = new GameObject("Floor");
        Transform floorParent = floor.transform;
        floorParent.position = Vector3.zero;
        floorParent.parent = transform;

        modelLoader.InitialzeModels(new List<DeployableObject> { sceneDescription.floorObject }, floorParent, path);

        // prepere prefabs from loaded objects and place them.
        Debug.Log("Placing " + sceneDescription.objectsOnScreen.Count + " objects");

        modelLoader.InitialzeModels(sceneDescription.objectsOnScreen, OOSParent.transform, path);
        

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

        Transform decorationsParent = new GameObject("Decorations").transform;
        decorationsParent.position = Vector3.zero;
        decorationsParent.parent = transform;

        // instantinate decorations at given postions
        Debug.Log("Placing " + sceneDescription.decorationsPositions.Count + " decorations");
        modelLoader.InitializeGameObjects(tableObject, sceneDescription.decorationsPositions, decorationsParent);

        var (x, z) = getBoundBox(floor);

        sceneDescription.boundBoxX = x;
        sceneDescription.boundBoxZ = z;


        // build navigation on map
        buildNavigation();

    }

    public void buildNavigation()
    {
        // get floor object
        Transform floorParent = GameObject.Find("Floor").GetComponent<Transform>();

        var floor = floorParent.GetChild(0);

        // try taking NavMeshSurface if not present add it
        var hasNavmesh = floor.TryGetComponent<NavMeshSurface>(out NavMeshSurface navMeshSurface);

        if (!hasNavmesh)
        {
            navMeshSurface = floor.gameObject.AddComponent<NavMeshSurface>();
        }

        // set up navmesh
        navMeshSurface.collectObjects = CollectObjects.All;


        navMeshSurface.BuildNavMesh();

 


    }

    private string StripPath(string path)
    {
        return path.Split('\\').Reverse().First();
    }

    private void CopyFile(string src, string dsc)
    {
        // check if file exists
        if (!File.Exists(src))
        {
            Debug.LogError("File does not exist: " + src);
            return;
        }

        // check if src is same as dsc
        if (Path.GetFullPath(src) == Path.GetFullPath(dsc))
        {
            return;
        }

        // if dsc exists delete it
        if (File.Exists(dsc))
        {
            File.Delete(dsc);
        }


        // copy file
        File.Copy(src, dsc);
    }

    public void saveScene(string path)
    {
        // check if path is pointing to json file, if so strip to directory
        if (Path.GetFileName(path).EndsWith(".json"))
        {
            path = Path.GetDirectoryName(path);
        }


        // handling current state of chosen path
        if ((!Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any())) {
            Debug.Log("Saving to path: " + path);
            System.IO.Directory.CreateDirectory(path);
        }
        else
        {
            //Debug.Log("Overwriting files at path: " + path);
            //System.IO.DirectoryInfo di = new DirectoryInfo(path);

            //foreach (FileInfo file in di.GetFiles())
            //{
            //    file.Delete();
            //}
            //foreach (DirectoryInfo dir in di.GetDirectories())
            //{
            //    dir.Delete(true);
            //}
        }

        // serialize all objects into SceneDescription and then save that as json

        // update scene desciption based on scene state
        // get all children of Doors
        // TODO GetComponentInChildren retruns also parent object so we need to skip this one first

        UUIDHandler handler = new();

        Transform doorParent = GameObject.Find("Doors").GetComponent<Transform>();
        var doors = doorParent.GetComponentsInChildren<Transform>().Where(t => t != doorParent && handler.IsUUIDValid(t.name));

        Transform barParent = GameObject.Find("Bars").GetComponent<Transform>();
        var bars = barParent.GetComponentsInChildren<Transform>().Where(t => t != barParent && handler.IsUUIDValid(t.name));

        Transform tableParent = GameObject.Find("Tables").GetComponent<Transform>();
        var tables = tableParent.GetComponentsInChildren<Transform>().Where(t => t != tableParent && handler.IsUUIDValid(t.name));

        Transform decorationsParent = GameObject.Find("Decorations").GetComponent<Transform>();
        var decorations = decorationsParent.GetComponentsInChildren<Transform>().Where(t => t != decorationsParent && handler.IsUUIDValid(t.name));

        sceneDescription.doorsPositions = doors.Select(x => Transformation.fromTransform(x)).ToList();
        sceneDescription.barsPositions = bars.Select(x => Transformation.fromTransform(x)).ToList();
        sceneDescription.tablesPositions = tables.Select(x => Transformation.fromTransform(x)).ToList();
        sceneDescription.decorationsPositions = decorations.Select(x => Transformation.fromTransform(x)).ToList();

        Transform objectsOnScreenParent = GameObject.Find("OOS").GetComponent<Transform>();
        var objectsOnScreen = objectsOnScreenParent.GetComponentsInChildren<DeployableObjectPath>().Where(t => t != objectsOnScreenParent && handler.IsUUIDValid(t.name));

        sceneDescription.objectsOnScreen = objectsOnScreen.Select(x => new DeployableObject
        {
            path = x.path,
            mtlPath = x.mtlPath,
            pos = x.GetComponent<Transform>().position,
            rot = x.GetComponent<Transform>().rotation,
            scale = x.GetComponent<Transform>().localScale,
            id = x.name,
        }).ToList();

        Transform floorParent = GameObject.Find("Floor").GetComponent<Transform>();
        var floor = floorParent.GetChild(0);


        sceneDescription.floorObject = new DeployableObject
        {
            path = floor.GetComponent<DeployableObjectPath>().path,
            mtlPath = floor.GetComponent<DeployableObjectPath>().mtlPath,
            pos = floor.position,
            rot = floor.rotation,
            scale = floor.localScale,
            id = floor.name,
        };

        // copy all needed resources 

        List<String> alreadyCopiedObj = new();
        List<String> alreadyCopiedMtl = new();

        var floorPath = sceneDescription.floorObject.path;
        alreadyCopiedObj.Add(floorPath);
        CopyFile(floorPath, path + "\\" + StripPath(floorPath));
        sceneDescription.floorObject.path = StripPath(floorPath);

        var mtlFloorPath = sceneDescription.floorObject.mtlPath;
        alreadyCopiedMtl.Add(mtlFloorPath);
        CopyFile(mtlFloorPath, path + "\\" + StripPath(mtlFloorPath));
        sceneDescription.floorObject.mtlPath = StripPath(mtlFloorPath);

       

        foreach (var oos in sceneDescription.objectsOnScreen){
            // .obj
            if (!alreadyCopiedObj.Contains(oos.path)) {
                alreadyCopiedObj.Add(oos.path);
                CopyFile(oos.path, path + "\\" + StripPath(oos.path));
                oos.path = StripPath(oos.path);
            }

            // .mtl
            if (!alreadyCopiedMtl.Contains(oos.mtlPath))
            {
                alreadyCopiedMtl.Add(oos.mtlPath);
                CopyFile(oos.mtlPath, path + "\\" + StripPath(oos.mtlPath));
                oos.mtlPath = StripPath(oos.mtlPath);
            }
        }

        // save to json
        string json = JsonUtility.ToJson(sceneDescription, true);

        File.WriteAllText(path + @"\scene.json", json);
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

    public (Vector2, Vector2) getBoundBox(GameObject obj)
    {
        var renderer = obj.GetComponentsInChildren<Renderer>();

        var rendererBounds = renderer.Select(x => x.bounds);

        var bouds = rendererBounds.Aggregate((x, y) => { x.Encapsulate(y); return x; });

        var x = new Vector2(bouds.min.x, bouds.max.x);
        var z = new Vector2(bouds.min.z, bouds.max.z);

        return (x, z);
    }
}
