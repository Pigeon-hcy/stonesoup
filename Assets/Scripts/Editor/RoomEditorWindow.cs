using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// A visual room editor for StoneSoup.
/// Open via Tools > Room Editor (Ctrl+Shift+R).
/// Reads/writes the txt grid files used by Room.fillRoom().
/// </summary>
public class RoomEditorWindow : EditorWindow
{
    // ─── Constants (must match LevelGenerator / Room) ───
    private const int ROOM_WIDTH = 10;
    private const int ROOM_HEIGHT = 8;
    private const int LOCAL_START_INDEX = 4;

    // ─── Serialized references (survive domain reload) ───
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject levelGeneratorPrefab;
    [SerializeField] private TextAsset textAsset;

    // ─── Runtime state ───
    private int[,] grid;                     // [col, row]  row 0 = first line of file = top of room
    private GameObject[] globalTilePrefabs;
    private GameObject[] localTilePrefabs;
    private Dictionary<int, Texture2D> previewCache = new Dictionary<int, Texture2D>();
    private Dictionary<int, string> nameCache = new Dictionary<int, string>();

    private int selectedTileIndex = 1;
    private Vector2 gridScrollPos;
    private Vector2 paletteScrollPos;
    private float cellSize = 56f;
    private bool isDirty;
    private string filePath;
    private string lastContent;
    private double lastCheckTime;

    // ─── Visual constants ───
    private static readonly Color colEmpty   = new Color(0.18f, 0.18f, 0.20f);
    private static readonly Color colWall    = new Color(0.55f, 0.36f, 0.20f);
    private static readonly Color colPlayer  = new Color(0.20f, 0.72f, 0.32f);
    private static readonly Color colExit    = new Color(0.92f, 0.82f, 0.18f);
    private static readonly Color colHover   = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color colBorder  = new Color(0.35f, 0.35f, 0.35f);

    // ═══════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════

    [MenuItem("Tools/Room Editor %#r")]   // Ctrl+Shift+R
    public static void ShowWindow()
    {
        var w = GetWindow<RoomEditorWindow>("Room Editor");
        w.minSize = new Vector2(680, 500);
    }

    private void OnEnable()
    {
        wantsMouseMove = true;
        EditorApplication.update += WatchFile;
        RestoreAfterReload();
    }

    private void OnDisable()
    {
        EditorApplication.update -= WatchFile;
    }

    /// <summary>After a domain reload the 2D array and prefab arrays are lost. Re‑derive them.</summary>
    private void RestoreAfterReload()
    {
        if (textAsset != null && grid == null)
            LoadFromText(textAsset);
        if (roomPrefab != null && localTilePrefabs == null)
        {
            Room room = roomPrefab.GetComponent<Room>();
            if (room != null) localTilePrefabs = room.localTilePrefabs;
        }
        if (levelGeneratorPrefab != null && globalTilePrefabs == null)
            LoadGlobals();
        if (levelGeneratorPrefab == null)
            TryAutoFindLevelGenerator();
    }

    // ═══════════════════════════════════════════════════════
    //  File watcher — auto‑refresh when file changes on disk
    // ═══════════════════════════════════════════════════════

    private void WatchFile()
    {
        if (EditorApplication.timeSinceStartup - lastCheckTime < 0.5) return;
        lastCheckTime = EditorApplication.timeSinceStartup;
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || isDirty) return;

        string cur = File.ReadAllText(filePath);
        if (cur != lastContent)
        {
            lastContent = cur;
            ParseGrid(cur);
            Repaint();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  OnGUI — main layout
    // ═══════════════════════════════════════════════════════

    private void OnGUI()
    {
        HandleKeyboard();
        DrawToolbar();

        if (grid == null)
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "1) Drag a Room Prefab (will auto‑load its txt + local tiles)\n" +
                "2) Or drag a TextAsset (.txt) directly\n" +
                "3) Optionally assign the LevelGenerator prefab for global tile sprites",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        DrawGridArea();
        GUILayout.Space(6);
        DrawPalette();
        EditorGUILayout.EndHorizontal();
        DrawStatusBar();
    }

    // ═══════════════════════════════════════════════════════
    //  Toolbar
    // ═══════════════════════════════════════════════════════

    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical("box");

        // Row 1: prefab references
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        roomPrefab = (GameObject)EditorGUILayout.ObjectField("Room Prefab", roomPrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck() && roomPrefab != null)
            LoadFromRoom();

        EditorGUI.BeginChangeCheck();
        levelGeneratorPrefab = (GameObject)EditorGUILayout.ObjectField("LevelGenerator", levelGeneratorPrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck() && levelGeneratorPrefab != null)
            LoadGlobals();

        EditorGUILayout.EndHorizontal();

        // Row 2: text asset + buttons
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        textAsset = (TextAsset)EditorGUILayout.ObjectField("Text File", textAsset, typeof(TextAsset), false);
        if (EditorGUI.EndChangeCheck() && textAsset != null)
            LoadFromText(textAsset);

        GUILayout.FlexibleSpace();

        if (grid == null)
        {
            if (GUILayout.Button("New Empty Room", GUILayout.Width(120)))
                CreateEmptyGrid();
        }

        GUI.enabled = isDirty;
        if (GUILayout.Button("Save", GUILayout.Width(60)))
            Save();
        GUI.enabled = true;

        if (GUILayout.Button("Reload", GUILayout.Width(60)))
            ReloadFromDisk();

        EditorGUILayout.EndHorizontal();

        // Dirty indicator
        if (isDirty)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var prev = GUI.color;
            GUI.color = new Color(1f, 0.6f, 0.2f);
            GUILayout.Label("● Unsaved changes", EditorStyles.boldLabel);
            GUI.color = prev;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    // ═══════════════════════════════════════════════════════
    //  Grid area
    // ═══════════════════════════════════════════════════════

    private void DrawGridArea()
    {
        float headerH = 18f;
        float labelW = 22f;
        float totalW = labelW + ROOM_WIDTH * cellSize + 16;
        float totalH = headerH + ROOM_HEIGHT * cellSize + 16;

        gridScrollPos = EditorGUILayout.BeginScrollView(gridScrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        Rect area = GUILayoutUtility.GetRect(totalW, totalH);

        // ── Column headers ──
        for (int c = 0; c < ROOM_WIDTH; c++)
        {
            Rect r = new Rect(area.x + labelW + c * cellSize, area.y, cellSize, headerH);
            EditorGUI.LabelField(r, c.ToString(), EditorStyles.centeredGreyMiniLabel);
        }

        // ── Rows ──
        for (int row = 0; row < ROOM_HEIGHT; row++)
        {
            // Row label
            Rect rl = new Rect(area.x, area.y + headerH + row * cellSize, labelW, cellSize);
            EditorGUI.LabelField(rl, row.ToString(), EditorStyles.centeredGreyMiniLabel);

            for (int col = 0; col < ROOM_WIDTH; col++)
            {
                Rect cell = new Rect(
                    area.x + labelW + col * cellSize,
                    area.y + headerH + row * cellSize,
                    cellSize, cellSize);

                Rect inner = new Rect(cell.x + 1, cell.y + 1, cell.width - 2, cell.height - 2);
                int idx = grid[col, row];

                // Background colour
                EditorGUI.DrawRect(inner, GetColor(idx));

                // Sprite preview
                Texture2D tex = GetPreview(idx);
                if (tex != null)
                    GUI.DrawTexture(inner, tex, ScaleMode.ScaleToFit);

                // Index number (bottom‑right)
                GUIStyle numStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.LowerRight,
                    fontSize = 9
                };
                numStyle.normal.textColor = new Color(1, 1, 1, 0.85f);
                GUI.Label(inner, idx.ToString(), numStyle);

                // Tile name (top‑left, small)
                if (idx != 0)
                {
                    GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.UpperLeft,
                        fontSize = 8,
                        wordWrap = true
                    };
                    nameStyle.normal.textColor = new Color(1, 1, 1, 0.7f);
                    GUI.Label(new Rect(inner.x + 2, inner.y, inner.width - 4, inner.height), GetName(idx), nameStyle);
                }

                // Border
                DrawBorder(cell, colBorder);

                // ── Interaction ──
                if (cell.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(inner, colHover);

                    bool leftBtn = Event.current.button == 0;
                    bool rightBtn = Event.current.button == 1;
                    bool paint = Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag;

                    if (paint && leftBtn)
                    {
                        if (grid[col, row] != selectedTileIndex)
                        {
                            grid[col, row] = selectedTileIndex;
                            isDirty = true;
                        }
                        Event.current.Use();
                    }
                    else if (paint && rightBtn)
                    {
                        if (grid[col, row] != 0)
                        {
                            grid[col, row] = 0;
                            isDirty = true;
                        }
                        Event.current.Use();
                    }

                    Repaint();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // ═══════════════════════════════════════════════════════
    //  Palette sidebar
    // ═══════════════════════════════════════════════════════

    private void DrawPalette()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(200));
        EditorGUILayout.LabelField("Tile Palette", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        paletteScrollPos = EditorGUILayout.BeginScrollView(paletteScrollPos, GUILayout.ExpandHeight(true));

        // Empty
        PaletteEntry(0, "0: Empty");

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("── Global Tiles ──", EditorStyles.centeredGreyMiniLabel);

        int globalCount = (globalTilePrefabs != null) ? globalTilePrefabs.Length : 3;
        for (int i = 0; i < globalCount; i++)
        {
            int ti = i + 1;
            string n = (globalTilePrefabs != null && i < globalTilePrefabs.Length && globalTilePrefabs[i] != null)
                ? globalTilePrefabs[i].name
                : "Global " + ti;
            PaletteEntry(ti, ti + ": " + n);
        }

        if (localTilePrefabs != null && localTilePrefabs.Length > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("── Local Tiles ──", EditorStyles.centeredGreyMiniLabel);
            for (int i = 0; i < localTilePrefabs.Length; i++)
            {
                int ti = i + LOCAL_START_INDEX;
                string n = (localTilePrefabs[i] != null) ? localTilePrefabs[i].name : "Local " + i;
                PaletteEntry(ti, ti + ": " + n);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Settings ──", EditorStyles.centeredGreyMiniLabel);
        selectedTileIndex = EditorGUILayout.IntField("Manual Index", selectedTileIndex);
        cellSize = EditorGUILayout.Slider("Cell Size", cellSize, 32, 96);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void PaletteEntry(int tileIndex, string label)
    {
        bool sel = (selectedTileIndex == tileIndex);

        Rect full = EditorGUILayout.BeginHorizontal(GUILayout.Height(24));
        if (sel)
            EditorGUI.DrawRect(full, new Color(0.25f, 0.45f, 0.75f, 0.35f));

        // Colour swatch + sprite
        Rect sw = GUILayoutUtility.GetRect(22, 22, GUILayout.Width(22));
        EditorGUI.DrawRect(sw, GetColor(tileIndex));
        Texture2D p = GetPreview(tileIndex);
        if (p != null) GUI.DrawTexture(sw, p, ScaleMode.ScaleToFit);
        if (sel) DrawBorder(sw, Color.yellow, 2);

        // Clickable label
        GUIStyle st = sel ? EditorStyles.whiteBoldLabel : EditorStyles.label;
        if (GUILayout.Button(label, st))
            selectedTileIndex = tileIndex;

        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════
    //  Status bar
    // ═══════════════════════════════════════════════════════

    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal("helpBox");
        string selName = GetName(selectedTileIndex);
        EditorGUILayout.LabelField(
            string.Format("Selected: {0} ({1})   |   Left-click: Paint   |   Right-click: Erase   |   0-9: Quick Select   |   Ctrl+S: Save",
                selectedTileIndex, selName),
            EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════
    //  Keyboard shortcuts
    // ═══════════════════════════════════════════════════════

    private void HandleKeyboard()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;

        // Ctrl+S  →  Save
        if (e.control && e.keyCode == KeyCode.S)
        {
            if (isDirty) Save();
            e.Use();
            return;
        }

        // 0‑9  →  Quick‑select tile index
        if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9)
        {
            selectedTileIndex = (int)(e.keyCode - KeyCode.Alpha0);
            e.Use();
            Repaint();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Data loading helpers
    // ═══════════════════════════════════════════════════════

    private void LoadFromRoom()
    {
        Room room = roomPrefab.GetComponent<Room>();
        if (room == null)
        {
            Debug.LogError("[RoomEditor] Selected prefab has no Room component.");
            return;
        }

        localTilePrefabs = room.localTilePrefabs;

        if (room.designedRoomFile != null)
            LoadFromText(room.designedRoomFile);

        ClearCache();
    }

    private void LoadGlobals()
    {
        LevelGenerator gen = levelGeneratorPrefab.GetComponent<LevelGenerator>();
        if (gen == null)
        {
            Debug.LogError("[RoomEditor] Selected prefab has no LevelGenerator component.");
            return;
        }
        globalTilePrefabs = gen.globalTilePrefabs;
        ClearCache();
    }

    private void LoadFromText(TextAsset asset)
    {
        textAsset = asset;
        string assetPath = AssetDatabase.GetAssetPath(asset);
        filePath = Path.GetFullPath(assetPath);
        lastContent = asset.text;
        ParseGrid(asset.text);
        isDirty = false;
    }

    private void ParseGrid(string content)
    {
        string[] rows = content.Trim().Split('\n');
        grid = new int[ROOM_WIDTH, ROOM_HEIGHT];

        for (int r = 0; r < Mathf.Min(rows.Length, ROOM_HEIGHT); r++)
        {
            string[] cols = rows[r].Trim().Split(',');
            for (int c = 0; c < Mathf.Min(cols.Length, ROOM_WIDTH); c++)
            {
                int.TryParse(cols[c].Trim(), out grid[c, r]);
            }
        }
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogWarning("[RoomEditor] No file path set. Assign a TextAsset first.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        for (int r = 0; r < ROOM_HEIGHT; r++)
        {
            for (int c = 0; c < ROOM_WIDTH; c++)
            {
                if (c > 0) sb.Append(',');
                sb.Append(grid[c, r]);
            }
            if (r < ROOM_HEIGHT - 1) sb.Append('\n');
        }

        File.WriteAllText(filePath, sb.ToString());
        lastContent = sb.ToString();
        isDirty = false;
        AssetDatabase.Refresh();
        Debug.Log("[RoomEditor] Saved → " + filePath);
    }

    private void ReloadFromDisk()
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            lastContent = File.ReadAllText(filePath);
            ParseGrid(lastContent);
            isDirty = false;
        }
    }

    private void CreateEmptyGrid()
    {
        grid = new int[ROOM_WIDTH, ROOM_HEIGHT];
        isDirty = true;
    }

    private void TryAutoFindLevelGenerator()
    {
        if (levelGeneratorPrefab != null) return;
        string[] guids = AssetDatabase.FindAssets("level_generator t:Prefab");
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null && go.GetComponent<LevelGenerator>() != null)
            {
                levelGeneratorPrefab = go;
                LoadGlobals();
                break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Visual helpers
    // ═══════════════════════════════════════════════════════

    private Color GetColor(int idx)
    {
        switch (idx)
        {
            case 0: return colEmpty;
            case 1: return colWall;
            case 2: return colPlayer;
            case 3: return colExit;
            default:
                // Consistent hue per index using golden ratio
                float hue = ((idx * 0.618033988f) % 1.0f);
                return Color.HSVToRGB(hue, 0.45f, 0.6f);
        }
    }

    private Texture2D GetPreview(int idx)
    {
        if (idx == 0) return null;

        if (previewCache.TryGetValue(idx, out Texture2D cached) && cached != null)
            return cached;

        GameObject prefab = GetPrefab(idx);
        if (prefab == null) return null;

        Texture2D tex = AssetPreview.GetAssetPreview(prefab);
        if (tex != null)
        {
            previewCache[idx] = tex;
            return tex;
        }

        // Preview not ready yet — request repaint so we retry
        Repaint();
        return null;
    }

    private string GetName(int idx)
    {
        if (idx == 0) return "Empty";

        if (nameCache.TryGetValue(idx, out string cached))
            return cached;

        GameObject prefab = GetPrefab(idx);
        string n = (prefab != null) ? prefab.name : "Tile " + idx;
        nameCache[idx] = n;
        return n;
    }

    private GameObject GetPrefab(int idx)
    {
        if (idx <= 0) return null;

        if (idx < LOCAL_START_INDEX)
        {
            int i = idx - 1;
            return (globalTilePrefabs != null && i >= 0 && i < globalTilePrefabs.Length) ? globalTilePrefabs[i] : null;
        }
        else
        {
            int i = idx - LOCAL_START_INDEX;
            return (localTilePrefabs != null && i >= 0 && i < localTilePrefabs.Length) ? localTilePrefabs[i] : null;
        }
    }

    private void ClearCache()
    {
        previewCache.Clear();
        nameCache.Clear();
    }

    private static void DrawBorder(Rect r, Color c, float w = 1f)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, w), c);                    // top
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - w, r.width, w), c);             // bottom
        EditorGUI.DrawRect(new Rect(r.x, r.y, w, r.height), c);                   // left
        EditorGUI.DrawRect(new Rect(r.xMax - w, r.y, w, r.height), c);            // right
    }
}
