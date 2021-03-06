﻿/****************************************************************************
Copyright (c) 2013-2015 scutgame.com

http://www.scutgame.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
****************************************************************************/

using System;
using ZyGames.Framework.Common.Log;
using ZyGames.Framework.Game.Context;
using ZyGames.Framework.Game.Lang;
using ZyGames.Framework.Game.Runtime;
using ZyGames.Framework.Game.Service;
using ZyGames.Framework.RPC.IO;
using ZyGames.Framework.Script;

namespace ZyGames.Framework.Game.Contract
{
    internal delegate void RemoteHandle(ActionGetter httpGet, MessageHead head, MessageStructure writer);

    /// <summary>
    /// 
    /// </summary>
    public abstract class GameBaseHost
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public abstract void Start(string[] args);

        /// <summary>
        /// 
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionGetter"></param>
        /// <param name="response"></param>
        protected void DoAction(ActionGetter actionGetter, BaseGameResponse response)
        {
            if (GameEnvironment.IsRunning)
            {
                OnRequested(actionGetter, response);
                ActionFactory.Request(actionGetter, response);
            }
            else
            {
                response.WriteError(actionGetter, Language.Instance.ErrorCode, Language.Instance.ServerMaintain);
            }
        }


        /// <summary>
        /// Raises the requested event.
        /// </summary>
        /// <param name="actionGetter">Http get.</param>
        /// <param name="response">Response.</param>
        protected virtual void OnRequested(ActionGetter actionGetter, BaseGameResponse response)
        {
        }

        /// <summary>
        /// Call remote method
        /// </summary>
        /// <param name="routePath"></param>
        /// <param name="actionGetter"></param>
        /// <param name="response"></param>
        protected virtual void OnCallRemote(string routePath, ActionGetter actionGetter, MessageStructure response)
        {
            try
            {
                string[] mapList = routePath.Split('.');
                string funcName = "";
                string routeName = routePath;
                if (mapList.Length > 1)
                {
                    funcName = mapList[mapList.Length - 1];
                    routeName = string.Join("/", mapList, 0, mapList.Length - 1);
                }
                string routeFile = "";
                int actionId = actionGetter.GetActionId();
                MessageHead head = new MessageHead(actionId);
                if (!ScriptEngines.SettupInfo.DisablePython)
                {
                    routeFile = string.Format("Remote.{0}", routeName);
                    dynamic scope = ScriptEngines.ExecutePython(routeFile);
                    if (scope != null)
                    {
                        var funcHandle = scope.GetVariable<RemoteHandle>(funcName);
                        if (funcHandle != null)
                        {
                            funcHandle(actionGetter, head, response);
                            response.WriteBuffer(head);
                            return;
                        }
                    }
                }
                string typeName = string.Format(GameEnvironment.Setting.RemoteTypeName, routeName);
                routeFile = string.Format("Remote.{0}", routeName);
                var args = new object[] { actionGetter, response };
                var instance = (object)ScriptEngines.Execute(routeFile, typeName, args);
                if (instance is RemoteStruct)
                {
                    var target = instance as RemoteStruct;
                    target.DoRemote();
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteError("OnCallRemote error:{0}", ex);
            }
        }

        /// <summary>
        /// Checks the remote.
        /// </summary>
        /// <returns><c>true</c>, if remote was checked, <c>false</c> otherwise.</returns>
        /// <param name="route">Route.</param>
        /// <param name="actionGetter">Http get.</param>
        protected virtual bool CheckRemote(string route, ActionGetter actionGetter)
        {
            return actionGetter.CheckSign();
        }
    }
}