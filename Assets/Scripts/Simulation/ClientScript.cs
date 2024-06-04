using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class ClientScript : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private CounterScript.Order order;


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
                t.currentOwner = this;
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

    bool findWaiter()
    {
        var waiter = GameObject.Find("Waiters");

        if (waiter == null)
        {
            Debug.LogError("Waiters object not found");
            return false;
        }

        var waiters = waiter.GetComponentsInChildren<WaiterScript>();

        // ask free random waiter to take order

       foreach (var w in waiters)
       {
            if(w.askForService(this)) {
                return true;
            }
       }
        return false;
    }

    public void orderReady(CounterScript counter)
    {
        // check if we are waiting for order
        if (state == AgentState.Wardering)
        {
            state = AgentState.GoingForOrder;
            agent.SetDestination(counter.target.position);
        }
    }

    public enum AgentState
    {
        WantsTable, 
        GoingToTable,
        WantsToOrder, // client is about to go to counter or waiter
        GoingToCounter,
        GoingForOrder,
        Wardering,
        WantsWaiter, // client waits for waiter to take order
        Ordering,    // client is ordering
        TakingOrder, // client is taking order
        Eating,      
        Leaving,
    }

    public AgentState state;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // dont change state here.
    void updateClient(AgentState state)
    {
        if (state == AgentState.WantsTable)
        {
            findTable();
        }

        if (state == AgentState.Eating)
        {
            animator.Play("Eating");
        }

        if (state == AgentState.Leaving)
        {
            // find closest door
            var doors = GameObject.Find("Doors").GetComponentsInChildren<DoorScript>();

            Func<Vector3, float> heuristic = (Vector3 pos) => Vector3.Distance(pos, transform.position) + UnityEngine.Random.Range(0, 1);
            
            var targets = doors.OrderBy(c => heuristic(c.transform.position));
            
            agent.SetDestination(targets.First().transform.position);
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

        if (state == AgentState.WantsToOrder)
        {
            float rnd = UnityEngine.Random.Range(0, 1);

            if (rnd < 0.1)
            {
                findCounter();
                state = AgentState.GoingToCounter;
                agent.SetDestination(counter.target.position);
                agent.stoppingDistance = 1;
            }
            else
            {
                if(findWaiter())
                {
                    state = AgentState.WantsWaiter;
                }
            }
        }


        if (state == AgentState.GoingToCounter)
        {
            if (isDone())
            {
                state = AgentState.Ordering;
            }
        }

        if(state == AgentState.Ordering)
        {
            if (isDone())
            {
                counter.addOrder(this);
                state = AgentState.Wardering;
            }
        }

        if (state == AgentState.Eating)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !animator.IsInTransition(0))
            {
                state = AgentState.Leaving;
            }
        }

        if (state == AgentState.Leaving)
        {
            if (isDone())
            {
                Destroy(gameObject);
            }
        }

        if (state == AgentState.GoingForOrder)
        {
            if (isDone())
            {
                // get order
                order = counter.takeOrder(this);
                // go to table
                agent.SetDestination(table.transform.position);
                state = AgentState.GoingToTable;

            }
        }

        if (state == AgentState.Wardering)
        {
            // set random destination
            if (isDone())
            {
                agent.SetDestination(new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10)));
            }
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

        // if moving set animation to walk and speed parameter to agent speed
        if (isMoving())
        {
            animator.SetFloat("speed", agent.velocity.magnitude/2);
        }
        else
        {
            animator.SetFloat("speed", 0);  
        }
    }
}
