using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ARClassroom
{
    /// <summary>Screen overlay: lesson title, live captions of what is being said, view and lesson buttons.</summary>
    public class ClassroomUI : MonoBehaviour
    {
        public ClassroomCameraRig cameraRig;
        public LessonDirector director;

        Font font;
        Text lessonText, captionText, hintText, viewLabel;
        GameObject captionPanel;
        float captionUntil;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        void Update()
        {
            if (captionPanel != null && captionPanel.activeSelf && Time.time > captionUntil) captionPanel.SetActive(false);
            if (viewLabel != null && cameraRig != null) viewLabel.text = "View: " + cameraRig.CurrentName;
        }

        public void SetLesson(string title) { if (lessonText != null) lessonText.text = title; }

        public void Say(string speaker, string line, float seconds)
        {
            if (captionText == null) return;
            captionText.text = "<b>" + speaker + "</b>   " + line;
            captionPanel.SetActive(true);
            captionUntil = Time.time + seconds + 1.5f;
        }

        void Build()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            // lesson title (top-left)
            var titlePanel = Panel(canvasGo.transform, "TitlePanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(430f, 86f), new Color(0.06f, 0.08f, 0.1f, 0.62f));
            Label(titlePanel.transform, "Classroom", 20, FontStyle.Normal, new Color(0.7f, 0.85f, 1f), new Vector2(18f, -8f), new Vector2(390f, 30f));
            lessonText = Label(titlePanel.transform, "", 34, FontStyle.Bold, Color.white, new Vector2(18f, -38f), new Vector2(390f, 44f));

            // captions (bottom-center)
            captionPanel = Panel(canvasGo.transform, "Caption", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1100f, 96f), new Color(0.04f, 0.05f, 0.07f, 0.72f));
            captionText = Label(captionPanel.transform, "", 30, FontStyle.Normal, Color.white, new Vector2(24f, -14f), new Vector2(1052f, 70f));
            captionText.supportRichText = true;
            captionText.alignment = TextAnchor.MiddleLeft;
            captionPanel.SetActive(false);

            // buttons (top-right)
            var viewBtn = Button(canvasGo.transform, "View: -", new Vector2(-24f, -24f), () => cameraRig?.Next());
            viewLabel = viewBtn.GetComponentInChildren<Text>();
            Button(canvasGo.transform, "Next lesson", new Vector2(-24f, -84f), () => director?.NextLesson());

            hintText = Label(canvasGo.transform, "V / 1-6: views    drag: orbit    wheel: zoom    click a student or the teacher", 20, FontStyle.Normal, new Color(1f, 1f, 1f, 0.72f), new Vector2(0f, 0f), new Vector2(900f, 30f));
            var hrt = (RectTransform)hintText.transform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0f, 0f);
            hrt.pivot = new Vector2(0f, 0f);
            hrt.anchoredPosition = new Vector2(24f, 12f);
        }

        GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            return go;
        }

        Text Label(Transform parent, string text, int size, FontStyle style, Color color, Vector2 pos, Vector2 boxSize)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = boxSize;
            var t = go.GetComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.raycastTarget = false;
            return t;
        }

        GameObject Button(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(250f, 48f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.22f, 0.82f);
            var b = go.GetComponent<UnityEngine.UI.Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            colors.pressedColor = new Color(0.2f, 0.35f, 0.6f, 1f);
            b.colors = colors;
            b.targetGraphic = img;
            b.onClick.AddListener(onClick);
            var text = Label(go.transform, label, 24, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero);
            var trt = (RectTransform)text.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            trt.pivot = new Vector2(0.5f, 0.5f);
            text.alignment = TextAnchor.MiddleCenter;
            return go;
        }
    }
}
