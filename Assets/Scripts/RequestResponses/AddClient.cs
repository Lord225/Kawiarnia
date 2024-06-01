using RESTfulHTTPServer.src.invoker;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AddClient : MonoBehaviour
{
    public GameObject client;

    public int Spawn(ClientInfo info)
    {
        GameObject door = GameObject.Find(info.doorId);

        if (door == null)
            return 1;

        Vector3 dp = door.transform.position;

        // delay or some random burst while spawning??
        for (int i = 0; i < info.count; i++) {
            GameObject newObject = Instantiate(client, dp, Quaternion.identity);

            NavMeshAgent agent = newObject.GetComponent<NavMeshAgent>();

            if(agent != null )
                agent.speed = UnityEngine.Random.Range(info.minSpeed, info.maxSpeed);

            ClientScript script = newObject.GetComponent<ClientScript>();

            if (script != null)
                script.patience = UnityEngine.Random.Range(info.minPatience, info.maxPatience);

            
        }

        return 0;
    }
}
