// RESTful-Unity
// Copyright (C) 2016 - Tim F. Rieck
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program. If not, see <http://www.gnu.org/licenses/>.
//
// <copyright file="ServerInit.cs" company="TRi">
// Copyright (c) 2016 All Rights Reserved
// </copyright>
// <author>Tim F. Rieck</author>
// <date>29/11/2016 10:13 AM</date>

using UnityEngine;
using System;
using System.Net;
using RESTfulHTTPServer.src.models;
using RESTfulHTTPServer.src.controller;

public class ClientInfo
{
    public string doorId = "";
    public int count = 1;

    public float minSpeed = 6;
    public float maxSpeed = 6;
    public int minOrderSize = 1;
    public int maxOrderSize = 1;

    public int minPatience = 10;
    public int maxPatience = 10;

    public string preferedTable = "";
}

public class AlterInfo
{
    public int baristaCount = 1;

    public float minBaristaSpeed = 4;
    public float maxBaristaSpeed = 4;

    public float coffeeCost = 6;
}

public class Load
{
    public int loadInTime = 0;
    public int idleTime = 0;

    public Load(int loadInTime, int idleTime)
    {
        this.loadInTime = loadInTime;
        this.idleTime = idleTime;
    }
    //smth else per table/bar?
}

public class Stats
{
    public int drinksServed = 0;
    public int grossIncome = 0;
    public int idleBaristaTime = 0;
    public int lostClients = 0;

    public Stats(int drinksServed, int grossIncome, int idleBaristaTime, int lostClients)
    {
        this.drinksServed = drinksServed;
        this.grossIncome = grossIncome;
        this.idleBaristaTime = idleBaristaTime;
        this.lostClients = lostClients;
    }
    //smth else per whole coffee?
}

namespace RESTfulHTTPServer.src.invoker
{
    public class TestInvoker
    {
        public static Response AddClient(Request request)
        {
            Response response = new();
            bool responseMade = false;
            ClientInfo clientInfo = new ClientInfo();

            try
            {
                clientInfo = JsonUtility.FromJson<ClientInfo>(request.GetPOSTData());
            }
            catch (FormatException)
            {
                response.SetContent("404");
                response.SetHTTPStatusCode(404);
                return response;
            }

            // if no door ID was provided then no spawn is possible
            if (clientInfo.doorId == "" || clientInfo.count < 0)
            {
                response.SetContent("403");
                response.SetHTTPStatusCode(403);
                return response;
            }


            UnityInvoker.ExecuteOnMainThread.Enqueue(() =>
            {
                AddClient addClient = GameObject.Find("AddClient").GetComponent<AddClient>();
                int returnCode = addClient.Spawn(clientInfo);

                if (returnCode == 0)
                {
                    response.SetContent("200");
                    response.SetHTTPStatusCode(200);
                    responseMade = true;
                }
                else
                {
                    response.SetContent("403");
                    response.SetHTTPStatusCode(403);
                    responseMade = true;
                }
            });

            while (!responseMade) { }

            return response;
        }

        public static Response AlterSettings(Request request)
        {
            Response response = new();
            bool responseMade = false;
            AlterInfo alterInfo = new AlterInfo();

            try
            {
                alterInfo = JsonUtility.FromJson<AlterInfo>(request.GetPOSTData());
            }
            catch (FormatException)
            {
                response.SetContent("404");
                response.SetHTTPStatusCode(404);
                return response;
            }

            if (alterInfo.baristaCount < 0)
            {
                response.SetContent("403");
                response.SetHTTPStatusCode(403);
                return response;
            }

            UnityInvoker.ExecuteOnMainThread.Enqueue(() =>
            {
                BaristaSettings baristaSettings = GameObject.Find("Waiters").GetComponent<BaristaSettings>();
                int returnCode = baristaSettings.AlterBaristas(alterInfo);

                if (returnCode == 0)
                {
                    response.SetContent("200");
                    response.SetHTTPStatusCode(200);
                    responseMade = true;
                }
                else
                {
                    response.SetContent("403");
                    response.SetHTTPStatusCode(403);
                    responseMade = true;
                }

                response.SetContent("200");
                response.SetHTTPStatusCode(200);
                responseMade = true;
            });

            while (!responseMade) { }

            return response;
        }

        public static Response GetLoad(Request request)
        {
            Response response = new();
            bool responseMade = false;
            string id = "";

            try
            {
                id = request.GetParameter("id");
            }
            catch (FormatException)
            {
                response.SetContent("404");
                response.SetHTTPStatusCode(404);
                return response;
            }

            Debug.Log("id: " + id);

            UnityInvoker.ExecuteOnMainThread.Enqueue(() =>
            {
                GameObject idObject = GameObject.Find(id);

                if (idObject == null)
                {
                    response.SetContent("403");
                    response.SetHTTPStatusCode(403);
                    responseMade = true;
                }
                else
                {
                    //TODO: set some actuall load data

                    int loadInTime;
                    float idleTime;

                    TableScript ts = idObject.GetComponent<TableScript>();

                    if (ts != null)
                    {
                        loadInTime = ts.clientsInTime;
                        idleTime = ts.idleTime;
                    }
                    else
                    {
                        CounterScript cs = idObject.GetComponent<CounterScript>();
                        loadInTime = cs.clientsInTime;
                        idleTime = cs.idleTime;
                    }

                    Load load = new(loadInTime, (int)idleTime);

                    string content = JsonUtility.ToJson(load);

                    response.SetContent(content);
                    response.SetHTTPStatusCode(200);
                    responseMade = true;
                }
            });

            while (!responseMade) { }

            return response;
        }

        public static Response GetStats(Request request)
        {
            Response response = new();
            bool responseMade = false;


            UnityInvoker.ExecuteOnMainThread.Enqueue(() =>
            {
                BaristaSettings baristaSettings = GameObject.Find("Waiters").GetComponent<BaristaSettings>();

                string content = JsonUtility.ToJson(baristaSettings.GetStats());

                response.SetContent(content);
                response.SetHTTPStatusCode(200);
                responseMade = true;
            });

            while (!responseMade) { }

            return response;
        }
    }
}

