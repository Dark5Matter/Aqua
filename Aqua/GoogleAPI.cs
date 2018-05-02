using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Aqua
{
    public class GoogleAPI
    {
        static string _BaseURL = @"https://www.googleapis.com/customsearch/v1?key=AIzaSyAXU29A5wKxgPDGliAgDhcdWdL3DCdyNlI&cx=007226491239982846055:64c6plabzgg&q=";

        public static async Task<GResults> GoogleAsync(string query, string parameters = "")
        {
            GResults results;
            using (HttpClient _c = new HttpClient())
            {
                var encodedQuery = WebUtility.UrlEncode(query);
                string request = await _c.GetStringAsync(_BaseURL + encodedQuery + parameters);
                results = JsonConvert.DeserializeObject<GResults>(request);
            }
            return results;
        }
    }

    public class GResults
    {
        [JsonProperty("items")]
        public GItem[] Items { get; set; }

        [JsonProperty("searchInformation")]
        public GSearchInformation SearchInformation { get; set; }
    }

    public class GItem
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("snippet")]
        public string Snippet { get; set; }
    }

    public class GSearchInformation
    {
        [JsonProperty("totalResults")]
        public string TotalResults { get; set; }

        [JsonProperty("searchTime")]
        public double SearchTime { get; set; }
    }
}
