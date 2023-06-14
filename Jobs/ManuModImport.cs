using log4net;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Nipendo.Common.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jobs
{
    internal class ManuModImport
    {

        IOrganizationService _service;
        ILog _log;
        Dictionary<string, dvd_manu> manuDic = new Dictionary<string, dvd_manu>();
        Dictionary<string, dvd_mod> modelDic = new Dictionary<string, dvd_mod>();
        Dictionary<string, dvd_countries> countriesDic = new Dictionary<string, dvd_countries>();
        internal  ManuModImport(IOrganizationService service, ILog log)
        {
            _service = service;
            _log = log;
        }

        internal void Execute()
        {
            // Get All the Model And Manufactureres currently stored at the Dynamics, log them (debug mode) and store them locally (private)
            _log.Info($"Start Import Models and Manufactureres Procedure!");
            GetAllModelAndManuFromDynamics();
            _log.Info($"Get Data From CRM - Found {manuDic.Count} manfacturers and {modelDic.Count} models");
            _log.Debug($"Models From CRM: /n");
            foreach (KeyValuePair<string, dvd_mod> kvp in modelDic)
            {
                _log.Debug($"|| {kvp.Key} ||");
            }
            _log.Debug($"Manufacturers From CRM: /n");
            foreach (KeyValuePair<string, dvd_manu> kvp in manuDic)
            {
                _log.Debug($"|| {kvp.Key} ||");
            }

            // Get All Countries curently stored at the Dynamics, store and log
            GetAllCountries();
            _log.Debug($"Manufacturers From CRM: /n");
            foreach (KeyValuePair<string, dvd_countries> kvp in countriesDic)
            {
                _log.Debug($"|| {kvp.Value.Id} ||");
            }

            // We Layout the groundwork for the mechanism responsible retriving the data from the GOV API
            DataGovHandler dataGovHandler = new DataGovHandler();
            string requestId = ConfigurationManager.AppSettings["requestId"];
            int offset = 0;
            DataGovHandler.Response response = null;
            Func<int, string> query = (os) =>
            {
                return $"filters={{\"shnat_yitzur\":\"2023\"}}&fields=sug_degem,tozeret_cd,tozar,tozeret_eretz_nm,degem_cd,degem_nm,kinuy_mishari" +
                $"&limit=100&offset={os}";
            };


            // First Call To Api 
            response = dataGovHandler.GetData(requestId, query(offset));
            while (response.result.records.Count > 0)
            {
                offset += response.result.records.Count;
                if (response.success && response.result.records != null && response.result.records.Count > 0)
                {
                    foreach (var model in response.result.records)
                    {
                        try
                        {
                            HandleManufacturerAndModel(model);
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"Error On Models loop for model {model.degem_cd}", ex);
                        }
                    }
                }
                else
                {
                    _log.Warn($"Faild to Retrieve Data from Gov Data {response.result}");
                }
                response = dataGovHandler.GetData(requestId, query(offset++));
            }


        }

        private void GetAllCountries()
        {
            QueryExpression query = new QueryExpression("dvd_countries");
            query.Criteria.AddCondition(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            query.ColumnSet = new ColumnSet("dvd_name", "dvd_countriesid");
            EntityCollection countriesCollection = _service.RetrieveMultiple(query);
            foreach (Entity country in countriesCollection.Entities)
            {
                countriesDic.Add(country.GetAttributeValue<string>("dvd_name"), country.ToEntity<dvd_countries>());
            }
        }
        private Guid CreateNewCountry(DataGovHandler.Record model)
        {
            dvd_countries country = new dvd_countries
            {
                dvd_Name = model.tozeret_eretz_nm
            };
            country.Id = _service.Create(country);
            countriesDic.Add(model.tozeret_eretz_nm, country);
            return country.Id;
        }

        private void CreateNewModel(DataGovHandler.Record govRec, Guid manufucturerId)
        {
            dvd_mod model = new dvd_mod
            {
                dvd_Name = govRec.kinuy_mishari + govRec.degem_nm,
                dvd_modcode = $"{govRec.tozeret_cd}{govRec.degem_cd}",
                dvd_mod_manu = new EntityReference("gad_manufacturer", manufucturerId)
            };

            model.Id = _service.Create(model);
            modelDic.Add($"{govRec.tozeret_cd}{govRec.degem_cd}", model);
        }
        private Guid CreateManufacturer(DataGovHandler.Record govRec, Guid originCountryId)
        {
            dvd_manu manufacture = new dvd_manu
            {
                dvd_manucode = govRec.sug_degem + govRec.tozeret_cd,
                dvd_Name = govRec.tozar,
                dvd_manu_countries = new EntityReference("dvd_countries", originCountryId)

            };
            Guid manufId = _service.Create(manufacture);
            manufacture.Id = manufId;
            manuDic.Add(govRec.sug_degem + govRec.tozeret_cd, manufacture);
            return manufId;
        }

        private void HandleManufacturerAndModel(DataGovHandler.Record govRec)
        {
            // Check if record already exist, if not create it for both manufactureres and countries
            Guid countryId = countriesDic.ContainsKey(govRec.tozeret_eretz_nm) ? countriesDic[govRec.tozeret_eretz_nm].Id : CreateNewCountry(govRec); 
            Guid manufucturerId = manuDic.ContainsKey(govRec.tozeret_cd) ? manuDic[govRec.tozeret_cd].Id : CreateManufacturer(govRec, countryId);           
            if (!modelDic.ContainsKey($"{govRec.tozeret_cd}{govRec.degem_cd}"))
            {
                CreateNewModel(govRec, manufucturerId);
            }
        }
        private void GetAllModelAndManuFromDynamics()
        {
            // Instantiate QueryExpression query
            var query = new QueryExpression("dvd_mod");

            // Add columns to query.ColumnSet
            query.ColumnSet.AddColumns("dvd_modid", "dvd_name", "dvd_modecode", "dvd_manuid");

            // Add link-entity manuf
            var manuf = query.AddLink("dvd_manu", "dvd_manuid", "dvd_manuid");
            manuf.EntityAlias = "manuf";

            // Add columns to manuf.Columns
            manuf.Columns.AddColumns("dvd_manucode", "dvd_name", "dvd_manuid");
            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = 500;
            query.PageInfo.PageNumber = 1;
            EntityCollection results = _service.RetrieveMultiple(query);
            HandleResults(results);
            while (results.MoreRecords)
            {
                // Retrieve the next page of results
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = results.PagingCookie;
                results = _service.RetrieveMultiple(query);
                // Process the next page of results
                HandleResults(results);
            }
        }

        private void HandleResults(EntityCollection results)
        {
            foreach (var entity in results.Entities)
            {
                try
                {
                    SetDictionary(entity);
                }
                catch (Exception ex)
                {
                    _log.Error($"Error in Entity Loop for entityId {entity.Id}", ex);
                }

            }
        }

        private void SetDictionary(Entity entity)
        {
            string modelCode = entity.Attributes.Contains("dvd_modcode") ? entity["dvd_modcode"].ToString() : string.Empty;
            string manufacturerCode = entity.Attributes.Contains("manuf.dvd_manu_code") ? ((AliasedValue)entity["manuf.dvd_manu_code"]).Value.ToString() : string.Empty;
            string manufacturerName = entity.Attributes.Contains("manuf.dvd_name") ? ((AliasedValue)entity["manuf.dvd_name"]).Value.ToString() : string.Empty;
            Guid manufacturerGuid = entity.Attributes.Contains("manuf.dvd_manuid") ? (Guid)((AliasedValue)entity["manuf.dvd_manuid"]).Value : Guid.Empty;
            if (string.IsNullOrEmpty(modelCode) || string.IsNullOrEmpty(manufacturerCode))
            {
                return;
            }
            if (!manuDic.ContainsKey(manufacturerCode))
            {
                manuDic.Add(manufacturerCode, new dvd_manu
                {
                    Id = manufacturerGuid,
                    dvd_manucode = manufacturerCode,
                    dvd_Name = manufacturerName
                });
            }
            if (!modelDic.ContainsKey(modelCode))
            {
                modelDic.Add(modelCode, entity.ToEntity<dvd_mod>());
            }

        }
    }
}

