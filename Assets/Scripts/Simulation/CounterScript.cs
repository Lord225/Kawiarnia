using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class CounterScript : MonoBehaviour
{
    [System.Serializable]
    public class Order
    {
        public ClientScript client;
    }


    public Transform target = null;
    public Transform baristaTarget = null;
    // show in Inspector

    public List<Order> orders = new List<Order>();
    public List<Order> finishedOrders = new List<Order>();
    public float idleTime;
    public int clientsInTime;

    private float timestamp = -1f;

    public void addOrder(ClientScript client)
    {
        Debug.Log("Order added for " + client.name);
        orders.Add(new Order { client = client });
    }

    public Order takeOrder(ClientScript client)
    {
        Debug.Log("Order taken for " + client.name);
        // find order in finished orders
        var order = finishedOrders.Find(o => o.client == client);

        if (order != null)
        {
            finishedOrders.Remove(order);
            return order;
        }

        return null;
    }

    public bool containsOrder(ClientScript client)
    {
        return finishedOrders.Find(o => o.client == client) != null;
    }


    void finishNextOrder()
    {
        if (orders.Count > 0)
        {
            var order = orders[0];
            orders.RemoveAt(0);

            finishedOrders.Add(order);
            // order finished, ask client to take it

            order.client.orderReady(this);
            Debug.Log("Finished order for " + order.client.name);
        }
    }

    public int ordersAvailable()
    {
        return orders.Count;
    }


    bool procesing = false;
    public void makeDrink()
    {
        if (timestamp != -1f)
        {
            clientsInTime += 1;
            idleTime += Time.time - timestamp;
            timestamp = -1f;
        }
        procesing = true;
    }

    public void stopMakingDrink()
    {
        if (timestamp == -1f)
        {
            timestamp = Time.time;
        }
        procesing = false;
    }

    public void drinkTask()
    {
        if (procesing)
        {
            finishNextOrder();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (timestamp == -1f)
        {
            timestamp = Time.time;
        }
        // get target empty from children
        target = transform.Find("client");
        baristaTarget = transform.Find("target");

        // start drink task
        InvokeRepeating("drinkTask", 0, 3);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
