using System.Collections.Generic;
using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// 登録されているミニゲームの一覧。
    /// メニューの MiniGame → カタログとBuild Settingsを更新 で自動生成されるので、手で編集しない。
    /// </summary>
    public class MiniGameCatalog : ScriptableObject
    {
        public List<MiniGameInfo> games = new List<MiniGameInfo>();
    }
}
