# ミニゲームの作り方

このドキュメントは、ミニゲームを作るメンバー向けの説明です。

---

## 0. 最初に1回だけやること

リポジトリをクローンしたら、プロジェクトのフォルダで次を実行してください（改行コードの設定です）。

```
git config core.autocrlf input
```

---

## 1. サンプルを動かしてみる

1. `Assets/MiniGames/_Template/Dodge.unity` を開いて **Play** します
2. 「よけろ！」と表示されたあと、矢印キー（または A / D）で左右に動いて石をよけます
3. 同じミニゲームが何度も繰り返されます（デバッグ起動）

デバッグ起動中のキー操作：

| キー | 内容 |
|---|---|
| F1 / F2 / F3 | 難易度を Lv1 / Lv2 / Lv3 にする |
| F5 / F6 | 速度を 0.1 下げる / 上げる |

設定は次の回から反映されます。

`Assets/Core/Scenes/Main.unity` を開いて Play すると、登録されている全ミニゲームを通しで遊べます。

---

## 2. 自分のミニゲームを作る

### フォルダを作る

`Assets/MiniGames/` の下に **`作者名_ゲーム名`** のフォルダを作ります（例：`Nietz_Catch`）。
**このフォルダの外は変更しないでください。**

```
Assets/MiniGames/Nietz_Catch/
├─ Catch.unity          ← シーン（1本につき1つ）
├─ CatchInfo.asset      ← MiniGameInfo
├─ Scripts/
│   ├─ MiniGames.Nietz.Catch.asmdef
│   └─ CatchGame.cs
└─ Art/ Audio/ など
```

### Assembly Definition を作る

1. `Scripts` フォルダで右クリック → Create → Scripting → **Assembly Definition**
2. 名前を `MiniGames.作者名.ゲーム名` にします
3. Inspector の **Assembly Definition References** に `MiniGameFramework` を追加して Apply

### スクリプトを書く

`MiniGameBase` を継承したクラスを1つ作ります。namespace は `MiniGames.作者名.ゲーム名` にしてください。

```csharp
using MiniGameFramework;
using UnityEngine;

namespace MiniGames.Nietz.Catch
{
    public class CatchGame : MiniGameBase
    {
        // 難易度に応じた準備（Start ではなくここで行う）
        protected override void OnSetup()
        {
            // Difficulty は 1〜3
        }

        // 操作開始
        protected override void OnBegin()
        {
        }

        void Update()
        {
            if (!IsPlaying) return;

            if (MiniGameInput.ActionDown)
            {
                // 達成したら
                Succeed();
            }
        }

        // 時間切れ（グローバル設定を変えた場合はここで戻す）
        protected override void OnTimeUp()
        {
        }
    }
}
```

### MiniGameInfo を作る

フォルダで右クリック → Create → MiniGame → **Info**

| 項目 | 内容 |
|---|---|
| gameId | 他と重ならないID（例：`nietz_catch`） |
| title / author | タイトルと作者名 |
| instruction | 開始前に出る指示（例：`つかめ！`）。短く！ |
| description | 練習モードの「操作説明」画面の **【説明】** に出る文。遊び方を数行で書きます（例：`かごを うごかして、おちてくる りんごを つかもう！`）。空なら instruction が出ます |
| inputType | 使う操作。練習モードの「操作説明」画面の **【操作タイプ】** に自動で表示されます |
| length | Normal（4秒） / Long（8秒） |
| judgeType | Survive（何もなければ成功） / Achieve（何もしなければ失敗） |
| scenePath | 空でOK（自動で入ります） |
| bgm / bgmVolume | このゲームの BGM と音量。空なら共通の BGM が流れます。1倍速でゲームの長さ（4秒 / 8秒）ぴったりの曲がおすすめ |
| noBgm | オンにすると BGM を流しません（共通の BGM も流れません） |

### シーンを作る

1. フォルダに新しいシーンを作ります（**1本につきシーンは1つ**）
2. カメラはそのまま使ってOKです（Orthographic Size 5、16:9 を想定）
3. 空の GameObject を作り、自分のスクリプトを付けて、**Info** に MiniGameInfo を設定します
4. このシーンで Play すれば、デバッグ起動で遊べます

---

## 3. 使える機能

| 機能 | 説明 |
|---|---|
| `Difficulty` | 難易度（1〜3）。**Lv1〜3 すべての実装が必須** |
| `IsPlaying` | プレイ中なら true。これが false の間は操作を受け付けない |
| `Succeed()` / `Fail()` | 結果を確定する。一度確定したら変わらない |
| `PlaySE(clip)` | 効果音。速度に合わせてピッチが上がる |
| `MiniGameInput.Direction` | 方向入力（矢印 / WASD / スティック） |
| `MiniGameInput.Action` / `ActionDown` / `ActionUp` | ボタン（Space / Z / Enter / ゲームパッドのA） |
| `MiniGameInput.Pointer` / `PointerDown` / `PointerUp` | マウス左ボタン |
| `MiniGameInput.PointerWorldPosition()` | マウスのワールド座標 |

---

## 4. ルール（重要）

ゲームの速度は Core が `Time.timeScale` で変えています。**普通に `Time.deltaTime` を使って1倍速で気持ちいいゲームを作れば、速度には自動で対応します。**

その仕組みを壊さないために、次は使わないでください。

| 使わないもの | 理由 |
|---|---|
| `Time.unscaledDeltaTime`、`WaitForSecondsRealtime`、`Time.realtimeSinceStartup`、`DateTime` | 速度についてこなくなる |
| `Time.timeScale` の変更 | Core が速度管理に使っている |
| Animator の Update Mode「Unscaled Time」、DOTween の `SetUpdate(true)` | 同上 |
| `SceneManager.LoadScene`、`DontDestroyOnLoad`、`Application.Quit` | Core のシーン管理が壊れる |
| `Input.GetKey` などの旧入力、Input System を直接使うこと | 必ず `MiniGameInput` を使う |
| static な変数に状態を保存すること | 2回目に同じゲームが出たとき前回の値が残る |
| Tags / Layers / Project Settings / パッケージの変更 | 全員に影響する。必要なら管理者に相談 |

その他：

- 難易度に応じた準備は `Start()` ではなく `OnSetup()` で行ってください
- `Physics2D.gravity` などを変えた場合は、`OnTimeUp()` で元に戻してください
- 素材（画像・音楽・フォント）は自作か、公開してよいライセンスのものだけを使ってください（リポジトリは公開されています）
- 画面が暗い場合は、シーンに Light 2D（Global）を追加してください

---

## 5. 提出する

1. `main` から自分用のブランチを作る（例：`nietz/catch`）
2. 自分のフォルダだけをコミットする
3. GitHub で Pull Request を作り、チェックリストを確認する
4. レビューでOKが出たらマージ

マージ後、管理者がメニューの **MiniGame → カタログとBuild Settingsを更新** を実行して、本編に登録します。
