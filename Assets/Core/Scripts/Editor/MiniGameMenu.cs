using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniGameFramework.EditorTools
{
    /// <summary>
    /// メニューバーの「MiniGame」メニュー。
    /// </summary>
    public static class MiniGameMenu
    {
        const string MainScenePath = GameLaunchSettings.MainScenePath;
        const string TitleScenePath = GameLaunchSettings.TitleScenePath;
        const string CatalogPath = "Assets/Core/MiniGameCatalog.asset";
        const string MiniGamesRoot = "Assets/MiniGames";
        const string TemplateRoot = "Assets/MiniGames/_Template";

        // ------------------------------------------------------------------
        // 初期セットアップ（Core 担当が最初に1回だけ実行する）
        // ------------------------------------------------------------------

        [MenuItem("MiniGame/初期セットアップ（Mainシーンとサンプルを生成）", priority = 100)]
        static void InitialSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var catalog = GetOrCreateCatalog();

            if (File.Exists(MainScenePath)) Debug.Log("[MiniGame] Main.unity は既にあるのでスキップしました");
            else CreateMainScene(catalog);

            if (File.Exists(TemplateRoot + "/Dodge.unity")) Debug.Log("[MiniGame] サンプルは既にあるのでスキップしました");
            else CreateSample();

            RefreshCatalog();
            EditorSceneManager.OpenScene(MainScenePath);
            Debug.Log("[MiniGame] 初期セットアップが完了しました。Main シーンで Play すると通しで遊べます");
        }

        // ------------------------------------------------------------------
        // カタログと Build Settings の更新（ミニゲームを取り込むたびに実行する）
        // ------------------------------------------------------------------

        [MenuItem("MiniGame/カタログとBuild Settingsを更新", priority = 101)]
        public static void RefreshCatalog()
        {
            var catalog = GetOrCreateCatalog();
            var infos = new List<MiniGameInfo>();
            var warnings = new List<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:MiniGameInfo", new[] { MiniGamesRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var info = AssetDatabase.LoadAssetAtPath<MiniGameInfo>(path);
                if (info == null) continue;

                // Info と同じフォルダ（サブフォルダ含む）にあるシーンを探す
                var folder = Path.GetDirectoryName(path).Replace('\\', '/');
                var scenes = AssetDatabase.FindAssets("t:Scene", new[] { folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();

                if (scenes.Length == 0)
                {
                    warnings.Add($"{path}: 同じフォルダにシーンがありません");
                    continue;
                }
                if (scenes.Length > 1 && !scenes.Contains(info.scenePath))
                {
                    warnings.Add($"{path}: フォルダ内にシーンが複数あります。ミニゲーム1本につきシーンは1つにしてください");
                    continue;
                }
                if (scenes.Length == 1 && info.scenePath != scenes[0])
                {
                    info.scenePath = scenes[0];
                    EditorUtility.SetDirty(info);
                }

                if (string.IsNullOrEmpty(info.gameId)) warnings.Add($"{path}: gameId が空です");
                if (string.IsNullOrEmpty(info.instruction)) warnings.Add($"{path}: instruction（指示文）が空です");

                infos.Add(info);
            }

            foreach (var group in infos.GroupBy(i => i.gameId).Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1))
            {
                warnings.Add($"gameId「{group.Key}」が {group.Count()} 本で重複しています");
            }

            infos.Sort((a, b) => string.CompareOrdinal(a.scenePath, b.scenePath));
            catalog.games = infos;
            EditorUtility.SetDirty(catalog);

            // Build Settings：Title → Main の順に置き、その後にミニゲームのシーンを並べる
            // （ビルドしたゲームは先頭のシーンから始まる）
            var buildScenes = new List<EditorBuildSettingsScene>();
            if (File.Exists(TitleScenePath)) buildScenes.Add(new EditorBuildSettingsScene(TitleScenePath, true));
            if (File.Exists(MainScenePath)) buildScenes.Add(new EditorBuildSettingsScene(MainScenePath, true));
            foreach (var path in infos.Select(i => i.scenePath).Distinct())
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();

            foreach (var w in warnings) Debug.LogWarning("[MiniGame] " + w);
            Debug.Log($"[MiniGame] {infos.Count} 本のミニゲームを登録しました（警告 {warnings.Count} 件）");
        }

        // ------------------------------------------------------------------

        static MiniGameCatalog GetOrCreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MiniGameCatalog>(CatalogPath);
            if (catalog != null) return catalog;

            EnsureFolder(Path.GetDirectoryName(CatalogPath).Replace('\\', '/'));
            catalog = ScriptableObject.CreateInstance<MiniGameCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        static void CreateMainScene(MiniGameCatalog catalog)
        {
            EnsureFolder(Path.GetDirectoryName(MainScenePath).Replace('\\', '/'));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Core のカメラ：ミニゲームのカメラより先に描画される（depth -100）
            // Camera.main がミニゲームのカメラを指すよう、MainCamera タグは付けない
            var cameraGo = new GameObject("CoreCamera");
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.depth = -100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            cameraGo.AddComponent<AudioListener>();

            var runnerGo = new GameObject("MiniGameRunner");
            var runner = runnerGo.AddComponent<MiniGameRunner>();
            SetReference(runner, "catalog", catalog);

            EditorSceneManager.SaveScene(scene, MainScenePath);
        }

        static void CreateSample()
        {
            var gameType = FindType("MiniGames.Template.Dodge.DodgeGame");
            var playerType = FindType("MiniGames.Template.Dodge.DodgePlayer");
            if (gameType == null || playerType == null)
            {
                Debug.LogError("[MiniGame] サンプルのスクリプト（DodgeGame / DodgePlayer）が見つかりません。コンパイルエラーがないか確認してください");
                return;
            }

            EnsureFolder(TemplateRoot + "/Art");
            var square = CreateShapeSprite(TemplateRoot + "/Art/Square.png", false);
            var circle = CreateShapeSprite(TemplateRoot + "/Art/Circle.png", true);

            var infoPath = TemplateRoot + "/DodgeInfo.asset";
            var info = AssetDatabase.LoadAssetAtPath<MiniGameInfo>(infoPath);
            if (info == null)
            {
                info = ScriptableObject.CreateInstance<MiniGameInfo>();
                info.gameId = "template_dodge";
                info.title = "よけろ！（サンプル）";
                info.author = "Core";
                info.instruction = "よけろ！";
                info.inputType = InputType.Direction;
                info.length = GameLength.Normal;
                info.judgeType = JudgeType.Survive;
                AssetDatabase.CreateAsset(info, infoPath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            cameraGo.AddComponent<AudioListener>();

            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, -4f, 0f);
            var renderer = playerGo.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = new Color(0.2f, 0.45f, 1f);
            var body = playerGo.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var box = playerGo.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.8f, 0.8f);
            var player = playerGo.AddComponent(playerType);

            var gameGo = new GameObject("DodgeGame");
            var game = gameGo.AddComponent(gameType);

            SetReference(game, "info", info);
            SetReference(game, "player", player);
            SetReference(game, "rockSprite", circle);
            SetReference(player, "game", game);

            EditorSceneManager.SaveScene(scene, TemplateRoot + "/Dodge.unity");
        }

        static Sprite CreateShapeSprite(string path, bool circle)
        {
            if (!File.Exists(path))
            {
                const int size = 32;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                float r = size / 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - r;
                        float dy = y + 0.5f - r;
                        bool inside = !circle || dx * dx + dy * dy <= r * r;
                        pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);

                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[MiniGame] {target.GetType().Name} に {propertyName} が見つかりません");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(fullName))
                .FirstOrDefault(t => t != null);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
