using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniGameFramework
{
    /// <summary>
    /// ミニゲーム用の入力。ミニゲームからは必ずここを経由して入力を取る。
    /// キーボード（矢印 / WASD、Space / Z / Enter）とゲームパッドに対応。
    /// </summary>
    public static class MiniGameInput
    {
        /// <summary>方向入力（長さは最大1）</summary>
        public static Vector2 Direction
        {
            get
            {
                Vector2 v = Vector2.zero;

                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) v.x -= 1f;
                    if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) v.x += 1f;
                    if (kb.upArrowKey.isPressed || kb.wKey.isPressed) v.y += 1f;
                    if (kb.downArrowKey.isPressed || kb.sKey.isPressed) v.y -= 1f;
                }

                var gp = Gamepad.current;
                if (gp != null)
                {
                    v += gp.leftStick.ReadValue();
                    v += gp.dpad.ReadValue();
                }

                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        /// <summary>ボタンを押している間 true</summary>
        public static bool Action
        {
            get
            {
                var kb = Keyboard.current;
                var gp = Gamepad.current;
                return (kb != null && (kb.spaceKey.isPressed || kb.zKey.isPressed || kb.enterKey.isPressed))
                    || (gp != null && gp.buttonSouth.isPressed);
            }
        }

        /// <summary>ボタンを押した瞬間だけ true</summary>
        public static bool ActionDown
        {
            get
            {
                var kb = Keyboard.current;
                var gp = Gamepad.current;
                return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            }
        }

        /// <summary>ボタンを離した瞬間だけ true</summary>
        public static bool ActionUp
        {
            get
            {
                var kb = Keyboard.current;
                var gp = Gamepad.current;
                return (kb != null && (kb.spaceKey.wasReleasedThisFrame || kb.zKey.wasReleasedThisFrame || kb.enterKey.wasReleasedThisFrame))
                    || (gp != null && gp.buttonSouth.wasReleasedThisFrame);
            }
        }

        /// <summary>マウス左ボタンを押している間 true</summary>
        public static bool Pointer => Mouse.current != null && Mouse.current.leftButton.isPressed;

        /// <summary>マウス左ボタンを押した瞬間だけ true</summary>
        public static bool PointerDown => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        /// <summary>マウス左ボタンを離した瞬間だけ true</summary>
        public static bool PointerUp => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;

        /// <summary>マウスの画面座標</summary>
        public static Vector2 PointerScreenPosition =>
            Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        /// <summary>マウスのワールド座標（2D用、z = 0）。カメラ省略時は Camera.main</summary>
        public static Vector3 PointerWorldPosition(Camera camera = null)
        {
            if (camera == null) camera = Camera.main;
            if (camera == null) return Vector3.zero;

            Vector2 p = PointerScreenPosition;
            Vector3 w = camera.ScreenToWorldPoint(new Vector3(p.x, p.y, -camera.transform.position.z));
            w.z = 0f;
            return w;
        }
    }
}
