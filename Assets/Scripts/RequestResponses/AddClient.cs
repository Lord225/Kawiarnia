using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddClient : MonoBehaviour
{
    public GameObject client;
    private Transform doorParent;

    public void Spawn(int doorId)
    {
        if (doorParent == null)
        {
            doorParent = GameObject.Find("Doors").transform;
        }
        Vector3 pos = doorParent.GetChild(doorId).position;
        Instantiate(client, pos, Quaternion.identity);
    }
}
