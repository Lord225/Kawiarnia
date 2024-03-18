using Dummiesman;
using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public class DeployableObject : Transformation
{
    public string path;
}

public class ModelLoader : MonoBehaviour
{
    public void InitialzeModels(List<DeployableObject> objectsToDeploy, Transform parent)
    {
        foreach (var deployableObject in objectsToDeploy)
        {
            if (System.IO.File.Exists(deployableObject.path))
            {
                // Loading and initializing .obj at given path 
                var loadedObj = new OBJLoader().Load(deployableObject.path);

                // Setting appropriate transform parameters
                loadedObj.transform.position = deployableObject.pos;
                loadedObj.transform.rotation = deployableObject.rot;
                loadedObj.transform.localScale = deployableObject.scale;
                loadedObj.transform.SetParent(parent);
            }
            else
            {
                Debug.LogWarning("Failed to load " + deployableObject.path);
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
                Instantiate(spawnedObject, location.pos, location.rot, parent);
            }
        }
    }
}
