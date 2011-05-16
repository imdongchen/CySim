using System;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;

namespace OpenSim.Server.Handlers.Map
{
    public class WebMapServiceConnector : ServiceConnector
    {
        private string m_ConfigName = "WebMapService";
        private IWebMapService m_webMapService;

        public WebMapServiceConnector(IConfigSource config, IHttpServer server, string configName) :
            base(config, server, configName)
        {
            IConfig serverConfig = config.Configs[m_ConfigName];
            if (serverConfig == null)
                throw new Exception(String.Format("No section {0} in config file", m_ConfigName));

            string webMapService = serverConfig.GetString("LocalServiceModule",
                    String.Empty);

            if (webMapService == String.Empty)
                throw new Exception("No LocalServiceModule in config file");

            Object[] args = new Object[] { config };
            m_webMapService = ServerUtils.LoadPlugin<IWebMapService>(webMapService, args);


            server.AddStreamHandler(new WebMapServerGetHandler(m_webMapService));
        }
    }
}
