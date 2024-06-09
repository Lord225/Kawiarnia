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
    private CounterScript.Order order = null;
    public GameObject hoverIcon;

    //hi - hover icon for this client
    private HoverIcon hi;

    private float timestamp = -1f;


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
    private bool angry = false;

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

    void findCounter(bool with_my_order = false)
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

        // get counters that have my order if with_my_order
        if (with_my_order)
        {
            targets = targets.Where(c => c.containsOrder(this))
                .OrderBy(c => heuristic(c.transform.position));
        }

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
        if (state == AgentState.Wardering || state == AgentState.GoingToTableToWait)
        {
            state = AgentState.GoingForOrder;
            agent.SetDestination(counter.target.position);
        }
    }

    public void waiterServes(WaiterScript waiter)
    {
        state = AgentState.GoingToTableToWait;
    }

    public enum AgentState
    {
        WantsTable, // client is looking for table, 
        GoingToTable, // client is going to table
        WantsToOrder, // client is about to go to counter or waiter
        GoingToCounter, // client is going to counter
        GoingForOrder, // client is going to take order
        Wardering,
        WantsWaiter, // client waits for waiter to take order
        Ordering,    // client is ordering
        TakingOrder, // client is taking order
        GoingToTableToWait, // client is going to table to wait for order
        Eating,      // client is eating
        Leaving,
    }

    public AgentState state;

    // Start is called before the first frame update
    void Start()
    {
        // initialize hoverIcon

        Transform hiCanvas = GameObject.Find("HoverIconCanvas").transform;
        hi = Instantiate(hoverIcon, hiCanvas).GetComponent<HoverIcon>();
        hi.followedTransform = transform;

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

        if (state == AgentState.Wardering || state == AgentState.WantsTable)
        {
            // set random destination
            if (isDone())
            {
                agent.SetDestination(new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10)));
            }
        }
    }


    void updateState()
    {
        if (state == AgentState.WantsTable && table != null)
        {
            timestamp = -1f;
            hi.ChangeRed(0f);
            state = AgentState.GoingToTable;
            agent.SetDestination(table.transform.position);
            agent.stoppingDistance = 2;
        }

        if (state == AgentState.WantsTable)
        {
            hi.ChangeIconVisibility(true);
            hi.ChangeIcon(0);

            if (timestamp == -1)
            {
                timestamp = Time.time;
            }
            else
            {
                float delta = Time.time - timestamp;
                hi.ChangeRed(delta/patience);
                if(delta > patience)
                {
                    angry = true;
                    state = AgentState.Leaving;
                }
            }
        }
            if (state == AgentState.GoingToTable)
        {
            hi.ChangeIconVisibility(false);
            if (order != null)
            {
                hi.ChangeIconVisibility(true);
                hi.ChangeIcon(2);
            }
            if (isDone())
            {
                if (order != null)
                {
                    state = AgentState.Eating;
                }
                else
                {
                    state = AgentState.WantsToOrder;
                }
            }
        }

        if(state == AgentState.Wardering)
        {
            hi.ChangeIconVisibility(false);
            hi.ChangeRed(0f);
        }

        if (state == AgentState.WantsToOrder)
        {
            float rnd = UnityEngine.Random.Range(0.0f, 1.0f);
            hi.ChangeIconVisibility(false);

            if (rnd < 0.5)
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
            hi.ChangeIconVisibility(false);
            if (isDone())
            {
                state = AgentState.Ordering;
            }
        }

        if(state == AgentState.Ordering)
        {
            hi.ChangeIconVisibility(true);
            hi.ChangeIcon(1);
            if (isDone())
            {
                counter.addOrder(this);
                float rnd = UnityEngine.Random.Range(0.0f, 1.0f);
                if (rnd < 0.1)
                {
                    state = AgentState.Wardering;
                } else
                {
                    state = AgentState.GoingToTableToWait;
                    agent.SetDestination(table.transform.position);
                }
            }
        }
        if(state == AgentState.TakingOrder)
        {
            hi.ChangeIconVisibility(true);
            hi.ChangeIcon(2);
        } 
        if (state == AgentState.Leaving)
        {
            hi.ChangeIconVisibility(true);
            hi.ChangeIcon(3);
            if (isDone())
            {
                Destroy(gameObject);
                if (angry) GameObject.Find("Waiters").GetComponent<BaristaSettings>().lostClients += 1;
            }
        }
        if (state == AgentState.Eating)
        {
            hi.ChangeIconVisibility(true);
            hi.ChangeIcon(2);
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !animator.IsInTransition(0))
            {
                state = AgentState.Leaving;
            }
        }
        if (state == AgentState.GoingForOrder)
        {
            if (isDone())
            {
                findCounter(with_my_order: true);
                if(counter == null)
                {
                    state = AgentState.Wardering;
                    return;
                }
                // get order
                order = counter.takeOrder(this);
                if(order == null)
                {
                    state = AgentState.WantsToOrder;
                    return;
                }
                // go to table
                agent.SetDestination(table.transform.position);
                state = AgentState.GoingToTable;
            }
        }
        if (state == AgentState.WantsWaiter)
        {
            hi.ChangeIconVisibility(true);
            hi.ChangeIcon(1);

            if (timestamp == -1)
            {
                timestamp = Time.time;
            }
            else
            {
                float delta = Time.time - timestamp;
                hi.ChangeRed(delta / patience);
                if (delta > patience)
                {
                    angry = true;
                    state = AgentState.Leaving;
                }
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

    private void OnDestroy()
    {
        if (hi != null) {
            Destroy(hi.gameObject);
        }
    }
}
