using System;
using System.Collections.Generic;
using log4net;
using System.Reflection;
using System.Text;
using System.IO;

namespace OpenSim.Framework.Servers.HttpServer
{
    public class OWSStreamHandler : BaseStreamHandler
    {
        public delegate string OWSMethod(string request, string path, string param,
        OSHttpRequest httpRequest, OSHttpResponse httpResponse);
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private OWSMethod m_owsMethod;

        public OWSMethod Method
        {
            get { return m_owsMethod; }
        }

        public override byte[] Handle(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            m_log.Info("[WebMap]: Web map request received");
            Encoding encoding = Encoding.UTF8;
            StreamReader streamReader = new StreamReader(request, encoding);

            string requestBody = streamReader.ReadToEnd();
            streamReader.Close();

            string param = GetParam(path);
            string responseString = m_owsMethod(requestBody, path, param, httpRequest, httpResponse);

            m_log.Info("[WebMap]: Web map request responded");
            if (httpResponse.ContentType == "image/png")
            {
                return Convert.FromBase64String(responseString);
            }

            return Encoding.UTF8.GetBytes(responseString);
        }

        public OWSStreamHandler(string httpMethod, string path, OWSMethod owsMethod)
            : base(httpMethod, path)
        {
            m_owsMethod = owsMethod;
        }
    }
}
