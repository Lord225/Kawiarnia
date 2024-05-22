using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class ClientScript : MonoBehaviour
{
    private NavMeshAgent agent;
    bool isDone()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool isMoving()
    {
        return agent.velocity.magnitude > 0 && !isDone();
    }

    bool hasPathButNotMoving()
    {
        return agent.hasPath && !isMoving();
    }

    public TableScript table;
    public CounterScript counter;
    public float patience = 10;
    public float tableStopDistance = 2;

    void findTable() {
        var tablesObject = GameObject.Find("Tables");
        if (tablesObject == null)
        {
            Debug.LogError("Tables object not found");
            return;
        }

        var tables = tablesObject.GetComponentsInChildren<TableScript>();
        
        // check if you can find yourself in a table
        if(tables.AsEnumerable().Any(table => table.currentOwner == this))
        {
            table = tables.First(table => table.currentOwner == this);
            return;
        }

        foreach (var t in tables)
        {
            if (t.currentOwner == null)
            {
                // Found a table
                Debug.Log("Client" + this + " found a table: " + t);
                table = t;
                break;
            }
        }
    }

    void findCounter()
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

    public enum AgentState
    {
        WantsTable,
        GoingToTable,
        WantsToOrder,
        GoingToCounter,
        Wardering,
        Ordering,
        Eating,
        Leaving,
    }

    public AgentState state;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // dont change state here.
    void updateClient(AgentState state)
    {
        if (state == AgentState.WantsTable)
        {
            findTable();
        }
        if(state == AgentState.WantsToOrder)
        {
            findCounter();
        }
    }


    void updateState()
    {
        if (state == AgentState.WantsTable && table != null)
        {
            state = AgentState.GoingToTable;
            agent.SetDestination(table.transform.position);
            agent.stoppingDistance = 2;
        }

        if(state == AgentState.GoingToTable)
        {
            if (isDone())
            {
                state = AgentState.WantsToOrder;
            }
        }

        if (state == AgentState.WantsToOrder && counter != null)
        {
            state = AgentState.GoingToCounter;
            agent.SetDestination(counter.target.position);
            agent.stoppingDistance = 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving())
        {
            transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
        }

        updateClient(this.state);

        updateState();
    }
}
