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

public class ModelLoader
{
    public List<DeployableObject> objectsToDeploy;

    private void Start()
    {
        InitialzeModels(objectsToDeploy);
    }

    private void InitialzeModels(List<DeployableObject> objectsToDeploy)
    {
        foreach (var deployableObject in objectsToDeploy)
        {
            var loadedObj = new OBJLoader().Load(deployableObject.path);
            loadedObj.transform.position = deployableObject.location;
        }
    }
}
