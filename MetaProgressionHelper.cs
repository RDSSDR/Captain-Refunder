using Landfall.Haste;
using System;
using System.Collections.Generic;
using System.Text;
using Zorro.Core;
using static Landfall.Haste.MetaProgression;

namespace CR
{
    public static class MetaProgressionHelper
    {
        public static int? GetLastLevel(this Entry entry)
        {
            int lastLevel = entry.CurrentLevel - 1;
            if (lastLevel < 0)
            {
                return null;
            }
            return lastLevel;
        }

        public static int GetRefundAmount(this Entry entry, int? level)
        {
            if (level == null)
            {
                return 0;
            }
            int refundAmount = 0;
            int currentLevel = entry.CurrentLevel;
            while (currentLevel > level)
            {
                refundAmount += entry.levels[currentLevel].cost;
                currentLevel--;
                if (currentLevel <= 0)
                {
                    break;
                }
            }
            return refundAmount;
        }

        public static float GetLastLevelValue(Kind kind)
        {
            if (!Player.localPlayer)
            {
                return -1f;
            }
            Entry entry = SingletonAsset<MetaProgression>.Instance.GetEntry(kind);
            EntryLevel currentLevel = GetCurrentLevel(entry);
            int? lastLevel = GetLastLevel(entry);
            if (lastLevel == null)
            {
                return -1f;
            }
            EntryLevel lastLevelEntry = entry.levels[(int)lastLevel];
            PlayerStat playerStat = GetPlayerStat(Player.localPlayer.stats, kind);
            playerStat = new PlayerStat
            {
                baseValue = playerStat.baseValue,
                multiplier = playerStat.multiplier

            };
            playerStat.RemoveStat(currentLevel.stat);
            playerStat.AddStat(lastLevelEntry.stat);
            return playerStat.baseValue * playerStat.multiplier;
        }
    }
}
