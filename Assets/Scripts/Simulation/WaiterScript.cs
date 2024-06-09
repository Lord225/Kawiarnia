using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class WaiterScript : MonoBehaviour
{
    private NavMeshAgent agent;
    public GameObject hoverIcon;

    //hi - hover icon for this client
    private HoverIcon hi;

    public enum State
    {
        Idle,
        Walking,
        Serving,
        GoToCounter,
        Baristing,
    }

    public State state = State.Idle;
    public CounterScript counter = null;
    public ClientScript currentClient = null;

    public int drinksServed = 0;
    public float timeIdle = 0f;

    private float timestamp = -1;

    bool isDone()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.5f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool isFree()
    {
        return state == State.Idle || state == State.Walking;
    }

    public void findCounter(bool withOrders = false)
    {
        var bar = GameObject.Find("Bars");
        if (bar == null)
        {
            Debug.LogError("Bars object not found");
            return;
        }

        var counter = bar.GetComponentsInChildren<CounterScript>();

        // sort by distance
        Func<Vector3, float> heuristic = (Vector3 pos) => Vector3.Distance(pos, transform.position) + 
                                                          UnityEngine.Random.Range(0, 1) + 
                                                          (withOrders ? counter.First().orders.Count : 0);

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
            currentClient = whoAsks;
            state = State.Serving;
            Debug.Log("Waiter " + name + " is serving client " + whoAsks.name);
            return this;
        }
        return null;
    }

    void serveClient()
    {
        if(currentClient == null )
        {
            state = State.Idle;
        }

        if(counter == null)
        {
            findCounter();
        }

        agent.SetDestination(currentClient.transform.position);

        if (isDone())
        {
            Debug.Log("Waiter: Hello " + currentClient.name + ", what would you like to order?");
            // serve client
            counter.addOrder(currentClient);
            currentClient.waiterServes(this);
            currentClient = null;
            state = State.Idle;
        } 
    }

    // Start is called before the first frame update
    void Start()
    {
        Transform hiCanvas = GameObject.Find("HoverIconCanvas").transform;
        hi = Instantiate(hoverIcon, hiCanvas).GetComponent<HoverIcon>();
        hi.followedTransform = transform;

        agent = GetComponent<NavMeshAgent>();
        findCounter();
    }

    // Update is called once per frame
    void Update()
    {
        if(state == State.Serving)
        {
            serveClient();
            if (timestamp != -1)
            {
                timeIdle += Time.time - timestamp;
                timestamp = -1;
            }
        }
        if (state == State.Idle)
        {
            var rnd = UnityEngine.Random.Range(0.0f, 1.0f);
            if (rnd < 0.5)
            {
                agent.SetDestination(new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10)));
                state = State.Walking;
            } else if (counter.ordersAvailable() > 0 && !counter.isSomeoneWorking())
            {
                state = State.GoToCounter;
            }
            if(timestamp == -1)
            {
                timestamp = Time.time;
            }
        }
        if(state == State.Walking)
        {
            if (isDone())
            {
                state = State.Idle;
            }
            if (timestamp != -1)
            {
                timeIdle += Time.time - timestamp;
                timestamp = -1;
            }
        }
        if(state == State.GoToCounter)
        {
            findCounter(withOrders: true);

            if (counter != null)
            {
                agent.SetDestination(counter.baristaTarget.position);
                state = State.Baristing;
            }
            else
            {
                state = State.Idle;
            }
        }
        if(state == State.Baristing)
        {   
            if (isDone())
            {
                counter.makeDrink();
                hi.ChangeIconVisibility(true);
                Invoke("waitForDrinksToFinish", 5);
            }
            if (timestamp != -1)
            {
                timeIdle += Time.time - timestamp;
                timestamp = -1;
            }
        }
    }

    void waitForDrinksToFinish()
    {
        hi.ChangeIconVisibility(false);

        drinksServed += 1;

        counter.stopMakingDrink();
        state = State.Idle;
    }

    private void OnDestroy()
    {
        if (hi != null)
        {
            Destroy(hi.gameObject);
        }
    }

}
