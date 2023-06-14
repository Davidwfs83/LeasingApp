using log4net;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Configuration;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Jobs
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            var service = GetService();
            new ManuModImport(service, log).Execute();
            //new Shed(service).DeleteAllManu();
        }

        public static IOrganizationService GetService()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["crm"].ConnectionString;
            var client = new CrmServiceClient(connectionString);
            if (client.IsReady == false)
            {
                throw new Exception($"Crm Service Client is not ready: {client.LastCrmError}", client.LastCrmException);
            }
            return client;
        }
    }
}
