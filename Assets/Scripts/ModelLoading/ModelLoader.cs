using Dummiesman;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DeployableObject
{
    public Vector3 location;
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
                loadedObj.transform.position = deployableObject.location;
                loadedObj.transform.SetParent(parent);
            }
            else
            {
                Debug.LogWarning("Failed to load " + deployableObject.path);
            }
                      
        }
    }

    public void InitializeGameObjects(GameObject spawnedObject, List<Vector3> locations, Transform parent)
    {
        // Loading and initializing .obj at given path 
        if (locations != null)
        {
            foreach (var location in locations)
            {
                Instantiate(spawnedObject, location, Quaternion.identity, parent);
            }
        }
    }
}
