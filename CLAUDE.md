# AC-MinigameCollection

サークルで制作している『メイドインワリオ』風のミニゲーム集（Unity）。
数秒のミニゲームが次々に出てきて、クリアするほどスピードアップする。
メンバーが1人1本以上ミニゲームを作り、このリポジトリに PR で追加していく。

- 管理者（Core 担当）：Nietz。Core・ProjectSettings・Packages・Shared の変更は管理者が行う
- リポジトリは **公開**。素材・フォントは再配布してよいライセンスのものだけを入れる
- 応答は日本語で。メンバーは Unity / Git の初心者も多い前提で説明する

## 環境

- Unity 6 系 LTS（正確なバージョンは `ProjectSettings/ProjectVersion.txt`）、Universal 2D（URP）
- 新 Input System、TextMeshPro（TMP Essentials 導入済み）
- 開発機は Windows。`git config core.autocrlf input`、`.gitattributes` で改行を正規化
- Asset Serialization = Force Text、Visible Meta Files

## フォルダと担当

```
Assets/
├─ Core/                 管理者のみ（CODEOWNERS で保護）
│   ├─ Scripts/Runtime/  MiniGameFramework.asmdef（ミニゲームが参照する唯一のアセンブリ）
│   ├─ Scripts/Title/    MiniGameFramework.Title.asmdef（タイトル画面。UI / TMP を参照）
│   ├─ Scripts/Editor/   MiniGameFramework.Editor.asmdef（MiniGame メニュー）
│   ├─ Scenes/           Title.unity（Build 先頭）→ Main.unity
│   ├─ Resources/Fonts/  HUD 用フォント（M PLUS Rounded 1c、OFL）
│   ├─ Prefabs/          Core 専用の Prefab
│   └─ MiniGameCatalog.asset（自動生成。手で編集しない）
├─ MiniGames/
│   ├─ _Template/        サンプル「よけろ！」（Dodge）
│   └─ 作者_ゲーム名/     各メンバーのミニゲーム（例：Nietz_Mash）。本人はここだけ触る
├─ Shared/               メンバーにも使ってほしい共通素材（管理者が追加。一度入れたら名前・中身を変えない）
└─ TextMesh Pro/         TMP Essentials（管理者が導入済み。メンバーは Import しない）
docs/minigame-guide.md   メンバー向けの作り方・ルール
```

## Core の仕組み

### 1ゲームの流れ（Main シーン、MiniGameRunner）

1. ベースの画面（MiniGameStage）が表示された状態で、開始前ジングル＋アニメ。**裏でミニゲームのシーンを Additive 読み込みし、`Prepare`（timeScale 0 のまま Setup）**
2. 指示文を表示しつつ、ベースの画面をフェードアウト（拡大しながら alpha 1→0）
3. `MiniGameSession.Play`：timeScale = 速度、BGM 開始、`Begin`。指示文はゲームを止めずにゲーム内 0.8 秒で消える。制限時間（Normal 4秒 / Long 8秒、ゲーム内時間）で `End`、timeScale 0、BGM 停止
4. ベースの画面をフェードイン → 裏でシーンを破棄、重力を元に戻す
5. 成功・失敗ジングル＋アニメ、ライフ更新。4ゲームごとにスピードアップ（1.0〜2.0倍）、12ゲームごとに難易度 Lv+1（最大3）
6. ライフ 0 でゲームオーバー → 「ボタン：もう一度 / Esc・B：タイトルへ」

MiniGameStage がシーンにないときは、OnGUI の文字だけの簡易表示で同じ流れが動く。

### 主なスクリプト（Assets/Core/Scripts/）

| ファイル | 役割 |
|---|---|
| `Runtime/MiniGameBase.cs` | ミニゲームの基底クラス。`OnSetup` / `OnBegin` / `OnTimeUp`、`Succeed()` / `Fail()`、`PlaySE` / `PlayBGM` / `StopBGM`。`Setup` / `Begin` / `End` は `internal`（Core 専用） |
| `Runtime/MiniGameInfo.cs` | ミニゲームのメタデータ（ScriptableObject）。gameId、title、author、instruction、inputType、length、judgeType、scenePath（自動）、bgm / bgmVolume / noBgm |
| `Runtime/MiniGameTypes.cs` | `MiniGameResult` / `JudgeType`（Survive＝何もなければ成功、Achieve＝何もしなければ失敗）/ `GameLength` / `InputType` |
| `Runtime/MiniGameSession.cs` | `Prepare` / `Play` / `Run`（Run はデバッグ起動用で結果を文字表示） |
| `Runtime/MiniGameRunner.cs` | 本番の進行役。通常プレイ / 練習モード、ライフ・速度・難易度、共通 BGM（Normal / Long） |
| `Runtime/MiniGameStage.cs` | ベースの画面（Screen Space - Overlay の Canvas、CanvasGroup の alpha でフェード、Sort Order を 1000 に）。ジングル、Animator トリガー `Intro` / `Success` / `Failure` / `SpeedUp` / `GameOver`、ライフ・スコアの UnityEvent |
| `Runtime/LifeIcons.cs` | ライフのアイコン表示。ミスで右から Trigger `Lose`、リトライで Rebind。アイコンは破壊・非表示にしない |
| `Runtime/MiniGameDebugRunner.cs` | ミニゲームのシーンを直接 Play したときの単体テスト（エディタ専用）。F1〜F3 難易度、F5/F6 速度 |
| `Runtime/MiniGameInput.cs` | 入力ラッパー。`Direction`、`Action` / `ActionDown` / `ActionUp`（Space / Z / Enter / パッドA）、`Pointer*`、`PointerWorldPosition()` |
| `Runtime/MiniGameAudio.cs` | SE / BGM 再生。ピッチ＝速度、音量は GameSettings に従う |
| `Runtime/GameSettings.cs` | 音量（全体＝AudioListener.volume / BGM / 効果音）。PlayerPrefs に保存 |
| `Runtime/GameLaunchSettings.cs` | タイトル → Main への受け渡し（`GameMode.Normal` / `Practice`、練習するゲーム、シーンパス） |
| `Runtime/MiniGameHud.cs` | 仮の OnGUI 表示（指示文・結果・タイマー・ヘッダー）。フォントは Resources から読み込み。将来 uGUI に置き換える想定 |
| `Runtime/BgmPlayer.cs` / `GameCursor.cs` | タイトル BGM / カーソル画像（DontDestroyOnLoad の単一インスタンス） |
| `Title/TitleScreen.cs` | タイトル → モード選択（通常 / 練習 / 設定 / やめる）→ 操作説明 → Main。uGUI の Submit は無効化し、決定は MiniGameInput で統一 |
| `Title/SettingsPanel.cs` | 音量スライダー（0〜1）と GameSettings の接続 |
| `Editor/MiniGameMenu.cs` | メニュー「MiniGame」：初期セットアップ / **カタログとBuild Settingsを更新**（Title → Main → 各ミニゲームの順に登録） |

### 設計上の約束

- **速度は `Time.timeScale` で一括制御**。ミニゲームは普通に `Time.deltaTime` で作れば自動で速くなる
- ミニゲームの合間（ジングル・フェード中）は timeScale = 0。Core の演出は unscaled time で動かす（Animator は Unscaled Time、`WaitForSecondsRealtime`）。Core が unscaled を使うのは OK、ミニゲームは禁止
- ジングル・BGM は `pitch = speed`。待ち時間は `clip.length / speed`
- ミニゲームの BGM は 1倍速でゲームの長さ（4秒 / 8秒）ぴったりの曲を推奨（ループしない）
- `Camera.main` がミニゲームのカメラを指すよう、Core のカメラ（depth -100）には MainCamera タグを付けない。ミニゲーム側の AudioListener は Runner が無効化する
- Runner は読み込んだミニゲームのシーンを Active Scene にする（Instantiate したものがシーンと一緒に破棄されるように）
- 既存のミニゲームを壊さないよう、`MiniGameBase` などの公開 API の名前変更・削除は避ける（必要なら `[Obsolete]` で移行期間を置く）

## ミニゲームの規約（メンバー向け。詳細は docs/minigame-guide.md）

- フォルダは `Assets/MiniGames/作者_ゲーム名/`、シーンは1本につき1つ、MiniGameInfo を同じフォルダに置く
- namespace は `MiniGames.作者.ゲーム名`、asmdef も同名で `MiniGameFramework` を参照（TMP を使うなら `Unity.TextMeshPro`、uGUI なら `UnityEngine.UI` も追加）
- 難易度 Lv1〜3 をすべて実装。難易度に応じた準備は `Start` ではなく `OnSetup` で行う
- 操作受付は `IsPlaying` が true の間だけ。入力は必ず `MiniGameInput` 経由
- 禁止：`Time.unscaledDeltaTime` / `WaitForSecondsRealtime` / `Time.realtimeSinceStartup` / `DateTime`、`Time.timeScale` の変更、Animator の Unscaled Time、`SceneManager.LoadScene`、`DontDestroyOnLoad`、`Application.Quit`、旧 Input、static な状態の保持、Tags / Layers / ProjectSettings / Packages の変更
- グローバル設定（`Physics2D.gravity` など）を変えたら `OnTimeUp` で戻す
- 音は `PlaySE` / `PlayBGM` で鳴らす（自前の AudioSource だと BGM / 効果音の音量設定が効かない）
- 開始から約 0.8 秒は画面中央に指示文が重なるので、その間にすぐ失敗する仕掛けを出さない
- UI の Canvas は Screen Space - Camera 推奨（Overlay なら Sort Order 1000 未満）
- シーンに AudioListener / EventSystem を置かない

## Git 運用

- `main` は Ruleset で保護（PR 必須・承認1・Code Owners レビュー必須）。管理者はバイパス可
- ブランチ名：Core は `core/内容`、ミニゲームは `名前/ゲーム名`、ドキュメントは `docs/内容`
- 作業前に `git switch main` → `git pull` → ブランチ作成
- Core の変更とミニゲームは別ブランチ・別 PR に分ける
- 重ねたブランチ（ブランチから作ったブランチ）をマージするときは **Create a merge commit** を使う（Squash / Rebase だと後続 PR に差分が残る）
- ミニゲームのマージ後、**管理者が** `MiniGame → カタログとBuild Settingsを更新` を実行してコミットする（メンバーは実行しない）
- `.gitignore` で `Library/` `Temp/` `*.sln` `*.slnx` などを除外済み

### つまずきやすい点

- **Unity を開いたまま git 操作をすると `Permission denied` になることがある** → Unity を閉じてから
- **`.meta` はファイル・フォルダとセット**でコミットする（フォルダの `.meta` も）。空フォルダの `.meta` だけはコミットしない
- `git add` はその時点のコピー。ファイルを変えたら再度 `git add`。外すときは `git restore --staged`
- 公開リポジトリなので、商用フォントなど再配布不可の素材は入れない（過去に商用フォントを OFL フォントに差し替え済み）。OFL フォントは OFL.txt を同梱する

## 作業するときの注意（Claude 向け）

- Unity エディタは実行できない。コンパイルや動作の確認はユーザーに依頼し、Console の内容を貼ってもらう
- `ProjectSettings/` `Packages/` を変える提案は、全員に影響することを明記してから行う
- Core を変更したら、`docs/minigame-guide.md` にメンバーへの影響（新機能・新ルール）を反映する。過去に案内した追記（PlayBGM、音の鳴らし方、Canvas の Sort Order、指示文の重なり、Info の BGM 項目）が反映済みか確認する
- シーンや Prefab のファイル（YAML）を手で書かない。必要ならエディタ拡張で生成するか、エディタでの手順を説明する
- ミニゲームのコードを書くときは、その作者のフォルダ・namespace・asmdef の中に収める
