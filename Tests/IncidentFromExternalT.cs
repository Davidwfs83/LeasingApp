using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Nipendo.Common.Entities;
using System;
using System.Configuration;

namespace Tests
{
    [TestClass]
    public class IncidentFromExternalT
    {
        private readonly IOrganizationService service;
        private EntityCollection drivers;
        private Random rnd = new Random();
        public IncidentFromExternalT()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["crm"].ConnectionString;
            var crmServiceClient = new CrmServiceClient(connectionString);
            service = crmServiceClient;
            drivers = GetRandomDriver();

        }
        private ParameterCollection ActionCaller(string hpnum, int incidientType, string incidentDescription, string idNum,string carNum)
        {
            var executeAction = service.Execute(new OrganizationRequest("dvd_AddIncidentFromExternal") {
                
                Parameters = {
                    { "hp_num" , hpnum },
                    { "incident_type" , incidientType },
                    { "incident_description" , incidentDescription },
                    { "id_num" , idNum },
                    { "car_num" , carNum }
                }
            });
            return executeAction.Results;
        }
        // Create Regular Incident With No Exptected Exceptions
        [TestMethod]
        public void CreateIncident()
        {
            Entity driver = drivers.Entities[rnd.Next(0, drivers.Entities.Count)];
            string carNumb = driver.Attributes.Contains("car.dvd_name") ? ((AliasedValue)driver["car.dvd_name"]).Value.ToString() : string.Empty;
            string driverId = driver.Attributes.Contains("dvd_name") ? ((string)driver["dvd_name"]) : string.Empty;
            string hpNumb = driver.Attributes.Contains("acc.dvd_numberhp") ? ((AliasedValue)driver["acc.dvd_numberhp"]).Value.ToString() : string.Empty;
            ActionCaller(hpNumb, rnd.Next(1,6), "testdescription33",driverId, carNumb); // Entered valid information for exisitng entities
        }

        private EntityCollection GetRandomDriver()
        {
            QueryExpression query = new QueryExpression("dvd_cardriver");
            query.ColumnSet = new ColumnSet("dvd_name", "dvd_cardriver_account" , "dvd_cardriver_car");
            var car = query.AddLink("dvd_car", "dvd_cardriver_car", "dvd_carid");
            car.EntityAlias = "car";
            car.Columns.AddColumns("dvd_name");
            var account = query.AddLink("account", "dvd_cardriver_account", "accountid");
            account.EntityAlias = "acc";
            account.Columns.AddColumns("dvd_numberhp");

            return  service.RetrieveMultiple(query);
            
        }
    }
}
