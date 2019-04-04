using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictingSoccer
{
    class MatchForm
    {
        public GameResult finalpoints;
        public Team team;
        public Team matchAgainst;
        public int score;

        public int goalsFor;
        public int goalsAgainst;

        public enum GameResult : byte { L, D, W = 3 };

        //score: (points got in game)*(point difference when teams played + constant)
        //constant: number, such that win against weaker team counts for more than 0

        public void CalculateScore()
        {
            int pointsGot = 0;

            switch (finalpoints)
            {
                case GameResult.L: pointsGot = 0; break;
                case GameResult.D: pointsGot = 1; break;
                case GameResult.W: pointsGot = 3; break;
            }

            //matchAgainst.points - team.points + team.points (constant)
            //recent wins therefore make bigger score, in reality they have more weight on current form
            //this score reflects power of the result right after the game ends
            score = pointsGot * matchAgainst.points;
        }

        public override string ToString()
        {
            switch (finalpoints)
            {
                case GameResult.L: return "L";
                case GameResult.D: return "D";
                case GameResult.W: return "W";
                default: return "";
            }
        }
    }
}
