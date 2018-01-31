using System;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Senstay.Dojo.Fantastic
{
    public class RestRequest
    {
        // Fantastic API specific resources
        public const string FANTASTIC_API_URL = "https://senstay.fantasian.io";
        public const string FANTASTIC_API_KEY = "9LLnJ5t1sEARggGbEmi5N6jy9YukRbs6";
        public const string FANTASTIC_CONTENT_TYPE = "application/x-www-form-urlencoded";

        #region  Web API request using api-key

        /// <summary>
        /// Make a generic GET call to Fantastic API.
        /// </summary>
        /// <param name="requestEndPoint">Fantastic API end point url including optional query paraemters</param>
        /// <returns>the response string of the 'GET' API call</returns>
        public static string Get(string requestEndPoint)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // setup client
                    client.BaseAddress = new Uri(requestEndPoint);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(FANTASTIC_CONTENT_TYPE));
                    client.DefaultRequestHeaders.Add("api-key", FANTASTIC_API_KEY);

                    // make GET request
                    HttpResponseMessage response = client.GetAsync(requestEndPoint).Result;
                    string responseJsonString = response.Content.ReadAsStringAsync().Result;

                    return responseJsonString;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get price list from Fantastic calendar API service
        /// </summary>
        /// <param name="requestEndPoint">Fantastic calendar API end point url including optional query paraemters</param>
        /// <returns>calendar result</returns>
        public static CalendarResult GetPrices(string requestEndPoint)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // setup client
                    client.BaseAddress = new Uri(requestEndPoint);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(FANTASTIC_CONTENT_TYPE));
                    client.DefaultRequestHeaders.Add("api-key", FANTASTIC_API_KEY);

                    // make GET request
                    HttpResponseMessage httpResponse = client.GetAsync(requestEndPoint).Result;
                    string content = httpResponse.Content.ReadAsStringAsync().Result;

                    var responseJson = JsonConvert.DeserializeObject<CalendarResult>(content);
                    return responseJson;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get custom stay list from Fantastic calendar API service
        /// </summary>
        /// <param name="requestEndPoint">Fantastic calendar API end point url including optional query paraemters</param>
        /// <returns>calendar result</returns>
        public static CustomStayResult GetCustomStays(string requestEndPoint)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // setup client
                    client.BaseAddress = new Uri(requestEndPoint);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(FANTASTIC_CONTENT_TYPE));
                    client.DefaultRequestHeaders.Add("api-key", FANTASTIC_API_KEY);

                    // make GET request
                    HttpResponseMessage httpResponse = client.GetAsync(requestEndPoint).Result;
                    string content = httpResponse.Content.ReadAsStringAsync().Result;

                    var responseJson = JsonConvert.DeserializeObject<CustomStayResult>(content);
                    return responseJson;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Make a POST call to Fantastic API with well-formed query string. The POST call response is in a form of success flag + error message if available.
        /// </summary>
        /// <param name="requestEndPoint">Fantastic API end point url</param>
        /// <param name="queryString">well-formed query string</param>
        /// <returns>the response string of the 'POST' API call</returns>
        public static PostResponse Post(string requestEndPoint, string queryString)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // setup client
                    client.BaseAddress = new Uri(requestEndPoint);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(FANTASTIC_CONTENT_TYPE));
                    client.DefaultRequestHeaders.Add("api-key", FANTASTIC_API_KEY);

                    // make POST request; note that FANTASTIC_CONTENT_TYPE is required to construct the request content
                    StringContent content = new StringContent(queryString, Encoding.UTF8, FANTASTIC_CONTENT_TYPE);
                    HttpResponseMessage httpResponse = client.PostAsync(requestEndPoint, content).Result;
                    string response = httpResponse.Content.ReadAsStringAsync().Result;

                    var responseJson = JsonConvert.DeserializeObject<PostResponse>(response);
                    return responseJson;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Make a POST call to Fantastic API with list of name-value query pairs. The POST call response is in a form of success flag + error message if available.
        /// </summary>
        /// <param name="requestEndPoint">Fantastic API end point url</param>
        /// <param name="queryParams">list of name-value pairs of string type</param>
        /// <returns>the response string of the 'POST' API call</returns>
        public static PostResponse Post(string requestEndPoint, List<KeyValuePair<string, string>> queryParams)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // setup client
                    client.BaseAddress = new Uri(requestEndPoint);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(FANTASTIC_CONTENT_TYPE));
                    client.DefaultRequestHeaders.Add("api-key", FANTASTIC_API_KEY);

                    // make POST request
                    StringContent content = new StringContent(Stringify(queryParams), Encoding.UTF8, FANTASTIC_CONTENT_TYPE); // covnert list object to query string first
                    HttpResponseMessage httpResponse = client.PostAsync(requestEndPoint, content).Result;
                    string response = httpResponse.Content.ReadAsStringAsync().Result;

                    var responseJson = JsonConvert.DeserializeObject<PostResponse>(response);
                    return responseJson;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Convert name-value pairs into restful query string; the string is not url-encoded
        /// </summary>
        /// <param name="query">list of name-value pairs of string type</param>
        /// <returns>a well-formed query string without url encoding</returns>
        private static string Stringify(List<KeyValuePair<string, string>> query)
        {
            string dataString = string.Empty;
            foreach(var param in query)
            {
                if (dataString != string.Empty) dataString += "&";
                dataString += string.Format("{0}={1}", param.Key, param.Value);
            }
            return dataString;
        }

        #endregion

        #region RESTful EndPoint formatters

        // routeUrl start with '/' character without trailing '/'
        public static string GetEndPoint(string routeUrl)
        {
            return GetEndPoint(FANTASTIC_API_URL, routeUrl, null);
        }

        public static string GetEndPoint(string routeUrl, int listingId)
        {
            var parameter = new KeyValuePair<string, object>("listing_id", listingId);
            var queryParams = new List<KeyValuePair<string, object>>() { parameter };
            return GetEndPoint(FANTASTIC_API_URL, routeUrl, queryParams);
        }

        public static string GetEndPoint(string routeUrl, List<KeyValuePair<string, object>> queryParams)
        {
            return GetEndPoint(FANTASTIC_API_URL, routeUrl, queryParams);
        }

        public static string GetEndPoint(string baseUrl, string route, object id, string action, List<KeyValuePair<string, object>> queryParams)
        {
            string routeUrl = string.Format("/{0}/{1}/{2}", route, id.ToString(), action);
            return GetEndPoint(baseUrl, routeUrl, queryParams);
        }

        public static string GetEndPoint(string baseUrl, string routeUrl, List<KeyValuePair<string, object>> queryParams)
        {
            StringBuilder queries = new StringBuilder(string.Empty);
            if (queryParams != null && queryParams.Count > 0)
            {
                queries.Append("?");
                foreach (var pair in queryParams)
                {
                    if (queries.Length > 2) queries.Append("&");
                    queries.AppendFormat("{0}={1}", pair.Key, pair.Value.ToString());
                }
            }
            return string.Format("{0}{1}{2}", baseUrl, routeUrl, queries);
        }
        
        #endregion
    }
}