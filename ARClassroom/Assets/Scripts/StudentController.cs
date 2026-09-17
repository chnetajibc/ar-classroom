using UnityEngine;

/// <summary>
/// One student sitting at a desk with a laptop. Procedural idle + typing motion.
/// </summary>
public class StudentController : MonoBehaviour
{
    public string studentName = "Student";
    public bool isEmptySeat = false;

    GameObject headGO;
    GameObject laptopScreen;
    Material screenMat;
    float phase;
    float typingSpeed = 6f;
    Color baseScreenColor;
    bool highlighted;

    static readonly Color[] ShirtColors = new Color[]
    {
        new Color(0.95f, 0.35f, 0.35f), new Color(0.35f, 0.8f, 0.4f),
        new Color(0.4f, 0.6f, 1f), new Color(1f, 0.75f, 0.3f),
        new Color(0.75f, 0.5f, 1f), new Color(0.3f, 0.9f, 0.85f),
    };

    public void Setup(string name, int colorIndex, bool empty)
    {
        studentName = name;
        isEmptySeat = empty;
        phase = Random.Range(0f, 10f);
        Build(colorIndex);
    }

    void Build(int colorIndex)
    {
        Material shirt = new Material(Shader.Find("Standard"))
            { color = ShirtColors[Mathf.Abs(colorIndex) % ShirtColors.Length] };
        Material skin = new Material(Shader.Find("Standard")) { color = new Color(0.96f, 0.78f, 0.62f) };
        if (Random.value > 0.5f) skin.color = new Color(0.45f, 0.3f, 0.22f);

        if (isEmptySeat)
        {
            // Empty chair marker: translucent chair + "EMPTY" tag
            var tag = new GameObject("EmptyTag");
            tag.transform.SetParent(transform, false);
            tag.transform.localPosition = new Vector3(0, 1.1f, 0);
            tag.transform.localRotation = Quaternion.Euler(0, 180f, 0); // counter parent Y-180
            var tm = tag.AddComponent<TextMesh>();
            tm.text = "— empty —";
            tm.fontSize = 42; tm.characterSize = 0.014f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 1f, 1f, 0.6f);
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return;
        }

        // Torso
        var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.transform.SetParent(transform, false);
        torso.transform.localPosition = new Vector3(0, 0.75f, -0.15f);
        torso.transform.localScale = new Vector3(0.42f, 0.6f, 0.42f);
        torso.GetComponent<Renderer>().material = shirt;

        // Head
        headGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headGO.transform.SetParent(transform, false);
        headGO.transform.localPosition = new Vector3(0, 1.35f, -0.12f);
        headGO.transform.localScale = Vector3.one * 0.32f;
        headGO.GetComponent<Renderer>().material = skin;

        // Laptop base + screen (built relative to student root; desk top ~0.75m)
        var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseGO.name = "LaptopBase";
        baseGO.transform.SetParent(transform, false);
        baseGO.transform.localPosition = new Vector3(0, 0.78f, 0.35f);
        baseGO.transform.localScale = new Vector3(0.5f, 0.04f, 0.34f);
        baseGO.GetComponent<Renderer>().material = new Material(Shader.Find("Standard")) { color = new Color(0.15f, 0.16f, 0.2f) };

        laptopScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        laptopScreen.name = "LaptopScreen";
        laptopScreen.transform.SetParent(transform, false);
        laptopScreen.transform.localPosition = new Vector3(0, 0.98f, 0.5f);
        laptopScreen.transform.localRotation = Quaternion.Euler(12, 0, 0); // lean back, away from student
        laptopScreen.transform.localScale = new Vector3(0.5f, 0.34f, 0.03f);
        screenMat = new Material(Shader.Find("Standard"));
        baseScreenColor = new Color(0.1f, 0.25f, 0.5f) * (0.8f + Random.value * 0.5f);
        screenMat.color = baseScreenColor;
        screenMat.EnableKeyword("_EMISSION");
        screenMat.SetColor("_EmissionColor", baseScreenColor * 1.6f);
        laptopScreen.GetComponent<Renderer>().material = screenMat;

        // Code lines on screen: 3 thin white strips
        for (int i = 0; i < 3; i++)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Quad);
            line.transform.SetParent(laptopScreen.transform, false);
            line.transform.localPosition = new Vector3(-0.15f + (i % 2) * 0.1f, 0.2f - i * 0.28f, -0.55f);
            line.transform.localRotation = Quaternion.Euler(0, 180, 0);
            line.transform.localScale = new Vector3(0.55f - i * 0.12f, 0.07f, 1);
            var lm = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.5f, 1f, 0.65f, 1f) };
            line.GetComponent<Renderer>().material = lm;
        }

        // Name tag
        var tag2 = new GameObject("NameTag");
        tag2.transform.SetParent(transform, false);
        tag2.transform.localPosition = new Vector3(0, 1.75f, -0.1f);
        tag2.transform.localRotation = Quaternion.Euler(0, 180f, 0); // counter parent Y-180
        var tm2 = tag2.AddComponent<TextMesh>();
        tm2.text = studentName;
        tm2.fontSize = 40; tm2.characterSize = 0.011f;
        tm2.anchor = TextAnchor.MiddleCenter;
        tm2.color = new Color(1f, 1f, 1f, 0.85f);
        tm2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void Update()
    {
        if (isEmptySeat || headGO == null) return;
        float time = Time.time * typingSpeed + phase;
        // Typing bob
        headGO.transform.localPosition = new Vector3(
            Mathf.Sin(time * 0.5f) * 0.02f, 1.35f + Mathf.Abs(Mathf.Sin(time)) * 0.02f, -0.12f);
        headGO.transform.localRotation = Quaternion.Euler(Mathf.Sin(time * 0.8f) * 6f, 0, 0);
        // Screen subtle flicker
        if (screenMat)
            screenMat.SetColor("_EmissionColor", baseScreenColor * (1.4f + Mathf.Sin(time * 1.7f) * 0.35f));
        // Highlight pulse
        if (highlighted && laptopScreen)
            laptopScreen.transform.localScale = new Vector3(0.5f, 0.34f + Mathf.Sin(Time.time * 8f) * 0.01f, 0.03f);
    }

    public void SetHighlight(bool on)
    {
        highlighted = on;
        if (screenMat && !isEmptySeat)
            screenMat.SetColor("_EmissionColor", on ? Color.yellow * 2f : baseScreenColor * 1.6f);
    }

    public string GetInfo()
    {
        if (isEmptySeat) return "Empty seat — free to join!";
        return $"{studentName}\nStatus: following along 💻\nTap tutor to ask a question!";
    }
}
