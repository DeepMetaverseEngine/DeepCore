using System;
using UnityEngine;

namespace DeepCore.Unity.OnGUI
{
    public delegate void DrawGridAction(int column, int row, Rect rect);
    public delegate void DrawGridActionRect(Rect rect);

    public class GUIUtils //: UnityEngine.GUI
    {
        //         public static Rect TooltipRect
        //         {
        //             get => UnityEngine.GUI.tooltipRect;
        //         }

        public static bool DrawCoolDownButton(Rect btnRect, GUIContent content, float amount, Texture2D texture)
        {
            var ret = GUI.Button(btnRect, content); 
            if (amount > 0)
            {
                btnRect.x += 4;
                btnRect.y += 4;
                btnRect.width -= 8;
                btnRect.height -= 8;
                float progress = amount; // 0.0 到 1.0 进度
                float angle = progress * 360f;
                var center = btnRect.center;
                var hsize = btnRect.height * progress;
                GUI.DrawTexture(new Rect(btnRect.x, btnRect.y + btnRect.height - hsize, btnRect.width, hsize), texture);
                //GUI.Box(new Rect(btnRect.x, btnRect.y + btnRect.height - hsize, btnRect.width, hsize), texture);
            }
            if (ret)
            {
                Input.ResetInputAxes();
            }
            return ret;
        }

        public static bool Toggle(Rect position, ref bool value, string text)
        {
            var new_value = UnityEngine.GUI.Toggle(position, value, text);
            if (value != new_value)
            {
                value = new_value;
                Input.ResetInputAxes();
                return true;
            }
            return false;
        }
        public static bool Toggle(Rect position, ref bool value, GUIContent content)
        {
            var new_value = UnityEngine.GUI.Toggle(position, value, content);
            if (value != new_value)
            {
                value = new_value;
                Input.ResetInputAxes();
                return true;
            }
            return false;
        }
        public static bool Toggle(Rect position, ref bool value, GUIContent content, GUIStyle style)
        {
            var new_value = UnityEngine.GUI.Toggle(position, value, content, style);
            if (value != new_value)
            {
                value = new_value;
                Input.ResetInputAxes();
                return true;
            }
            return false;
        }

        public static bool Button(Rect position, GUIContent content)
        {
            if (UnityEngine.GUI.Button(position, content))
            {
                Input.ResetInputAxes();
                return true;
            }
            return false;
        }
        public static bool Button(Rect position, GUIContent content, GUIStyle style)
        {
            if (UnityEngine.GUI.Button(position, content, style))
            {
                Input.ResetInputAxes();
                return true;
            }
            return false;
        }

        public static Rect DrawGrid(Vector2 start, Vector2 cell, int columnCount, int rowCount, DrawGridAction draw)
        {
            var sh = Math.Abs(cell.y);
            var sw = Math.Abs(cell.x);
            var sy = start.y + (cell.y < 0 ? cell.y : 0);
            var sx = start.x + (cell.x < 0 ? cell.x : 0);
            var rect = new Rect(start, new Vector2(columnCount * sw, rowCount * sh));
            if (cell.x < 0)
            {
                start.x -= rect.width;
                rect.x = start.x;
            }
            if (cell.y < 0)
            {
                start.y -= rect.height;
                rect.y = start.y;
            }
            for (int dy = 0; dy < rowCount; dy++)
            {
                for (int dx = 0; dx < columnCount; dx++)
                {
                    var grid = new Rect(sx + dx * cell.x, sy + dy * cell.y, sw, sh);
                    draw.Invoke(dx, dy, grid);
                }
            }
            return rect;
        }
        public static Rect DrawGrid(Vector2 start, Vector2 cell, int columnCount, int rowCount, params DrawGridAction[] draw)
        {
            var di = 0;
            return DrawGrid(start, cell, columnCount, rowCount, (c, r, rect) =>
            {
                if (di < draw.Length)
                {
                    draw[di].Invoke(c, r, rect);
                    di++;
                }
            });
        }
        public static Rect DrawGrid(Vector2 start, Vector2 cell, DrawGridAction[,] draw)
        {
            int columnCount = draw.GetLength(0);
            int rowCount = draw.GetLength(1);
            return DrawGrid(start, cell, columnCount, rowCount, (c, r, rect) =>
            {
                draw[c, r].Invoke(c, r, rect);
            });
        }

        public static Rect DrawGrid(Vector2 start, Vector2 cell, int columnCount, int rowCount, DrawGridActionRect draw)
        {
            return DrawGrid(start, cell, columnCount, rowCount, (c, r, rect) => draw(rect));
        }
        public static Rect DrawGrid(Vector2 start, Vector2 cell, int columnCount, int rowCount, params DrawGridActionRect[] draw)
        {
            var di = 0;
            return DrawGrid(start, cell, columnCount, rowCount, (c, r, rect) =>
            {
                if (di < draw.Length)
                {
                    draw[di].Invoke(rect);
                    di++;
                }
            });
        }
        public static Rect DrawGrid(Vector2 start, Vector2 cell, DrawGridActionRect[,] draw)
        {
            int columnCount = draw.GetLength(1);
            int rowCount = draw.GetLength(0);
            return DrawGrid(start, cell, columnCount, rowCount, (c, r, rect) =>
            {
                draw[r, c].Invoke(rect);
            });
        }
        public static Rect DrawGrid(Vector2 start, Vector2 cell, int columnCount, int rowCount, DrawGridActionRect[][] draw)
        {
            return DrawGrid(start, cell, columnCount, rowCount, (c, r, rect) =>
            {
                draw[r][c].Invoke(rect);
            });
        }

        public static Rect AutoTooltips()
        {
            return AutoTooltips(new GUIContent() { text = UnityEngine.GUI.tooltip }, Event.current.mousePosition, new Vector2(200, 400));
        }
        public static Rect AutoTooltips(Vector2 pos)
        {
            return AutoTooltips(new GUIContent() { text = UnityEngine.GUI.tooltip }, pos, new Vector2(200, 400));
        }
        public static Rect AutoTooltips(GUIContent content)
        {
            return AutoTooltips(content, Event.current.mousePosition, new Vector2(200, 400));
        }
        public static Rect AutoTooltips(GUIContent content, Vector2 pos)
        {
            return AutoTooltips(content, pos, new Vector2(200, 400));
        }
        private static readonly int[][] NEXT_INDEX_TABLE = new int[][] {
            new int[]{-1,-1}, new int[]{0,-1}, new int[]{ 1,-1},
            new int[]{-1, 0},/*new int[]{0,0}*/new int[]{ 1, 0},
            new int[]{-1, 1}, new int[]{0, 1}, new int[]{ 1, 1} };
        public static Rect AutoTooltips(GUIContent text, Vector2 pos, Vector2 size)
        {

            var space = 10;
            var mp = pos;// GUIUtils.tooltipRect.position;//Event.current.mousePosition;
            var stype = new GUIStyle()
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
            };
            var rect = new Rect(mp.x + space, mp.y + space, size.x, size.y);
            if (mp.x > Screen.width / 2f)
            {
                rect.x = mp.x - size.x - space;
                if (mp.y > Screen.height / 2f)
                {
                    stype.alignment = TextAnchor.LowerRight;
                    rect.y = mp.y - size.y - space;
                }
            }
            else
            {
                if (mp.y > Screen.height / 2f)
                {
                    stype.alignment = TextAnchor.LowerLeft;
                    rect.y = mp.y - size.y - space;
                }
            }
            rect.x = Math.Max(rect.x, 0);
            rect.x = Math.Min(rect.x, Screen.width - rect.width);
            rect.y = Math.Max(rect.y, 0);
            rect.y = Math.Min(rect.y, Screen.height - rect.height);
            if (!string.IsNullOrEmpty(text.text))
            {
                foreach (var tb in NEXT_INDEX_TABLE)
                {
                    var r = rect;
                    r.x += tb[0];
                    r.y += tb[1];
                    stype.normal.textColor = Color.black;
                    UnityEngine.GUI.Label(r, text, stype);
                }
                {
                    stype.normal.textColor = Color.white;
                    UnityEngine.GUI.Box(rect, text, stype);
                }
            }
            return rect;
        }
    }
}
