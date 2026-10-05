using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGameFramework.Title
{
    /// <summary>
    /// 設定画面。音量のスライダーを GameSettings につなぐ。
    /// 設定画面のパネル自体に付け、TitleScreen の Settings Panel に設定する。
    /// スライダーは Min Value 0 / Max Value 1 にしておく。
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("スライダー（0〜1）")]
        [SerializeField] Slider masterSlider;
        [SerializeField] Slider bgmSlider;
        [SerializeField] Slider seSlider;

        [Header("数値の表示（なくてもよい）")]
        [SerializeField] TMP_Text masterValueText;
        [SerializeField] TMP_Text bgmValueText;
        [SerializeField] TMP_Text seValueText;

        [Header("試し聞き（なくてもよい）")]
        [Tooltip("効果音の音量を変えたときに鳴らす音")]
        [SerializeField] AudioClip sePreview;

        float lastPreviewTime = -1f;

        /// <summary>設定画面を開いたときに最初に選ぶ項目</summary>
        public Selectable FirstSelectable => masterSlider;

        void Awake()
        {
            masterSlider.onValueChanged.AddListener(v =>
            {
                GameSettings.MasterVolume = v;
                UpdateLabels();
            });
            bgmSlider.onValueChanged.AddListener(v =>
            {
                GameSettings.BgmVolume = v;
                UpdateLabels();
            });
            seSlider.onValueChanged.AddListener(v =>
            {
                GameSettings.SeVolume = v;
                UpdateLabels();
                PlayPreview();
            });
        }

        void OnEnable()
        {
            // 開くたびに、今の設定をスライダーに反映する（このときは試し聞きを鳴らさない）
            masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            bgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
            seSlider.SetValueWithoutNotify(GameSettings.SeVolume);
            UpdateLabels();
        }

        void OnDisable()
        {
            // 閉じたら保存する
            GameSettings.Save();
        }

        void UpdateLabels()
        {
            SetLabel(masterValueText, GameSettings.MasterVolume);
            SetLabel(bgmValueText, GameSettings.BgmVolume);
            SetLabel(seValueText, GameSettings.SeVolume);
        }

        static void SetLabel(TMP_Text text, float value)
        {
            if (text != null) text.text = Mathf.RoundToInt(value * 100f).ToString();
        }

        void PlayPreview()
        {
            // スライダーをドラッグしている間に鳴りすぎないように、間隔をあける
            if (sePreview == null || Time.unscaledTime - lastPreviewTime < 0.15f) return;

            lastPreviewTime = Time.unscaledTime;
            MiniGameAudio.PlaySE(sePreview);
        }
    }
}
