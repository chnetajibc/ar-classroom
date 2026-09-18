using System.Collections;
using UnityEngine;

/// <summary>
/// Procedural low-poly tutor standing by the board.
/// Idle bob, wave on click, point-at-board while explaining.
/// Speech bubble = world-space canvas with UI.Text (no TMP/font asset needed).
/// </summary>
public class TutorController : MonoBehaviour
{
    public string tutorName = "Prof. Ada — DSA Tutor";

    [Header("Patrol — walks the front lane, pauses at each spot to teach")]
    public Vector3[] patrolPoints = new Vector3[]
    {
        new Vector3(2.9f, 0, -1.5f),
        new Vector3(0.2f, 0, -1.0f),
        new Vector3(-2.4f, 0, -1.5f),
        new Vector3(0.2f, 0, -1.0f),
    };
    public float walkSpeed = 0.7f;
    public float pauseSeconds = 3f;
    int wpIndex = 1;
    float pauseUntil = 0f;
    bool walking = true;
    int lineIndex = 0;
    readonly string[] patrolLines = new string[]
    {
        "Arrays: O(1) lookup!",
        "Follow the pointer!",
        "Questions? Tap me!",
        "Big-O first, code later!",
    };

    GameObject bodyGO;
    GameObject headGO;
    GameObject armL, armR;
    // Base pose stored at build so Update/CoWave work for both the FBX body
    // (root on the floor) and the procedural fallback (body at hip height).
    Vector3 tutorBodyBasePos = Vector3.zero;
    Quaternion tutorHeadBaseRot = Quaternion.identity;
    Vector3 tutorHeadBaseScale = Vector3.one;
    bool tutorProcedural;
    GameObject bubbleGO;
    UnityEngine.UI.Text bubbleText;
    BoardController board;
    float t;
    bool explaining;
    Vector3 armRBaseRot = new Vector3(0, 0, 160);

    void Awake()
    {
        BuildBody();
        BuildBubble();
    }

    public void BindBoard(BoardController b) { board = b; }

    void BuildBody()
    {
        // Realistic tutor: downloaded FBX standing on the floor at the tutor root.
        // (human1 file has no mesh — animation data only — so tutor uses human2,
        // Mei at 1.699m ≈ 1.7m, scale 1.) Mei's file is Z-up: tip upright
        // (pitch -90 first). Yaw 90 faces her toward the class from the
        // tutor's front-right patrol spot — verified with headless renders
        // (yaw 0 faced the east wall, 180/270 faced away).
        // Missing/broken FBX (e.g. Git LFS model never pulled → Resources.Load
        // returns null, and Instantiate(null) throws ArgumentException) falls
        // back to the procedural tutor so the classroom is never without teacher.
        GameObject prefab = null;
        try { prefab = Resources.Load<GameObject>("Models/Humans/human2"); }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Tutor] Load failed for human2: " + e.Message);
        }
        bool prefabUsable = prefab != null && prefab.GetComponentInChildren<Renderer>() != null;
        if (prefabUsable)
        {
            try
            {
                bodyGO = Instantiate(prefab, transform, false);
                bodyGO.name = "TutorBody";
                bodyGO.transform.localPosition = Vector3.zero;
                tutorHeadBaseRot = Quaternion.Euler(0, 90f, 0) * Quaternion.Euler(-90f, 0, 0);
                bodyGO.transform.localRotation = tutorHeadBaseRot;
                bodyGO.transform.localScale = Vector3.one;
                tutorBodyBasePos = Vector3.zero;
                tutorHeadBaseScale = Vector3.one;
                tutorProcedural = false;
                headGO = bodyGO; // whole-body nod preserves the old animation code
                armL = null; armR = null; // FBX brings its own arms
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Tutor] Instantiate failed for human2: " + e.Message + " — using fallback.");
                if (bodyGO != null) Destroy(bodyGO);
                bodyGO = null;
                prefabUsable = false;
            }
        }
        if (!prefabUsable)
        {
            Debug.LogWarning("[Tutor] human2 FBX missing — run `git lfs pull`; procedural fallback will be used.");
            BuildProceduralBody();
        }

        // Name tag floating above head
        var tag = new GameObject("NameTag");
        tag.transform.SetParent(transform, false);
        tag.transform.localPosition = new Vector3(0, tutorProcedural ? 2.35f : 2.05f, 0);
        var tm = tag.AddComponent<TextMesh>();
        tm.text = tutorName;
        tm.fontSize = 48;
        tm.characterSize = 0.012f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // Procedural low-poly fallback: capsule body + legs + head + glasses + arms
    // + pointer stick. Needs no external assets.
    void BuildProceduralBody()
    {
        tutorProcedural = true;
        Material skin = new Material(Shader.Find("Standard")) { color = new Color(0.96f, 0.78f, 0.62f) };
        Material shirt = new Material(Shader.Find("Standard")) { color = new Color(0.15f, 0.45f, 0.95f) };
        Material pants = new Material(Shader.Find("Standard")) { color = new Color(0.12f, 0.14f, 0.2f) };

        bodyGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bodyGO.name = "TutorBody";
        bodyGO.transform.SetParent(transform, false);
        tutorBodyBasePos = new Vector3(0, 0.95f, 0);
        bodyGO.transform.localPosition = tutorBodyBasePos;
        bodyGO.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
        bodyGO.GetComponent<Renderer>().material = shirt;

        GameObject legs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        legs.name = "TutorLegs";
        legs.transform.SetParent(transform, false);
        legs.transform.localPosition = new Vector3(0, 0.3f, 0);
        legs.transform.localScale = new Vector3(0.32f, 0.6f, 0.32f);
        legs.GetComponent<Renderer>().material = pants;

        headGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headGO.name = "TutorHead";
        headGO.transform.SetParent(transform, false);
        headGO.transform.localPosition = new Vector3(0, 1.85f, 0);
        tutorHeadBaseScale = Vector3.one * 0.42f;
        headGO.transform.localScale = tutorHeadBaseScale;
        headGO.GetComponent<Renderer>().material = skin;
        tutorHeadBaseRot = Quaternion.identity;

        for (int i = -1; i <= 1; i += 2)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.transform.SetParent(headGO.transform, false);
            g.transform.localPosition = new Vector3(0.28f * i, 0.08f, 0.85f);
            g.transform.localScale = new Vector3(0.32f, 0.2f, 0.1f);
            g.GetComponent<Renderer>().material = new Material(Shader.Find("Standard")) { color = Color.black };
        }

        armL = MakeArm(new Vector3(-0.42f, 1.25f, 0), shirt, true);
        armR = MakeArm(new Vector3(0.42f, 1.25f, 0), shirt, false);

        var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "Pointer";
        stick.transform.SetParent(armR.transform, false);
        stick.transform.localPosition = new Vector3(0, -0.55f, 0.15f);
        stick.transform.localRotation = Quaternion.Euler(70, 0, 0);
        stick.transform.localScale = new Vector3(0.06f, 0.7f, 0.06f);
        stick.GetComponent<Renderer>().material = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.85f, 0.3f) };
    }

    GameObject MakeArm(Vector3 pos, Material m, bool left)
    {
        var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        arm.name = left ? "ArmL" : "ArmR";
        arm.transform.SetParent(transform, false);
        arm.transform.localPosition = pos;
        arm.transform.localScale = new Vector3(0.18f, 0.55f, 0.18f);
        arm.transform.localRotation = Quaternion.Euler(0, 0, left ? -160 : 160);
        arm.GetComponent<Renderer>().material = m;
        return arm;
    }

    void BuildBubble()
    {
        bubbleGO = new GameObject("SpeechBubble");
        bubbleGO.transform.SetParent(transform, false);
        bubbleGO.transform.localPosition = new Vector3(0, 2.4f, 0);
        var canvas = bubbleGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460, 150);

        var bg = new GameObject("BubbleBG");
        bg.transform.SetParent(bubbleGO.transform, false);
        var img = bg.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(1f, 1f, 1f, 0.95f);
        var brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

        var tgo = new GameObject("BubbleText");
        tgo.transform.SetParent(bubbleGO.transform, false);
        bubbleText = tgo.AddComponent<UnityEngine.UI.Text>();
        bubbleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bubbleText.fontSize = 26;
        bubbleText.color = Color.black;
        bubbleText.alignment = TextAnchor.MiddleCenter;
        bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
        var trt = tgo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14, 10); trt.offsetMax = new Vector2(-14, -10);

        bubbleGO.transform.localScale = Vector3.one * 0.004f;
        bubbleGO.SetActive(false);
    }

    void Update()
    {
        t += Time.deltaTime;
        // Idle / walk bob (bigger bounce + sway while walking). Base pose comes
        // from the build step so both FBX (floor root) and fallback animate right.
        float bobAmp = walking ? 0.035f : 0.02f;
        float bobFreq = walking ? 7f : 2f;
        if (bodyGO) bodyGO.transform.localPosition = tutorBodyBasePos + new Vector3(
            walking ? Mathf.Sin(t * bobFreq * 0.5f) * 0.025f : 0f,
            Mathf.Sin(t * bobFreq) * bobAmp, 0);
        if (headGO) headGO.transform.localRotation = tutorHeadBaseRot
            * Quaternion.Euler(0, Mathf.Sin(t * 0.7f) * 12f, 0);
        // Right arm points at board while explaining
        if (armR)
        {
            float target = explaining ? 55f : 160f + Mathf.Sin(t * 1.5f) * 8f;
            var e = armR.transform.localRotation.eulerAngles;
            e.z = Mathf.LerpAngle(e.z, target, Time.deltaTime * 4f);
            armR.transform.localRotation = Quaternion.Euler(e);
        }
        // Face the camera-ish: keep upright, slowly face board->class
        PatrolUpdate();
    }

    void PatrolUpdate()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        Vector3 target = patrolPoints[wpIndex];
        Vector3 to = target - transform.localPosition;
        to.y = 0f;
        if (to.magnitude < 0.15f)
        {
            // Arrived: pause here and teach a line (once per stop)
            if (walking)
            {
                walking = false;
                pauseUntil = Time.time + pauseSeconds;
                Speak(patrolLines[lineIndex % patrolLines.Length], pauseSeconds);
                lineIndex++;
            }
            FaceYaw(0f); // face the class while teaching
            if (Time.time >= pauseUntil)
            {
                walking = true;
                wpIndex = (wpIndex + 1) % patrolPoints.Length;
            }
        }
        else
        {
            walking = true;
            Vector3 step = to.normalized * walkSpeed * Time.deltaTime;
            if (step.magnitude > to.magnitude) step = to;
            transform.localPosition += step;
            FaceYaw(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
        }
    }

    void FaceYaw(float yaw)
    {
        var e = transform.localRotation.eulerAngles;
        e.x = 0f; e.z = 0f;
        e.y = Mathf.LerpAngle(e.y, yaw, Time.deltaTime * 5f);
        transform.localRotation = Quaternion.Euler(e);
    }

    // Called by tap / UI button
    public void OnTapped()
    {
        StopAllCoroutines();
        StartCoroutine(CoWave());
        ExplainCurrentTopic();
    }

    System.Collections.IEnumerator CoWave()
    {
        float end = Time.time + 1.2f;
        while (Time.time < end)
        {
            if (armL) armL.transform.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 18f) * 35f, 0, -160);
            // little hop
            transform.localPosition += Vector3.zero; // keep anchored
            if (headGO) headGO.transform.localScale = tutorHeadBaseScale * (1f + Mathf.Sin(Time.time * 18f) * 0.02f);
            yield return null;
        }
        if (armL) armL.transform.localRotation = Quaternion.Euler(0, 0, -160);
        if (headGO) headGO.transform.localScale = tutorHeadBaseScale;
    }

    public void ExplainCurrentTopic()
    {
        string msg = "Welcome to DSA class!";
        if (board != null && board.CurrentTopic() != null)
        {
            var topic = board.CurrentTopic().title;
            if (topic.Contains("ARRAY")) msg = "Arrays give O(1) lookup by index!";
            else if (topic.Contains("LINKED")) msg = "Linked lists: O(1) insert at head!";
            else if (topic.Contains("STACK")) msg = "Stack = LIFO, Queue = FIFO!";
            else if (topic.Contains("BIG")) msg = "Always think Big-O before coding!";
            else if (topic.Contains("BINARY")) msg = "BST: left < root < right!";
            else if (topic.Contains("QUICK")) msg = "Quick ref: O(1) fast, O(n^2) slow!";
            else if (topic.Contains("MOTTO") || topic.Contains("CLASSROOM")) msg = "Code, debug, repeat — stay curious!";
        }
        Speak(msg, 3.5f);
        StopCoroutine("CoExplain");
        StartCoroutine("CoExplain");
    }

    IEnumerator CoExplain()
    {
        explaining = true;
        yield return new WaitForSeconds(3.5f);
        explaining = false;
    }

    public void Speak(string msg, float seconds = 3f)
    {
        StopCoroutine("CoBubble");
        StartCoroutine(CoBubble(msg, seconds));
    }

    IEnumerator CoBubble(string msg, float seconds)
    {
        bubbleGO.SetActive(true);
        bubbleText.text = msg;
        // Face bubble toward main camera
        yield return new WaitForSeconds(seconds);
        bubbleGO.SetActive(false);
    }

    void LateUpdate()
    {
        if (bubbleGO && bubbleGO.activeSelf && Camera.main)
        {
            // Billboard the bubble toward the camera. Per Unity docs,
            // LookRotation logs/ asserts on a zero forward vector, so guard it
            // (camera exactly at the bubble position) before calling.
            Vector3 toCam = Camera.main.transform.position - bubbleGO.transform.position;
            if (toCam.sqrMagnitude > 1e-6f)
            {
                // +Z (readable face) toward the camera
                bubbleGO.transform.rotation = Quaternion.LookRotation(toCam);
            }
        }
    }
}
