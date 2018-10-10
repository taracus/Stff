using DAL.SqlCompact;
using Shared;
using Shared.MatchUpdates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace StffParser
{
    public class Parser
    {
        private const string TableUrl = "http://www.stff.se/information/?scr=table&ftid={0}";
        private const string TeamUrl = "http://www.stff.se/information/?flid={0}";
        private const string MatchesUrl = "http://www.stff.se/information/?scr=fixturelist&ftid={0}";
        private const string MatchDetailsUrl = "http://www.stff.se/match/?scr=result&fmid={0}";

        private const string StartTableText = "<table class=\"clCommonGrid clTblStandings clTblWithFullToggle\" cellspacing=\"0\">";
        private const string EndTableText = "</table>";

        private const string TeamIdRegExp = "flid=\\d+";

        private const string TeamNamePattern = "Kontaktuppgifter och tävlingar 2018 - (.*?)<";
        private static readonly int StartIndexTeamName = "Kontaktuppgifter och tävlingar 2018 - ".Length;

        private const string TeamColors = "<dt>Färger</dt>\r\n<dd>(.*?)<";
        private static readonly int StartIndexTeamColors = "<dt>Färger</dt>\r\n<dd>".Length;

        private const string MatchesStartText = "<td colspan=\"5\">Spelprogram";
        private const string MatchesEndText = "</tbody>";

        public static List<long> GetTeamIdsForLeague(long leagueId)
        {
            List<long> teamIds = new List<long>();
            var html = DownloadString(string.Format(TableUrl, leagueId));
            int startIndex = html.IndexOf(StartTableText);
            int endIndex = html.IndexOf(EndTableText, startIndex);
            int length = endIndex - startIndex;
            var tableHtml = html.Substring(startIndex, length);
            Regex regex = new Regex(TeamIdRegExp);
            var matches = regex.Matches(tableHtml);
            foreach(System.Text.RegularExpressions.Match match in matches)
            {
                teamIds.Add(long.Parse(match.Value.Substring(5)));
            }

            return teamIds;
        }

        public static List<Team> GetTeams(IEnumerable<long> teamIds)
        {
            List<Team> teams = new List<Team>();
            foreach (var teamId in teamIds)
            {
                var teamHtml = DownloadString(string.Format(TeamUrl, teamId));
                Regex ex = new Regex(TeamNamePattern);
                var match = ex.Match(teamHtml);
                if (match != null && match.Success)
                {
                    Team t = new Team();
                    string name = match.Value.Substring(StartIndexTeamName, match.Value.Length - StartIndexTeamName - 1);
                    t.Name = name;
                    t.Colors = "NA";
                    t.AwayColors = "NA";
                    t.ExternalId = teamId;
                    teams.Add(t);
                }
            }
            return teams;
        }

        public static List<long> GetMatchIds(long leagueId)
        {
            int maxIds = 1;
            List<long> matches = new List<long>();
            var html = DownloadString(string.Format(MatchesUrl, leagueId));
            int start = html.IndexOf(MatchesStartText);
            int end = html.IndexOf(MatchesEndText, start);
            var matchHtml = html.Substring(start, end - start);
            var matchRows = matchHtml.Split(new string[] { "<tr" }, StringSplitOptions.RemoveEmptyEntries);
            Regex ex = new Regex("fmid=(.*?)\"");
            int counter = 0;
            foreach(var matchRow in matchRows)
            {
                var cols = matchRow.Split("<td>".ToCharArray());

                var idMatch = ex.Match(matchRow);
                if (!idMatch.Success)
                {
                    continue;
                }
                var matchId = idMatch.Value.Substring(5, idMatch.Value.Length - 6);
                matches.Add(long.Parse(matchId));
                counter++;
                if (counter > maxIds)
                {
                    break;
                }
            }

            return matches;
        }

        public static List<Shared.Match> GetMatchDetails(IEnumerable<long> matchIds)
        {
            List<Shared.Match> matches = new List<Shared.Match>();
            foreach(var matchId in matchIds)
            {
                var html = DownloadString(string.Format(MatchDetailsUrl, matchId));

                int start = html.IndexOf("<div class=\"squad hometeam-squad\">");
                int end = html.IndexOf("<div class=\"squad awayteam-squad", start);
                var homeSquadHtml = html.Substring(start, end - start);
                var homePlayers = GetPlayers(homeSquadHtml);

                start = html.IndexOf("<div class=\"squad awayteam-squad\">");
                end = html.IndexOf("<div class=\"box livescore-box gameinfo-section", start);
                var awaySquadHtml = html.Substring(start, end - start);
                var awayPlayers = GetPlayers(awaySquadHtml);

                start = html.IndexOf("<ul class=\"events");
                end = html.IndexOf("</ul>", start);
                var eventsHtml = html.Substring(start, end - start);
                var updates = GetUpdates(eventsHtml, new List<Player>(homePlayers.Concat(awayPlayers)));

                start = html.IndexOf("<div class=\"arena-refs col");
                end = html.IndexOf("</div", start);
                var refHtml = html.Substring(start, end - start);

                var referee = GetReferee(refHtml);
                var assRef1 = GetAssReferee1(refHtml);
                var assRef2 = GetAssReferee2(refHtml);

                Regex teamIdEx = new Regex("flid=(.*?)\"");

                start = html.IndexOf("<div class=\"home\">");
                end = html.IndexOf("<div class=\"away\"");
                var homeHtml = html.Substring(start, end - start);
                var homeMatch = teamIdEx.Match(homeHtml);
                long homeId = long.Parse(homeMatch.Value.Substring(5, homeMatch.Value.Length - 6));


                start = html.IndexOf("<div class=\"away\">");
                end = html.IndexOf("<div class=\"timer\"");
                var awayHtml = html.Substring(start, end - start);
                var awayMatch = teamIdEx.Match(awayHtml);
                long awayId = long.Parse(awayMatch.Value.Substring(5, awayMatch.Value.Length - 6));

                Shared.Match m = new Shared.Match();
                m.AwayTeamPlayers = awayPlayers;
                m.HomeTeamPlayers = homePlayers;
                m.MatchUpdates = updates;
                m.Ref = referee;
                m.AssRef1 = assRef1;
                m.AssRef2 = assRef2;
                m.ExternalId = matchId;
                m.HomeTeam = homeId;
                m.AwayTeam = awayId;

                matches.Add(m);
            }
            return matches;
        }

        private static Referee GetReferee(string refHtml)
        {
            Referee r = new Referee();
            string[] refs = refHtml.Split(new string[] { "<p>" }, StringSplitOptions.RemoveEmptyEntries);
            Regex refEx = new Regex("\">(.*?)</a>");
            Regex idEx = new Regex("fpid=(.*?)\"");
            foreach(var refStr in refs)
            {
                if (refStr.Contains("Domare"))
                {
                    var refMatch = refEx.Match(refStr);
                    var refSub = refMatch.Value.Substring(2, refMatch.Value.Length - 6);
                    var nameAndPlace = refSub.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    var names = nameAndPlace[0].Split(' ');
                    r.FirstName = names[0];
                    StringBuilder lastName = new StringBuilder();
                    for (int i = 1; i < names.Length; i++)
                    {
                        lastName.Append(names[i]);
                        if (names.Length > i+1 )
                        {
                            lastName.Append(" ");
                        }
                    }
                    r.LastName = lastName.ToString();

                    var idMatch = idEx.Match(refStr);
                    var idStr = idMatch.Value.Substring(5, idMatch.Value.Length - 6);
                    r.ExternalId = long.Parse(idStr);
                    if (nameAndPlace.Length > 1)
                    {
                        r.Location = nameAndPlace[1];
                    }
                    else
                    {
                        r.Location = "NA";
                    }
                    return r;
                }
            }
            return null;
        }

        private static Referee GetAssReferee1(string refHtml)
        {
            Referee r = new Referee();
            string[] refs = refHtml.Split(new string[] { "<p>" }, StringSplitOptions.RemoveEmptyEntries);
            Regex refEx = new Regex("\">(.*?)</a>");
            Regex idEx = new Regex("fpid=(.*?)\"");
            foreach (var refStr in refs)
            {
                if (refStr.Contains("Ass. domare 1"))
                {
                    var refMatch = refEx.Match(refStr);
                    var refSub = refMatch.Value.Substring(2, refMatch.Value.Length - 6);
                    var nameAndPlace = refSub.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    var names = nameAndPlace[0].Split(' ');
                    r.FirstName = names[0];
                    StringBuilder lastName = new StringBuilder();
                    for (int i = 1; i < names.Length; i++)
                    {
                        lastName.Append(names[i]);
                        if (names.Length > i + 1)
                        {
                            lastName.Append(" ");
                        }
                    }
                    r.LastName = lastName.ToString();

                    var idMatch = idEx.Match(refStr);
                    var idStr = idMatch.Value.Substring(5, idMatch.Value.Length - 6);
                    r.ExternalId = long.Parse(idStr);
                    if (nameAndPlace.Length > 1)
                    {
                        r.Location = nameAndPlace[1];
                    }
                    else
                    {
                        r.Location = "NA";
                    }
                    return r;
                }
            }
            return null;
        }

        private static Referee GetAssReferee2(string refHtml)
        {
            Referee r = new Referee();
            string[] refs = refHtml.Split(new string[] { "<p>" }, StringSplitOptions.RemoveEmptyEntries);
            Regex refEx = new Regex("\">(.*?)</a>");
            Regex idEx = new Regex("fpid=(.*?)\"");
            foreach (var refStr in refs)
            {
                if (refStr.Contains("Ass. domare 2"))
                {
                    var refMatch = refEx.Match(refStr);
                    var refSub = refMatch.Value.Substring(2, refMatch.Value.Length - 6);
                    var nameAndPlace = refSub.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    var names = nameAndPlace[0].Split(' ');
                    r.FirstName = names[0];
                    StringBuilder lastName = new StringBuilder();
                    for (int i = 1; i < names.Length; i++)
                    {
                        lastName.Append(names[i]);
                        if (names.Length > i + 1)
                        {
                            lastName.Append(" ");
                        }
                    }
                    r.LastName = lastName.ToString();

                    var idMatch = idEx.Match(refStr);
                    var idStr = idMatch.Value.Substring(5, idMatch.Value.Length - 6);
                    r.ExternalId = long.Parse(idStr);
                    if (nameAndPlace.Length > 1)
                    {
                        r.Location = nameAndPlace[1];
                    }
                    else
                    {
                        r.Location = "NA";
                    }
                    return r;
                }
            }
            return null;
        }
        private static List<MatchUpdate> GetUpdates(string eventsHtml, IEnumerable<Player> players)
        {
            List<MatchUpdate> updates = new List<MatchUpdate>();
            var events = eventsHtml.Split(new string[] { "<li" }, StringSplitOptions.RemoveEmptyEntries);
            Regex timeEx = new Regex("<time>(.*?)</time>");
            Regex pidEx = new Regex("fplid=(.*?)\"");
            foreach(var e in events)
            {
                bool isHome = e.Contains("hometeam");
                var timeMatch = timeEx.Match(e);
                var pidMatch = pidEx.Match(e);
                if (!timeMatch.Success || !pidMatch.Success)
                {
                    continue;
                }
                var stringMinute = timeMatch.Value.Substring(6, timeMatch.Value.Length - 14);
                if (stringMinute.Contains("+"))
                {
                    stringMinute = stringMinute.Split('+')[0];
                }
                var stringPid = pidMatch.Value.Substring(6, pidMatch.Value.Length - 7);
                long pid = long.Parse(stringPid);
                MatchUpdate mu = null;
                if (e.Contains("yellow-card"))
                {
                    mu = new YellowCardUpdate();
                }
                else if (e.Contains("red-card"))
                {
                    mu = new RedCardUpdate();
                }
                else if (e.Contains("goal"))
                {
                    mu = new GoalUpdate();
                }
                else
                {
                    continue; // ?
                }
                mu.Minute = int.Parse(stringMinute);
                mu.HomeTeam = isHome;
                var player = players.Where(p => p.ExternalId == pid);
                var tmp = new List<Player>(player);
                if (tmp.Count != 1)
                {
                    throw new InvalidOperationException();
                }
                mu.Player = player.Single();
                updates.Add(mu);
            }
            return updates;
        }

        private static List<Player> GetPlayers(string squadHtml)
        {
            List<Player> players = new List<Player>();
            var listItems = squadHtml.Split(new string[] { "<a href=" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var li in listItems)
            {
                Regex pidEx = new Regex("fplid=(.*?)\">");
                Regex nameEx = new Regex("class=\"name\">(.*?)<");
                var idMatch = pidEx.Match(li);
                var nameMatch = nameEx.Match(li);
                if (!idMatch.Success || !nameMatch.Success)
                {
                    continue;
                }
                Player p = new Player();
                string idString = idMatch.Value.Substring(6, idMatch.Value.Length - 8);
                string nameString = nameMatch.Value.Substring(13, nameMatch.Value.Length - 14);
                p.ExternalId = long.Parse(idString);
                string[] names = nameString.Split(" ".ToCharArray());
                p.FirstName = names[0];
                string lastNames = "";
                for (int i = 1; i < names.Length; i++)
                {
                    lastNames += names[i];
                    if (names.Length > i+1)
                    {
                        lastNames += " ";
                    }
                }

                p.LastName = lastNames;
                players.Add(p);
            }
            return players;
        }
        private static string DownloadString(string url)
        {
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                return wc.DownloadString(url);
            }
        }

        private static XmlNode GetTableNode(XmlNode parent)
        {

            return null;
        }
    }
}
