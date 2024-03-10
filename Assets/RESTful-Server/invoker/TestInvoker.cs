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

namespace RESTfulHTTPServer.src.invoker
{
    public class TestInvoker
    {
        public static Response AddClient(Request request)
        {
            Response response = new();
            string responseData = "";
            int doorId = 0;

            try
            {
                doorId = Int32.Parse(request.GetParameter("id"));
            }
            catch (FormatException)
            {
                response.SetContent("404");
                response.SetHTTPStatusCode(404);
                return response;
            }

            UnityInvoker.ExecuteOnMainThread.Enqueue(() =>
            {
                SceneLoaderController SLC = GameObject.Find("ObjectLoader").GetComponent<SceneLoaderController>();


                if (SLC.sceneDescription.doorsPositions.Count <= doorId)
                {
                    responseData = "403";
                    response.SetContent("403");
                    response.SetHTTPStatusCode(403);
                }
                else
                {
                    AddClient addClient = GameObject.Find("AddClient").GetComponent<AddClient>();
                    addClient.Spawn(doorId);

                    responseData = "200";
                    response.SetContent("200");
                    response.SetHTTPStatusCode(200);
                }
            });

            while (responseData.Equals("")) { }

            return response;
        }
    }
}

