﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using NLog;
using System.Net.Http.Headers;

namespace Avid5.Auth.Controllers
{
    //  To use this OAUTH code in another project, you will need three pieces of data.
    //  One is the public URL on which this long-lived service will be running.
    //  The other two are a ClientID and ClientSecret obrained from the Spotify
    //  developer management website.
    //  These must be edited below before building and deployment to that URL

    public class AuthController : Controller
    {
        //  Your client Id
        private string ClientId = Config.ClientId ?? "";
        //  Your client Secret
        private string ClientSecret = Config.ClientSecret ?? "";
        //  The authentication service public URL which has been registered with Spotify
        private string RedirectUri = Config.RedirectUri ?? "";

        static Logger logger = LogManager.GetCurrentClassLogger();

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
                //  A collection of data values that we must send to Spotify to authenticate
                var col = new Dictionary<string,string>();

                //  How are we authenticating?
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

                //  Tell Spotify the three pieces of information that it needs to identify this service
                col.Add("redirect_uri", RedirectUri);
                col.Add("client_id", ClientId);
                col.Add("client_secret", ClientSecret);

                String responseData = "";
                //  Send the data to Spotify for processing and authentication
                using (var request = new HttpRequestMessage(HttpMethod.Post, Config.SpotifyUrl ?? "https://accounts.spotify.com/api/token"))
                {
                    request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
                    request.Content = new FormUrlEncodedContent(col);
                    using (HttpResponseMessage response = httpClient.Send(request))
                    {
                        response.EnsureSuccessStatusCode();
                        responseData = Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result);
                    }
                }

                //  The response from Spotify is a JSON-encoded structure
                logger.Info("Response JSON {0}", responseData);

                var token = JsonConvert.DeserializeObject<Token>(responseData);
                lastRefreshToken = token?.RefreshToken ?? "";

                //  The caller has two minutes to fetch the token after Authentication before it is forgotten here
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

        //  Auth/Probe is not part of the protocol, but can be used to confirm that the service is up and running
        public ContentResult Probe()
        {
            logger.Info("Probe");
            return this.Content("OK");
        }

        //  Auth/Exit is not part of the protocol, but can be used to stop the service
        public ContentResult Exit()
        {
            logger.Info("Exit");
            if (_appLifetime != null)
            {
                _appLifetime.StopApplication();
            }
            return this.Content("OK");
        }

        //  Auth/Authenticate is used to initially authenticate using a call to
        //  SpotifyAPI.Web.LoginRequest(), starting with just the ClientId.
        //  LoginRequest() will return to the caller a handshake URL to display a browser, which
        //  (unless they are already locally cached) may need entry of credentials to complete the login.
        public ContentResult Authenticate(
            string code,
            string error,
            string state)
        {
            logger.Info("Authenticate {0}", code);
            return DoAuthentication(code, null);
        }

        //  Having authenticated, Auth/GetLastRefreshToken will return a "RefreshToken" that can be used thereafter
        //  to authenticate future sesions without any UI.
        //  The client MUST save this token for any future authenticaton without UI via Auth/Refresh
        //  The caller has two minutes to fetch the token after Authentication before it is forgotten here
        public ContentResult GetLastRefreshToken()
        {
            if (DateTime.UtcNow > refreshTokenFetchExpiry)
            {
                lastRefreshToken = "";
            }
            logger.Info("GetLastRefreshToken {0}", lastRefreshToken);
            return this.Content(lastRefreshToken);
        }

        //  Authentication will grant use of Spotify for a short period. Before that period expires,
        //  Auth/Refresh must be called to allocate a new RefreshToken with more lifetime.
        //  See class Token below for the structure returned by Spotify encoded as JSON.
        public ContentResult Refresh(
            string refresh_token)
        {
            logger.Info("Refresh {0}", refresh_token);
            return DoAuthentication(null, refresh_token);
        }

    }

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

}

