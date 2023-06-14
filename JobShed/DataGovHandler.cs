using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;

namespace Jobs
{
    internal class DataGovHandler
    {
        readonly string baseUrl = "https://data.gov.il/api/3/action/datastore_search?resource_id=142afde2-6228-49f9-8a29-9b6c3a0cbe40";
        public DataGovHandler()
        {


        }

        public Response GetData( string query)
        {
            string url = $"{baseUrl}&{query}";
            string host = "data.gov.il";

            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Host = host;

                HttpResponseMessage response =  client.SendAsync(request).Result;
                string jsonResponse =  response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<Response>(jsonResponse);
            }
        }


        public class Record
        {
            public string sug_degem { get; set; }
            public string tozeret_cd { get; set; }
            public string tozar { get; set; }
            public string tozeret_eretz_nm { get; set; }
            public string degem_cd { get; set; }
            public string degem_nm { get; set; }
            public string kinuy_mishari { get; set; }           
        }

        public class Result
        {
            public bool include_total { get; set; }
            public int limit { get; set; }
            public string q { get; set; }
            public string records_format { get; set; }
            public string resource_id { get; set; }
            public object total_estimation_threshold { get; set; }
            public List<Record> records { get; set; }
            public int total { get; set; }
            public bool total_was_estimated { get; set; }
        }

        public class Response
        {
            public bool success { get; set; }
            public Result result { get; set; }
        }
    }
}