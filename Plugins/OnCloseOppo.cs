using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Nipendo.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugins
{
    public class OnCloseOppo : IPlugin
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
            if (context.InputParameters.Contains("OpportunityClose") & context.InputParameters["OpportunityClose"] is Entity)
            {
                Entity opportunityCloseEntity = (Entity)context.InputParameters["OpportunityClose"];
                Guid opportunityId = opportunityCloseEntity.GetAttributeValue<EntityReference>("opportunityid").Id;

                // we cant use images in this type of message apprently so we need to do a retrive
                Opportunity OpportunityEnt = service.Retrieve("opportunity", opportunityId, new ColumnSet("dvd_driverphone", "dvd_driveremail", "dvd_oppor_car", "dvd_driverlastname", "dvd_driverfirstname", "dvd_driverid", "parentaccountid")).ToEntity<Opportunity>();
                dvd_cardriver newCarDriver = new dvd_cardriver
                {
                    dvd_driverid = OpportunityEnt.dvd_driverid,       
                    dvd_firstname = OpportunityEnt.dvd_driverfirstname,
                    dvd_lastname = OpportunityEnt.dvd_driverlastname,
                    dvd_phone = OpportunityEnt.dvd_driverphone,
                    dvd_email = OpportunityEnt.EmailAddress,
                    dvd_Name = OpportunityEnt.dvd_driverfirstname + " - " + OpportunityEnt.dvd_driverlastname,
                    dvd_cardriver_account = OpportunityEnt.ParentAccountId,
                    dvd_cardriver_car = OpportunityEnt.dvd_oppor_car,
                    dvd_cardriver_oppor = new EntityReference("opportunity", opportunityId)
                };
                service.Create(newCarDriver);
            }
        }
    }
}
