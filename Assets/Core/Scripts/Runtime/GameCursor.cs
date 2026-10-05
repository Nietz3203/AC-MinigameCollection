using UnityEngine;

namespace MiniGameFramework
{
    /// <summary>
    /// マウスカーソルの見た目を変える。Title シーン（と Main シーン）の一番上の階層に置く。
    /// シーンを切り替えても残り、2つ目以降は自動で消える。
    ///
    /// 画像は Texture Type を「Cursor」にしてインポートする。
    /// </summary>
    public class GameCursor : MonoBehaviour
    {
        [Tooltip("通常のカーソル")]
        [SerializeField] Texture2D normal;

        [Tooltip("クリック中のカーソル（なくてもよい）")]
        [SerializeField] Texture2D pressed;

        [Tooltip("クリックの位置（画像の左上を 0,0 としたピクセル座標）。矢印なら先端、照準なら中心")]
        [SerializeField] Vector2 hotspot;

        [Tooltip("画像が大きくて表示がおかしいときはオンにする（Unity がカーソルを描く方式になる）")]
        [SerializeField] bool forceSoftware;

        static GameCursor instance;
        bool isPressed;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Apply(false);
        }

        void Update()
        {
            if (pressed == null) return;

            bool now = MiniGameInput.Pointer;
            if (now != isPressed) Apply(now);
        }

        void Apply(bool down)
        {
            isPressed = down;
            var texture = down && pressed != null ? pressed : normal;
            Cursor.SetCursor(texture, hotspot, forceSoftware ? CursorMode.ForceSoftware : CursorMode.Auto);
        }

        void OnDestroy()
        {
            if (instance != this) return;

            instance = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
