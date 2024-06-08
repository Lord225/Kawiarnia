using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableScript : MonoBehaviour
{

    public Object currentOwner;
    public float idleTime;
    public int clientsInTime;

    private float timestamp = -1f;

    void Update()
    {
        if (currentOwner == null)
        {
            if (timestamp == -1f)
            {
                timestamp = Time.time;
            }
        }
        else
        {
            if (timestamp != -1f)
            {
                clientsInTime += 1;
                idleTime += Time.time - timestamp;
                timestamp = -1f;
            }
        }
    }
}
