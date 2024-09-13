using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade
{
    public class RestController
    {
        public const string TDApiUri = "https://api.tdameritrade.com/v1";

        /// <summary>
        /// Process a GET request.
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public string GetData(string bearerToken, string url, Dictionary<string, object> parameters = null)
        {
            var baseUrl = TDApiUri + url;
            if (parameters != null && parameters.Count > 0)
            {
                baseUrl += "?";
                foreach(KeyValuePair<string, object> parameter in parameters)
                {
                    baseUrl += String.Format("{0}{1}={2}", (baseUrl.EndsWith("?") ? "" : "&"), parameter.Key, parameter.Value);
                }
            }

            var client = new RestClient(baseUrl)
            {
                Timeout = -1
            };

            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", "Bearer " + bearerToken);
            return ProcessResponse(client, request);
        }

        /// <summary>
        /// Process POST with URL encoded parameters in the message body.
        /// </summary>
        /// <param name="bodyParam"></param>
        /// <returns></returns>
        public string PostAuthWithBodyParamData(string bodyParam)
        {
            var client = new RestClient(TDApiUri + "/oauth2/token")
            {
                Timeout = -1
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", bodyParam, ParameterType.RequestBody);
            return ProcessResponse(client, request);
        }

        /// <summary>
        /// DELETE a resource.
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public string Delete(string bearerToken, string path)
        {
            var client = new RestClient(TDApiUri + path)
            {
                Timeout = -1
            };

            var request = new RestRequest(Method.DELETE);
            request.AddHeader("Authorization", "Bearer " + bearerToken);
            return ProcessResponse(client, request);
        }

        /// <summary>
        /// POST a payload.
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public string Post(string bearerToken, string path, string payload)
        {
            var client = new RestClient(TDApiUri + path)
            {
                Timeout = -1
            };
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Bearer " + bearerToken);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", payload, ParameterType.RequestBody);

            return ProcessResponse(client, request);
        }

        /// <summary>
        /// PUT a payload.
        /// </summary>
        /// <param name="bearerToken"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public string Put(string bearerToken, string path, string payload)
        {
            var client = new RestClient(TDApiUri + path)
            {
                Timeout = -1
            };
            var request = new RestRequest(Method.PUT);
            request.AddHeader("Authorization", "Bearer " + bearerToken);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", payload, ParameterType.RequestBody);

            return ProcessResponse(client, request);
        }

        /// <summary>
        /// Process the request and return the Http response.
        /// </summary>
        /// <param name="restClient"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private string ProcessResponse(RestClient restClient, RestRequest request)
        {
            IRestResponse response = restClient.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK
                || response.StatusCode == System.Net.HttpStatusCode.Accepted
                || response.StatusCode == System.Net.HttpStatusCode.Created)
                return response.Content;
            else
            {
                throw new Exception(response.Content);
            }
        }
    }
}
