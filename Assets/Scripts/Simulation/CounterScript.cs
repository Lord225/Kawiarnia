using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class CounterScript : MonoBehaviour
{
    public class Order
    {
        public ClientScript client;
    }


    public Transform target = null;
    public List<Order> orders = new List<Order>();
    public List<Order> finishedOrders = new List<Order>();

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

    // Start is called before the first frame update
    void Start()
    {
        // get target empty from children
        target = transform.Find("target");

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
