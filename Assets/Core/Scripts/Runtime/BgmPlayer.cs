using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// タイトル画面などで BGM を流す。同じ GameObject の AudioSource の音量を、設定画面の BGM 音量に合わせる。
    /// AudioSource に BGM の Clip を設定し、Play On Awake をオンにしておく。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BgmPlayer : MonoBehaviour
    {
        [Tooltip("この BGM 自体の音量（設定画面の BGM 音量と掛け合わされる）")]
        [Range(0f, 1f)]
        [SerializeField] float volume = 1f;

        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            Apply();
            GameSettings.Changed += Apply;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= Apply;
        }

        void Apply()
        {
            if (source != null) source.volume = volume * GameSettings.BgmVolume;
        }
    }
}
