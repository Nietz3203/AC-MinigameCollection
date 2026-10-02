using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// 効果音の再生。AudioSource は Time.timeScale の影響を受けないので、
    /// ゲーム速度に合わせてピッチを上げて鳴らす。
    /// </summary>
    public static class MiniGameAudio
    {
        static AudioSource source;

        /// <summary>現在のゲーム速度（Core が設定する）</summary>
        public static float Speed { get; internal set; } = 1f;

        public static void PlaySE(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            if (source == null)
            {
                var go = new GameObject("[MiniGameAudio]");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }

            source.pitch = Speed;
            source.PlayOneShot(clip, volume);
        }
    }
}
