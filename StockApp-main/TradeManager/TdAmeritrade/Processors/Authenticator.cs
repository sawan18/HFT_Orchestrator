using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TdAmeritrade.Models;

namespace TdAmeritrade
{
    public class Authenticator
    {
        private readonly RestController restController = new RestController();
        private int refreshTokenValidityPeriodDays = 90;
        private int accessTokenValidityPeriodMins = 30;

        public Authenticator(string clientKey, string code, string refreshToken, DateTime refreshTokenCreationDate)
        {
            this.ClientKey = clientKey;
            this.Code = code;
            this.RefreshToken = refreshToken;
            this.RefreshTokenCreationDate = refreshTokenCreationDate;
        }

        #region properties.
        /// <summary>
        /// Security code associated with the TD Ameritrade Account.
        /// </summary>
        public string Code { get; set; }


        /// <summary>
        /// Refresh token that is used to generate the Access token.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Refresh Token creation date
        /// </summary>
        public DateTime RefreshTokenCreationDate { get; set; }

        /// <summary>
        /// Access Token creation date
        /// </summary>
        public DateTime AccessTokenCreationDate { get; set; }

        /// <summary>
        /// Access Token generated through the API calls.
        /// </summary>
        public string AccessToken { get; private set; }

        /// <summary>
        /// Client API key tied to the TD Stock trading application.
        /// </summary>
        public string ClientKey { get; set; }

        /// <summary>
        /// The Client TD Stock Redirect URI.
        /// </summary>
        public string RedirectUri { get; set; }
        #endregion

        #region public
        public void ProcessAuthentication(bool getNewRefreshToken = false)
        {
            // Generate a new refresh token.
            if (getNewRefreshToken)
            {
                GetNewRefreshToken();
            }

            // Refresh an existing refresh token.
            if(!IsRefreshTokenValid())
            {
                GetUpdatedRefreshToken();
            }

            // Refresh an access token.
            if (!IsAccessTokenValid())
            {
                GetAccessToken();                
            }
        }

        #endregion

        #region private methods
        /// <summary>
        /// Gets the access token from the refresh token.
        /// </summary>
        /// <returns></returns>
        private bool GetAccessToken()
        {
            var refreshToken = HttpUtility.UrlEncode(RefreshToken);
            var clientKey = HttpUtility.UrlEncode(ClientKey);
            return ProcessAuthenticationResponse(String.Format("grant_type=refresh_token&refresh_token={0}&access_type=&code=&client_id={1}&redirect_uri=", refreshToken, clientKey));
        }

        /// <summary>
        /// Get a new refresh token from the authentication code.
        /// </summary>
        /// <returns></returns>
        private bool GetNewRefreshToken()
        {
            var redirectUri = HttpUtility.UrlEncode(RedirectUri);
            var clientKey = HttpUtility.UrlEncode(ClientKey);
            var code = HttpUtility.UrlEncode(Code);
            return ProcessAuthenticationResponse(String.Format("grant_type=authorization_code&refresh_token=&access_type=offline&code={0}&client_id={1}&redirect_uri={2}", code, clientKey, redirectUri));
        }

        /// <summary>
        /// Gets an updated refresh token.
        /// </summary>
        /// <returns></returns>
        private bool GetUpdatedRefreshToken()
        {
            var refreshToken = HttpUtility.UrlEncode(RefreshToken);
            var clientKey = HttpUtility.UrlEncode(ClientKey);
            return ProcessAuthenticationResponse(String.Format("grant_type=refresh_token&refresh_token={0}&access_type=offline&code=&client_id={1}&redirect_uri=", refreshToken, clientKey));
        }

        /// <summary>
        /// Process the authentication response.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ProcessAuthenticationResponse(string data)
        {
            var response = JsonConvert.DeserializeObject<TokenResponse>(restController.PostAuthWithBodyParamData(data));

            if (!String.IsNullOrEmpty(response.access_token))
            {
                AccessToken = response.access_token;
                AccessTokenCreationDate = DateTime.Now;

                // If we are fetching the refresh token.
                // Retrieve this token as well.
                if (!String.IsNullOrEmpty(response.refresh_token))
                {
                    RefreshToken = response.refresh_token;
                    RefreshTokenCreationDate = DateTime.Now;
                }

                return true;
            }
            else
            {
                AccessToken = String.Empty;
                return false;
            }
        }

        /// <summary>
        /// Check if there is need to refresh the access token.
        /// </summary>
        /// <returns></returns>
        private bool IsAccessTokenValid()
        {
            if (AccessTokenCreationDate == null)
                return true;
            else
            {
                var difference = DateTime.Now - AccessTokenCreationDate;
                if (difference.TotalMinutes > accessTokenValidityPeriodMins)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            
        }

        /// <summary>
        /// Check if there is need to refresh the refresh token.
        /// </summary>
        /// <returns></returns>
        private bool IsRefreshTokenValid()
        {
            if (RefreshTokenCreationDate == null)
                return true;
            else
            {
                var difference = DateTime.Now - RefreshTokenCreationDate;
                if (difference.TotalDays > refreshTokenValidityPeriodDays)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

        }
        #endregion
    }
}
