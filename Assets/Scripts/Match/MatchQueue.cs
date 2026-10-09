using System;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Relogio do servidor: a entrada de novos jogadores nao reinicia a espera.
    public sealed class MatchQueue
    {
        double deadline = double.NaN;

        public int SecondsLeft(int readyPlayers, double now)
        {
            if (readyPlayers == 0) { Reset(); return GameConfig.MatchmakingSeconds; }
            if (double.IsNaN(deadline)) deadline = now + GameConfig.MatchmakingSeconds;
            return Math.Max(0, (int)Math.Ceiling(deadline - now));
        }

        public bool ShouldStart(int readyPlayers, double now) =>
            readyPlayers > 0 && (SecondsLeft(readyPlayers, now) == 0 || readyPlayers >= GameConfig.MatchCapacity);

        public static int BotsNeeded(int readyPlayers) => Math.Max(0, GameConfig.MatchCapacity - readyPlayers);
        public void Reset() { deadline = double.NaN; }
    }
}
