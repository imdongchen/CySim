﻿/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Reflection;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Connectors.Hypergrid;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;

using GridRegion = OpenSim.Services.Interfaces.GridRegion;

using OpenMetaverse;
using log4net;
using Nini.Config;

namespace OpenSim.Region.CoreModules.Framework.EntityTransfer
{
    public class HGEntityTransferModule : EntityTransferModule, ISharedRegionModule, IEntityTransferModule, IUserAgentVerificationModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Initialized = false;

        private GatekeeperServiceConnector m_GatekeeperConnector;

        #region ISharedRegionModule

        public override string Name
        {
            get { return "HGEntityTransferModule"; }
        }

        public override void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("EntityTransferModule", "");
                if (name == Name)
                {
                    m_agentsInTransit = new List<UUID>();
                    
                    m_Enabled = true;
                    m_log.InfoFormat("[HG ENTITY TRANSFER MODULE]: {0} enabled.", Name);
                }
            }
        }

        public override void AddRegion(Scene scene)
        {
            base.AddRegion(scene);
            if (m_Enabled)
            {
                scene.RegisterModuleInterface<IUserAgentVerificationModule>(this);
            }
        }

        protected override void OnNewClient(IClientAPI client)
        {
            client.OnTeleportHomeRequest += TeleportHome;
            client.OnConnectionClosed += new Action<IClientAPI>(OnConnectionClosed);
        }


        public override void RegionLoaded(Scene scene)
        {
            base.RegionLoaded(scene);
            if (m_Enabled)
                if (!m_Initialized)
                {
                    m_GatekeeperConnector = new GatekeeperServiceConnector(scene.AssetService);
                    m_Initialized = true;
                }

        }
        public override void RemoveRegion(Scene scene)
        {
            base.AddRegion(scene);
            if (m_Enabled)
            {
                scene.UnregisterModuleInterface<IUserAgentVerificationModule>(this);
            }
        }


        #endregion

        #region HG overrides of IEntiryTransferModule

        protected override GridRegion GetFinalDestination(GridRegion region)
        {
            int flags = m_aScene.GridService.GetRegionFlags(m_aScene.RegionInfo.ScopeID, region.RegionID);
            m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: region {0} flags: {1}", region.RegionID, flags);
            if ((flags & (int)OpenSim.Data.RegionFlags.Hyperlink) != 0)
            {
                m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: Destination region {0} is hyperlink", region.RegionID);
                return m_GatekeeperConnector.GetHyperlinkRegion(region, region.RegionID);
            }
            return region;
        }

        protected override bool NeedsClosing(uint oldRegionX, uint newRegionX, uint oldRegionY, uint newRegionY, GridRegion reg)
        {
            if (base.NeedsClosing(oldRegionX, newRegionX, oldRegionY, newRegionY, reg))
                return true;

            int flags = m_aScene.GridService.GetRegionFlags(m_aScene.RegionInfo.ScopeID, reg.RegionID);
            if (flags == -1 /* no region in DB */ || (flags & (int)OpenSim.Data.RegionFlags.Hyperlink) != 0)
                return true;

            return false;
        }

        protected override void AgentHasMovedAway(UUID sessionID, bool logout)
        {
            if (logout)
                // Log them out of this grid
                m_aScene.PresenceService.LogoutAgent(sessionID);
        }

        protected override bool CreateAgent(ScenePresence sp, GridRegion reg, GridRegion finalDestination, AgentCircuitData agentCircuit, uint teleportFlags, out string reason, out bool logout)
        {
            reason = string.Empty;
            logout = false;
            int flags = m_aScene.GridService.GetRegionFlags(m_aScene.RegionInfo.ScopeID, reg.RegionID);
            if (flags == -1 /* no region in DB */ || (flags & (int)OpenSim.Data.RegionFlags.Hyperlink) != 0)
            {
                // this user is going to another grid
                if (agentCircuit.ServiceURLs.ContainsKey("HomeURI"))
                {
                    string userAgentDriver = agentCircuit.ServiceURLs["HomeURI"].ToString();
                    IUserAgentService connector = new UserAgentServiceConnector(userAgentDriver);
                    bool success = connector.LoginAgentToGrid(agentCircuit, reg, finalDestination, out reason);
                    logout = success; // flag for later logout from this grid; this is an HG TP

                    return success;
                }
                else
                {
                    m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: Agent does not have a HomeURI address");
                    return false;
                }
            }

            return m_aScene.SimulationService.CreateAgent(reg, agentCircuit, teleportFlags, out reason);
        }

        public override void TeleportHome(UUID id, IClientAPI client)
        {
            m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: Request to teleport {0} {1} home", client.FirstName, client.LastName);

            // Let's find out if this is a foreign user or a local user
            UserAccount account = m_aScene.UserAccountService.GetUserAccount(m_aScene.RegionInfo.ScopeID, id);
            if (account != null)
            {
                // local grid user
                m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: User is local");
                base.TeleportHome(id, client);
                return;
            }

            // Foreign user wants to go home
            // 
            AgentCircuitData aCircuit = ((Scene)(client.Scene)).AuthenticateHandler.GetAgentCircuitData(client.CircuitCode);
            if (aCircuit == null || (aCircuit != null && !aCircuit.ServiceURLs.ContainsKey("HomeURI")))
            {
                client.SendTeleportFailed("Your information has been lost");
                m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: Unable to locate agent's gateway information");
                return;
            }

            IUserAgentService userAgentService = new UserAgentServiceConnector(aCircuit.ServiceURLs["HomeURI"].ToString());
            Vector3 position = Vector3.UnitY, lookAt = Vector3.UnitY;
            GridRegion finalDestination = userAgentService.GetHomeRegion(aCircuit.AgentID, out position, out lookAt);
            if (finalDestination == null)
            {
                client.SendTeleportFailed("Your home region could not be found");
                m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: Agent's home region not found");
                return;
            }

            ScenePresence sp = ((Scene)(client.Scene)).GetScenePresence(client.AgentId);
            if (sp == null)
            {
                client.SendTeleportFailed("Internal error");
                m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: Agent not found in the scene where it is supposed to be");
                return;
            }

            IEventQueue eq = sp.Scene.RequestModuleInterface<IEventQueue>();
            GridRegion homeGatekeeper = MakeRegion(aCircuit);
            
            m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: teleporting user {0} {1} home to {2} via {3}:{4}:{5}",
                aCircuit.firstname, aCircuit.lastname, finalDestination.RegionName, homeGatekeeper.ExternalHostName, homeGatekeeper.HttpPort, homeGatekeeper.RegionName);

            DoTeleport(sp, homeGatekeeper, finalDestination, position, lookAt, (uint)(Constants.TeleportFlags.SetLastToTarget | Constants.TeleportFlags.ViaHome), eq);
        }
        #endregion

        #region IUserAgentVerificationModule

        public bool VerifyClient(AgentCircuitData aCircuit, string token)
        {
            if (aCircuit.ServiceURLs.ContainsKey("HomeURI"))
            {
                string url = aCircuit.ServiceURLs["HomeURI"].ToString();
                IUserAgentService security = new UserAgentServiceConnector(url);
                return security.VerifyClient(aCircuit.SessionID, token);
            }

            return false;
        }

        void OnConnectionClosed(IClientAPI obj)
        {
            if (obj.IsLoggingOut)
            {
                object sp = null;
                if (obj.Scene.TryGetScenePresence(obj.AgentId, out sp))
                {
                    if (((ScenePresence)sp).IsChildAgent)
                        return;
                }

                // Let's find out if this is a foreign user or a local user
                UserAccount account = m_aScene.UserAccountService.GetUserAccount(m_aScene.RegionInfo.ScopeID, obj.AgentId);
                if (account != null)
                {
                    // local grid user
                    return;
                }

                AgentCircuitData aCircuit = ((Scene)(obj.Scene)).AuthenticateHandler.GetAgentCircuitData(obj.CircuitCode);

                if (aCircuit.ServiceURLs.ContainsKey("HomeURI"))
                {
                    string url = aCircuit.ServiceURLs["HomeURI"].ToString();
                    IUserAgentService security = new UserAgentServiceConnector(url);
                    security.LogoutAgent(obj.AgentId, obj.SessionId);
                    //m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: Sent logout call to UserAgentService @ {0}", url);
                }
                else
                    m_log.DebugFormat("[HG ENTITY TRANSFER MODULE]: HomeURI not found for agent {0} logout", obj.AgentId);
            }
        }

        #endregion

        private GridRegion MakeRegion(AgentCircuitData aCircuit)
        {
            GridRegion region = new GridRegion();

            Uri uri = null;
            if (!aCircuit.ServiceURLs.ContainsKey("HomeURI") || 
                (aCircuit.ServiceURLs.ContainsKey("HomeURI") && !Uri.TryCreate(aCircuit.ServiceURLs["HomeURI"].ToString(), UriKind.Absolute, out uri)))
                return null;

            region.ExternalHostName = uri.Host;
            region.HttpPort = (uint)uri.Port;
            region.RegionName = string.Empty;
            region.InternalEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("0.0.0.0"), (int)0);
            return region;
        }
    }
}
