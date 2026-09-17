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

    void Awake()
    {
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
        Material deskMat = Mat(new Color(0.72f, 0.55f, 0.36f));
        Material chairMat = Mat(new Color(0.2f, 0.22f, 0.3f));

        int idx = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = (c - (cols - 1) / 2f) * 1.7f;
                float z = 0.2f + r * 1.35f;
                bool empty = System.Array.IndexOf(emptySeats, idx) >= 0;

                // Desk
                var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                desk.name = $"Desk_{r}_{c}";
                desk.transform.SetParent(transform, false);
                desk.transform.localPosition = new Vector3(x, 0.72f, z);
                desk.transform.localScale = new Vector3(1.1f, 0.08f, 0.7f);
                desk.GetComponent<Renderer>().material = deskMat;

                // Desk legs
                for (int lx = -1; lx <= 1; lx += 2)
                    for (int lz = -1; lz <= 1; lz += 2)
                    {
                        var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        leg.transform.SetParent(transform, false);
                        leg.transform.localPosition = new Vector3(x + lx * 0.48f, 0.36f, z + lz * 0.28f);
                        leg.transform.localScale = new Vector3(0.06f, 0.72f, 0.06f);
                        leg.GetComponent<Renderer>().material = chairMat;
                    }

                // Chair (far side of desk from the board — students face the board)
                var chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chair.transform.SetParent(transform, false);
                chair.transform.localPosition = new Vector3(x, 0.45f, z + 0.65f);
                chair.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
                chair.GetComponent<Renderer>().material = empty
                    ? Mat(new Color(0.5f, 0.5f, 0.55f, 0.5f))
                    : chairMat;
                var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
                back.transform.SetParent(transform, false);
                back.transform.localPosition = new Vector3(x, 0.85f, z + 0.88f);
                back.transform.localScale = new Vector3(0.5f, 0.6f, 0.07f);
                back.GetComponent<Renderer>().material = chair.transform.GetComponent<Renderer>().material;

                // Student root (clickable), rotated to face the board (-Z)
                var sgo = new GameObject($"Student_{Names[idx]}");
                sgo.transform.SetParent(transform, false);
                sgo.transform.localPosition = new Vector3(x, 0, z + 0.35f);
                sgo.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                // Big invisible hitbox so tapping is easy (esp. on phones)
                var hit = sgo.AddComponent<BoxCollider>();
                hit.size = new Vector3(1.1f, 2f, 1.4f);
                hit.center = new Vector3(0, 1f, 0);
                var sc = sgo.AddComponent<StudentController>();
                sc.Setup(Names[idx], idx, empty);
                Students[idx] = sc;

                // Laptop for empty seats too (lid closed look = flat dark slab on desk)
                if (empty)
                {
                    var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    slab.transform.SetParent(transform, false);
                    slab.transform.localPosition = new Vector3(x, 0.78f, z + 0.05f);
                    slab.transform.localScale = new Vector3(0.5f, 0.03f, 0.34f);
                    slab.GetComponent<Renderer>().material = Mat(new Color(0.25f, 0.26f, 0.3f));
                }

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
