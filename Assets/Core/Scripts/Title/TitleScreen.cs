using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGameFramework.Title
{
    /// <summary>
    /// タイトル画面の進行役。Title シーンに置き、各パネルとボタンを Inspector で設定する。
    ///
    ///   タイトル（ボタンを押してね）
    ///     → モード選択（通常プレイ / 練習 / 設定 / やめる）
    ///         通常プレイ → 操作説明 → Main シーンへ
    ///         練習       → ミニゲーム一覧 → 操作説明 → Main シーンへ
    ///         設定       → 音量の設定
    ///
    /// 操作：十字キー / WASD / スティックで選択、Space / Z / Enter / Aボタンで決定、
    ///       Esc / Bボタンで戻る。マウスでのクリックも可。
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] MiniGameCatalog catalog;

        [Header("画面（パネル）")]
        [SerializeField] GameObject titlePanel;
        [SerializeField] GameObject modePanel;
        [SerializeField] GameObject practicePanel;
        [SerializeField] GameObject howToPanel;

        [Header("モード選択")]
        [SerializeField] Button normalButton;
        [SerializeField] Button practiceButton;
        [Tooltip("なくてもよい")]
        [SerializeField] Button settingsButton;
        [Tooltip("なくてもよい")]
        [SerializeField] Button quitButton;

        [Header("練習（ミニゲーム一覧）")]
        [Tooltip("ボタンを並べる親（ScrollView の Content）")]
        [SerializeField] Transform practiceListContent;
        [Tooltip("見本のボタン（Content の中に置いたもの、または Prefab）。これをコピーして並べる")]
        [SerializeField] Button practiceItemTemplate;
        [SerializeField] ScrollRect practiceScroll;
        [SerializeField] Button practiceBackButton;

        [Header("操作説明")]
        [Tooltip("「通常プレイ」「練習：〇〇」を表示するテキスト（なくてもよい）")]
        [SerializeField] TMP_Text howToModeText;
        [SerializeField] Button startButton;
        [SerializeField] Button howToBackButton;

        [Header("設定（なくてもよい）")]
        [Tooltip("設定画面のパネルに付けた SettingsPanel")]
        [SerializeField] SettingsPanel settingsPanel;
        [SerializeField] Button settingsBackButton;

        enum Page { Title, Mode, Practice, HowTo, Settings }

        Page current;
        Page howToReturnPage;
        GameObject defaultSelection;
        GameObject lastSelected;
        bool loading;
        readonly List<Button> practiceButtons = new List<Button>();

        void Awake()
        {
            Time.timeScale = 1f;

            // 決定は MiniGameInput（Space / Z / Enter / Aボタン）で統一するので、
            // uGUI 側の決定（Submit）は無効にして二重に押されるのを防ぐ
            var module = FindFirstObjectByType<InputSystemUIInputModule>();
            if (module != null) module.submit = null;
            else Debug.LogWarning("[Title] EventSystem に InputSystemUIInputModule がありません");

            normalButton.onClick.AddListener(() =>
            {
                GameLaunchSettings.Mode = GameMode.Normal;
                GameLaunchSettings.PracticeGame = null;
                ShowHowTo(Page.Mode);
            });
            practiceButton.onClick.AddListener(() => Show(Page.Practice));
            if (quitButton != null) quitButton.onClick.AddListener(Quit);

            practiceBackButton.onClick.AddListener(Back);
            startButton.onClick.AddListener(StartGame);
            howToBackButton.onClick.AddListener(Back);

            if (settingsButton != null)
            {
                if (settingsPanel != null) settingsButton.onClick.AddListener(() => Show(Page.Settings));
                else settingsButton.interactable = false;
            }
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(Back);

            BuildPracticeList();
            Show(Page.Title);
        }

        void BuildPracticeList()
        {
            // シーンに置いた見本のときだけ非表示にする（Prefab のファイルは書き換えない）
            if (practiceItemTemplate.gameObject.scene.IsValid())
            {
                practiceItemTemplate.gameObject.SetActive(false);
            }

            if (catalog != null)
            {
                foreach (var info in catalog.games)
                {
                    if (info == null) continue;

                    var button = Instantiate(practiceItemTemplate, practiceListContent);
                    button.gameObject.SetActive(true);
                    button.name = "Item_" + info.gameId;

                    var label = button.GetComponentInChildren<TMP_Text>();
                    if (label != null) label.text = $"{info.title}　<size=70%>by {info.author}</size>";

                    var target = info;
                    button.onClick.AddListener(() =>
                    {
                        GameLaunchSettings.Mode = GameMode.Practice;
                        GameLaunchSettings.PracticeGame = target;
                        ShowHowTo(Page.Practice);
                    });
                    practiceButtons.Add(button);
                }
            }

            practiceButton.interactable = practiceButtons.Count > 0;
        }

        void Update()
        {
            if (loading) return;

            // タイトル：どのボタンでも次へ
            if (current == Page.Title)
            {
                if (MiniGameInput.ActionDown || MiniGameInput.PointerDown) Show(Page.Mode);
                return;
            }

            if (BackDown())
            {
                Back();
                return;
            }

            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            // マウスで何もない所をクリックすると選択が外れるので、キー操作で選択し直す
            var selected = eventSystem.currentSelectedGameObject;
            if (selected == null || !selected.activeInHierarchy)
            {
                bool keyInput = MiniGameInput.ActionDown || MiniGameInput.Direction.sqrMagnitude > 0.25f;
                if (keyInput && defaultSelection != null) eventSystem.SetSelectedGameObject(defaultSelection);
                return;
            }

            if (MiniGameInput.ActionDown)
            {
                var button = selected.GetComponent<Button>();
                if (button != null && button.interactable) button.onClick.Invoke();
                return;
            }

            if (selected != lastSelected)
            {
                lastSelected = selected;
                if (current == Page.Practice) ScrollTo(selected);
            }
        }

        /// <param name="select">最初に選ぶ項目。null ならその画面の既定の項目</param>
        void Show(Page page, GameObject select = null)
        {
            current = page;
            titlePanel.SetActive(page == Page.Title);
            modePanel.SetActive(page == Page.Mode);
            practicePanel.SetActive(page == Page.Practice);
            howToPanel.SetActive(page == Page.HowTo);
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(page == Page.Settings);

            switch (page)
            {
                case Page.Mode:
                    defaultSelection = normalButton.gameObject;
                    break;
                case Page.Practice:
                    defaultSelection = practiceButtons.Count > 0
                        ? practiceButtons[0].gameObject
                        : practiceBackButton.gameObject;
                    if (practiceScroll != null) practiceScroll.verticalNormalizedPosition = 1f;
                    break;
                case Page.HowTo:
                    defaultSelection = startButton.gameObject;
                    break;
                case Page.Settings:
                    defaultSelection = settingsPanel.FirstSelectable != null
                        ? settingsPanel.FirstSelectable.gameObject
                        : settingsBackButton != null ? settingsBackButton.gameObject : null;
                    break;
                default:
                    defaultSelection = null;
                    break;
            }

            var first = select != null ? select : defaultSelection;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first);
            lastSelected = first;
        }

        void ShowHowTo(Page returnPage)
        {
            howToReturnPage = returnPage;
            if (howToModeText != null)
            {
                howToModeText.text = GameLaunchSettings.Mode == GameMode.Practice && GameLaunchSettings.PracticeGame != null
                    ? $"練習：{GameLaunchSettings.PracticeGame.title}"
                    : "通常プレイ";
            }
            Show(Page.HowTo);
        }

        void Back()
        {
            switch (current)
            {
                case Page.Mode:
                    Show(Page.Title);
                    break;
                case Page.Practice:
                    Show(Page.Mode, practiceButton.gameObject);
                    break;
                case Page.Settings:
                    Show(Page.Mode, settingsButton != null ? settingsButton.gameObject : null);
                    break;
                case Page.HowTo:
                    Show(howToReturnPage);
                    break;
            }
        }

        void StartGame()
        {
            if (loading) return;
            loading = true;
            SceneManager.LoadScene(GameLaunchSettings.MainScenePath);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>キー操作で選んだ項目が見えるように、一覧をスクロールする</summary>
        void ScrollTo(GameObject selected)
        {
            if (practiceScroll == null || practiceButtons.Count <= 1) return;

            int index = practiceButtons.FindIndex(b => b.gameObject == selected);
            if (index < 0) return;

            practiceScroll.verticalNormalizedPosition = 1f - (float)index / (practiceButtons.Count - 1);
        }

        static bool BackDown()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && kb.escapeKey.wasPressedThisFrame)
                || (gp != null && gp.buttonEast.wasPressedThisFrame);
        }
    }
}
