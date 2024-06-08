using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BaristaSettings : MonoBehaviour
{

    public int currentCount;
    public GameObject barista;
    public Transform spawnPoint;

    public int lostClients = 0;
    public float coffeeCost;


    void Start()
    {
        currentCount = transform.childCount;
    }

    public int AlterBaristas(AlterInfo info)
    {
        coffeeCost = info.coffeeCost;

        if(currentCount < info.baristaCount)
        {
            for (int i = 0; i < (info.baristaCount - currentCount); i++)
            {
                Instantiate(barista, spawnPoint.position, spawnPoint.rotation, transform);
            }
        }
        else if(currentCount > info.baristaCount)
        {
            for (int i = 0; i < (currentCount - info.baristaCount); i++)
            {
                int toDestroy = Random.Range(0, currentCount - i);
                Destroy(transform.GetChild(toDestroy).gameObject);
            }
        }

        foreach (NavMeshAgent barista in transform.GetComponentsInChildren<NavMeshAgent>())
        {
            barista.speed = Random.Range(info.minBaristaSpeed, info.maxBaristaSpeed);
        }

        currentCount = info.baristaCount;

        return 0;
    }

    public Stats GetStats()
    {
        int drinksServed = 0;
        float idleTime = 0f;

        foreach (WaiterScript barista in transform.GetComponentsInChildren<WaiterScript>())
        {
            drinksServed += barista.drinksServed;
            idleTime += barista.timeIdle;
        }

        Stats stats = new(drinksServed, (int)(drinksServed * coffeeCost), (int)idleTime, lostClients);

        return stats;
    }
}
