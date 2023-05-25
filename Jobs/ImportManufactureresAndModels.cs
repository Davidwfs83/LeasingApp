using Jobs;
using log4net;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;



namespace ConsoleAppTest
{

    internal class ImportManufactureresAndModels
    {
        IOrganizationService _service;
        ILog _log;
        public ImportManufactureresAndModels(IOrganizationService service, ILog log)
        {
            _service = service;
            _log = log;
        }
        internal void Execute()
        {
            string requestId = ConfigurationManager.AppSettings["requestId"];
            int offset = 0;
            _log.Info($"Start ImpoertManufacturersAndModels");
            DataGovHandler dataGovHandler = new DataGovHandler();
            GetAllModelsAndManufacturers(out Dictionary<string, Entity> manufaturersDic, out Dictionary<string, Entity> modelsDic);
            _log.Info($"Get Data From CRM - Found {manufaturersDic.Count} manfacturers and {modelsDic.Count} models");

            var res = dataGovHandler.GetData(requestId, $"q=2023&limit=100&offset={offset}");
            while (res.result.records.Count > 0)
            {
                offset += res.result.records.Count;
                if (res.success && res.result.records != null && res.result.records.Count > 0)
                {
                    foreach (var model in res.result.records)
                    {
                        try
                        {
                            HandleManufacturerAndModel(model, modelsDic, manufaturersDic);
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"Error On Models loop for model {model.degem_cd}", ex);
                        }
                    }
                }
                else
                {
                    _log.Warn($"Faild to Retrieve Data from Gov Data {res.result}");
                }
                res = dataGovHandler.GetData(requestId, $"q=2023&limit=100&offset={offset}");
            }
        }

        private void HandleManufacturerAndModel(DataGovHandler.Record model, Dictionary<string, Entity> modelsDic, Dictionary<string, Entity> manufaturersDic)
        {
            Guid manufucturerId = manufaturersDic.ContainsKey(model.tozeret_cd) ? manufaturersDic[model.tozeret_cd].Id : CreateManufacturer(model, manufaturersDic);
            if (!modelsDic.ContainsKey($"{model.tozeret_cd}{model.degem_cd}"))
            {
                CreateNewModel(model, manufucturerId);
            }
        }

        private void CreateNewModel(DataGovHandler.Record model, Guid manufucturerId)
        {
            Entity model_CRM = new Entity("gad_model");
            model_CRM.Attributes.Add("gad_name", model.kinuy_mishari + model.degem_nm);
            model_CRM.Attributes.Add("gad_code", $"{model.tozeret_cd}{model.degem_cd}");
            model_CRM.Attributes.Add("gad_manufacturerid", new EntityReference("gad_manufacturer", manufucturerId));
            _service.Create(model_CRM);
        }

        private Guid CreateManufacturer(DataGovHandler.Record model, Dictionary<string, Entity> manufaturersDic)
        {
            Entity entity = new Entity("gad_manufacturer");
            entity.Attributes.Add("gad_manufacturer_code", model.tozeret_cd);
            entity.Attributes.Add("gad_name", model.tozeret_nm);
            entity.Attributes.Add("gad_counryorigin", model.tozeret_eretz_nm);
            Guid manufId = _service.Create(entity);
            entity.Id = manufId;
            manufaturersDic.Add(model.tozeret_cd, entity);
            return manufId;
        }

        private void GetAllModelsAndManufacturers(out Dictionary<string, Entity> manufaturersDic, out Dictionary<string, Entity> modelsDic)
        {
            manufaturersDic = new Dictionary<string, Entity>();
            modelsDic = new Dictionary<string, Entity>();
            // Instantiate QueryExpression query
            var query = new QueryExpression("gad_model");

            // Add columns to query.ColumnSet
            query.ColumnSet.AddColumns("gad_modelid", "gad_name", "gad_code", "gad_manufacturerid");

            // Add link-entity manuf
            var manuf = query.AddLink("gad_manufacturer", "gad_manufacturerid", "gad_manufacturerid");
            manuf.EntityAlias = "manuf";

            // Add columns to manuf.Columns
            manuf.Columns.AddColumns("gad_manufacturer_code", "gad_name", "gad_manufacturerid");
            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = 500;
            query.PageInfo.PageNumber = 1;
            EntityCollection results = _service.RetrieveMultiple(query);
            HandleResults(manufaturersDic, modelsDic, results);
            while (results.MoreRecords)
            {
                // Retrieve the next page of results
                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = results.PagingCookie;
                results = _service.RetrieveMultiple(query);
                // Process the next page of results
                HandleResults(manufaturersDic, modelsDic, results);
            }
        }

        private void HandleResults(Dictionary<string, Entity> manufaturersDic, Dictionary<string, Entity> modelsDic, EntityCollection results)
        {
            foreach (var entity in results.Entities)
            {
                try
                {
                    SetDictionary(entity, manufaturersDic, modelsDic);
                }
                catch (Exception ex)
                {
                    _log.Error($"Error in Entity Loop for entityId {entity.Id}", ex);
                }

            }
        }

        private static void SetDictionary(Entity entity, Dictionary<string, Entity> manufaturersDic, Dictionary<string, Entity> modelsDic)
        {
            string modelCode = entity.Attributes.Contains("gad_code") ? entity["gad_code"].ToString() : string.Empty;
            string manufacturerCode = entity.Attributes.Contains("manuf.gad_manufacturer_code") ? ((AliasedValue)entity["manuf.gad_manufacturer_code"]).Value.ToString() : string.Empty;
            string manufacturerName = entity.Attributes.Contains("manuf.gad_name") ? ((AliasedValue)entity["manuf.gad_name"]).Value.ToString() : string.Empty;
            Guid manufacturerGuid = entity.Attributes.Contains("manuf.gad_manufacturerid") ? (Guid)((AliasedValue)entity["manuf.gad_manufacturerid"]).Value : Guid.Empty;
            if (string.IsNullOrEmpty(modelCode) || string.IsNullOrEmpty(manufacturerCode))
            {
                return;
            }
            if (!manufaturersDic.ContainsKey(manufacturerCode))
            {
                manufaturersDic.Add(manufacturerCode, new Entity
                {
                    LogicalName = "gad_manufacturer",
                    Id = manufacturerGuid,
                    Attributes = new AttributeCollection
                    {
                        new KeyValuePair<string, object>("gad_manufacturer_code", manufacturerCode ),
                        new KeyValuePair<string, object>("gad_name", manufacturerName ),
                    }
                });
            }
            if (!modelsDic.ContainsKey(modelCode))
            {
                modelsDic.Add(manufacturerCode + modelCode, entity);
            }

        }
    }
}