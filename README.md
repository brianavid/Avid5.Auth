# Avid5.Auth
This project is an OAUTH authenticator suitable for programmatic access to Spotify and (presumeably) other systems that can use OAUTH.

It is a .Net 6 MVC project which can  be deployed at any publicly accessible URL. It has a single controller ("/Auth") with a number of web methods available.

The only method used by Spotify (or other service requiring OAUTH authentication) is "/Auth/Authenticate" and this is the URL (at the host address) which should be registered with Spotify - e.g. "http://MYDOMAIN.com/Auth/Authenticate". Spotify will call this URL from a browser page opened on the URL returned by SpotifyAPI.Web.LoginRequest() [q.v.].

Other methods used are:
- /Auth/Probe, which is not part of the protocol, but can be used before authentication to confirm that the service is up and running.
- /Auth/Exit, which is not part of the protocol, but can be used to stop the service.
- /Auth/GetLastRefreshToken, which will return a "RefreshToken" that can be used thereafter to authenticate future sessions without any user interaction. The caller has two minutes to fetch the token after the /Auth/Authenticate call before the token is forgotten. The token returned has a limited lifetime. The client **MUST** save this token persistently for any future authenticaton without UI via /Auth/Refresh.
- /Auth/Refresh will allocate a new RefreshToken from an old one, but with more lifetime. In this call and in GetLastRefreshToken, the refresh tokens are returned with theit lifetime in a JSON structure.

The program is a self-hosted (Kestrel) MVC web service. It requires a single argument which is a path to an XML config file:
```
<Config>
	<RedirectUri>http://MYDOMAIN.com/Auth/Authenticate</RedirectUri>
	<ClientId>The ID allocated by Spotify</ClientId>
	<ClientSecret>The Secret allocated by Spotify</ClientSecret>
	<SpotifyUrl>https://accounts.spotify.com/api/token</SpotifyUrl>
</Config>
```
It runs on port localhost:5010, and it is expected that IIS URL rewriting or (e.g.) NGINX reverse proxy will make it available on a suitable external domain and port.
