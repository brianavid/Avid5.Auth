using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using NLog;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Avid5.Auth.Controllers
{
    public class Token
    {
        [JsonProperty("access_token")]
        public String AccessToken { get; set; }

        [JsonProperty("token_type")]
        public String TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public String RefreshToken { get; set; }

        [JsonProperty("error")]
        public String Error { get; set; }
        [JsonProperty("error_description")]
        public String ErrorDescription { get; set; }

        public DateTime CreateDate { get; set; }
        public Token()
        {
            AccessToken = "";
            TokenType = "";
            RefreshToken = "";
            Error = "";
            ErrorDescription = "";
            CreateDate = DateTime.Now;
        }
        public Boolean IsExpired()
        {
            return CreateDate.Add(TimeSpan.FromSeconds(ExpiresIn)) >= DateTime.Now;
        }
    }

    public class AuthController : Controller
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        //Your client Id
        private const string ClientId = "7f47df26bc5f4d79abafc0b8396fe208";
        private const string ClientSecret = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"; //  Replace with real secret before deploying
        private const string RedirectUri = "http://brianavid.dnsalias.com:88/Auth/Authenticate";

        private static string lastRefreshToken = "";
        private static DateTime refreshTokenFetchExpiry = DateTime.MinValue;

        static readonly HttpClient httpClient = new HttpClient();

        static IHostApplicationLifetime? _appLifetime;

        public static void Initialize(IHostApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;
        }

        ContentResult DoAuthentication(
            string? code,
            string? refresh_token)
        {
            try
            {
                var col = new Dictionary<string,string>();
                col.Add("grant_type", code == null ? "refresh_token" : "authorization_code");
                if (code != null)
                {
                    col.Add("code", code);
                }
                else if (refresh_token != null)
                {
                    col.Add("refresh_token", refresh_token);
                }
                else
                {
                    throw new Exception("Either code or refresh_token must be non-null");
                }
                col.Add("redirect_uri", RedirectUri);
                col.Add("client_id", ClientId);
                col.Add("client_secret", ClientSecret);

                String responseData = "";
                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token"))
                    {
                        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
                        request.Content = new FormUrlEncodedContent(col);
                        using (HttpResponseMessage response = httpClient.Send(request))
                        {
                            response.EnsureSuccessStatusCode();
                            responseData = Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result);
                        }
                    }
                }
                catch (WebException e)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    responseData = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
                logger.Info("Response JSON {0}", responseData);
                var token = JsonConvert.DeserializeObject<Token>(responseData);
                lastRefreshToken = token?.RefreshToken ?? "";
                refreshTokenFetchExpiry = DateTime.UtcNow.AddSeconds(120);
                logger.Info("Last Refresh Token {0}", lastRefreshToken);
                logger.Info("Fetch Refresh Token before {0}", refreshTokenFetchExpiry.ToShortTimeString());
                logger.Info("New Token {0}", token?.AccessToken ?? "");
                return this.Content(responseData, "application/json");
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return this.Content("");
            }
        }

        public ContentResult Probe()
        {
            logger.Info("Probe");
            return this.Content(ClientSecret.StartsWith("X") ? "Bad Secret" : "OK");    // in case I forget!
        }

        public ContentResult Exit()
        {
            logger.Info("Exit");
            if (_appLifetime != null)
            {
                _appLifetime.StopApplication();
            }
            return this.Content("OK");
        }

        public ContentResult Authenticate(
            string code,
            string error,
            string state)
        {
            logger.Info("Authenticate {0}", code);
            return DoAuthentication(code, null);
        }

        public ContentResult GetLastRefreshToken()
        {
            if (DateTime.UtcNow > refreshTokenFetchExpiry)
            {
                lastRefreshToken = "";
            }
            logger.Info("GetLastRefreshToken {0}", lastRefreshToken);
            return this.Content(lastRefreshToken);
        }

        public ContentResult Refresh(
            string refresh_token)
        {
            logger.Info("Refresh {0}", refresh_token);
            return DoAuthentication(null, refresh_token);
        }

    }
}

