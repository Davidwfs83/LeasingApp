using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Nipendo.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins.Actions
{
    public class AddIncidentFromExternal : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace($"On Oppo Close Create Car Driver");

            if (context.Depth > 1)
                return;

            // Extract all the incoming parameters
            string hpNum = context.InputParameters.Contains("hp_num") ? context.InputParameters["hp_num"].ToString() : null;
            // Both idNum and carNum are required by the Action but we still make the check
            string idNum = context.InputParameters.Contains("id_num") ? context.InputParameters["id_num"].ToString() : null;
            string carNum = context.InputParameters.Contains("car_num") ? context.InputParameters["car_num"].ToString() : null;
            int incidentType = context.InputParameters.Contains("incident_type") ? (int)context.InputParameters["incident_type"] : 0;
            if (incidentType < 1 || incidentType > 5)
            {
                context.OutputParameters["response_is_success"] = false;
                context.OutputParameters["response_description"] = "Invalid Incident Type!";
                return;
            }
            string incdientDescription = context.InputParameters.Contains("incident_description") ? (string)context.InputParameters["incident_description"] : null;


            // Find Car
            dvd_car car = FindCarByNumber(carNum, service);
            if (car is null)
            {
                context.OutputParameters["response_is_success"] = false;
                context.OutputParameters["response_description"] = "Car Number not found!";
                return;
            }

            // Find Driver
            dvd_cardriver driver = FindDriverByCarOrId(service, car, idNum);
            if (driver is null)
            {
                context.OutputParameters["response_is_success"] = false;
                context.OutputParameters["response_description"] = "Driver not found!";
                return;
            }

            // Find Account
            Account account = null;
            EntityReference accountRef = null;
            if (!String.IsNullOrEmpty(hpNum)) // Since its not a required field, we Check
            {
                account = FindAccountByHpNum(service, hpNum);
            }
            if (account is null)
            {

                if (driver.dvd_cardriver_account is null)
                {
                    context.OutputParameters["response_is_success"] = false;
                    context.OutputParameters["response_description"] = "Account Not Found";
                    return;
                }
                accountRef = driver.dvd_cardriver_account;
            }
            else
            {
                accountRef = new EntityReference("account", account.Id);
            }
            Guid newIncidentGuid = CreateIncident(service, incidentType, car, incdientDescription, driver, accountRef);
            context.OutputParameters["response_is_success"] = true;
            context.OutputParameters["response_description"] = "Incident Created";
            context.OutputParameters["created_incident"] = new EntityReference("incident", newIncidentGuid);

        }
        private dvd_car FindCarByNumber(string carNum, IOrganizationService service)
        {
            QueryExpression query = new QueryExpression("dvd_car");
            query.ColumnSet = new ColumnSet("dvd_carid", "dvd_name");
            query.Criteria.AddCondition("dvd_name", ConditionOperator.Equal, carNum);
            EntityCollection Cars = service.RetrieveMultiple(query);
            if (Cars.Entities.Count > 0)
            {
                return Cars.Entities[0].ToEntity<dvd_car>();
            }
            return null;
        }

        private dvd_cardriver FindDriverByCarOrId(IOrganizationService service, dvd_car car, string driverId)
        {
            QueryExpression query = new QueryExpression("dvd_cardriver");
            query.ColumnSet = new ColumnSet("dvd_driverid", "dvd_cardriverid", "dvd_cardriver_car", "dvd_cardriver_account"); // dvd_driverid - ID in israeli ID, dvd_cardriverid - Dynamics entity Id
            FilterExpression filter = new FilterExpression(LogicalOperator.Or);
            filter.AddCondition(new ConditionExpression("dvd_cardriver_car", ConditionOperator.Equal, car.Id));
            filter.AddCondition(new ConditionExpression("dvd_driverid", ConditionOperator.Equal, driverId));
            query.Criteria.AddFilter(filter);
            EntityCollection carDrivers = service.RetrieveMultiple(query);
            dvd_cardriver result = null;
            foreach (Entity carDriver in carDrivers.Entities)
            {
                dvd_cardriver driver = carDriver.ToEntity<dvd_cardriver>();
                // Check to see if the ID is the criteria that matched, if yes sincce its first priority criteria than return imddedialtly
                if (driver.dvd_driverid == driverId)
                {
                    result = driver;
                    break;
                }

                else  // If Car Matched, we make sure id is null/empty or matching aswell
                {
                    if (!(String.IsNullOrEmpty(driver.dvd_driverid)) && (driver.dvd_driverid != driverId)) // there is ID but dosent match the one provided
                    {
                      continue;
                    }
                    result = driver;
                }
                
            }
            return result;
        }

        private Account FindAccountByHpNum(IOrganizationService service, string hpNum)
        {
            QueryExpression query = new QueryExpression("account");
            query.ColumnSet = new ColumnSet("accountid");
            query.Criteria.AddCondition("dvd_numberhp", ConditionOperator.Equal, hpNum);
            EntityCollection accounts = service.RetrieveMultiple(query);
            if (accounts.Entities.Count > 0)
            {
                return accounts.Entities[0].ToEntity<Account>();
            }
            return null;
        }
        private Guid CreateIncident(IOrganizationService service, int incidentType, dvd_car car, string incidentDesc, dvd_cardriver driver, EntityReference accountRef)
        {
            // This specific Global Set is local to Incident so we cannot use an early generated model
            string incidentName = incidentType == 1 ? "תקלה" :
                  incidentType == 2 ? "תאונה" :
                  incidentType == 3 ? "טסט" :
                  incidentType == 4 ? "טיפול" :
                  incidentType == 5 ? "אחר" :
                  "אחר";
            Incident newIncident = new Incident
            {
                Title = car.dvd_Name + " - " + incidentName,
                Description = incidentDesc,
                CaseTypeCode = new OptionSetValue(incidentType),
                dvd_incident_car = new EntityReference("dvd_car", car.Id),
                dvd_incident_cardriver = new EntityReference("dvd_cardriver", driver.Id),
                CustomerId = accountRef
            };
            return service.Create(newIncident);
        }

    }
}
