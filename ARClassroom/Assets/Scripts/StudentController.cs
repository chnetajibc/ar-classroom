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
    Vector3 headBasePos;
    float headBaseYaw;
    bool headPitched;

    public void Setup(string name, int colorIndex, bool empty, GameObject bodyPrefab, float bodyScale, float bodyYaw, bool bodyNeedsPitch)
    {
        studentName = name;
        isEmptySeat = empty;
        phase = Random.Range(0f, 10f);
        Build(colorIndex, bodyPrefab, bodyScale, bodyYaw, bodyNeedsPitch);
    }

    void Build(int colorIndex, GameObject bodyPrefab, float bodyScale, float bodyYaw, bool bodyNeedsPitch)
    {
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

        if (bodyPrefab == null)
        {
            Debug.LogError("[Student] Missing body FBX for " + studentName + " — check Assets/Resources/Models/Humans.");
            return;
        }

        // Realistic body: downloaded FBX, seated on the chair (root is the seat).
        // Z-up files tip upright with pitch -90 first, then the facing yaw.
        headGO = Instantiate(bodyPrefab, transform, false);
        headGO.name = "Body";
        headGO.transform.localPosition = Vector3.zero;
        if (bodyNeedsPitch)
            headGO.transform.localRotation = Quaternion.Euler(0, bodyYaw, 0) * Quaternion.Euler(-90f, 0, 0);
        else
            headGO.transform.localRotation = Quaternion.Euler(0, bodyYaw, 0);
        headGO.transform.localScale = Vector3.one * bodyScale;
        headBasePos = Vector3.zero;
        headBaseYaw = bodyYaw;
        headPitched = bodyNeedsPitch;

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
        // Typing bob (relative to the FBX root so it works for any model).
        // Whole body moves now, so the nod is gentler and keeps the base rotation.
        headGO.transform.localPosition = headBasePos + new Vector3(
            Mathf.Sin(time * 0.5f) * 0.01f, Mathf.Abs(Mathf.Sin(time)) * 0.01f, 0);
        Quaternion baseRot = headPitched
            ? Quaternion.Euler(0, headBaseYaw, 0) * Quaternion.Euler(-90f, 0, 0)
            : Quaternion.Euler(0, headBaseYaw, 0);
        headGO.transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(time * 0.8f) * 3f, 0, 0);
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
