using RESTfulHTTPServer.src.invoker;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddClient : MonoBehaviour
{
    public GameObject client;

    public void Spawn(ClientInfo info)
    {
        Vector3 dp = GameObject.Find(info.doorId).transform.position;
        GameObject newObject = Instantiate(client, dp, Quaternion.identity);
    }
}
