using DAL.SqlCompact;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StffParser
{
    class Program
    {
        static void Main(string[] args)
        {

            long leagueId = 71927;
            Console.WriteLine($"Getting leagueinfo");
            var teamIds = Parser.GetTeamIdsForLeague(leagueId);
            var teams = Parser.GetTeams(teamIds);
            var matchIds = Parser.GetMatchIds(leagueId);
            Console.WriteLine($"Getting matchInfo for {matchIds.Count} matches.");
            List<Match> matches = Parser.GetMatchDetails(matchIds);
            Console.WriteLine($"Finished getting match-details found {matches.Count} matches");
            DbCache cache = new DbCache();
            Console.WriteLine($"Writing {teams.Count} teams to db");
            foreach(var team in teams)
            {
                cache.EnsureTeamExist(team);
            }
            Console.WriteLine($"Writing {matches.Count} matches to db");
            foreach(var match in matches)
            {
                cache.EnsureRefereeExists(match.Ref);
                cache.EnsureRefereeExists(match.AssRef1);
                cache.EnsureRefereeExists(match.AssRef2);
                cache.EnsurePlayersExistsForTeam(match.HomeTeamPlayers, match.HomeTeam);
                cache.EnsurePlayersExistsForTeam(match.AwayTeamPlayers, match.AwayTeam);
                cache.EnsureMatchExists(match);
            }
            Console.WriteLine("Finished");
            Console.ReadKey();
        }
    }
}
