using System;
using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// ゲーム全体の設定（音量）。PlayerPrefs に保存され、次に起動したときも残る。
    ///   MasterVolume：全体の音量（AudioListener.volume。ミニゲームが自分で鳴らす音も含めてすべてに効く）
    ///   BgmVolume   ：BGM とジングル
    ///   SeVolume    ：効果音（MiniGameBase.PlaySE で鳴らしたもの）
    /// </summary>
    public static class GameSettings
    {
        const string MasterKey = "Settings.MasterVolume";
        const string BgmKey = "Settings.BgmVolume";
        const string SeKey = "Settings.SeVolume";

        const float DefaultMaster = 1.0f;
        const float DefaultBgm = 0.8f;
        const float DefaultSe = 0.8f;

        /// <summary>音量が変わったときに呼ばれる</summary>
        public static event Action Changed;

        static float master = DefaultMaster;
        static float bgm = DefaultBgm;
        static float se = DefaultSe;

        public static float MasterVolume
        {
            get => master;
            set
            {
                master = Mathf.Clamp01(value);
                AudioListener.volume = master;
                Changed?.Invoke();
            }
        }

        public static float BgmVolume
        {
            get => bgm;
            set
            {
                bgm = Mathf.Clamp01(value);
                Changed?.Invoke();
            }
        }

        public static float SeVolume
        {
            get => se;
            set
            {
                se = Mathf.Clamp01(value);
                Changed?.Invoke();
            }
        }

        /// <summary>設定を保存する（設定画面を閉じたときに呼ぶ）</summary>
        public static void Save()
        {
            PlayerPrefs.SetFloat(MasterKey, master);
            PlayerPrefs.SetFloat(BgmKey, bgm);
            PlayerPrefs.SetFloat(SeKey, se);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Changed = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            master = PlayerPrefs.GetFloat(MasterKey, DefaultMaster);
            bgm = PlayerPrefs.GetFloat(BgmKey, DefaultBgm);
            se = PlayerPrefs.GetFloat(SeKey, DefaultSe);
            AudioListener.volume = master;
        }
    }
}
