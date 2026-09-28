using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RealisticClassroom
{
    /// <summary>Chalk lesson notes on the blackboard (world-space canvas): title, bullet points and a code sample.</summary>
    public class BoardText : MonoBehaviour
    {
        public const int MaxBullets = 4;
        public const int MaxCode = 6;
        public float width = 2.8f;
        public float height = 1.4f;

        const float CanvasW = 1000f, CanvasH = 500f;
        Text title;
        Text[] bullets, code;
        Font font, mono;
        Color accent = Color.white;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mono = Font.CreateDynamicFontFromOSFont(new[] { "Menlo", "Courier New", "Consolas", "monospace" }, 32);
            var canvasGo = new GameObject("BoardCanvas", typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CanvasW, CanvasH);
            rt.localScale = new Vector3(width / CanvasW, height / CanvasH, 1f);

            title = MakeText("Title", RowY(0), 54, FontStyle.Bold, font);
            bullets = new Text[MaxBullets];
            for (int i = 0; i < MaxBullets; i++) bullets[i] = MakeText("Bullet" + i, RowY(1 + i), 36, FontStyle.Normal, font);
            code = new Text[MaxCode];
            for (int i = 0; i < MaxCode; i++) code[i] = MakeText("Code" + i, RowY(1 + MaxBullets + i), 30, FontStyle.Normal, mono ?? font);
            Clear();
        }

        static float RowY(int row)
        {
            if (row == 0) return 205f;
            if (row <= MaxBullets) return 140f - (row - 1) * 46f;
            return -40f - (row - 1 - MaxBullets) * 33f;
        }

        Text MakeText(string n, float y, int size, FontStyle style, Font f)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform.GetChild(0), false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(940f, 50f);
            rt.anchoredPosition = new Vector2(0f, y);
            var t = go.GetComponent<Text>();
            t.font = f; t.fontSize = size; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public void Clear()
        {
            title.text = "";
            foreach (var t in bullets) t.text = "";
            foreach (var t in code) t.text = "";
        }

        public void SetAccent(Color c)
        {
            accent = c;
            title.color = c;
            foreach (var t in bullets) t.color = new Color(0.96f, 0.97f, 0.94f, 0.93f);
            foreach (var t in code) t.color = Color.Lerp(c, Color.white, 0.35f);
        }

        public IEnumerator WriteTitle(string text, float seconds) { yield return Type(title, text, seconds); }
        public IEnumerator WriteBullet(int i, string text, float seconds) { if (i < bullets.Length) yield return Type(bullets[i], text, seconds); }
        public IEnumerator WriteCode(int i, string text, float seconds) { if (i < code.Length) yield return Type(code[i], text, seconds); }

        /// <summary>World position of a board row (0 title, 1-4 bullets, 5+ code) at horizontal fraction 0..1 of the text width.</summary>
        public Vector3 RowWorld(int row, float fraction)
        {
            float x = -CanvasW * 0.47f + fraction * CanvasW * 0.6f;
            return transform.TransformPoint(new Vector3(x * (width / CanvasW), RowY(row) * (height / CanvasH), 0f));
        }

        IEnumerator Type(Text target, string text, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                target.text = text.Substring(0, Mathf.Clamp(Mathf.CeilToInt(text.Length * t / seconds), 0, text.Length));
                yield return null;
            }
            target.text = text;
        }
    }
}
