using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class WaiterScript : MonoBehaviour
{
    private NavMeshAgent agent;
    public GameObject hoverIcon;

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

    private bool IsDone() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && (agent.hasPath && agent.velocity.sqrMagnitude < 0.5f);

    public bool IsFree() => state == State.Idle || state == State.Walking;

    private void FindCounter(bool withOrders = false)
    {
        var bar = GameObject.Find("Bars");
        if (bar == null)
        {
            Debug.LogError("Bars object not found");
            return;
        }

        var counters = bar.GetComponentsInChildren<CounterScript>();
        if (counters.Length == 0) return;

        counter = counters
            .OrderBy(c => Vector3.Distance(c.transform.position, transform.position) +
                          (withOrders ? c.orders.Count : 0))
            .FirstOrDefault();
    }

    public WaiterScript AskForService(ClientScript whoAsks)
    {
        if (IsFree())
        {
            currentClient = whoAsks;
            state = State.Serving;
            Debug.Log($"Waiter {name} is serving client {whoAsks.name}");
            return this;
        }
        return null;
    }

    private void ServeClient()
    {
        if (currentClient == null)
        {
            state = State.Idle;
            return;
        }

        if (counter == null) FindCounter();

        agent.SetDestination(currentClient.transform.position);

        if (IsDone())
        {
            Debug.Log($"Waiter: Hello {currentClient.name}, what would you like to order?");
            counter?.addOrder(currentClient);
            currentClient.WaiterServes(this);
            currentClient = null;
            state = State.Idle;
        }
    }

    private void GoToIdle()
    {
        var randomDecision = UnityEngine.Random.Range(0f, 1f);
        if (randomDecision < 0.5f)
        {
            agent.SetDestination(new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10)));
            state = State.Walking;
        }
        else if (counter.ordersAvailable() > 0 && !counter.isSomeoneWorking())
        {
            state = State.GoToCounter;
        }
        if (timestamp == -1) timestamp = Time.time;
    }

    private void HandleWalking()
    {
        if (IsDone()) state = State.Idle;

        if (timestamp != -1)
        {
            timeIdle += Time.time - timestamp;
            timestamp = -1;
        }
    }

    private void HandleCounterVisit()
    {
        FindCounter(withOrders: true);

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

    private void HandleBaristing()
    {
        if (IsDone())
        {
            counter?.makeDrink();
            hi.ChangeIconVisibility(true);
            Invoke(nameof(CompleteDrink), 5f);
        }
        if (timestamp != -1)
        {
            timeIdle += Time.time - timestamp;
            timestamp = -1;
        }
    }

    private void CompleteDrink()
    {
        hi.ChangeIconVisibility(false);
        drinksServed++;
        counter?.stopMakingDrink();
        state = State.Idle;
    }

    private void Start()
    {
        Transform hiCanvas = GameObject.Find("HoverIconCanvas").transform;
        hi = Instantiate(hoverIcon, hiCanvas).GetComponent<HoverIcon>();
        hi.followedTransform = transform;

        agent = GetComponent<NavMeshAgent>();
        FindCounter();
    }

    private void Update()
    {
        switch (state)
        {
            case State.Serving:
                ServeClient();
                if (timestamp != -1)
                {
                    timeIdle += Time.time - timestamp;
                    timestamp = -1;
                }
                break;

            case State.Idle:
                GoToIdle();
                break;

            case State.Walking:
                HandleWalking();
                break;

            case State.GoToCounter:
                HandleCounterVisit();
                break;

            case State.Baristing:
                HandleBaristing();
                break;
        }
    }

    private void OnDestroy()
    {
        if (hi != null) Destroy(hi.gameObject);
    }
}
