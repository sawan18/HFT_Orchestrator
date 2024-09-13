using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Rest
{
    public enum RestMethods
    {
        GET,
        POST,
        PUT,
        DELETE
    }
    public class RESTClient
    {
        private RunSettings settings = RunSettings.Instance;
        #region properties

        /// <summary>
        /// Gets or sets the endpoint Url
        /// </summary>
        public string EndPointUrl { get; set; }

        /// <summary>
        /// Gets or sets the method
        /// </summary>
        public RestMethods Method { get; set; }

        /// <summary>
        /// Gets or set the content type
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the data to post to the REST service
        /// </summary>
        public string PostData { get; set; }

        /// <summary>
        /// Gets and sets the authentication username for the rest service
        /// </summary>
        public string AuthenticationUsername { get; set; }

        /// <summary>
        /// Gets and sets the authentication password for the rest service.
        /// </summary>
        public string AuthenticationPassword { get; set; }

        public bool LogPostDate { get; set; }
        #endregion

        #region public 

        /// <summary>
        /// Sends REST request and returns a string response
        /// </summary>
        /// <returns></returns>
        public async Task<String> SendAsync()
        {
            try
            {
                IgnoreBadCertificates();

                var request = (HttpWebRequest)WebRequest.Create(EndPointUrl);
                settings.Log.LogMessage(String.Format("Request Url:{0}", EndPointUrl));

                if (!String.IsNullOrEmpty(AuthenticationUsername))
                {
                    request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(String.Format("{0}:{1}", AuthenticationUsername, AuthenticationPassword)));
                }

                if (String.IsNullOrEmpty(ContentType))
                {
                    ContentType = "application/xml";
                }

                request.Method = Method.ToString();
                request.ContentLength = 0;
                request.ContentType = ContentType;

                if (!string.IsNullOrEmpty(PostData)
                        && (Method == RestMethods.POST || Method == RestMethods.PUT || Method == RestMethods.DELETE))
                {
                    var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(PostData);
                    request.ContentLength = bytes.Length;

                    if (LogPostDate)
                        settings.Log.LogMessage(String.Format("{0} Data:{1}", Method.ToString(), PostData));
                    else
                        settings.Log.LogMessage(String.Format("{0} Data Size: {1}", Method.ToString(), PostData.Length));

                    using (var writeStream = await request.GetRequestStreamAsync())
                    {
                        writeStream.Write(bytes, 0, bytes.Length);
                    }
                }

                settings.Log.LogMessage(String.Format("Initiating REST Request: {0}", EndPointUrl));

                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    var responseValue = string.Empty;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream != null)
                                using (var reader = new StreamReader(responseStream))
                                {
                                    responseValue = reader.ReadToEnd();
                                }
                        }
                    }

                    //settings.Log.LogMessage(String.Format("Response:{0}", responseValue));
                    return responseValue;
                }
            }
            catch (Exception ex)
            {
                settings.Log.LogMessage(String.Format("REST Request failed: {0}", ex.Message));
                return string.Empty;
            }

        }

        #endregion

        #region certificate management
        /// <summary>
        /// Together with the AcceptAllCertifications method right
        /// below this causes to bypass errors caused by SLL-Errors.
        /// </summary>
        public static void IgnoreBadCertificates()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
            ServicePointManager.CheckCertificateRevocationList = false;
        }

        /// <summary>
        /// In Short: the Method solves the Problem of broken Certificates.
        /// Sometime when requesting Data and the sending Webserverconnection
        /// is based on a SSL Connection, an Error is caused by Servers whoes
        /// Certificate(s) have Errors. Like when the Cert is out of date
        /// and much more... So at this point when calling the method,
        /// this behaviour is prevented
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certification"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns>true</returns>
        private static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        #endregion
    }
}
