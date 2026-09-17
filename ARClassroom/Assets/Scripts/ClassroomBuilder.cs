using UnityEngine;

/// <summary>
/// Builds the whole classroom procedurally: floor, board wall, desks grid,
/// students with laptops, empty seats, tutor, lights. No external assets.
/// Classroom footprint ~ 7m x 5m, designed as AR tabletop OR room-scale.
/// </summary>
public class ClassroomBuilder : MonoBehaviour
{
    [Header("Layout")]
    public int rows = 3;
    public int cols = 3;
    // 6 occupied, 3 empty (indices into flattened grid)
    public int[] emptySeats = new int[] { 2, 5, 7 };

    public BoardController Board { get; private set; }
    public TutorController Tutor { get; private set; }
    public StudentController[] Students { get; private set; }

    static readonly string[] Names = new string[]
        { "Aarav", "Mia", "Leo", "Zara", "Kabir", "Nina", "Arjun", "Sara", "Dev" };

    // Realistic characters (downloaded FBX in Assets/Resources/Models/Humans).
    // human1 has no mesh (animation file only): tutor uses human2 (Mei, 1.699m).
    // Students cycle human3 (Marina, 1.753m), human4 (man in black), human5 (boy, 2.647m).
    // Scales convert each file to a ~1.5m student. Yaws face each model at the
    // board; pitched=true tips Z-up files upright (pitch -90 about X first).
    // If a model faces backwards in Play, flip its yaw 180 <-> 0.
    GameObject[] studentBodyPrefabs;
    readonly float[] studentBodyScales = new float[] { 0.856f, 0.092f, 0.567f };
    readonly float[] studentBodyYaws = new float[] { 180f, 180f, 0f };
    readonly bool[] studentBodyPitched = new bool[] { true, true, false };
    readonly string[] studentBodyPaths = new string[] { "Models/Humans/human3", "Models/Humans/human4", "Models/Humans/human5" };

    // Realistic furniture (Kenney FBX in Assets/Resources/Models/Kenney)
    GameObject deskPrefab;
    GameObject chairPrefab;
    GameObject laptopPrefab;

    void Awake()
    {
        studentBodyPrefabs = new GameObject[studentBodyPaths.Length];
        for (int i = 0; i < studentBodyPaths.Length; i++)
            studentBodyPrefabs[i] = Resources.Load<GameObject>(studentBodyPaths[i]);
        deskPrefab = Resources.Load<GameObject>("Models/Kenney/desk");
        chairPrefab = Resources.Load<GameObject>("Models/Kenney/chair");
        laptopPrefab = Resources.Load<GameObject>("Models/Kenney/laptop");
        if (deskPrefab == null || chairPrefab == null || laptopPrefab == null)
            Debug.LogError("[Classroom] Missing Kenney FBX — check Assets/Resources/Models/Kenney.");
        // Regenerate from scratch: clear anything generated earlier (e.g. by the
        // Setup menu in edit mode and saved into the scene) so Play never stacks
        // duplicates on top of saved content.
        for (int i = transform.childCount - 1; i >= 0; --i)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        BuildRoom();
        BuildBoard();
        BuildDesksAndStudents();
        BuildTutor();
        BuildLights();
    }

    Material Mat(Color c, float metallic = 0f, float smooth = 0.5f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", smooth);
        return m;
    }

    void BuildRoom()
    {
        // Floor platform
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(transform, false);
        floor.transform.localPosition = new Vector3(0, -0.05f, 0.5f);
        floor.transform.localScale = new Vector3(7.5f, 0.1f, 6f);
        floor.GetComponent<Renderer>().material = Mat(new Color(0.55f, 0.42f, 0.3f));

        // Rug under desks
        var rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rug.name = "Rug";
        rug.transform.SetParent(transform, false);
        rug.transform.localPosition = new Vector3(0, 0.005f, 0.9f);
        rug.transform.localScale = new Vector3(5.6f, 0.02f, 3.6f);
        rug.GetComponent<Renderer>().material = Mat(new Color(0.16f, 0.25f, 0.45f));

        // Front wall (behind board)
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "FrontWall";
        wall.transform.SetParent(transform, false);
        wall.transform.localPosition = new Vector3(0, 1.5f, -2.5f);
        wall.transform.localScale = new Vector3(7.5f, 3f, 0.15f);
        wall.GetComponent<Renderer>().material = Mat(new Color(0.88f, 0.89f, 0.92f));

        // Boundary walls: back + left + right so the room feels enclosed
        var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "BackWall";
        backWall.transform.SetParent(transform, false);
        backWall.transform.localPosition = new Vector3(0, 1.5f, 3.5f);
        backWall.transform.localScale = new Vector3(7.5f, 3f, 0.15f);
        backWall.GetComponent<Renderer>().material = Mat(new Color(0.82f, 0.84f, 0.88f));

        var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "LeftWall";
        leftWall.transform.SetParent(transform, false);
        leftWall.transform.localPosition = new Vector3(-3.75f, 1.5f, 0.5f);
        leftWall.transform.localScale = new Vector3(0.15f, 3f, 6f);
        leftWall.GetComponent<Renderer>().material = Mat(new Color(0.85f, 0.86f, 0.9f));

        var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "RightWall";
        rightWall.transform.SetParent(transform, false);
        rightWall.transform.localPosition = new Vector3(3.75f, 1.5f, 0.5f);
        rightWall.transform.localScale = new Vector3(0.15f, 3f, 6f);
        rightWall.GetComponent<Renderer>().material = Mat(new Color(0.85f, 0.86f, 0.9f));

        // DSA posters on the side walls
        MakePoster(new Vector3(-3.65f, 1.7f, 0.8f), 90f,
            "BIG-O\nO(1) FAST\nO(n^2) SLOW", new Color(0.12f, 0.3f, 0.55f));
        MakePoster(new Vector3(3.65f, 1.7f, 0.8f), -90f,
            "ARRAYS O(1)\nLISTS O(n)\nMAPS O(1)", new Color(0.45f, 0.2f, 0.35f));

        // Motivational quotes: back wall + front wall beside the board
        MakePoster(new Vector3(-1.6f, 1.7f, 3.42f), 180f,
            "CODE\nDEBUG\nREPEAT", new Color(0.1f, 0.45f, 0.35f));
        MakePoster(new Vector3(1.6f, 1.7f, 3.42f), 180f,
            "STAY\nCURIOUS", new Color(0.6f, 0.3f, 0.1f));
        MakePoster(new Vector3(-3.0f, 1.7f, -2.42f), 0f,
            "LEARN\nSOMETHING\nDAILY", new Color(0.2f, 0.35f, 0.65f));
        MakePoster(new Vector3(3.0f, 1.7f, -2.42f), 0f,
            "O(log n)\nMINDSET", new Color(0.5f, 0.15f, 0.3f));

        // Side title banner
        var banner = new GameObject("Banner");
        banner.transform.SetParent(transform, false);
        banner.transform.localPosition = new Vector3(0, 2.85f, -2.4f);
        var tm = banner.AddComponent<TextMesh>();
        tm.text = "AR CLASSROOM  •  DATA STRUCTURES & ALGORITHMS";
        tm.fontSize = 52; tm.characterSize = 0.016f;
        tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(0.1f, 0.2f, 0.5f);
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void MakePoster(Vector3 pos, float yaw, string text, Color bg)
    {
        var poster = GameObject.CreatePrimitive(PrimitiveType.Quad);
        poster.name = "Poster";
        poster.transform.SetParent(transform, false);
        poster.transform.localPosition = pos;
        poster.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        poster.transform.localScale = new Vector3(2.4f, 1.5f, 1);
        poster.GetComponent<Renderer>().material = Mat(bg);

        var label = new GameObject("PosterText");
        label.transform.SetParent(poster.transform, false);
        label.transform.localPosition = new Vector3(0, 0, 0.01f);
        label.transform.localRotation = Quaternion.identity;
        // Counter-scale so text renders at true size regardless of poster scale
        label.transform.localScale = new Vector3(1f / 2.4f, 1f / 1.5f, 1);
        var tm = label.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 48;
        tm.characterSize = 0.008f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void BuildBoard()
    {
        var boardGO = new GameObject("Board");
        boardGO.transform.SetParent(transform, false);
        boardGO.transform.localPosition = new Vector3(0, 1.6f, -2.4f);

        // Frame
        var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "BoardFrame";
        frame.transform.SetParent(boardGO.transform, false);
        frame.transform.localPosition = Vector3.zero;
        frame.transform.localScale = new Vector3(4.8f, 2.7f, 0.08f);
        frame.GetComponent<Renderer>().material = Mat(new Color(0.35f, 0.25f, 0.15f));

        // Collider for tap detection (InteractionManager raycasts this)
        var col = boardGO.AddComponent<BoxCollider>();
        col.size = new Vector3(4.8f, 2.7f, 0.4f);
        col.isTrigger = false;

        Board = boardGO.AddComponent<BoardController>();

        // Chalk tray + chalk sticks + eraser (parented to the board)
        var tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tray.name = "ChalkTray";
        tray.transform.SetParent(boardGO.transform, false);
        tray.transform.localPosition = new Vector3(0, -1.45f, 0.12f);
        tray.transform.localScale = new Vector3(3f, 0.06f, 0.2f);
        tray.GetComponent<Renderer>().material = Mat(new Color(0.4f, 0.28f, 0.16f));
        for (int i = -1; i <= 1; i += 2)
        {
            var chalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chalk.name = "Chalk";
            chalk.transform.SetParent(boardGO.transform, false);
            chalk.transform.localPosition = new Vector3(0.5f * i, -1.39f, 0.12f);
            chalk.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            chalk.transform.localScale = new Vector3(0.035f, 0.22f, 0.035f);
            chalk.GetComponent<Renderer>().material = Mat(Color.white);
        }
        var eraser = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eraser.name = "Eraser";
        eraser.transform.SetParent(boardGO.transform, false);
        eraser.transform.localPosition = new Vector3(1.1f, -1.38f, 0.12f);
        eraser.transform.localScale = new Vector3(0.3f, 0.09f, 0.13f);
        eraser.GetComponent<Renderer>().material = Mat(new Color(0.15f, 0.15f, 0.18f));
    }

    void BuildDesksAndStudents()
    {
        Students = new StudentController[rows * cols];

        int idx = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // Real furniture is bigger than the old slabs: rows start at z=0
                // with 1.3m spacing so back-row chair backs stay inside the wall.
                float x = (c - (cols - 1) / 2f) * 1.7f;
                float z = r * 1.3f;
                bool empty = System.Array.IndexOf(emptySeats, idx) >= 0;

                // Desk — Kenney FBX, scale 0.2 → 1.45m wide, top at 0.77m.
                // (File origin at front-right-bottom corner, hence the offsets.)
                var desk = Instantiate(deskPrefab, transform, false);
                desk.name = $"Desk_{r}_{c}";
                desk.transform.localPosition = new Vector3(x + 0.715f, 0f, z - 0.37f);
                desk.transform.localRotation = Quaternion.identity;
                desk.transform.localScale = Vector3.one * 0.2f;

                // Chair — Kenney FBX, scale 0.2 → seat at ~0.44m, backrest faces +Z
                // (student side). Students face the board (-Z).
                var chair = Instantiate(chairPrefab, transform, false);
                chair.name = $"Chair_{r}_{c}";
                chair.transform.localPosition = new Vector3(x + 0.2f, 0f, z + 0.45f);
                chair.transform.localRotation = Quaternion.identity;
                chair.transform.localScale = Vector3.one * 0.2f;

                // Laptop on every desk — Kenney FBX, screen faces the student (+Z).
                var lap = Instantiate(laptopPrefab, transform, false);
                lap.name = $"Laptop_{r}_{c}";
                lap.transform.localPosition = new Vector3(x - 0.24f, 0.77f, z + 0.27f);
                lap.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                lap.transform.localScale = Vector3.one * 0.18f;

                // Student root (clickable), rotated to face the board (-Z).
                // Sitting: hips rest on the ~0.45m seat, so the body origin (feet)
                // sits 0.3m below the floor — legs stay hidden under the desk.
                var sgo = new GameObject($"Student_{Names[idx]}");
                sgo.transform.SetParent(transform, false);
                sgo.transform.localPosition = new Vector3(x, -0.3f, z + 0.55f);
                sgo.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                // Big invisible hitbox so tapping is easy (esp. on phones)
                var hit = sgo.AddComponent<BoxCollider>();
                hit.size = new Vector3(1.1f, 2f, 1.4f);
                hit.center = new Vector3(0, 1f, 0);
                var sc = sgo.AddComponent<StudentController>();
                GameObject body = null;
                float bodyScale = 1f;
                float bodyYaw = 180f;
                bool bodyNeedsPitch = true;
                if (!empty && studentBodyPrefabs != null && studentBodyPrefabs.Length > 0)
                {
                    int pick = idx % studentBodyPrefabs.Length;
                    body = studentBodyPrefabs[pick];
                    bodyScale = studentBodyScales[pick % studentBodyScales.Length];
                    bodyYaw = studentBodyYaws[pick % studentBodyYaws.Length];
                    bodyNeedsPitch = studentBodyPitched[pick % studentBodyPitched.Length];
                }
                sc.Setup(Names[idx], idx, empty, body, bodyScale, bodyYaw, bodyNeedsPitch);
                Students[idx] = sc;

                idx++;
            }
        }
    }

    void BuildTutor()
    {
        var tgo = new GameObject("Tutor");
        tgo.transform.SetParent(transform, false);
        tgo.transform.localPosition = new Vector3(2.9f, 0, -1.5f);
        tgo.transform.localRotation = Quaternion.Euler(0, -50f, 0);
        var hit = tgo.AddComponent<BoxCollider>();
        hit.size = new Vector3(1.2f, 2.4f, 1.2f);
        hit.center = new Vector3(0, 1.2f, 0);
        Tutor = tgo.AddComponent<TutorController>();
        Tutor.BindBoard(Board);

        // Tutor platform disc
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.transform.SetParent(transform, false);
        disc.transform.localPosition = new Vector3(2.9f, 0.02f, -1.5f);
        disc.transform.localScale = new Vector3(1f, 0.04f, 1f);
        disc.GetComponent<Renderer>().material = Mat(new Color(0.15f, 0.5f, 0.9f));
    }

    void BuildLights()
    {
        var sun = new GameObject("ClassroomSun");
        sun.transform.SetParent(transform, false);
        var l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;
        sun.transform.localRotation = Quaternion.Euler(50, -30, 0);
    }
}
