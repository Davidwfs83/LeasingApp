
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
            // Get All the Model  stored at the Dynamics, log them (debug mode) and store them locally (private)
            _log.Info($"Start Import Models and Manufactureres Procedure!");
            GetAllModelFromDynamics();
            _log.Info($"Get Data From CRM - Found {manuDic.Count} manfacturers and {modelDic.Count} models");
            _log.Debug($"Models From CRM: /n");
            foreach (KeyValuePair<string, dvd_mod> kvp in modelDic)
            {
                _log.Debug($"|| {kvp.Key} ||");
            }

            // Get All the Model  stored at the Dynamics, log them (debug mode) and store them locally (private)
            GetAllManuFromDynamics();
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
            int offset = 0;
            DataGovHandler.Response response = null;
            Func<int, string> query = (os) =>
            {
                return $"filters={{\"shnat_yitzur\":\"2023\"}}&fields=sug_degem,tozeret_cd,tozar,tozeret_eretz_nm,degem_cd,degem_nm,kinuy_mishari" +
                $"&limit=100&offset={os}";
            };


            // First Call To Api 
            response = dataGovHandler.GetData(query(offset));
            while (response.result.records.Count > 0)
            {
                offset += response.result.records.Count;
                if (response.success && response.result.records != null && response.result.records.Count > 0)
                {
                    foreach (var model in response.result.records)
                    {
                        try
                        {
                            HandleManufacturerAndModelFromGov(model);
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
                response = dataGovHandler.GetData(query(offset++));
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
                dvd_modcode = govRec.degem_cd + govRec.degem_nm,
                dvd_mod_manu = new EntityReference("dvd_manu", manufucturerId)
            };

            model.Id = _service.Create(model);
            modelDic.Add(govRec.tozeret_cd + govRec.degem_cd , model);
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

        private void HandleManufacturerAndModelFromGov(DataGovHandler.Record govRec)
        {
            // Check if record already exist, if not create it for both manufactureres and countries
            Guid countryId = countriesDic.ContainsKey(govRec.tozeret_eretz_nm) ? countriesDic[govRec.tozeret_eretz_nm].Id : CreateNewCountry(govRec); 
            Guid manufucturerId = manuDic.ContainsKey(govRec.sug_degem + govRec.tozeret_cd) ? manuDic[govRec.sug_degem + govRec.tozeret_cd].Id : CreateManufacturer(govRec, countryId);           
            if (!modelDic.ContainsKey(govRec.degem_cd + govRec.degem_nm))
            {
                CreateNewModel(govRec, manufucturerId);
            }
        }
        private void GetAllModelFromDynamics()
        {
            // Instantiate QueryExpression query
            var query = new QueryExpression("dvd_mod");

            // Add columns to query.ColumnSet
            query.ColumnSet.AddColumns("dvd_mod_manu", "dvd_name", "dvd_modcode");

           
            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = 500;
            query.PageInfo.PageNumber = 1;
            EntityCollection results = _service.RetrieveMultiple(query);
            HandleModelResult(results);
            while (results.MoreRecords)
            {
                // Retrieve the next page of results
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = results.PagingCookie;
                results = _service.RetrieveMultiple(query);
                // Process the next page of results
                HandleModelResult(results);
            }
        }
        private void GetAllManuFromDynamics()
        {
            // Instantiate QueryExpression query
            var query = new QueryExpression("dvd_manu");

            // Add columns to query.ColumnSet
            query.ColumnSet.AddColumns("dvd_manu_countries", "dvd_name", "dvd_manucode");


            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = 500;
            query.PageInfo.PageNumber = 1;
            EntityCollection results = _service.RetrieveMultiple(query);
            HandleManuResult(results);
            while (results.MoreRecords)
            {
                // Retrieve the next page of results
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = results.PagingCookie;
                results = _service.RetrieveMultiple(query);
                // Process the next page of results
                HandleManuResult(results);
            }
        }
        private void HandleManuResult(EntityCollection results)
        {
            foreach (var manu in results.Entities)
            {
                try
                {
                    string manuCode = manu.Attributes.Contains("dvd_manucode") ? manu["dvd_manucode"].ToString() : string.Empty;
                    if (!manuDic.ContainsKey(manuCode))
                    {
                        manuDic.Add(manuCode, manu.ToEntity<dvd_manu>());
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error in Model Loop for entityId {manu.Id}", ex);
                }
            }
        }

        private void HandleModelResult(EntityCollection results)
        {
            foreach (var model in results.Entities)
            {
                try
                {
                    string modelCode = model.Attributes.Contains("dvd_modcode") ? model["dvd_modcode"].ToString() : string.Empty;
                    if (!modelDic.ContainsKey(modelCode))
                    {
                        modelDic.Add(modelCode, model.ToEntity<dvd_mod>());
                    }

                }
                catch (Exception ex)
                {
                    _log.Error($"Error in Model Loop for entityId {model.Id}", ex);
                }

            }
        }

        
    }
}

