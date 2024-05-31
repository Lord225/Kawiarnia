using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class WaiterScript : MonoBehaviour
{
    private NavMeshAgent agent;

    public enum State
    {
        Idle,
        Serving,
        Baristing
    }

    public State state = State.Idle;
    public CounterScript counter = null;
    public TableScript currentTable = null;

    public bool isFree()
    {
        return state == State.Idle;
    }

    public void findCounter()
    {
        var bar = GameObject.Find("Bars");
        if (bar == null)
        {
            Debug.LogError("Bars object not found");
            return;
        }

        var counter = bar.GetComponentsInChildren<CounterScript>();

        // sort by distance
        Func<Vector3, float> heuristic = (Vector3 pos) => Vector3.Distance(pos, transform.position) + UnityEngine.Random.Range(0, 1);

        var targets = counter.OrderBy(c => heuristic(c.transform.position));

        // try getting first counter,
        if (targets.Count() > 0)
        {
            this.counter = targets.First();
        }
    }

    public WaiterScript askForService(ClientScript whoAsks)
    {
        if (isFree())
        {
            currentTable = whoAsks.table;
            state = State.Serving;

            return this;
        }
        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
