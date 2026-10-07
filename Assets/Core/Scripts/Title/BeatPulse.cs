using UnityEngine;

namespace MiniGameFramework.Title
{
    /// <summary>
    /// BGM のテンポに合わせて、付けたオブジェクトを拍ごとに「ドクン」と大きくする。
    /// 拍は BGM の AudioSource の再生位置から計算するので、曲がループしてもずれない。
    /// BGM の AudioSource は自動で探す（BgmPlayer があればそれ、なければループ再生中のもの）。
    ///
    /// 使い方：背景の図形（Image など）に付けて、BPM を曲に合わせる。
    /// Play 中に BPM / Offset を変えるとすぐ反映されるので、聞きながら合わせるとよい。
    /// EffectSpawner で出す Prefab に付けてもよい（BackGroundEffect が決めた大きさを基準に拍動する）。
    /// </summary>
    public class BeatPulse : MonoBehaviour
    {
        [Tooltip("曲のテンポ（1分あたりの拍数）")]
        [SerializeField] float bpm = 120f;
        [Tooltip("曲の頭から最初の拍までの秒数。拍とずれて聞こえるときに調整する")]
        [SerializeField] float offset = 0f;
        [Tooltip("何拍ごとに拍動するか。1 = 毎拍、2 = 2拍に1回、0.5 = 8分音符ごと")]
        [SerializeField] float beatsPerPulse = 1f;
        [Tooltip("拍動のタイミングをずらす（拍単位）。0.5 なら裏拍。図形ごとに変えると交互に動く")]
        [SerializeField] float beatShift = 0f;

        [Header("動き")]
        [Tooltip("拍の瞬間にどれだけ大きくなるか。0.15 なら 1.15 倍")]
        [SerializeField] float pulseAmount = 0.15f;
        [Tooltip("大きくなってから元に戻るまでの長さ（1拍に対する割合）")]
        [Range(0.05f, 1f)]
        [SerializeField] float pulseLength = 0.5f;

        [Header("回転")]
        [Tooltip("ずっと回る速さ（度/秒）。マイナスで逆回り")]
        [SerializeField] float rotateSpeed = 0f;
        [Tooltip("拍動のたびに回る角度（度）。拍の瞬間にカクッと回り、Pulse Length かけて止まる。マイナスで逆回り")]
        [SerializeField] float rotatePerPulse = 0f;

        const float SearchInterval = 1f;

        AudioSource source;
        float nextSearchTime;
        Vector3 baseScale;
        float lastBeat = float.NaN;
        float lastStepAngle;

        void Start()
        {
            // BackGroundEffect などが Awake で決めた大きさを基準にする
            baseScale = transform.localScale;
        }

        void Update()
        {
            if (bpm <= 0f || beatsPerPulse <= 0f) return;

            float beat = (CurrentTime() - offset) * bpm / 60f;
            float pulse = beat / beatsPerPulse - beatShift;
            float phase = Mathf.Repeat(pulse, 1f);

            // 拍の瞬間に最大、pulseLength かけて元に戻る（ease-out）
            float k = Mathf.Clamp01(1f - phase / pulseLength);
            k *= k;

            transform.localScale = baseScale * (1f + pulseAmount * k);

            // 回転は今の向きに足していく（BackGroundEffect などの回転と重ねられるように）
            float stepAngle = rotatePerPulse * (Mathf.Floor(pulse) + (1f - k));
            float delta = rotateSpeed * Time.deltaTime;
            // 曲がループして再生位置が戻ったときは、拍ごとの回転を足さない
            if (!float.IsNaN(lastBeat) && beat >= lastBeat) delta += stepAngle - lastStepAngle;
            lastBeat = beat;
            lastStepAngle = stepAngle;

            if (delta != 0f) transform.Rotate(0f, 0f, delta);
        }

        /// <summary>今の再生位置（秒）。BGM がなければ起動からの時間</summary>
        float CurrentTime()
        {
            if (source == null && Time.unscaledTime >= nextSearchTime)
            {
                nextSearchTime = Time.unscaledTime + SearchInterval;
                source = FindBgmSource();
            }

            if (source != null && source.isPlaying && source.clip != null)
            {
                return (float)source.timeSamples / source.clip.frequency;
            }
            return Time.unscaledTime;
        }

        /// <summary>BgmPlayer の AudioSource を探す。なければ、ループ再生中の AudioSource を使う</summary>
        static AudioSource FindBgmSource()
        {
            var player = FindFirstObjectByType<BgmPlayer>();
            if (player != null) return player.GetComponent<AudioSource>();

            foreach (var s in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (s.isPlaying && s.loop && s.clip != null) return s;
            }
            return null;
        }

        void OnDisable()
        {
            if (baseScale != Vector3.zero) transform.localScale = baseScale;
        }
    }
}
