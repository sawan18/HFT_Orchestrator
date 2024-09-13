using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade
{
    public sealed class TdAmeritradeSettings
    {
        private static volatile TdAmeritradeSettings instance;
        private static object syncRoot = new Object();

        /// <summary>
        /// Returns only one instance for all classes
        /// </summary>
        public static TdAmeritradeSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new TdAmeritradeSettings();
                    }
                }

                return instance;
            }
        }

        public Authenticator AuthenticationProcessor { get; set; }

        /// <summary>
        /// Gets and sets the code associated with the account.
        /// </summary>
        public string ClientKey { get; set; }

        /// <summary>
        /// Gets and sets the code associated with the account.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets the refresh token associated with this account.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets and sets the refresh token creation date.
        /// </summary>
        public DateTime RefreshTokenCreationDate { get; set; }

        public void Authenticate(string clientKey, string code, string refreshToken, DateTime refreshTokenCreateDate)
        {
            this.ClientKey = clientKey;
            this.Code = code;
            this.RefreshToken = refreshToken;
            this.RefreshTokenCreationDate = refreshTokenCreateDate;

            this.AuthenticationProcessor = new Authenticator(this.ClientKey, this.Code, this.RefreshToken, this.RefreshTokenCreationDate);
            this.AuthenticationProcessor.ProcessAuthentication();
        }
    }
}
