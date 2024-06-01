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
}

namespace RESTfulHTTPServer.src.invoker
{
    public class TestInvoker
    {
        public static Response AddClient(Request request)
        {
            Response response = new();
            string responseData = "";
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
                    responseData = "200";
                    response.SetContent("200");
                    response.SetHTTPStatusCode(200);
                }
                else
                {
                    responseData = "403";
                    response.SetContent("403");
                    response.SetHTTPStatusCode(403);
                }
            });

            while (responseData.Equals("")) { }

            return response;
        }

        public static Response AlterSettings(Request request)
        {
            Response response = new();
            string responseData = "";
            AlterInfo alterInfo = new AlterInfo();

            try
            {
                alterInfo = JsonUtility.FromJson<AlterInfo>(request.GetPOSTData());
            }
            catch (FormatException)
            {
                responseData = "404";
                response.SetContent("404");
                response.SetHTTPStatusCode(404);
                return response;
            }

            if (alterInfo.baristaCount < 0)
            {
                responseData = "403";
                response.SetContent("403");
                response.SetHTTPStatusCode(403);
                return response;
            }

            UnityInvoker.ExecuteOnMainThread.Enqueue(() =>
            {
                //TODO: do something with the settings here

                responseData = "200";
                response.SetContent("200");
                response.SetHTTPStatusCode(200);
            });

            while (responseData.Equals("")) { }

            return response;
        }
    }
}

