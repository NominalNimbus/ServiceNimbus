/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.Collections.Generic;

namespace ServerCommonObjects.ServerClasses
{
    /// <summary>
    /// The class implements functionality to route incoming and outgoing messages from connection service to feeder and vice-versa
    /// </summary>
    public class MessageRouter
    {
        /// <summary>
        /// global object that requires synchronized access
        /// </summary>
        public static MessageRouter gMessageRouter;

        /// <summary>
        /// message router event argument   
        /// </summary>
        public class MessageRouter_EventArgs : EventArgs
        {
            /// <summary>
            /// unique ID, generally, session ID
            /// </summary>
            public readonly string ID;
            /// <summary>
            /// object that represents session/user info specified by ID
            /// </summary>
            public readonly IUserInfo UserInfo;
            public readonly IWCFProcessorInfo ProcessorInfo;
            /// <summary>
            /// original incoming request message
            /// </summary>
            public readonly RequestMessage Request;
            /// <summary>
            /// true, if the request must be ignored
            /// </summary>
            public bool Cancel = false;
            /// <summary>
            /// reason to ignore the request
            /// </summary>
            public string Reason = String.Empty;

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="aID">unique ID, generally it's session ID</param>
            /// <param name="aUserInfo">session/user info object that implements IUserInfo interface</param>
            public MessageRouter_EventArgs(string aID, IUserInfo aUserInfo)
            {
                ID = aID;
                UserInfo = aUserInfo;
                Request = null;
            }

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="aID">unique ID, generally it's session ID</param>
            /// <param name="aUserInfo">session/user info object that implements IUserInfo interface</param>
            /// <param name="aRequest">request message</param>
            public MessageRouter_EventArgs(string aID, IUserInfo aUserInfo, RequestMessage aRequest)
            {
                ID = aID;
                UserInfo = aUserInfo;
                Request = aRequest;
            }

            public MessageRouter_EventArgs(string aID, IWCFProcessorInfo processorInfo, RequestMessage aRequest)
            {
                ID = aID;
                ProcessorInfo = processorInfo;
                Request = aRequest;
            }
        }

        public class ScriptingProcessorEventArgs : EventArgs
        {
            public readonly string ID;
            public readonly IWCFProcessorInfo ProcessorInfo;
            public readonly RequestMessage Request;
            public bool Cancel = false;
            public string Reason = string.Empty;

            public ScriptingProcessorEventArgs(string aID, IWCFProcessorInfo processorInfo)
            {
                ID = aID;
                ProcessorInfo = processorInfo;
                Request = null;
            }

            public ScriptingProcessorEventArgs(string aID, IWCFProcessorInfo processorInfo, RequestMessage aRequest)
            {
                ID = aID;
                ProcessorInfo = processorInfo;
                Request = aRequest;
            }
        }

        public event EventHandler<MessageRouter_EventArgs> AddedSession;
        public event EventHandler<ScriptingProcessorEventArgs> AddedProcessorSession;

        public event EventHandler<MessageRouter_EventArgs> RemovedSession;
        public event EventHandler<ScriptingProcessorEventArgs> RemovedProcessorSession;

        public event EventHandler<MessageRouter_EventArgs> RouteRequest;

        protected Dictionary<string, IUserInfo> m_ActiveSessions;
        protected Dictionary<string, string> m_ActiveLoginSessions;
        protected Dictionary<string, IWCFProcessorInfo> m_ActiveProcessorSessions;

        private Authentication m_Authenticator;

        public Authentication Authenticator => m_Authenticator;

        /// <summary>
        /// initializes message router
        /// </summary>
        /// <param name="aAuthenticator">authenticator</param>
        public void Init(Authentication aAuthenticator)
        {
            m_ActiveSessions = new Dictionary<string, IUserInfo>();
            m_ActiveLoginSessions = new Dictionary<string, string>();
            m_ActiveProcessorSessions = new Dictionary<string, IWCFProcessorInfo>();
            m_Authenticator = aAuthenticator;
        }

        /// <summary>
        /// get session/user info by ID
        /// </summary>
        /// <param name="aID">unique ID to identify session</param>
        /// <returns>session/user info</returns>
        public virtual IUserInfo GetUserInfo(string aID)
        {
            lock (m_ActiveSessions)
            {
                if (m_ActiveSessions.TryGetValue(aID, out var aUserInfo))
                    return aUserInfo;
                else
                    return null;
            }
        }

        /// <summary>
        /// get session/user info by Login
        /// </summary>
        /// <param name="aID">Login</param>
        /// <returns>session/user info</returns>
        public virtual IUserInfo GetUserInfoByLogin(string login)
        {
            lock (m_ActiveSessions)
            {
                if (m_ActiveLoginSessions.TryGetValue(login, out var id) && m_ActiveSessions.TryGetValue(id, out var aUserInfo))
                    return aUserInfo;
                else
                    return null;
            }
        }

        public virtual IWCFProcessorInfo GetProcessorInfo(string aID)
        {
            lock (m_ActiveProcessorSessions)
            {
                return m_ActiveProcessorSessions.TryGetValue(aID, out var processorInfo) ? processorInfo : null;
            }
        }

        /// <summary>
        /// get user info by an object, generally, the object is session context
        /// </summary>
        /// <param name="obj">session's object to identify session</param>
        /// <returns></returns>
        public virtual IUserInfo GetUserInfo(object obj)
        {
            lock (m_ActiveSessions)
            {
                foreach (IUserInfo item in m_ActiveSessions.Values)
                {
                    if (item.SessionObject != null && ReferenceEquals(item.SessionObject, obj))
                        return item;
                }
            }
            return null;
        }

        public virtual IWCFProcessorInfo GetProcessorInfo(object obj)
        {
            lock (m_ActiveProcessorSessions)
            {
                foreach (var item in m_ActiveProcessorSessions.Values)
                {
                    if (item.SessionObject != null && ReferenceEquals(item.SessionObject, obj))
                        return item;
                }
            }
            return null;
        }

        public bool IsUserConnected(string id)
        {
            lock(m_ActiveSessions)
                return m_ActiveSessions.ContainsKey(id);
        }

        /// <summary>
        /// returns unique ID if logon is complete
        /// </summary>
        /// <param name="aLogin">login, generally user name or email</param>
        /// <param name="aPassword">password</param>
        /// <returns>true, if credentials are valid</returns>
        public bool Authenticate(string aLogin, string aPassword)
        {
            if (Authenticator != null)
                return Authenticator.Login(aLogin, aPassword);

            return false;
        }

        /// <summary>
        /// adds a session identified by uniques ID
        /// </summary>
        /// <param name="aID">unique ID</param>
        /// <param name="aUserInfo">session/user info</param>
        public virtual void AddSession(string aID, IUserInfo aUserInfo)
        {
            lock (m_ActiveSessions)
            {
                var args = new MessageRouter_EventArgs(aID, aUserInfo);

                if (!args.Cancel)
                {
                    if (m_ActiveSessions.ContainsKey(aID))
                        return;

                    m_ActiveSessions.Add(aID, aUserInfo);
                    m_ActiveLoginSessions[aUserInfo.Login] = aID;
                    AddedSession?.Invoke(this, new MessageRouter_EventArgs(aID, aUserInfo));
                }
                else
                    throw new ApplicationException("The session is not enabled. Reason: " + args.Reason);
            }
        }
        
        public virtual void AddProcessorSession(string aID, IWCFProcessorInfo processorInfo)
        {
            lock (m_ActiveProcessorSessions)
            {
                var args = new ScriptingProcessorEventArgs(aID, processorInfo);

                if (!args.Cancel)
                {
                    m_ActiveProcessorSessions.Add(aID, processorInfo);
                    AddedProcessorSession?.Invoke(this, new ScriptingProcessorEventArgs(aID, processorInfo));
                }
                else
                    throw new ApplicationException("The session is not enabled. Reason: " + args.Reason);
            }
        }

        /// <summary>
        /// removes session by ID
        /// </summary>
        /// <param name="aID">session ID</param>
        /// <returns>removed session/user info</returns>
        public virtual IUserInfo RemoveSession(string aID)
        {
            lock (m_ActiveSessions)
            {
                if (m_ActiveSessions.TryGetValue(aID, out var aUserInfo))
                {
                    m_ActiveSessions.Remove(aID);
                    m_ActiveLoginSessions.Remove(aUserInfo.Login);
                    RemovedSession?.Invoke(this, new MessageRouter_EventArgs(aID, aUserInfo));

                    return aUserInfo;
                }

                return null;
            }
        }

        public virtual IWCFProcessorInfo RemoveProcessorSession(string aID)
        {
            lock (m_ActiveProcessorSessions)
            {
                if (!m_ActiveProcessorSessions.TryGetValue(aID, out var processorInfo)) return null;

                m_ActiveProcessorSessions.Remove(aID);
                RemovedProcessorSession?.Invoke(this, new ScriptingProcessorEventArgs(aID, processorInfo));

                return processorInfo;
            }
        }

        /// <summary>
        /// routes incoming request
        /// </summary>
        /// <param name="aID">session ID where the request received</param>
        /// <param name="aRequest">incoming request</param>
        public virtual void ProcessRequest(string aID, RequestMessage aRequest)
        {
            var aUserInfo = GetUserInfo(aID);
            var processorInfo = GetProcessorInfo(aID);

            if (aUserInfo != null)
                RouteRequest?.Invoke(this, new MessageRouter_EventArgs(aID, aUserInfo, aRequest));
            else if (processorInfo != null)
                RouteRequest?.Invoke(this, new MessageRouter_EventArgs(aID, processorInfo, aRequest));
        }

        /// <summary>
        /// dispose object
        /// </summary>
        public void Dispose()
        {
            lock (m_ActiveSessions)
            {
                m_ActiveSessions.Clear();
                m_ActiveSessions = null;
            }
            m_Authenticator = null;
        }
    }
}
