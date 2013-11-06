using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;

public class Halo4Api
{
    private class BetterWebRequest
    {
        public static HttpWebResponse Post(string url, string body) // Implies "POST"
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.AllowAutoRedirect = false;
            request.Method = "POST";
            request.CookieContainer = Halo4Api.Cookies;
            HttpWebResponse response;
            System.Text.ASCIIEncoding newBody = new System.Text.ASCIIEncoding();
            request.ContentLength = body.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            StreamWriter writer = new StreamWriter(request.GetRequestStream());
            writer.Write(body);
            writer.Close();

            response = (HttpWebResponse)request.GetResponse();
            response.Close();

            return response;
        }
    }

    private class BetterWebResponse
    {
        public static string GetBody(HttpWebResponse response)
        {
            try {
                Stream dataStream = response.GetResponseStream();

                StreamReader reader = new StreamReader(dataStream);

                string body = reader.ReadToEnd();

                dataStream.Close();
                reader.Close();

                return body;
            }
            catch(Exception)
            {
                return null;
            }
        }
    }

    private class WaypointAuthTokenDocument
    {
        public string access_token { get; set; }
        public string AuthenticationToken { get; set; }
        public int expires_in { get; set; }
    }

    private class UserInformation
    {
        public string Gamertag { get; set; }
        public string AnalyticsToken { get; set; }
    }

    private class WaypointSpartanTokenDocument
    {
        public int ResponseCode { get; set; }
        public string SpartanToken { get; set; }
        private UserInformation UserInformation { get; set; }
    }

    // Enter an email or pass here to use the GetAPIKey() method without passing values
    private string microsoftEmail = "";
    private string microsoftPassword = "";

    private string msLogin = "https://login.live.com/login.srf?id=2";
    private string waypointGateway = "https://app.halowaypoint.com/oauth/signin?returnUrl=https%3A%2F%2Fapp.halowaypoint.com%2Fen-us";
    private string waypointRegisterUrl = "https://settings.svc.halowaypoint.com/RegisterClientService.svc/spartantoken/wlid?_={0}";

    // New URL's
    private string urlToScrape = "https://login.live.com/oauth20_authorize.srf?client_id=000000004C0BD2F1&scope=xbox.basic+xbox.offline_access&response_type=code&redirect_uri=https://www.halowaypoint.com:443/oauth/callback&state=https%3A%2F%2Fapp.halowaypoint.com%2Fen-us%2F&locale=en-US&display=touch";
    private string urlToPost = "https://login.live.com/ppsecure/post.srf?client_id=000000004C0BD2F1&scope=xbox.basic+xbox.offline_access&response_type=code&redirect_uri=https://www.halowaypoint.com:443/oauth/callback&state=https%3A%2F%2Fapp.halowaypoint.com%2Fen-us%2F&locale=en-US&display=touch&bk=1383096785";


    public static CookieContainer Cookies = new CookieContainer();

    private string GetMicrosoftLoginPPFT(int iterationCount = 0) // Going to scrape a page and return an array of strings containing essential values for the login transaction
    {
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(urlToScrape);
        HttpWebResponse response;
        try
        {
            response = (HttpWebResponse)request.GetResponse();
        }
        catch(Exception)
        {
            response = null;
        }

        if (response == null && iterationCount < 10)
        {
            return GetMicrosoftLoginPPFT(iterationCount++);
        }
        else if(iterationCount == 10)
        {
            if (response != null)
            {
                response.Close();
            }
            throw new Exception("Failed to get login parameters after 10 attempts, Microsoft may be down.");
        }

        string responseBody = BetterWebResponse.GetBody(response);

        Regex cookieRegex = new Regex(@"MSPRequ=(.*?);.*,.*MSPOK=(.*?);");

        Match cookieMatch = cookieRegex.Match(response.Headers.Get("Set-Cookie"));

        Cookie mspRequest = new Cookie("MSPRequ", cookieMatch.Groups[1].Value, "/", "login.live.com");

        Cookie mspOk = new Cookie("MSPOK", cookieMatch.Groups[2].Value, cookieMatch.Groups[6].Value, "login.live.com");

        Halo4Api.Cookies.Add(mspRequest);
        Halo4Api.Cookies.Add(mspOk);

        response.Close();

        Regex regex = new Regex("name=\"PPFT\".*?value=\"(.*?)\"");

        Match match = regex.Match(responseBody);

        string PPFT;

        if (match.Success)
        {
            PPFT = match.Groups[1].Value;
        }
        else
        {
            throw new Exception("Failed to get login parameters. Scrape returned no results, Microsoft may have changed their code.");
        }

        return PPFT;
    }

    private string GetWaypointState(int iterationCount = 0)
    {
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(waypointGateway);
        request.CookieContainer = Halo4Api.Cookies;
        HttpWebResponse response;
        try
        {
            response = (HttpWebResponse)request.GetResponse();
        }
        catch (Exception)
        {
            response = null;
        }

        if (response == null && iterationCount < 10)
        {
            return GetWaypointState(++iterationCount);
        }
        else if (iterationCount == 10)
        {
            throw new Exception("Failed to get login parameters after 10 attempts, Microsoft may be down.");
        }
        
        string waypointState;
        try
        {
            NameValueCollection queryString = HttpUtility.ParseQueryString(response.ResponseUri.Query);
            waypointState = queryString["state"];
        }
        catch (Exception)
        {
            return GetWaypointState(++iterationCount);
        }

        Halo4Api.Cookies.Add(response.Cookies);

        response.Close();

        return waypointState;
    }

    private string PerformWaypointLogin(string email, string pass)
    {
        string PPFT = GetMicrosoftLoginPPFT();
        string PPSX = "Pass";

        /*
           PPFT:[loginPPFT]
           login:[login]
           passwd:[passwd]
           LoginOptions:3
           NewUser:1
           PPSX:[loginPPSX]
           type:11
           i3:[rand]
           m1:1680
           m2:1050
           m3:0
           i12:1
           i17:0
           i18:__MobileLogin|1
         */
        string query = String.Format("PPFT={0}&login={1}&passwd={2}&LoginOptions=3&NewUser=1&PPSX={3}&type=11&i3={4}&m1=1680&m2=1050&m3=0&i12=1&i17=0&i18=__MobileLogin|1", PPFT, HttpUtility.UrlEncode(email), HttpUtility.UrlEncode(pass), PPSX, new Random().Next(15000, 50000).ToString());

        // Get the response of the query using the parameters we created
        HttpWebResponse response = BetterWebRequest.Post(urlToPost, query);

        // Add the response cookies to our container
        Halo4Api.Cookies.Add(response.Cookies);

        // Close the response object
        response.Close();

        // Return the header that we need
        return response.Headers.Get("Location");
    }

    private WaypointAuthTokenDocument GetWaypointAuthToken(string homepageUrl)
    {
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(homepageUrl);
        request.CookieContainer = Halo4Api.Cookies;
        request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.52 Safari/537.17";

        WaypointAuthTokenDocument authToken;

        HttpWebResponse response;

        try
        {
            response = (HttpWebResponse)request.GetResponse();

            Regex regex = new Regex("var user.=.*?({.*?});", RegexOptions.Multiline);

            string body = BetterWebResponse.GetBody(response);

            Match match = regex.Match(body);

            if (!match.Success)
            {
                throw new Exception();
            }

            authToken = JsonConvert.DeserializeObject<WaypointAuthTokenDocument>(match.Groups[1].Value);

            Halo4Api.Cookies.Add(response.Cookies);
        }
        catch (Exception)
        {
            throw new Exception("Failed to get Waypoint auth tokens. This is likely a scraping error. Perhaps 343i changed their code layout.");
        }

        response.Close();

        return authToken;
    }

    private WaypointSpartanTokenDocument GetWaypointSpartanToken(WaypointAuthTokenDocument authToken)
    {
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(String.Format(waypointRegisterUrl, (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds));
        request.Accept = "application/json";
        request.Headers.Add("Origin", "https://app.halowaypoint.com");
        request.Headers.Add("X-343-Authorization-WLID", "v1=" + authToken.access_token);
        request.CookieContainer = Halo4Api.Cookies;

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        WaypointSpartanTokenDocument spartanToken;

        try
        {
            spartanToken = JsonConvert.DeserializeObject<WaypointSpartanTokenDocument>(BetterWebResponse.GetBody(response));
        }
        catch (Exception)
        {
            throw new Exception("Could not parse Spartan token.");
        }

        Halo4Api.Cookies.Add(response.Cookies);

        return spartanToken;
    }

    private string GetWaypointWebAuthTokenAndHomepage(string callbackUrl)
    {
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(callbackUrl);
        request.CookieContainer = Halo4Api.Cookies;
        request.AllowAutoRedirect = false;

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

        bool webAuth = false;

        foreach (Cookie cookie in response.Cookies)
        {
            if (cookie.Name == "WebAuth")
            {
                webAuth = true;
                break;
            }
        }

        if (webAuth == false)
        {
            throw new Exception("Could not get WebAuth token.");
        }

        Halo4Api.Cookies.Add(response.Cookies);

        response.Close();

        return response.Headers.Get("Location");
    }

    private string GetSpartanTokenViaMicrosoftAuth(string email, string pass)
    {
        string waypointCallbackUrl = PerformWaypointLogin(email, pass);
        string waypointHomepage = GetWaypointWebAuthTokenAndHomepage(waypointCallbackUrl); // Loads the WebAuth token into the cookie store
        WaypointAuthTokenDocument waypointAuthToken = GetWaypointAuthToken(waypointHomepage);
        WaypointSpartanTokenDocument waypointSpartanToken = GetWaypointSpartanToken(waypointAuthToken);

        return waypointSpartanToken.SpartanToken;
    }

    public string GetAPIKey()
    {
        try
        {
            return GetSpartanTokenViaMicrosoftAuth(microsoftEmail, microsoftPassword);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message != null ? e.Message : "An unknown error occurred.");
        }
    }

    public string GetAPIKey(String email, String pass)
    {
        try
        {
            return GetSpartanTokenViaMicrosoftAuth(email, pass);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message != null ? e.Message : "An unknown error occurred.");
        }
    }
}