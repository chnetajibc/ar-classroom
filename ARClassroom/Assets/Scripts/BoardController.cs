using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DSA whiteboard. Renders topics on a World-Space canvas and notifies tutor.
/// Attach to the Board GameObject created by ClassroomBuilder.
/// </summary>
public class BoardController : MonoBehaviour
{
    [Serializable]
    public class DsaTopic
    {
        public string title;
        [TextArea(3, 8)] public string body;
        public string code;
        public Color accent = new Color(0.2f, 0.9f, 0.6f);
    }

    public DsaTopic[] topics = new DsaTopic[]
    {
        new DsaTopic {
            title = "1/7  ARRAYS  •  O(1) access",
            body = "• Contiguous memory\n• arr[i] = base + i * size\n• Insert at end O(1), middle O(n)\n• Great for cache + index lookup",
            code = "int[] a = {10,20,30};\n// read:  a[1] -> 20  O(1)\n// push:  O(1) / O(n) if resize",
            accent = new Color(0.25f, 0.85f, 1f)
        },
        new DsaTopic {
            title = "2/7  LINKED LIST  •  O(1) insert",
            body = "• Nodes + pointers, not contiguous\n• Insert/delete at head O(1)\n• Search O(n) — must walk\n• No resize cost like arrays",
            code = "class Node { int val; Node next; }\n// head -> [10] -> [20] -> null\n// insert head: O(1)",
            accent = new Color(0.35f, 1f, 0.55f)
        },
        new DsaTopic {
            title = "3/7  STACK & QUEUE",
            body = "• Stack: LIFO — Push / Pop / Peek O(1)\n• Queue: FIFO — Enqueue / Dequeue O(1)\n• Undo, BFS, brackets, sliding window",
            code = "Stack<int> s = new();\ns.Push(1); s.Pop();  // LIFO\nQueue<int> q = new();\nq.Enqueue(1); q.Dequeue(); // FIFO",
            accent = new Color(1f, 0.8f, 0.3f)
        },
        new DsaTopic {
            title = "4/7  BIG-O CHEAT SHEET",
            body = "• O(1) < O(log n) < O(n) < O(n log n) < O(n²)\n• Binary search O(log n)\n• Two pointers saves a loop\n• Hash map trades space for time",
            code = "// n = 1_000_000\n// O(n²)  ~ 31 years (bad!)\n// O(n log n) ~ 20M ops (good)",
            accent = new Color(1f, 0.45f, 0.6f)
        },
        new DsaTopic {
            title = "5/7  BINARY SEARCH TREE",
            body = "• Left < Root < Right\n• Search / Insert avg O(log n)\n• Worst O(n) if unbalanced\n• AVL / Red-Black keep it balanced",
            code = "      8\n    /   \\\n   3     10\n  / \\      \\\n 1   6      14\n// search(6): 8->3->6  O(log n)",
            accent = new Color(0.7f, 0.6f, 1f)
        },
        // ---- Moved here from the walls (were TextMesh posters/banner) ----
        // High-res board canvas keeps this crisp; wall TextMesh was blurry.
        new DsaTopic {
            title = "6/7  QUICK REFERENCE  •  ex-wall posters",
            body = "• BIG-O: O(1) FAST, O(n^2) SLOW\n• ARRAYS O(1) lookup\n• LISTS O(n) search\n• MAPS O(1) average",
            code = "// O(1) FAST, O(n^2) SLOW\n// arrays O(1), lists O(n)\n// maps O(1) avg",
            accent = new Color(0.12f, 0.3f, 0.55f)
        },
        new DsaTopic {
            title = "7/7  AR CLASSROOM  •  DSA MOTTO",
            body = "• CODE / DEBUG / REPEAT\n• STAY CURIOUS\n• LEARN SOMETHING DAILY\n• O(log n) MINDSET",
            code = "// AR CLASSROOM\n// DATA STRUCTURES &\n// ALGORITHMS",
            accent = new Color(0.1f, 0.45f, 0.35f)
        },
    };

    public int currentIndex = 0;

    Transform boardFace;
    Text titleText;
    Text bodyText;
    Text codeText;
    Image accentBar;
    Text hintText;

    public event Action<int> OnTopicChanged;

    void Awake()
    {
        BuildBoardVisuals();
        ShowTopic(0);
    }

    void BuildBoardVisuals()
    {
        // Board frame already exists as parent; add face canvas
        boardFace = transform.Find("BoardCanvas");
        if (boardFace != null) return;

        GameObject canvasGO = new GameObject("BoardCanvas");
        canvasGO.transform.SetParent(transform, false);
        // Board is 4.4 wide x 2.4 tall, positioned at front wall
        canvasGO.transform.localPosition = new Vector3(0, 0, 0.06f);
        // Face the students: per Unity world-space UI behavior (verified:
        // Transform.LookAt(viewer) on UI renders mirrored; the fix used
        // across Unity Discussions/StackOverflow is a 180° Y flip so the
        // canvas +Z points AWAY from the viewer i.e. into the wall (-Z),
        // leaving the readable face toward the students at +Z).
        // Without this the board reads backwards (see screenshot).
        canvasGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        // World-space Canvas: sizeDelta is a pixel RESOLUTION, not meters, so
        // scale it down (880px -> 4.4m, 480px -> 2.4m = 0.005). Without this the
        // board face renders ~200x oversized and the text flies off the wall.
        canvasGO.transform.localScale = Vector3.one * 0.005f;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // 880 x 480 px over 4.4 x 2.4 m => 200 px per meter
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(880, 480);

        // Chalkboard face
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.10f, 0.14f, 0.13f, 1f); // slate-green chalkboard
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        accentBar = CreateUIBar(canvasGO.transform, new Color(1f, 0.9f, 0.55f)); // chalk yellow

        titleText = CreateUIText(canvasGO.transform, "Title",
            new Rect(20, 10, 840, 60), 34, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.99f, 0.94f));
        bodyText = CreateUIText(canvasGO.transform, "Body",
            new Rect(20, 80, 500, 340), 24, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.93f, 0.93f, 0.88f));
        codeText = CreateUIText(canvasGO.transform, "Code",
            new Rect(540, 80, 320, 340), 21, FontStyle.Normal, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.55f));
        hintText = CreateUIText(canvasGO.transform, "Hint",
            new Rect(20, 430, 840, 40), 18, FontStyle.Italic, TextAnchor.LowerLeft,
            new Color(1f, 1f, 1f, 0.55f));
        hintText.text = "Click / tap the board  •  Next topic button works too";
    }

    Image CreateUIBar(Transform parent, Color c)
    {
        GameObject go = new GameObject("Accent");
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = c;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -72); rt.offsetMax = new Vector2(0, -64);
        return img;
    }

    Text CreateUIText(Transform parent, string name, Rect rect, int size, FontStyle style, TextAnchor anchor, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rt = go.GetComponent<RectTransform>();
        // Anchors pinned top-left (0,1) of the 880x480 canvas, pivot (0,1):
        // per Unity RectTransform docs, offsetMin = lower-left corner
        // relative to the lower-left anchor, offsetMax = upper-right corner
        // relative to the upper-right anchor. With both anchors collapsed at
        // top-left, offsets ARE canvas pixels from that corner, so for a
        // rect (x,y,w,h) measured down/right from the top-left:
        //   offsetMin = (x, -y-h), offsetMax = (x+w, -y).
        // (The old code added the canvas half-size again (-440/+240),
        // pushing every label ~240px ≈ 1.2m above the board into the sky —
        // only the accent bar, which used plain negative-Y offsets, landed
        // on the board. That matches the screenshot: cyan bar on board,
        // all text floating mirrored above it.)
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.offsetMin = new Vector2(rect.x, -rect.y - rect.height);
        rt.offsetMax = new Vector2(rect.x + rect.width, -rect.y);
        rt.localScale = Vector3.one;
        return t;
    }

    public void ShowTopic(int i)
    {
        if (topics == null || topics.Length == 0) return;
        currentIndex = ((i % topics.Length) + topics.Length) % topics.Length;
        var t = topics[currentIndex];
        if (titleText) titleText.text = t.title;
        if (bodyText) bodyText.text = t.body;
        if (codeText) codeText.text = t.code;
        if (accentBar) accentBar.color = t.accent;
        OnTopicChanged?.Invoke(currentIndex);
    }

    public void NextTopic() { ShowTopic(currentIndex + 1); }
    public void PrevTopic() { ShowTopic(currentIndex - 1); }
    public DsaTopic CurrentTopic() { return topics[currentIndex]; }
}
