Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime
Imports System.Text
Imports System.Collections.Specialized
Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Web
Imports Newtonsoft.Json

Public Class Halo4API

    Private Class BetterWebRequest
        Public Shared Function Post(url As String, body As String) As HttpWebResponse  'Implies "POST"
            Dim request As HttpWebRequest = DirectCast(HttpWebRequest.Create(url), HttpWebRequest)
            request.AllowAutoRedirect = False
            request.Method = "POST"
            request.CookieContainer = Halo4API.Cookies
            Dim response As HttpWebResponse
            Dim newBody As System.Text.ASCIIEncoding = New System.Text.ASCIIEncoding()
            request.ContentLength = body.Length
            request.ContentType = "application/x-www-form-urlencoded"

            Dim writer As StreamWriter = New StreamWriter(request.GetRequestStream())
            writer.Write(body)
            writer.Close()

            response = DirectCast(request.GetResponse(), HttpWebResponse)
            response.Close()

            Return response
        End Function
    End Class

    Private Class BetterWebResponse
        Public Shared Function GetBody(response As HttpWebResponse) As String
            Try
                Dim dataStream As Stream = response.GetResponseStream()

                Dim reader As StreamReader = New StreamReader(dataStream)

                Dim body As String = reader.ReadToEnd()

                dataStream.Close()
                reader.Close()

                Return body
            Catch ex As Exception
                Return Nothing
            End Try
        End Function
    End Class

    Private Class WaypointAuthTokenDocument
        Public access_token As String
        Public AuthenticationToken As String
        Public expires_in As Integer
    End Class

    Private Class UserInformation
        Public Gamertag As String
        Public AnalyticsToken As String
    End Class

    Private Class WaypointSpartanTokenDocument
        Public ResponseCode As Integer
        Public SpartanToken As String
        Private UserInformation As UserInformation
    End Class

    'Enter an email or pass here to use the GetAPIKey() method without passing values
    Private microsoftEmail As String = ""
    Private microsoftPassword As String = ""

    Private msLogin As String = "https://login.live.com/login.srf?id=2"
    Private waypointGateway As String = "https://app.halowaypoint.com/oauth/signin?returnUrl=https%3A%2F%2Fapp.halowaypoint.com%2Fen-us"
    Private waypointRegisterUrl As String = "https://settings.svc.halowaypoint.com/RegisterClientService.svc/spartantoken/wlid?_={0}"

    'New URL's
    Private urlToScrape As String = "https://login.live.com/oauth20_authorize.srf?client_id=000000004C0BD2F1&scope=xbox.basic+xbox.offline_access&response_type=code&redirect_uri=https://www.halowaypoint.com:443/oauth/callback&state=https%3A%2F%2Fapp.halowaypoint.com%2Fen-us%2F&locale=en-US&display=touch"
    Private urlToPost As String = "https://login.live.com/ppsecure/post.srf?client_id=000000004C0BD2F1&scope=xbox.basic+xbox.offline_access&response_type=code&redirect_uri=https://www.halowaypoint.com:443/oauth/callback&state=https%3A%2F%2Fapp.halowaypoint.com%2Fen-us%2F&locale=en-US&display=touch&bk=1383096785"


    Public Shared Cookies As CookieContainer = New CookieContainer()

    Private Function GetMicrosoftLoginPPFT(Optional iterationCount As Integer = 0) As String 'Going to scrape a page and return an array of strings containing essential values for the login transaction
        Dim request As HttpWebRequest = DirectCast(HttpWebRequest.Create(urlToScrape), HttpWebRequest)
        Dim response As HttpWebResponse
        Try
            response = DirectCast(request.GetResponse(), HttpWebResponse)
        Catch ex As Exception
            response = Nothing
        End Try

        If (response Is Nothing AndAlso iterationCount < 10) Then
            Return GetMicrosoftLoginPPFT(iterationCount)
            iterationCount += 1
        ElseIf (iterationCount = 10) Then
            If (Not response Is Nothing) Then
                response.Close()
            End If
            Throw New Exception("Failed to get login parameters after 10 attempts, Microsoft may be down.")
        End If

        Dim responseBody As String = BetterWebResponse.GetBody(response)

        Dim cookieRegex As Regex = New Regex("MSPRequ=(.*?);.*,.*MSPOK=(.*?);")

        Dim cookieMatch As Match = cookieRegex.Match(response.Headers.Get("Set-Cookie"))

        Dim mspRequest As Cookie = New Cookie("MSPRequ", cookieMatch.Groups(1).Value, "/", "login.live.com")

        Dim mspOk As Cookie = New Cookie("MSPOK", cookieMatch.Groups(2).Value, cookieMatch.Groups(6).Value, "login.live.com")

        Halo4API.Cookies.Add(mspRequest)
        Halo4API.Cookies.Add(mspOk)

        response.Close()

        Dim regex As Regex = New Regex("name=""PPFT"".*?value=""(.*?)""")

        Dim match As Match = Regex.Match(responseBody)

        Dim PPFT As String

        If (match.Success) Then
            PPFT = match.Groups(1).Value
        Else
            Throw New Exception("Failed to get login parameters. Scrape returned no results, Microsoft may have changed their code.")
        End If

        Return PPFT
    End Function

    Private Function GetWaypointState(Optional iterationCount As Integer = 0) As String
        Dim request As HttpWebRequest = DirectCast(HttpWebRequest.Create(waypointGateway), HttpWebRequest)
        request.CookieContainer = Halo4API.Cookies
        Dim response As HttpWebResponse

        Try
            response = DirectCast(request.GetResponse(), HttpWebResponse)
        Catch ex As Exception
            response = Nothing
        End Try

        If (response Is Nothing AndAlso iterationCount < 10) Then
            Return GetWaypointState(iterationCount)
            iterationCount += 1
        ElseIf (iterationCount = 10) Then
            Throw New Exception("Failed to get login parameters after 10 attempts, Microsoft may be down.")
        End If

        Dim waypointState As String
        Try
            Dim queryString As NameValueCollection = System.Web.HttpUtility.ParseQueryString(response.ResponseUri.Query)
            waypointState = queryString("state")
        Catch ex As Exception
            Return GetWaypointState(++iterationCount)
        End Try

        Halo4API.Cookies.Add(response.Cookies)

        response.Close()

        Return waypointState
    End Function

    Private Function PerformWaypointLogin(email As String, pass As String) As String
        Dim PPFT As String = GetMicrosoftLoginPPFT()
        Dim PPSX As String = "Pass"

        'PPFT:   [loginPPFT]()
        'login:  [login]()
        'passwd: [passwd]()
        '           LoginOptions:3
        '           NewUser:1
        'PPSX:   [loginPPSX]()
        '           type:11
        'i3:     [rand]()
        '           m1:1680
        '           m2:1050
        '           m3:0
        '           i12:1
        '           i17:0
        '           i18:__MobileLogin|1
        Dim query As String = String.Format("PPFT={0}&login={1}&passwd={2}&LoginOptions=3&NewUser=1&PPSX={3}&type=11&i3={4}&m1=1680&m2=1050&m3=0&i12=1&i17=0&i18=__MobileLogin|1", PPFT, HttpUtility.UrlEncode(email), HttpUtility.UrlEncode(pass), PPSX, New Random().Next(15000, 50000).ToString())

        'Get the response of the query using the parameters we created
        Dim response As HttpWebResponse = BetterWebRequest.Post(urlToPost, query)

        'Add the response cookies to our container
        Halo4API.Cookies.Add(response.Cookies)

        'Close the response object
        response.Close()

        'Return the header that we need
        Return response.Headers.Get("Location")
    End Function

    Private Function GetWaypointAuthToken(homepageUrl As String) As WaypointAuthTokenDocument
        Dim request As HttpWebRequest = DirectCast(HttpWebRequest.Create(homepageUrl), HttpWebRequest)
        request.CookieContainer = Halo4API.Cookies
        request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.52 Safari/537.17"

        Dim authToken As WaypointAuthTokenDocument

        Dim response As HttpWebResponse

        Try
            response = DirectCast(request.GetResponse(), HttpWebResponse)

            Dim regex As Regex = New Regex("var user.=.*?({.*?});", RegexOptions.Multiline)

            Dim body As String = BetterWebResponse.GetBody(response)

            Dim match As Match = regex.Match(body)

            If (Not match.Success) Then
                Throw New Exception()
            End If

            authToken = JsonConvert.DeserializeObject(Of WaypointAuthTokenDocument)(match.Groups(1).Value)

            Halo4API.Cookies.Add(response.Cookies)
        Catch ex As Exception
            Throw New Exception("Failed to get Waypoint auth tokens. This is likely a scraping error. Perhaps 343i changed their code layout.")
        End Try

        response.Close()

        Return authToken
    End Function

    Private Function GetWaypointSpartanToken(authToken As WaypointAuthTokenDocument) As WaypointSpartanTokenDocument
        Dim request As HttpWebRequest = DirectCast(HttpWebRequest.Create(String.Format(waypointRegisterUrl, Convert.ToInt32((DateTime.UtcNow - New DateTime(1970, 1, 1)).TotalSeconds))), HttpWebRequest)
        request.Accept = "application/json"
        request.Headers.Add("Origin", "https://app.halowaypoint.com")
        request.Headers.Add("X-343-Authorization-WLID", "v1=" + authToken.access_token)
        request.CookieContainer = Halo4API.Cookies

        Dim response As HttpWebResponse = DirectCast(request.GetResponse(), HttpWebResponse)

        Dim spartanToken As WaypointSpartanTokenDocument

        Try
            spartanToken = JsonConvert.DeserializeObject(Of WaypointSpartanTokenDocument)(BetterWebResponse.GetBody(response))
        Catch ex As Exception
            Throw New Exception("Could not parse Spartan token.")
        End Try

        Halo4API.Cookies.Add(response.Cookies)

        Return spartanToken
    End Function

    Private Function GetWaypointWebAuthTokenAndHomepage(callbackUrl As String) As String
        Dim request As HttpWebRequest = DirectCast(HttpWebRequest.Create(callbackUrl), HttpWebRequest)
        request.CookieContainer = Halo4API.Cookies
        request.AllowAutoRedirect = False

        Dim response As HttpWebResponse = DirectCast(request.GetResponse(), HttpWebResponse)

        Dim webAuth As Boolean = False

        For Each cookie As Cookie In response.Cookies
            If (cookie.Name = "WebAuth") Then
                webAuth = True
                Exit For
            End If
        Next

        If (webAuth = False) Then
            Throw New Exception("Could not get WebAuth token.")
        End If

        Halo4API.Cookies.Add(response.Cookies)

        response.Close()

        Return response.Headers.Get("Location")
    End Function

    Private Function GetSpartanTokenViaMicrosoftAuth(email As String, pass As String) As String
        Dim waypointCallbackUrl As String = PerformWaypointLogin(email, pass)
        Dim waypointHomepage As String = GetWaypointWebAuthTokenAndHomepage(waypointCallbackUrl) 'Loads the WebAuth token into the cookie store
        Dim waypointAuthToken As WaypointAuthTokenDocument = GetWaypointAuthToken(waypointHomepage)
        Dim waypointSpartanToken As WaypointSpartanTokenDocument = GetWaypointSpartanToken(waypointAuthToken)

        Return waypointSpartanToken.SpartanToken
    End Function

    Public Function GetAPIKey() As String
        Try
            Return GetSpartanTokenViaMicrosoftAuth(microsoftEmail, microsoftPassword)
        Catch e As Exception
            Throw New Exception(DirectCast(IIf(Not String.IsNullOrEmpty(e.Message), e.Message, "An unknown error occurred."), String))
        End Try
    End Function

    Public Function GetAPIKey(email As String, pass As String) As String
        Try
            Return GetSpartanTokenViaMicrosoftAuth(email, pass)
        Catch e As Exception
            Throw New Exception(DirectCast(IIf(Not String.IsNullOrEmpty(e.Message), e.Message, "An unknown error occurred."), String))
        End Try
    End Function
End Class
