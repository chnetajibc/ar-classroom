using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime screen-space UI built 100% in code (no prefabs needed).
/// Buttons: Next Topic | Tutor Explains | Rotate | Size +/- | Reset.
/// Plus info panel + toast + help footer. One-click Play friendly.
/// </summary>
public class ClassroomUI : MonoBehaviour
{
    Text infoTitle, infoBody, toastText, statusText;
    GameObject infoPanel, toastGO;
    float toastUntil;

    void Start()
    {
        BuildCanvas();
        SetStatus("Desktop Preview: click board / tutor / students. Right-drag orbits, wheel zooms.");
        TryDetectAR();
    }

    void TryDetectAR()
    {
        bool hasAR = false;
#pragma warning disable CS0618 // classic API: compiles on every Unity version, warning suppressed
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
#pragma warning restore CS0618
        {
            string tn = mb.GetType().FullName ?? "";
            if (tn.Contains("ARSession") || tn.Contains("ARPlaneManager")) { hasAR = true; break; }
        }
        if (hasAR) SetStatus("AR mode: point at floor, tap a plane to place the classroom.");
    }

    void Update()
    {
        if (toastGO && Time.time > toastUntil) toastGO.SetActive(false);
        // Billboard fix not needed for screen-space UI
    }

    public void ShowInfo(string title, string body)
    {
        infoPanel.SetActive(true);
        infoTitle.text = title;
        infoBody.text = body;
        CancelInvoke("HideInfo");
        Invoke("HideInfo", 5f);
    }

    void HideInfo() { if (infoPanel) infoPanel.SetActive(false); }

    public void Toast(string msg)
    {
        toastGO.SetActive(true);
        toastText.text = msg;
        toastUntil = Time.time + 2.5f;
    }

    public void SetStatus(string msg) { if (statusText) statusText.text = msg; }

    // ---------- construction ----------

    void BuildCanvas()
    {
        var canvasGO = new GameObject("ClassroomUI_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();
        // EventSystem (needed for buttons) — create if missing
        if (!FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Top title bar
        var top = Bar(canvasGO.transform, new Vector2(0, 1), new Vector2(0, -0), 54,
            new Color(0.08f, 0.12f, 0.2f, 0.92f));
        Label(top.transform, "AR Classroom — DSA  •  click Play, then click anything in the scene",
            20, TextAnchor.MiddleCenter, Color.white);

        // Bottom button bar
        var bottom = Bar(canvasGO.transform, new Vector2(0, 0), new Vector2(0, 0), 84,
            new Color(0.08f, 0.12f, 0.2f, 0.92f));
        var layout = bottom.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10; layout.padding = new RectOffset(12, 12, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;

        Button(bottom.transform, "◀ Topic", () => { var b = FindBoard(); if (b) { b.PrevTopic(); Toast(b.CurrentTopic().title); } });
        Button(bottom.transform, "Next Topic ▶", () => { var b = FindBoard(); if (b) { b.NextTopic(); Toast(b.CurrentTopic().title); } });
        Button(bottom.transform, "🎓 Tutor Explains", () => { var t = FindTutor(); if (t) { t.OnTapped(); Toast("Tutor is explaining…"); } });
        Button(bottom.transform, "⟳ Rotate", () => { var r = FindRoot(); if (r) r.Rotate(0, 25, 0); });
        Button(bottom.transform, "＋ Size", () => ScaleRoot(1.15f));
        Button(bottom.transform, "－ Size", () => ScaleRoot(1f / 1.15f));
        Button(bottom.transform, "📷 View", () => { var cc = CameraCorners.Instance; if (cc) { cc.Next(); Toast("Camera: " + cc.CurrentName); } });
        Button(bottom.transform, "Reset", () => { var p = FindFirstObjectByType<ARClassroomPlacer>(); if (p) p.ResetPlacement(); if (CameraCorners.Instance) CameraCorners.Instance.GoTo(0); });

        // Info popup (right side)
        infoPanel = Panel(canvasGO.transform, new Vector2(1, 0.5f), new Vector2(320, 170));
        infoTitle = Label(infoPanel.transform, "Student", 22, TextAnchor.UpperLeft, Color.yellow);
        infoBody = Label(infoPanel.transform, "...", 18, TextAnchor.UpperLeft, Color.white);
        // shift body down
        (infoBody.transform as RectTransform).offsetMin = new Vector2(12, 12);
        (infoBody.transform as RectTransform).offsetMax = new Vector2(-12, -44);
        infoPanel.SetActive(false);

        // Toast (top-center below title)
        toastGO = Panel(canvasGO.transform, new Vector2(0.5f, 1f), new Vector2(460, 44), new Vector2(0, -110));
        toastText = Label(toastGO.transform, "", 18, TextAnchor.MiddleCenter, Color.white);
        toastGO.SetActive(false);

        // Status footer (above button bar)
        var statusGO = new GameObject("Status");
        statusGO.transform.SetParent(canvasGO.transform, false);
        var srt = statusGO.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 0);
        srt.offsetMin = new Vector2(10, 88); srt.offsetMax = new Vector2(-10, 116);
        statusText = statusGO.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 16; statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(1f, 1f, 1f, 0.75f);
    }

    BoardController FindBoard() { return FindFirstObjectByType<ClassroomBuilder>()?.Board; }
    TutorController FindTutor() { return FindFirstObjectByType<ClassroomBuilder>()?.Tutor; }
    Transform FindRoot() { var c = FindFirstObjectByType<ClassroomBuilder>(); return c ? c.transform : null; }
    void ScaleRoot(float f)
    {
        var r = FindRoot(); if (!r) return;
        float s = Mathf.Clamp(r.localScale.x * f, 0.3f, 3f);
        r.localScale = Vector3.one * s;
    }

    GameObject Bar(Transform parent, Vector2 anchor, Vector2 pivot, float height, Color c)
    {
        var go = new GameObject("Bar");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchor.x, anchor.y); rt.anchorMax = new Vector2(anchor.x + 1, anchor.y);
        if (anchor.y > 0.5f) { rt.offsetMin = new Vector2(0, -height); rt.offsetMax = new Vector2(0, 0); }
        else { rt.offsetMin = new Vector2(0, 0); rt.offsetMax = new Vector2(0, height); }
        go.AddComponent<Image>().color = c;
        return go;
    }

    GameObject Panel(Transform parent, Vector2 anchor, Vector2 size, Vector2? offset = null)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset ?? (anchor.x > 0.9f ? new Vector2(-180, 0) : Vector2.zero);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.05f, 0.08f, 0.14f, 0.9f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.25f);
        return go;
    }

    Text Label(Transform parent, string text, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12, 6); rt.offsetMax = new Vector2(-12, -6);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text; t.fontSize = size; t.alignment = anchor; t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void Button(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.45f, 0.9f);
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120, 56);
        var tgo = new GameObject("Text");
        tgo.transform.SetParent(go.transform, false);
        var trt = tgo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = label; t.fontSize = 17; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
    }
}
