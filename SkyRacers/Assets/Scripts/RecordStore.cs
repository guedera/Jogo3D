using UnityEngine;

namespace SkyRacers
{
    public static class RecordStore
    {
        const string Key = "SkyRacers.ReturnMission.v5.BestSeconds";
        public static float Best => PlayerPrefs.GetFloat(Key, 0);
        public static bool Submit(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0 || (Best > 0 && seconds >= Best)) return false;
            PlayerPrefs.SetFloat(Key, seconds); PlayerPrefs.Save(); return true;
        }
        public static string Format(float seconds)
        {
            int ms = Mathf.FloorToInt(Mathf.Max(0, seconds) * 1000);
            return $"{ms / 60000:00}:{ms / 1000 % 60:00}.{ms % 1000:000}";
        }
    }
}
