using UnityEngine;

/// <summary>
/// One student sitting at a desk with a laptop. Procedural idle + typing motion.
/// </summary>
public class StudentController : MonoBehaviour
{
    public string studentName = "Student";
    public bool isEmptySeat = false;
    public string bodyId = "?"; // prefab asset name / procedural / empty (diagnostics)

    GameObject headGO;
    GameObject laptopScreen;
    Material screenMat;
    float phase;
    float typingSpeed = 6f;
    Color baseScreenColor;
    bool highlighted;
    Quaternion bodyBaseRot;
    bool useProceduralBody;
    Vector3 procHeadBase;
    // Typing rig: hand + head bones discovered by name at build (the bodies
    // are Generic rigs, so HumanBodyBones/GetBoneTransform can't be used).
    // Static posed meshes have no bones -> gentle sway/nod of the whole body.
    Transform handL, handR, headBone;
    Quaternion handLBase = Quaternion.identity;
    Quaternion handRBase = Quaternion.identity;
    Quaternion headBoneBase = Quaternion.identity;
    bool hasHands;

    static readonly Color[] ShirtColors = new Color[]
    {
        new Color(0.95f, 0.35f, 0.35f), new Color(0.35f, 0.8f, 0.4f),
        new Color(0.4f, 0.6f, 1f), new Color(1f, 0.75f, 0.3f),
        new Color(0.75f, 0.5f, 1f), new Color(0.3f, 0.9f, 0.85f),
    };

    public void Setup(string name, int colorIndex, bool empty, GameObject bodyPrefab, float bodyScale, float bodyYaw, bool bodyNeedsPitch, float bodyPitchDeg, float seatTopY)
    {
        studentName = name;
        isEmptySeat = empty;
        bodyId = empty ? "empty" : (bodyPrefab != null ? bodyPrefab.name : "procedural");
        phase = Random.Range(0f, 10f);
        Build(colorIndex, bodyPrefab, bodyScale, bodyYaw, bodyNeedsPitch, bodyPitchDeg, seatTopY);
    }

    void Build(int colorIndex, GameObject bodyPrefab, float bodyScale, float bodyYaw, bool bodyNeedsPitch, float bodyPitchDeg, float seatTopY)
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

        if (bodyPrefab == null || bodyPrefab.GetComponentInChildren<Renderer>() == null)
        {
            // Missing/broken FBX (classic cause: Git LFS model never pulled, so
            // Resources.Load gave null or a mesh-less import). Fall back to the
            // procedural low-poly student so the seat is never empty.
            Debug.LogWarning("[Student] Body FBX missing for " + studentName + " — using procedural fallback.");
            BuildProcedural(colorIndex);
            return;
        }

        // Realistic body: downloaded FBX, SEATED on the chair. The body origin
        // is at the feet, so sink the root until the hips rest on the seat:
        // rootY = seatTop - 0.52 * measuredHeight. Dangling legs disappear
        // under the floor/desk while the torso stays at the desk typing.
        // Z-up files tip upright with pitch -90 first, then the facing yaw.
        // Instantiate is guarded: a corrupt prefab must not kill the whole row.
        try
        {
            headGO = Instantiate(bodyPrefab, transform, false);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Student] Instantiate failed for " + studentName + ": " + e.Message + " — using fallback.");
            BuildProcedural(colorIndex);
            return;
        }
        headGO.name = "Body";
        headGO.transform.localPosition = Vector3.zero;
        // Per-model pitch tips Z-up downloads upright (human4 needs -117, see
        // ClassroomBuilder), then yaw faces the board.
        bodyBaseRot = bodyNeedsPitch
            ? Quaternion.Euler(0, bodyYaw, 0) * Quaternion.Euler(bodyPitchDeg, 0, 0)
            : Quaternion.Euler(0, bodyYaw, 0);
        headGO.transform.localRotation = bodyBaseRot;
        headGO.transform.localScale = Vector3.one * bodyScale;
        useProceduralBody = false;

        // Sit: measure the posed body height, drop hips onto the seat.
        float bodyH = MeasureBodyHeight(headGO);
        if (bodyH > 0.2f)
        {
            float rootY = seatTopY - bodyH * 0.52f;
            transform.localPosition = new Vector3(transform.localPosition.x, rootY, transform.localPosition.z);
        }
        DiscoverTypingRig(headGO);

        // Name tag floats just above the seated head.
        var tag2 = new GameObject("NameTag");
        tag2.transform.SetParent(transform, false);
        tag2.transform.localPosition = new Vector3(0, 1.55f, -0.1f);
        tag2.transform.localRotation = Quaternion.Euler(0, 180f, 0); // counter parent Y-180
        var tm2 = tag2.AddComponent<TextMesh>();
        tm2.text = studentName;
        tm2.fontSize = 40; tm2.characterSize = 0.011f;
        tm2.anchor = TextAnchor.MiddleCenter;
        tm2.color = new Color(1f, 1f, 1f, 0.85f);
        tm2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // Height of the posed body (translation-invariant), used to sink the hips
    // exactly onto the seat regardless of which model was randomly picked.
    static float MeasureBodyHeight(GameObject body)
    {
        Bounds b = new Bounds(body.transform.position, Vector3.zero);
        bool init = false;
        foreach (var r in body.GetComponentsInChildren<Renderer>())
        {
            if (!init) { b = r.bounds; init = true; }
            else b.Encapsulate(r.bounds);
        }
        return init ? b.size.y : 0f;
    }

    // Find hand + head bones by name for the typing animation. Works on any
    // Generic rig (e.g. Rigify's c_hand_ik.l/r). Static posed meshes have no
    // bones -> hasHands stays false and Update sways the whole body instead.
    // Only small relative rotations are ever applied, so a wrong-axis guess
    // still reads as fidgeting rather than a broken limb.
    void DiscoverTypingRig(GameObject body)
    {
        handL = handR = headBone = null;
        hasHands = false;
        try
        {
            Transform[] all = body.GetComponentsInChildren<Transform>(true);
            Transform hL = null, hR = null, hd = null;
            int hLScore = -1, hRScore = -1;
            foreach (var t in all)
            {
                string n = t.name.ToLower();
                if (n.Contains("handle")) continue;
                if (n.Contains("hand") || n.Contains("palm") || n.Contains("wrist") || n.Contains("fist"))
                {
                    bool left = n.Contains(".l") || n.Contains("_l") || n.Contains("-l") || n.Contains("left") || n.EndsWith("l");
                    bool right = n.Contains(".r") || n.Contains("_r") || n.Contains("-r") || n.Contains("right") || n.EndsWith("r");
                    int score = (n.Contains("ik") ? 2 : 0) + (n.Contains("palm") ? 1 : 0);
                    if (left && score >= hLScore) { hL = t; hLScore = score; }
                    else if (right && score >= hRScore) { hR = t; hRScore = score; }
                    else if (!left && !right)
                    {
                        if (hL == null) { hL = t; hLScore = score; }
                        else if (hR == null) { hR = t; hRScore = score; }
                    }
                }
                else if (n == "head" || n.EndsWith(".head") || n.EndsWith("_head"))
                {
                    if (hd == null) hd = t;
                }
            }
            if (hd == null)
            {
                foreach (var t in all)
                {
                    string n = t.name.ToLower();
                    if (n.Contains("head") && !n.Contains("forehead")) { hd = t; break; }
                }
            }
            handL = hL; handR = (hR != hL) ? hR : null; headBone = hd;
            if (handL != null) handLBase = handL.localRotation;
            if (handR != null) handRBase = handR.localRotation;
            if (headBone != null) headBoneBase = headBone.localRotation;
            hasHands = handL != null || handR != null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Student] Rig discovery failed for " + studentName + ": " + e.Message);
        }
    }
    // Procedural low-poly fallback: capsule torso + sphere head + laptop with
    // glowing code screen. Needs no external assets. Y values are the classic
    // floor-root heights shifted +0.3 because this root sits sunk (y=-0.3).
    void BuildProcedural(int colorIndex)
    {
        useProceduralBody = true;
        Material shirt = new Material(Shader.Find("Standard"))
            { color = ShirtColors[Mathf.Abs(colorIndex) % ShirtColors.Length] };
        Material skin = new Material(Shader.Find("Standard")) { color = new Color(0.96f, 0.78f, 0.62f) };
        if (Random.value > 0.5f) skin.color = new Color(0.45f, 0.3f, 0.22f);

        var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(transform, false);
        torso.transform.localPosition = new Vector3(0, 1.05f, -0.15f);
        torso.transform.localScale = new Vector3(0.42f, 0.6f, 0.42f);
        torso.GetComponent<Renderer>().material = shirt;

        headGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headGO.name = "Head";
        headGO.transform.SetParent(transform, false);
        procHeadBase = new Vector3(0, 1.65f, -0.12f);
        headGO.transform.localPosition = procHeadBase;
        headGO.transform.localScale = Vector3.one * 0.32f;
        headGO.GetComponent<Renderer>().material = skin;

        var baseGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseGO.name = "LaptopBase";
        baseGO.transform.SetParent(transform, false);
        baseGO.transform.localPosition = new Vector3(0, 1.08f, 0.35f);
        baseGO.transform.localScale = new Vector3(0.5f, 0.04f, 0.34f);
        baseGO.GetComponent<Renderer>().material = new Material(Shader.Find("Standard")) { color = new Color(0.15f, 0.16f, 0.2f) };

        laptopScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        laptopScreen.name = "LaptopScreen";
        laptopScreen.transform.SetParent(transform, false);
        laptopScreen.transform.localPosition = new Vector3(0, 1.28f, 0.5f);
        laptopScreen.transform.localRotation = Quaternion.Euler(12, 0, 0);
        laptopScreen.transform.localScale = new Vector3(0.5f, 0.34f, 0.03f);
        screenMat = new Material(Shader.Find("Standard"));
        baseScreenColor = new Color(0.1f, 0.25f, 0.5f) * (0.8f + Random.value * 0.5f);
        screenMat.color = baseScreenColor;
        screenMat.EnableKeyword("_EMISSION");
        screenMat.SetColor("_EmissionColor", baseScreenColor * 1.6f);
        laptopScreen.GetComponent<Renderer>().material = screenMat;

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

        var tag2 = new GameObject("NameTag");
        tag2.transform.SetParent(transform, false);
        tag2.transform.localPosition = new Vector3(0, 2.05f, -0.1f);
        tag2.transform.localRotation = Quaternion.Euler(0, 180f, 0);
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
        if (useProceduralBody)
        {
            // Typing bob for the procedural head (base pose stored at build).
            headGO.transform.localPosition = procHeadBase + new Vector3(
                Mathf.Sin(time * 0.5f) * 0.02f, Mathf.Abs(Mathf.Sin(time)) * 0.02f, 0);
            headGO.transform.localRotation = Quaternion.Euler(Mathf.Sin(time * 0.8f) * 6f, 0, 0);
        }
        else if (hasHands)
        {
            // Seated typing: hands tap at the keyboard, head nods at the
            // screen. ROTATION ONLY — the body never translates, so students
            // stay seated (no sit/stand bobbing).
            float tap = Mathf.Sin(time * 2.2f);
            float tap2 = Mathf.Sin(time * 2.2f + 1.3f);
            if (handL != null)
                handL.localRotation = handLBase * Quaternion.Euler(tap * 7f, 0, tap2 * 5f);
            if (handR != null)
                handR.localRotation = handRBase * Quaternion.Euler(tap2 * 7f, 0, tap * 5f);
            if (headBone != null)
                headBone.localRotation = headBoneBase * Quaternion.Euler(
                    Mathf.Sin(time * 0.35f) * 4f, Mathf.Sin(time * 0.22f) * 6f, 0);
        }
        else
        {
            // Static posed mesh (no bones): a barely-there rotational sway and
            // nod keeps the student alive without ever leaving the seat.
            headGO.transform.localRotation = bodyBaseRot * Quaternion.Euler(
                Mathf.Sin(time * 0.35f) * 2f, Mathf.Sin(time * 0.22f) * 2.5f, Mathf.Sin(time * 0.5f) * 1f);
        }
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
