using RESTfulHTTPServer.src.invoker;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AddClient : MonoBehaviour
{
    public GameObject client;

    public void Spawn(ClientInfo info)
    {
        Vector3 dp = GameObject.Find(info.doorId).transform.position;

        // delay or some random burst while spawning??
        for (int i = 0; i < info.count; i++) {
            GameObject newObject = Instantiate(client, dp, Quaternion.identity);

            NavMeshAgent agent = newObject.GetComponent<NavMeshAgent>();
            agent.speed = UnityEngine.Random.Range(info.minSpeed, info.maxSpeed);

            ClientScript script = newObject.GetComponent<ClientScript>();
            script.patience = UnityEngine.Random.Range(info.minPatience, info.maxPatience);
        }
    }
}
