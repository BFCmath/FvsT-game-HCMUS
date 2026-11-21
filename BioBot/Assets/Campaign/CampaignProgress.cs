using System;
using UnityEngine;

namespace BioBot
{
    [Serializable]
    public sealed class CampaignProgress
    {
        private const string PrefKey = "BioBot.V2.Progress";

        public int UnlockedLevel = 1;
        public int EcoCoins = 60;
        public int EndlessBestWave = 0;
        public int EndlessBestScore = 0;
        public bool Muted = false;
        public int[] Stars = new int[10];
        public int[] Upgrades = new int[6];
        public bool[] Achievements = new bool[10];

        public static CampaignProgress Load()
        {
            string raw = PlayerPrefs.GetString(PrefKey, "");
            CampaignProgress data = null;
            if (!string.IsNullOrEmpty(raw))
            {
                try { data = JsonUtility.FromJson<CampaignProgress>(raw); }
                catch { data = null; }
            }
            data ??= new CampaignProgress();
            data.Normalize();
            return data;
        }

        public void Normalize()
        {
            UnlockedLevel = Mathf.Clamp(UnlockedLevel, 1, CampaignCatalog.Levels.Length);
            if (Stars == null || Stars.Length != CampaignCatalog.Levels.Length)
                Stars = Resize(Stars, CampaignCatalog.Levels.Length);
            if (Upgrades == null || Upgrades.Length != CampaignCatalog.Upgrades.Length)
                Upgrades = Resize(Upgrades, CampaignCatalog.Upgrades.Length);
            if (Achievements == null || Achievements.Length != CampaignCatalog.Achievements.Length)
                Achievements = ResizeBool(Achievements, CampaignCatalog.Achievements.Length);
            for (int i = 0; i < Stars.Length; i++)
                Stars[i] = Mathf.Clamp(Stars[i], 0, 3);
            for (int i = 0; i < Upgrades.Length; i++)
                Upgrades[i] = Mathf.Clamp(Upgrades[i], 0, CampaignCatalog.Upgrades[i].MaxLevel);
        }

        public void Save()
        {
            Normalize();
            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public int UpgradeLevel(int index) =>
            index >= 0 && index < Upgrades.Length ? Upgrades[index] : 0;

        public bool BuyUpgrade(int index)
        {
            if (index < 0 || index >= CampaignCatalog.Upgrades.Length || index == CampaignCatalog.RetiredPulseUpgrade) return false;
            var spec = CampaignCatalog.Upgrades[index];
            int lv = Upgrades[index];
            if (lv >= spec.MaxLevel) return false;
            int cost = spec.CostAt(lv);
            if (EcoCoins < cost) return false;
            EcoCoins -= cost;
            Upgrades[index] = lv + 1;
            Save();
            return true;
        }

        public bool UnlockAchievement(string id, out AchievementSpec spec)
        {
            spec = null;
            for (int i = 0; i < CampaignCatalog.Achievements.Length; i++)
            {
                if (CampaignCatalog.Achievements[i].Id == id)
                {
                    if (Achievements[i]) return false;
                    Achievements[i] = true;
                    spec = CampaignCatalog.Achievements[i];
                    EcoCoins += spec.RewardCoins;
                    Save();
                    return true;
                }
            }
            return false;
        }

        public int TotalStars()
        {
            int sum = 0;
            for (int i = 0; i < CampaignCatalog.PlayableStageCount; i++) sum += Stars[i];
            return sum;
        }

        public int TotalAchievements()
        {
            int sum = 0;
            foreach (int id in CampaignCatalog.PlayableAchievementIds)
                if (Achievements[id]) sum++;
            return sum;
        }

        private static int[] Resize(int[] src, int len)
        {
            var dst = new int[len];
            if (src != null) Array.Copy(src, dst, Mathf.Min(src.Length, len));
            return dst;
        }

        private static bool[] ResizeBool(bool[] src, int len)
        {
            var dst = new bool[len];
            if (src != null) Array.Copy(src, dst, Mathf.Min(src.Length, len));
            return dst;
        }
    }
}
