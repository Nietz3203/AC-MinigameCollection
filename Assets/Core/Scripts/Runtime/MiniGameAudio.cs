using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// ミニゲーム中の効果音と BGM。
    /// AudioSource は Time.timeScale の影響を受けないので、ゲーム速度に合わせてピッチを上げて鳴らす。
    /// 音量は設定画面の値（GameSettings）に従う。
    /// </summary>
    public static class MiniGameAudio
    {
        static AudioSource seSource;
        static AudioSource bgmSource;
        static float bgmBaseVolume = 1f;

        /// <summary>現在のゲーム速度（Core が設定する）</summary>
        public static float Speed { get; internal set; } = 1f;

        public static void PlaySE(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            EnsureSources();

            seSource.pitch = Speed;
            seSource.PlayOneShot(clip, volume * GameSettings.SeVolume);
        }

        /// <summary>BGM を鳴らす。ミニゲームが終わると Core が自動で止める</summary>
        public static void PlayBGM(AudioClip clip, float volume = 1f, bool loop = true)
        {
            if (clip == null) return;
            EnsureSources();

            bgmBaseVolume = volume;
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.pitch = Speed;
            bgmSource.volume = bgmBaseVolume * GameSettings.BgmVolume;
            bgmSource.Play();
        }

        public static void StopBGM()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        static void EnsureSources()
        {
            if (seSource != null && bgmSource != null) return;

            var go = new GameObject("[MiniGameAudio]");
            Object.DontDestroyOnLoad(go);

            seSource = go.AddComponent<AudioSource>();
            seSource.playOnAwake = false;

            bgmSource = go.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;

            // 二重登録を防ぐために、一度外してから登録する
            GameSettings.Changed -= ApplyVolume;
            GameSettings.Changed += ApplyVolume;
        }

        static void ApplyVolume()
        {
            if (bgmSource != null) bgmSource.volume = bgmBaseVolume * GameSettings.BgmVolume;
        }
    }
}
