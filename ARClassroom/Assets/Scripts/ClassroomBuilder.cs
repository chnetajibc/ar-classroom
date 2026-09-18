using System.Collections.Generic;
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
    // Students are drawn RANDOMLY from human3 (Marina, 1.753m), human4 (man in
    // black, ~1.62m) and human5 (rigged boy, 2.691m) — never human2 (teacher).
    // Scales convert each file to a ~1.5m student (human4 was 0.092 by mistake,
    // which shrank it to a 15cm miniature — measured true length is ~1.62m).
    // Yaws face each model at the board; pitched=true tips Z-up files upright.
    // Per-model pitch: human3 is Z-up flat (-90), human4 lies 27° nose-up in
    // the file (PCA-measured axis) so -117 stands it vertical, human5 is Y-up
    // already (no pitch). Yaws verified against headless renders: human3
    // needs yaw 0 (180 showed back-of-head), human4 faces the board at 180,
    // human5 is correct at 0 under the Y-180 student root.
    GameObject[] studentBodyPrefabs;
    readonly float[] studentBodyScales = new float[] { 0.856f, 0.92f, 0.567f };
    readonly float[] studentBodyYaws = new float[] { 0f, 180f, 0f };
    readonly bool[] studentBodyPitched = new bool[] { true, true, false };
    readonly float[] studentBodyPitchDeg = new float[] { -90f, -117f, 0f };
    readonly string[] studentBodyPaths = new string[] { "Models/Humans/human3", "Models/Humans/human4", "Models/Humans/human5" };
    // Shuffle bag so consecutive seats don't repeat a body (refilled per cycle).
    List<int> bodyBag;

    // ---- Seat-unit furniture kit (measured Unity meters, headless Editor run
    // instantiating each FBX at scale 1: desk 7.345x3.844x3.923 / chair
    // 2.0x4.7x2.0 with backrest at +Z / laptop 6.0x3.677x5.456 with the screen
    // slab at +Z). Scales below yield real-world sizes: desk 1.47m wide with
    // top at 0.77m (standard 74-76cm), chair 0.4x0.94x0.4m, laptop 0.36m wide.
    const float DeskScale = 0.2f;
    const float ChairScale = 0.2f;
    const float LaptopScale = 0.06f;
    const float ChairGap = 0.06f;       // desk back edge -> chair front edge
    const float ChairSeatRatio = 0.43f; // seat-top height as fraction of chair AABB height
    const float LaptopDeskInset = 0.1f; // laptop center ahead of desk center, toward student
    const float HipHeightRatio = 0.52f; // hip joint above feet as fraction of stature

    // Realistic furniture (Kenney FBX in Assets/Resources/Models/Kenney)
    GameObject deskPrefab;
    GameObject chairPrefab;
    GameObject laptopPrefab;

    void Awake()
    {
        Build();
    }

    // Full classroom build. Public (and idempotent) so headless tooling and
    // tests can build deterministically: in batch mode Unity does not invoke
    // Awake on AddComponent, so automation calls Build() explicitly.
    bool built;
    public void Build()
    {
        if (built) return;
        built = true;
        studentBodyPrefabs = new GameObject[studentBodyPaths.Length];
        for (int i = 0; i < studentBodyPaths.Length; i++)
        {
            studentBodyPrefabs[i] = Resources.Load<GameObject>(studentBodyPaths[i]);
            // Per Unity docs, Resources.Load returns null when the asset is missing
            // (e.g. FBX stored in Git LFS that was never pulled). Validate meshes too:
            // a broken import yields a prefab with no Renderer, which must fall back.
            if (!HasMeshes(studentBodyPrefabs[i]))
            {
                Debug.LogWarning("[Classroom] Student body missing or has no meshes: "
                    + studentBodyPaths[i] + " — run `git lfs pull`; procedural fallback will be used.");
                studentBodyPrefabs[i] = null;
            }
        }
        deskPrefab = ValidateFurniture("Models/Kenney/desk");
        chairPrefab = ValidateFurniture("Models/Kenney/chair");
        laptopPrefab = ValidateFurniture("Models/Kenney/laptop");
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

    // A prefab is usable only if it loaded AND actually contains renderable
    // meshes. Covers: missing file (Resources.Load -> null) and broken imports
    // (e.g. un-pulled Git LFS pointer text imported as a mesh-less model).
    static bool HasMeshes(GameObject prefab)
    {
        return prefab != null && prefab.GetComponentInChildren<Renderer>() != null;
    }

    GameObject ValidateFurniture(string path)
    {
        var prefab = Resources.Load<GameObject>(path);
        if (!HasMeshes(prefab))
        {
            Debug.LogWarning("[Classroom] Furniture missing or mesh-less: "
                + path + " — run `git lfs pull`; procedural fallback will be used.");
            return null;
        }
        return prefab;
    }

    // Instantiate that never throws: returns null instead of raising
    // ArgumentException ("The thing you want to instantiate is null") on a
    // missing prefab, so one bad asset can't abort the whole classroom build.
    // The part spawns parented at the unit origin; callers place it with
    // ShiftTo() using its measured bounds (see SeatUnit below).
    GameObject SafeInstantiate(GameObject prefab, Transform parent, float scale, Quaternion rot, string name, string what)
    {
        if (!HasMeshes(prefab)) return null;
        try
        {
            var go = Instantiate(prefab, parent, false);
            go.name = name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = rot;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Classroom] Instantiate failed for " + what + ": " + e.Message);
            return null;
        }
    }

    // Combined world-space AABB of every renderer under root (Renderer.bounds
    // is the documented world-space box). Only used on freshly spawned static
    // parts whose ancestors are unscaled/unrotated at build time, so the box
    // exactly describes the part's placement frame.
    static Bounds MeasureBounds(GameObject root)
    {
        var result = new Bounds(root.transform.position, Vector3.zero);
        bool init = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (!init) { result = r.bounds; init = true; }
            else result.Encapsulate(r.bounds);
        }
        return result;
    }

    // Shift a freshly spawned part so its bounds land on target: center XZ (and
    // Y) goes to desiredCenter, then the base sits on desiredMinY. Because the
    // classroom root is unscaled/unrotated while building, a world-space shift
    // equals the same local-space shift.
    static void ShiftTo(GameObject go, Vector3 desiredCenter, float desiredMinY)
    {
        Bounds b = MeasureBounds(go);
        go.transform.localPosition += desiredCenter - b.center;
        b = MeasureBounds(go);
        go.transform.localPosition += new Vector3(0, desiredMinY - b.min.y, 0);
    }

    // Random student body that is never the teacher's model: the bag holds only
    // student indices, shuffled per cycle for an even random spread.
    int NextBodyIndex()
    {
        if (bodyBag == null) bodyBag = new List<int>();
        if (bodyBag.Count == 0)
        {
            for (int i = 0; i < studentBodyPrefabs.Length; i++)
                if (studentBodyPrefabs[i] != null) bodyBag.Add(i);
            for (int i = bodyBag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = bodyBag[i]; bodyBag[i] = bodyBag[j]; bodyBag[j] = tmp;
            }
        }
        if (bodyBag.Count == 0) return -1;
        int last = bodyBag.Count - 1;
        int pick = bodyBag[last];
        bodyBag.RemoveAt(last);
        return pick;
    }

    void BuildRoom()
    {
        // Roomy footprint 10m x 8m (x in [-5,5], z in [-2.5,5.5]): the realistic
        // FBX desks/chairs/bodies are bulkier than the old primitive slabs, so
        // the room was widened to keep chairs, students and the tutor's patrol
        // lane clear of the walls.
        // Floor platform
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(transform, false);
        floor.transform.localPosition = new Vector3(0, -0.05f, 1.5f);
        floor.transform.localScale = new Vector3(10f, 0.1f, 8f);
        floor.GetComponent<Renderer>().material = Mat(new Color(0.55f, 0.42f, 0.3f));

        // Rug under desks
        var rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rug.name = "Rug";
        rug.transform.SetParent(transform, false);
        rug.transform.localPosition = new Vector3(0, 0.005f, 1.6f);
        rug.transform.localScale = new Vector3(7.6f, 0.02f, 5.2f);
        rug.GetComponent<Renderer>().material = Mat(new Color(0.16f, 0.25f, 0.45f));

        // Front wall (behind board)
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "FrontWall";
        wall.transform.SetParent(transform, false);
        wall.transform.localPosition = new Vector3(0, 1.5f, -2.5f);
        wall.transform.localScale = new Vector3(10f, 3f, 0.15f);
        wall.GetComponent<Renderer>().material = Mat(new Color(0.88f, 0.89f, 0.92f));

        // Boundary walls: back + left + right so the room feels enclosed
        var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "BackWall";
        backWall.transform.SetParent(transform, false);
        backWall.transform.localPosition = new Vector3(0, 1.5f, 5.5f);
        backWall.transform.localScale = new Vector3(10f, 3f, 0.15f);
        backWall.GetComponent<Renderer>().material = Mat(new Color(0.82f, 0.84f, 0.88f));

        var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "LeftWall";
        leftWall.transform.SetParent(transform, false);
        leftWall.transform.localPosition = new Vector3(-5f, 1.5f, 1.5f);
        leftWall.transform.localScale = new Vector3(0.15f, 3f, 8f);
        leftWall.GetComponent<Renderer>().material = Mat(new Color(0.85f, 0.86f, 0.9f));

        var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "RightWall";
        rightWall.transform.SetParent(transform, false);
        rightWall.transform.localPosition = new Vector3(5f, 1.5f, 1.5f);
        rightWall.transform.localScale = new Vector3(0.15f, 3f, 8f);
        rightWall.GetComponent<Renderer>().material = Mat(new Color(0.85f, 0.86f, 0.9f));

        // Wall text was moved onto the blackboard (BoardController topics):
        // walls stay clean for readability — all DSA posters + banner content
        // now lives on the board's high-res World-Space Canvas (880x480 @
        // 0.005 scale ≈ 200px/m) where text stays crisp. See BoardController.
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
                // Each seat is isolated: one bad model/asset logs a warning and
                // falls back to primitives instead of aborting the whole loop
                // (an exception here used to skip every later seat + the tutor).
                try { BuildSeat(r, c, idx, deskMat, chairMat); }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Classroom] Seat {idx} build failed, using fallback: {e.Message}");
                    try { BuildSeatFallback(r, c, idx, deskMat, chairMat); }
                    catch (System.Exception e2)
                    {
                        Debug.LogError($"[Classroom] Seat {idx} fallback also failed: {e2.Message}");
                    }
                }
                idx++;
            }
        }
    }

    void BuildSeat(int r, int c, int idx, Material deskMat, Material chairMat)
    {
        BuildSeatUnit(r, c, idx, deskMat, chairMat, false);
    }

    // Last-resort path: plain primitives, no external assets at all.
    void BuildSeatFallback(int r, int c, int idx, Material deskMat, Material chairMat)
    {
        BuildSeatUnit(r, c, idx, deskMat, chairMat, true);
    }

    // One aligned SeatUnit group: [desk + chair + laptop + student], built once
    // per grid cell from measured bounds so chair/desk/laptop/student always
    // line up. Layout (unit-local, board toward -Z, student side +Z):
    //   desk centered on the unit with its base on the floor;
    //   chair centered on the same X, front edge tucked just behind the desk;
    //   laptop centered on the desk top, screen facing the student (+Z);
    //   student rooted over the chair so the hips land on the seat.
    void BuildSeatUnit(int r, int c, int idx, Material deskMat, Material chairMat, bool forceProcedural)
    {
        // Generous spacing for the bulky FBX furniture: 2.2m columns, 1.6m rows.
        float x = (c - (cols - 1) / 2f) * 2.2f;
        float z = r * 1.6f;
        bool empty = System.Array.IndexOf(emptySeats, idx) >= 0;

        var unit = new GameObject($"SeatUnit_{r}_{c}");
        unit.transform.SetParent(transform, false);
        unit.transform.localPosition = new Vector3(x, 0, z);
        Vector3 unitPos = unit.transform.position; // build-time world frame

        // --- Desk ---
        float deskTop = 0.76f;   // procedural fallback values, overwritten by
        float deskBack = z + 0.35f; // measured bounds when the FBX is used
        var desk = forceProcedural ? null
            : SafeInstantiate(deskPrefab, unit.transform, DeskScale, Quaternion.identity, $"Desk_{r}_{c}", $"Desk_{r}_{c}");
        if (desk != null)
        {
            ShiftTo(desk, new Vector3(unitPos.x, 0, unitPos.z), 0f);
            Bounds db = MeasureBounds(desk);
            deskTop = db.max.y;
            deskBack = db.max.z;
        }
        else BuildProceduralDesk(unit.transform, r, c, deskMat, chairMat);

        // --- Chair (tucked behind the desk, same center line) ---
        float seatTop = 0.49f; // procedural fallback seat height
        float chairCenterZ = deskBack + ChairGap + 0.2f;
        var chair = forceProcedural ? null
            : SafeInstantiate(chairPrefab, unit.transform, ChairScale, Quaternion.identity, $"Chair_{r}_{c}", $"Chair_{r}_{c}");
        if (chair != null)
        {
            // Chair identity rotation already has the backrest at +Z (measured).
            Bounds cb0 = MeasureBounds(chair);
            float depth = cb0.size.z;
            ShiftTo(chair, new Vector3(unitPos.x, 0, deskBack + ChairGap + depth / 2f), 0f);
            Bounds cb = MeasureBounds(chair);
            seatTop = cb.min.y + cb.size.y * ChairSeatRatio;
            chairCenterZ = cb.center.z;
        }
        else BuildProceduralChair(unit.transform, r, c, empty, chairMat);

        // --- Laptop (every desk; screen faces the student) ---
        var lap = forceProcedural ? null
            : SafeInstantiate(laptopPrefab, unit.transform, LaptopScale, Quaternion.Euler(0, 180f, 0), $"Laptop_{r}_{c}", $"Laptop_{r}_{c}");
        if (lap != null)
        {
            // The screen slab sits at the model's +Z; yawed 180 it ends up at
            // the desk's far edge with its face toward the student (+Z).
            ShiftTo(lap, new Vector3(unitPos.x, deskTop, z + LaptopDeskInset), deskTop);
        }
        else if (empty)
        {
            // Lid-closed look for empty seats when the FBX is unavailable.
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"LaptopSlab_{r}_{c}";
            slab.transform.SetParent(unit.transform, false);
            slab.transform.localPosition = new Vector3(0, 0.78f, 0.05f);
            slab.transform.localScale = new Vector3(0.5f, 0.03f, 0.34f);
            slab.GetComponent<Renderer>().material = Mat(new Color(0.25f, 0.26f, 0.3f));
        }

        BuildStudent(unit.transform, chairCenterZ, seatTop, idx, empty);
    }

    void BuildProceduralDesk(Transform parent, int r, int c, Material deskMat, Material legMat)
    {
        var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        desk.name = $"Desk_{r}_{c}";
        desk.transform.SetParent(parent, false);
        desk.transform.localPosition = new Vector3(0, 0.72f, 0);
        desk.transform.localScale = new Vector3(1.1f, 0.08f, 0.7f);
        desk.GetComponent<Renderer>().material = deskMat;
        for (int lx = -1; lx <= 1; lx += 2)
            for (int lz = -1; lz <= 1; lz += 2)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"DeskLeg_{r}_{c}";
                leg.transform.SetParent(parent, false);
                leg.transform.localPosition = new Vector3(lx * 0.48f, 0.36f, lz * 0.28f);
                leg.transform.localScale = new Vector3(0.06f, 0.72f, 0.06f);
                leg.GetComponent<Renderer>().material = legMat;
            }
    }

    void BuildProceduralChair(Transform parent, int r, int c, bool empty, Material chairMat)
    {
        var seatMat = empty ? Mat(new Color(0.5f, 0.5f, 0.55f, 0.5f)) : chairMat;
        var chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chair.name = $"Chair_{r}_{c}";
        chair.transform.SetParent(parent, false);
        chair.transform.localPosition = new Vector3(0, 0.45f, 0.65f);
        chair.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
        chair.GetComponent<Renderer>().material = seatMat;
        var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = $"ChairBack_{r}_{c}";
        back.transform.SetParent(parent, false);
        back.transform.localPosition = new Vector3(0, 0.85f, 0.88f);
        back.transform.localScale = new Vector3(0.5f, 0.6f, 0.07f);
        back.GetComponent<Renderer>().material = seatMat;
    }

    void BuildStudent(Transform unit, float chairCenterZ, float seatTop, int idx, bool empty)
    {
        // Student root (clickable), rotated to face the board (-Z), centered
        // over the chair. The exact sit height is solved inside Setup from the
        // measured body height (hips land on seatTop); -0.3 is just the
        // fallback for the procedural body.
        var sgo = new GameObject($"Student_{Names[idx]}");
        sgo.transform.SetParent(unit, false);
        float localChairZ = unit.InverseTransformPoint(new Vector3(0, 0, chairCenterZ)).z;
        sgo.transform.localPosition = new Vector3(0, -0.3f, localChairZ);
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
        float bodyPitchDeg = -90f;
        if (!empty)
        {
            // Random student body, never the teacher's model (bag holds only
            // validated student indices). Null = procedural fallback.
            int pick = NextBodyIndex();
            if (pick >= 0)
            {
                body = studentBodyPrefabs[pick];
                bodyScale = studentBodyScales[pick % studentBodyScales.Length];
                bodyYaw = studentBodyYaws[pick % studentBodyYaws.Length];
                bodyNeedsPitch = studentBodyPitched[pick % studentBodyPitched.Length];
                bodyPitchDeg = studentBodyPitchDeg[pick % studentBodyPitchDeg.Length];
            }
        }
        sc.Setup(Names[idx], idx, empty, body, bodyScale, bodyYaw, bodyNeedsPitch, bodyPitchDeg, seatTop);
        Students[idx] = sc;
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
