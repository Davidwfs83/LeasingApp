using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace Jobs
{
    internal class DataGovHandler
    {
        readonly string baseUrl = "https://data.gov.il/api/3/action/datastore_search?resource_id=";
        public DataGovHandler()
        {


        }

        public Response GetData(string requestId, string query)
        {

            string url = $"{baseUrl}{requestId}&{query}";
            string host = "data.gov.il"; // replace with your desired host name

            HttpClient client = new HttpClient();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Host = host;

            HttpResponseMessage response = client.SendAsync(request).Result;
            var jsonResponse = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<Response>(jsonResponse);


        }

        public class Record
        {
            public string _id { get; set; }
            public string sug_degem { get; set; }
            public string tozeret_cd { get; set; }
            public string tozeret_nm { get; set; }
            public string tozeret_eretz_nm { get; set; }
            public string tozar { get; set; }
            public string degem_cd { get; set; }
            public string degem_nm { get; set; }
            public string shnat_yitzur { get; set; }
            public string kvuzat_agra_cd { get; set; }
            public string nefah_manoa { get; set; }
            public string mishkal_kolel { get; set; }
            public string gova { get; set; }
            public string hanaa_cd { get; set; }
            public string hanaa_nm { get; set; }
            public string mazgan_ind { get; set; }
            public string abs_ind { get; set; }
            public string kariot_avir_source { get; set; }
            public string mispar_kariot_avir { get; set; }
            public string hege_koah_ind { get; set; }
            public string automatic_ind { get; set; }
            public string halonot_hashmal_source { get; set; }
            public string mispar_halonot_hashmal { get; set; }
            public string halon_bagg_ind { get; set; }
            public string galgaley_sagsoget_kala_ind { get; set; }
            public string argaz_ind { get; set; }
            public string merkav { get; set; }
            public string ramat_gimur { get; set; }
            public string delek_cd { get; set; }
            public string delek_nm { get; set; }
            public string mispar_dlatot { get; set; }
            public string koah_sus { get; set; }
            public string mispar_moshavim { get; set; }
            public string bakarat_yatzivut_ind { get; set; }
            public string kosher_grira_im_blamim { get; set; }
            public string kosher_grira_bli_blamim { get; set; }
            public string sug_tkina_cd { get; set; }
            public string sug_tkina_nm { get; set; }
            public string sug_mamir_cd { get; set; }
            public string sug_mamir_nm { get; set; }
            public string technologiat_hanaa_cd { get; set; }
            public string technologiat_hanaa_nm { get; set; }
            public object kamut_CO2 { get; set; }
            public object kamut_NOX { get; set; }
            public string kamut_PM10 { get; set; }
            public object kamut_HC { get; set; }
            public object kamut_HC_NOX { get; set; }
            public object kamut_CO { get; set; }
            public string kamut_CO2_city { get; set; }
            public string kamut_NOX_city { get; set; }
            public string kamut_PM10_city { get; set; }
            public string kamut_HC_city { get; set; }
            public string kamut_CO_city { get; set; }
            public string kamut_CO2_hway { get; set; }
            public string kamut_NOX_hway { get; set; }
            public string kamut_PM10_hway { get; set; }
            public string kamut_HC_hway { get; set; }
            public string kamut_CO_hway { get; set; }
            public string madad_yarok { get; set; }
            public string kvutzat_zihum { get; set; }
            public string bakarat_stiya_menativ_ind { get; set; }
            public string bakarat_stiya_menativ_makor_hatkana { get; set; }
            public string nitur_merhak_milfanim_ind { get; set; }
            public string nitur_merhak_milfanim_makor_hatkana { get; set; }
            public string zihuy_beshetah_nistar_ind { get; set; }
            public string bakarat_shyut_adaptivit_ind { get; set; }
            public string zihuy_holchey_regel_ind { get; set; }
            public string zihuy_holchey_regel_makor_hatkana { get; set; }
            public string maarechet_ezer_labalam_ind { get; set; }
            public string matzlemat_reverse_ind { get; set; }
            public string hayshaney_lahatz_avir_batzmigim_ind { get; set; }
            public string hayshaney_hagorot_ind { get; set; }
            public string nikud_betihut { get; set; }
            public string ramat_eivzur_betihuty { get; set; }
            public string teura_automatit_benesiya_kadima_ind { get; set; }
            public string shlita_automatit_beorot_gvohim_ind { get; set; }
            public string shlita_automatit_beorot_gvohim_makor_hatkana { get; set; }
            public string zihuy_matzav_hitkarvut_mesukenet_ind { get; set; }
            public string zihuy_tamrurey_tnua_ind { get; set; }
            public string zihuy_rechev_do_galgali { get; set; }
            public string zihuy_tamrurey_tnua_makor_hatkana { get; set; }
            public string CO2_WLTP { get; set; }
            public string HC_WLTP { get; set; }
            public string PM_WLTP { get; set; }
            public string NOX_WLTP { get; set; }
            public string CO_WLTP { get; set; }
            public string CO2_WLTP_NEDC { get; set; }
            public string bakarat_stiya_activ_s { get; set; }
            public string blima_otomatit_nesia_leahor { get; set; }
            public string bakarat_mehirut_isa { get; set; }
            public string blimat_hirum_lifnei_holhei_regel_ofanaim { get; set; }
            public string hitnagshut_cad_shetah_met { get; set; }
            public string alco_lock { get; set; }
            public string kinuy_mishari { get; set; }
            public string rank { get; set; }
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