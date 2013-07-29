Halo 4 API Authentication
=========================

This class enables authenticated access to the Halo 4 API located at HaloWaypoint.com

To use this API, please import the file into your C# project. Make sure to change the values of the class variables `microsoftEmail` and `microsoftPassword` - these correspond to your Xbox Live account (which will be making the requests.)

The purpose of this class is to provide you with an authentication token, which you can then use to sign your requests to the Halo Waypoint API.

If you are unaware of these API endpoints, or would just like them available for reference, I've listed a (non-exaustive) list of endpoints.

Most API calls will be in the form of a `GET` request. The requests must contain the following headers:

> Accept: application/json
> Origin: https://app.halowaypoint.com
> X-343-Authorization-Spartan: [Spartan token goes here (use the Halo4Api.GetAPIKey method for this)]

A side note: The "Spartan token" you receive from Halo Waypoint is useful for 4 hours. DO NOT REQUEST A NEW KEY WITH EVERY GET, YOU'LL GET YOURSELF BANNED.

Meta information
----------------

###Challenge periods
#####0: Daily
#####1: Weekly
#####2: Monthly

###Challenge types
#####0: Campaign
#####1: SpartanOps
#####2: WarGames
#####3: Waypoint

###Game modes
#####0: ?
#####1: ?
#####2: ?
#####3: Matchmaking
#####4: Campaign
#####5: SpartanOps
#####6: Custom

###Game results
#####1: Unknown
#####0: Lost
#####1: Tied
#####2: Won

###Team appearance (colours)
#####0: Red
#####1: Blue
#####2: Gold
#####3: Green
#####4: Orange
#####5: Purple
#####6: Magenta
#####7: Cyan

Endpoints
---------

###Campaign details
`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/servicerecord/campaign`

###Custom game details
`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/servicerecord/custom`

###War games details
`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/servicerecord/wargames`

###Spartan ops details

`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/servicerecord/spartanops`

###Specific game details
`GET https://stats.svc.halowaypoint.com/h4/matches/[game id]`

###Player's game history
`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/matches?gamemodeid=[game mode id]&count=[number of games]&startat=[offset number]&df=2`

###Multiple player cards
`GET https://stats.svc.halowaypoint.com/h4/playercards?gamertags=[csv of gamertags]`

###Single player card
`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/playercard`

###Player ranks
`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/ranks`

###Player service record
`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/servicerecord`

###Player challenges
`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/challenges`

###Commendations
`GET https://stats.svc.halowaypoint.com/players/iLoch/h4/commendations`

###Playlists
`GET https://presence.svc.halowaypoint.com/en-us/h4/playlists?auth=st`

###Challenges
`GET https://stats.svc.halowaypoint.com/h4/challenges`

###Player's spartan image (as it appears in game)
`GET https://spartans.svc.halowaypoint.com/players/iLoch/h4/spartans/[pose id]`

###Game meta data
`GET https://stats.svc.halowaypoint.com/h4/metadata`

###Terminal meta data
`GET https://app.halowaypoint.com/en-us/DomainTerminals`

Happy coding!
