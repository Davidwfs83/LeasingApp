﻿using Microsoft.Xrm.Sdk;
using System;
using System.Configuration;

namespace Jobs
{
    class Program
    {
        static void Main(string[] args)
        {
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
