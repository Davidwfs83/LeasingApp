using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jobs
{
    class Shed
    {
        IOrganizationService _service;
        
        internal Shed(IOrganizationService service)
        {
            _service = service;            
        }
        // becuase of bug in powerapps (the bug when you open organization level entity in hebrew you get no ribbon)
        // so i used this helper method as a short cut of a sorts
        internal void DeleteAllManu()
        {
            // Create a query expression to retrieve all the Lead records
            QueryExpression query = new QueryExpression("dvd_manu");

            // Set the column set to retrieve only the lead's ID
            query.ColumnSet = new ColumnSet("dvd_manuid");

            // Retrieve the Lead records
            EntityCollection leads = _service.RetrieveMultiple(query);

            // Delete each Lead record
            foreach (var lead in leads.Entities)
            {
                _service.Delete("dvd_manu", lead.Id);
            }

        }
    }
}
