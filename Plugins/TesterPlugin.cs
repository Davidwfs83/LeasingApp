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
    public class TesterPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace($"On Update Asset Calc Expected Profit of Account");
            try
            {
                if (context.InputParameters.Contains("Target") & context.InputParameters["Target"] is Entity)
                {
                    Entity target = (Entity)context.InputParameters["Target"];
                    string name = target.Attributes.Contains("new_name") ? (string)target["new_name"] : null;
                    new_Model model = new new_Model();

                    GetAllCountries(service);
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private EntityCollection GetAllCountries(IOrganizationService service)
        {
            QueryExpression query = new QueryExpression("new_country");
            query.Criteria.AddCondition(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            query.ColumnSet = new ColumnSet(true);
            return service.RetrieveMultiple(query);
        }
    }
}
