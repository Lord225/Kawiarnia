using Dummiesman;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DeployableObject : Transformation
{
    public string path;
    public string mtlPath;
}

public class ModelLoader : MonoBehaviour
{
    public void InitialzeModels(List<DeployableObject> objectsToDeploy, Transform parent, string projectPath)
    {
        foreach (var deployableObject in objectsToDeploy)
        {
            var path = projectPath + "\\" + deployableObject.path;
            var mtlPath = projectPath + "\\" + deployableObject.mtlPath;

            if (System.IO.File.Exists(path))
            {
                // Loading and initializing .obj at given path 
                var loadedObj = new OBJLoader().Load(path, mtlPath);


                // Setting appropriate transform parameters
                loadedObj.transform.position = deployableObject.pos;
                loadedObj.transform.rotation = deployableObject.rot;
                loadedObj.transform.localScale = deployableObject.scale;
                loadedObj.transform.SetParent(parent);

                // add path to this object's obj and mtl file for later serialization
                var dop = loadedObj.AddComponent<DeployableObjectPath>();

                dop.path = path;
                dop.mtlPath = mtlPath;



                // set object name to id
                loadedObj.name = deployableObject.id;
                
            }
            else
            {
                Debug.LogWarning("Failed to load " + path);
            }
                      
        }
    }

    public void InitializeGameObjects(GameObject spawnedObject, List<Transformation> transforms, Transform parent)
    {
        // Loading and initializing .obj at given path 
        if (transforms != null)
        {
            foreach (var location in transforms)
            {
                var newObject = Instantiate(spawnedObject, location.pos, location.rot, parent);

                newObject.name = location.id;
            }
        }
    }
}
