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

    private HoverIcon hi;

    private float timestamp = -1f;
    private bool angry = false;

    public TableScript table;
    public CounterScript counter;
    public float patience = 10f;
    public float tableStopDistance = 2f;

    public enum AgentState
    {
        FindingTable,
        GoingToTable,
        Ordering,
        WaitingForOrder,
        PickingUpOrder,
        GoingToTableWithOrder,
        Eating,
        Leaving
    }

    public AgentState state;

    private bool IsDone() => agent.remainingDistance <= agent.stoppingDistance && agent.velocity.sqrMagnitude < 0.1f;
    private bool IsMoving() => agent.velocity.magnitude > 0 && !IsDone();

    void Start()
    {
        Transform hiCanvas = GameObject.Find("HoverIconCanvas").transform;
        hi = Instantiate(hoverIcon, hiCanvas).GetComponent<HoverIcon>();
        hi.followedTransform = transform;

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        state = AgentState.FindingTable;
    }

    void Update()
    {
        UpdateClientState();
        HandleStateTransitions();

        if (IsMoving())
        {
            transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
            animator.SetFloat("speed", agent.velocity.magnitude / 2);
        }
        else
        {
            animator.SetFloat("speed", 0);
        }
    }

    private void UpdateClientState()
    {
        switch (state)
        {
            case AgentState.FindingTable:
                FindTable();
                if (table != null)
                {
                    state = AgentState.GoingToTable;
                    agent.SetDestination(table.transform.position);
                    agent.stoppingDistance = tableStopDistance;
                }
                break;

            case AgentState.GoingToTable:
                hi.ChangeIconVisibility(false);
                if (IsDone())
                {
                    state = AgentState.Ordering;
                    FindCounter();
                    if (counter != null)
                    {
                        agent.SetDestination(counter.target.position);
                        agent.stoppingDistance = 1f;
                    }
                }
                break;

            case AgentState.Ordering:
                hi.ChangeIconVisibility(true);
                hi.ChangeIcon(1);
                if (IsDone() && counter != null)
                {
                    if (!counter.containsOrder(this)) // Prevent redundant ordering
                    {
                        counter.addOrder(this);
                    }
                    state = AgentState.WaitingForOrder;
                    agent.SetDestination(table.transform.position);
                    agent.stoppingDistance = tableStopDistance;
                }
                break;

            case AgentState.WaitingForOrder:
                hi.ChangeIconVisibility(true);
                hi.ChangeIcon(2);
                break;

            case AgentState.PickingUpOrder:
                if (IsDone())
                {
                    order = counter?.takeOrder(this);
                    if (order != null)
                    {
                        agent.SetDestination(table.transform.position);
                        state = AgentState.GoingToTableWithOrder;
                    }
                }
                break;

            case AgentState.GoingToTableWithOrder:
                hi.ChangeIconVisibility(false);
                if (IsDone())
                {
                    state = AgentState.Eating; 
                }
                break;

            case AgentState.Eating:
                hi.ChangeIconVisibility(true);
                hi.ChangeIcon(2);
                animator.Play("Eating");
                if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !animator.IsInTransition(0))
                {
                    state = AgentState.Leaving;
                }
                break;

            case AgentState.Leaving:
                hi.ChangeIconVisibility(true);
                hi.ChangeIcon(3);
                if (IsDone())
                {
                    Destroy(gameObject);
                    if (angry)
                    {
                        GameObject.Find("Waiters").GetComponent<BaristaSettings>().lostClients += 1;
                    }
                }
                break;
        }
    }

    private void HandleStateTransitions()
    {
        if (state == AgentState.WaitingForOrder && counter.containsOrder(this))
        {
            OrderReady();
        }

        if (state == AgentState.Leaving)
        {
            var doors = GameObject.Find("Doors").GetComponentsInChildren<DoorScript>();
            if (doors.Length > 0)
            {
                var closestDoor = doors.OrderBy(d => Vector3.Distance(d.transform.position, transform.position)).First();
                agent.SetDestination(closestDoor.transform.position);
            }
        }
    }

    public void WaiterServes(WaiterScript waiter)
    {
        state = AgentState.WaitingForOrder;
        agent.SetDestination(table.transform.position);
    }

    public void OrderReady()
    {
        if(state == AgentState.WaitingForOrder)
        {
            state = AgentState.PickingUpOrder;
            agent.SetDestination(counter.target.position);
        }
    }

    private void FindTable()
    {
        var tablesObject = GameObject.Find("Tables");
        if (tablesObject == null)
        {
            Debug.LogError("Tables object not found");
            return;
        }

        var tables = tablesObject.GetComponentsInChildren<TableScript>();

        foreach (var t in tables)
        {
            if (t.currentOwner == null)
            {
                t.currentOwner = this;
                table = t;
                return;
            }
        }
    }

    private void FindCounter()
    {
        var bar = GameObject.Find("Bars");
        if (bar == null)
        {
            Debug.LogError("Bars object not found");
            return;
        }

        var counters = bar.GetComponentsInChildren<CounterScript>();
        counter = counters.OrderBy(c => Vector3.Distance(c.transform.position, transform.position)).FirstOrDefault();
    }

    private void OnDestroy()
    {
        if (hi != null)
        {
            Destroy(hi.gameObject);
        }
    }
}
