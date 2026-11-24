using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TestTask.EditorTools
{
    public static class ProjectBuilder
    {
        private const string CharacterSourcePath = "ArtSource/character_sheet.png";
        private const string CharacterSheetPath = "Assets/Art/Character/character_sheet.png";
        private const string WallPath = "Assets/Art/Environment/wall.png";
        private const string FloorPath = "Assets/Art/Environment/floor.png";
        private const string BookPath = "Assets/Art/Props/book.png";
        private const string RockPath = "Assets/Art/Props/rock.png";

        private const string SettingsFolder = "Assets/Settings";
        private const string SpriteSetPath = SettingsFolder + "/CharacterSprites.asset";
        private const string NoFrictionPath = SettingsFolder + "/NoFriction.physicsMaterial2D";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string BuildExePath = "Build/TestTask2D.exe";

        private const string UnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const string BackgroundLayer = "Background";
        private const string OverlayLayer = "Overlay";

        private const float EnvironmentPpu = 200f;
        private const float CharacterCellHeight = 2.6f;
        private const float BookWidth = 0.85f;
        private const float RockWidth = 2.1f;

        private const int SheetColumns = 5;
        private const int SheetRows = 5;
        private const byte AlphaThreshold = 40;

        private const float LevelHalfWidth = 19.2f;
        private const float StripHeight = 5.4f;
        private const float WalkTop = -0.15f;
        private const float WalkBottom = -5.2f;
        private const float WalkHalfWidth = 18.9f;

        private static readonly Vector2 PlayerStart = new Vector2(-15.5f, -2.6f);
        private static readonly Vector2 RockPosition = new Vector2(2.5f, -2.3f);
        private static readonly Vector2[] BookPositions =
        {
            new Vector2(-11.5f, -1.4f),
            new Vector2(-7.5f, -4.3f),
            new Vector2(-3f, -2f),
            new Vector2(7.5f, -4.2f),
            new Vector2(11.5f, -1.3f),
            new Vector2(16f, -3.4f),
        };

        private static readonly string[] RockMessages =
        {
            "Тяжёлый камень. Сдвинуть не выйдет.",
            "На камне выцарапано: «Не все книги стоит читать».",
            "Под камнем пусто. Только холодная пыль.",
        };

        [MenuItem("TestTask/Setup All (Import + Scene)")]
        public static void SetupAll()
        {
            ImportArt();
            BuildScene();
        }

        [MenuItem("TestTask/1. Import Art")]
        public static void ImportArt()
        {
            EnsureFolder(SettingsFolder);
            EnsureSortingLayers();

            ImportEnvironment(WallPath, new Vector2(0.5f, 0f));
            ImportEnvironment(FloorPath, new Vector2(0.5f, 1f));
            ImportProp(BookPath, BookWidth, 512);
            ImportProp(RockPath, RockWidth, 1024);
            ImportCharacterSheet();
            CreateNoFrictionMaterial();

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectBuilder] Art imported.");
        }

        [MenuItem("TestTask/2. Build Scene")]
        public static void BuildScene()
        {
            EnsureFolder("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (spriteMaterial == null)
                Debug.LogWarning("[ProjectBuilder] Sprite-Unlit-Default not found, default sprite material is used.");

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject level = new GameObject("Level");
            LevelBounds bounds = level.AddComponent<LevelBounds>();
            Configure(bounds, so => so.FindProperty("area").rectValue =
                new Rect(-LevelHalfWidth, -StripHeight, LevelHalfWidth * 2f, StripHeight * 2f));

            CreateTiledBackground("Wall", level.transform, WallPath, 0, spriteMaterial);
            CreateTiledBackground("Floor", level.transform, FloorPath, 1, spriteMaterial);
            CreateLevelColliders(level.transform);

            GameObject game = new GameObject("Game");
            CollectibleCounter counter = game.AddComponent<CollectibleCounter>();
            Configure(counter, so => so.FindProperty("total").intValue = BookPositions.Length);
            game.AddComponent<QuitOnEscape>();

            MessageView messageView;
            CreateHud(font, counter, out messageView);

            GameObject player = CreatePlayer(spriteMaterial);

            GameObject props = new GameObject("Props");
            for (int i = 0; i < BookPositions.Length; i++)
                CreateBook(props.transform, i + 1, BookPositions[i], counter, spriteMaterial);
            CreateRock(props.transform, RockPosition, messageView, font, spriteMaterial);

            CreateCamera(player.transform, bounds);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "Test Task 2D";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectBuilder] Scene built: " + ScenePath);
        }

        [MenuItem("TestTask/3. Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildExePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[ProjectBuilder] Build {summary.result}: {summary.outputPath}, " +
                      $"{summary.totalSize / (1024f * 1024f):F1} MB, errors: {summary.totalErrors}");

            if (summary.result != BuildResult.Succeeded && Application.isBatchMode)
                EditorApplication.Exit(1);
        }

        private static void ImportEnvironment(string path, Vector2 pivot)
        {
            TextureImporter ti = Importer(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = EnvironmentPpu;
            ti.maxTextureSize = 4096;
            ti.mipmapEnabled = false;
            ti.isReadable = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }

        private static void ImportProp(string path, float worldWidth, int maxSize)
        {
            Color32[] px = ReadPixelsForAnalysis(path, 1024, out int w, out int h);
            if (!TryAlphaBounds(px, w, new RectInt(0, 0, w, h), out RectInt content))
                throw new Exception("Prop has no opaque pixels: " + path);

            TextureImporter ti = Importer(path);
            ti.GetSourceTextureWidthAndHeight(out int srcW, out int _);
            float contentWidthSrc = content.width * (srcW / (float)w);

            var pivot = new Vector2((content.xMin + content.xMax) * 0.5f / w, content.yMin / (float)h);

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = contentWidthSrc / worldWidth;
            ti.maxTextureSize = maxSize;
            ti.mipmapEnabled = false;
            ti.isReadable = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Compressed;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.Tight;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();

            Debug.Log($"[ProjectBuilder] {path}: content {content}, pivot {pivot}, ppu {ti.spritePixelsPerUnit:F1}");
        }

        [MenuItem("TestTask/0. Clean Character Sheet")]
        public static void CleanCharacterSheet()
        {
            if (!File.Exists(CharacterSourcePath)) throw new FileNotFoundException(CharacterSourcePath);

            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(CharacterSourcePath));
            int w = src.width, h = src.height, n = w * h;
            Color32[] px = src.GetPixels32();
            Object.DestroyImmediate(src);

            bool IsWhite(Color32 c) => c.r >= 232 && c.g >= 232 && c.b >= 232;

            var state = new byte[n];
            var queue = new int[n];

            int FloodFill(int start, byte mark, List<int> collected)
            {
                int head = 0, tail = 0, count = 0;
                state[start] = mark;
                queue[tail++] = start;
                while (head < tail)
                {
                    int i = queue[head++];
                    count++;
                    collected?.Add(i);
                    int x = i % w, y = i / w;
                    if (x > 0) TryPush(i - 1);
                    if (x < w - 1) TryPush(i + 1);
                    if (y > 0) TryPush(i - w);
                    if (y < h - 1) TryPush(i + w);
                }
                return count;

                void TryPush(int j)
                {
                    if (state[j] != 0 || !IsWhite(px[j])) return;
                    state[j] = mark;
                    queue[tail++] = j;
                }
            }

            for (int x = 0; x < w; x++)
            {
                if (state[x] == 0 && IsWhite(px[x])) FloodFill(x, 1, null);
                int b = (h - 1) * w + x;
                if (state[b] == 0 && IsWhite(px[b])) FloodFill(b, 1, null);
            }
            for (int y = 0; y < h; y++)
            {
                int l = y * w, r = y * w + w - 1;
                if (state[l] == 0 && IsWhite(px[l])) FloodFill(l, 1, null);
                if (state[r] == 0 && IsWhite(px[r])) FloodFill(r, 1, null);
            }

            int minHole = (w * h) / 20000;
            var component = new List<int>(4096);
            int holes = 0;
            for (int i = 0; i < n; i++)
            {
                if (state[i] != 0 || !IsWhite(px[i])) continue;
                component.Clear();
                FloodFill(i, 2, component);
                if (component.Count < minHole) continue;
                foreach (int j in component) state[j] = 1;
                holes++;
            }

            var ring = new byte[n];
            for (int pass = 1; pass <= 2; pass++)
            {
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (state[i] == 1 || ring[i] != 0) continue;
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                    for (int dx = -1; dx <= 1 && !near; dx++)
                    {
                        int xx = x + dx, yy = y + dy;
                        if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                        int j = yy * w + xx;
                        near = pass == 1 ? state[j] == 1 : ring[j] == 1;
                    }
                    if (near) ring[i] = (byte)pass;
                }
            }

            const float ink = 40f;
            for (int i = 0; i < n; i++)
            {
                if (state[i] == 1)
                {
                    px[i] = new Color32(px[i].r, px[i].g, px[i].b, 0);
                    continue;
                }
                if (ring[i] == 0) continue;

                Color32 c = px[i];
                float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                float a = Mathf.Clamp01((255f - lum) / (255f - ink));
                if (a >= 0.98f) continue;
                if (a <= 0.02f)
                {
                    px[i] = new Color32(c.r, c.g, c.b, 0);
                    continue;
                }
                byte Unblend(byte v) => (byte)Mathf.Clamp(Mathf.RoundToInt((v - (1f - a) * 255f) / a), 0, 255);
                px[i] = new Color32(Unblend(c.r), Unblend(c.g), Unblend(c.b), (byte)Mathf.RoundToInt(a * 255f));
            }

            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dst.SetPixels32(px);
            dst.Apply();
            EnsureFolder(Path.GetDirectoryName(CharacterSheetPath)?.Replace('\\', '/'));
            File.WriteAllBytes(CharacterSheetPath, dst.EncodeToPNG());
            Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(CharacterSheetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"[ProjectBuilder] Character sheet cleaned: {w}x{h}, enclosed holes removed: {holes}");
        }

        private static void ImportCharacterSheet()
        {
            if (!File.Exists(CharacterSheetPath)) CleanCharacterSheet();

            Color32[] px = ReadPixelsForAnalysis(CharacterSheetPath, 8192, out int w, out int h);

            TextureImporter ti = Importer(CharacterSheetPath);
            ti.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
            float scale = srcW / (float)w;
            float bandH = h / (float)SheetRows;

            var clips = new RectInt[SheetRows, SheetColumns];
            var anchors = new float[SheetRows, SheetColumns];
            var feet = new List<float>();

            for (int row = 0; row < SheetRows; row++)
            {
                int yMin = Mathf.RoundToInt(h - (row + 1) * bandH);
                int yMax = Mathf.RoundToInt(h - row * bandH);
                List<(int start, int end)> figures = FindFigures(px, w, yMin, yMax, SheetColumns);

                for (int col = 0; col < SheetColumns; col++)
                {
                    int left = col == 0 ? 0 : (figures[col - 1].end + figures[col].start) / 2;
                    int right = col == SheetColumns - 1 ? w : (figures[col].end + figures[col + 1].start) / 2;
                    var clip = new RectInt(left, yMin, right - left, yMax - yMin);
                    if (!TryAlphaBounds(px, w, clip, out RectInt b))
                        throw new Exception($"Empty frame r{row} c{col}");

                    clips[row, col] = clip;
                    anchors[row, col] = UpperBodyCenterX(px, w, b);
                    feet.Add((b.yMin - yMin) / bandH);

                    Debug.Log($"[ProjectBuilder] frame r{row} c{col}: clip x {left}-{right}, " +
                              $"figure x {b.xMin}-{b.xMax}, anchor {anchors[row, col]:F0}, feet {(b.yMin - yMin) / bandH:F3}");
                }
            }

            feet.Sort();
            float pivotY = feet[feet.Count / 2];

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = srcH / (float)SheetRows / CharacterCellHeight;
            ti.maxTextureSize = 2048;
            ti.mipmapEnabled = false;
            ti.isReadable = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(ti);
            provider.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>();
            for (int row = 0; row < SheetRows; row++)
            for (int col = 0; col < SheetColumns; col++)
            {
                RectInt clip = clips[row, col];
                rects.Add(new SpriteRect
                {
                    name = FrameName(row, col),
                    rect = new Rect(clip.x * scale, clip.y * scale, clip.width * scale, clip.height * scale),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2((anchors[row, col] - clip.x) / clip.width, pivotY),
                    spriteID = GUID.Generate(),
                });
            }

            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            ti.SaveAndReimport();

            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(CharacterSheetPath)
                .OfType<Sprite>()
                .ToDictionary(s => s.name);

            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(SpriteSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CharacterSpriteSet>();
                AssetDatabase.CreateAsset(set, SpriteSetPath);
            }

            for (int row = 0; row < SheetRows; row++)
            {
                var frames = new Sprite[SheetColumns];
                for (int col = 0; col < SheetColumns; col++)
                    frames[col] = sprites[FrameName(row, col)];
                set.EditorSetFrames((Facing)row, frames);
            }

            EditorUtility.SetDirty(set);
            Debug.Log($"[ProjectBuilder] Character sheet sliced {SheetColumns}x{SheetRows}, pivotY {pivotY:F3}, ppu {ti.spritePixelsPerUnit:F1}");
        }

        private static List<(int start, int end)> FindFigures(Color32[] px, int w, int yMin, int yMax, int count)
        {
            var density = new int[w];
            for (int y = yMin; y < yMax; y++)
            {
                int rowStart = y * w;
                for (int x = 0; x < w; x++)
                    if (px[rowStart + x].a >= AlphaThreshold) density[x]++;
            }

            const int minDensity = 3;
            int minGap = Mathf.Max(4, w / 100);

            var runs = new List<(int start, int end)>();
            int runStart = -1;
            for (int x = 0; x <= w; x++)
            {
                bool occupied = x < w && density[x] >= minDensity;
                if (occupied && runStart < 0) runStart = x;
                if (!occupied && runStart >= 0)
                {
                    if (runs.Count > 0 && runStart - runs[runs.Count - 1].end < minGap)
                        runs[runs.Count - 1] = (runs[runs.Count - 1].start, x);
                    else
                        runs.Add((runStart, x));
                    runStart = -1;
                }
            }

            while (runs.Count > count)
            {
                int smallest = 0;
                for (int i = 1; i < runs.Count; i++)
                    if (runs[i].end - runs[i].start < runs[smallest].end - runs[smallest].start) smallest = i;
                runs.RemoveAt(smallest);
            }

            while (runs.Count < count)
            {
                int widest = 0;
                for (int i = 1; i < runs.Count; i++)
                    if (runs[i].end - runs[i].start > runs[widest].end - runs[widest].start) widest = i;

                (int s, int e) = runs[widest];
                int from = s + (e - s) / 5, to = e - (e - s) / 5, split = from;
                for (int x = from; x < to; x++)
                    if (density[x] < density[split]) split = x;

                runs[widest] = (s, split);
                runs.Insert(widest + 1, (split + 1, e));
                Debug.LogWarning($"[ProjectBuilder] figures touch, split at x={split}");
            }

            return runs;
        }

        private static float UpperBodyCenterX(Color32[] px, int w, RectInt figure)
        {
            int yFrom = figure.yMax - Mathf.RoundToInt(figure.height * 0.3f);
            double sum = 0;
            long n = 0;
            for (int y = yFrom; y < figure.yMax; y++)
            {
                int rowStart = y * w;
                for (int x = figure.xMin; x < figure.xMax; x++)
                {
                    if (px[rowStart + x].a < AlphaThreshold) continue;
                    sum += x;
                    n++;
                }
            }
            return n > 0 ? (float)(sum / n) : figure.center.x;
        }

        private static string FrameName(int row, int col) => $"character_{(Facing)row}_{col}";

        private static void CreateNoFrictionMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
            if (material != null) return;
            material = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, NoFrictionPath);
        }

        private static Color32[] ReadPixelsForAnalysis(string path, int maxSize, out int width, out int height)
        {
            TextureImporter ti = Importer(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.isReadable = true;
            ti.maxTextureSize = maxSize;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            width = tex.width;
            height = tex.height;
            return tex.GetPixels32();
        }

        private static bool TryAlphaBounds(Color32[] px, int texWidth, RectInt area, out RectInt bounds)
        {
            int xMin = int.MaxValue, yMin = int.MaxValue, xMax = int.MinValue, yMax = int.MinValue;
            int texHeight = px.Length / texWidth;
            int yEnd = Mathf.Min(area.yMax, texHeight);
            int xEnd = Mathf.Min(area.xMax, texWidth);

            for (int y = Mathf.Max(area.yMin, 0); y < yEnd; y++)
            {
                int rowStart = y * texWidth;
                for (int x = Mathf.Max(area.xMin, 0); x < xEnd; x++)
                {
                    if (px[rowStart + x].a < AlphaThreshold) continue;
                    if (x < xMin) xMin = x;
                    if (x > xMax) xMax = x;
                    if (y < yMin) yMin = y;
                    if (y > yMax) yMax = y;
                }
            }

            if (xMax < xMin)
            {
                bounds = default;
                return false;
            }

            bounds = new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
            return true;
        }

        private static void EnsureSortingLayers()
        {
            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");

            bool Exists(string layerName)
            {
                for (int i = 0; i < layers.arraySize; i++)
                    if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
                        return true;
                return false;
            }

            void Insert(int index, string layerName, uint id)
            {
                layers.InsertArrayElementAtIndex(index);
                SerializedProperty element = layers.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("name").stringValue = layerName;
                element.FindPropertyRelative("uniqueID").longValue = id;
            }

            if (!Exists(BackgroundLayer)) Insert(0, BackgroundLayer, 3141592601u);
            if (!Exists(OverlayLayer)) Insert(layers.arraySize, OverlayLayer, 3141592602u);
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateTiledBackground(string name, Transform parent, string spritePath, int order, Material material)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Sprite sprite = LoadSprite(spritePath);
            float half = sprite.bounds.size.x * 0.5f;

            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "Left" : "Right (mirrored)");
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(i == 0 ? -half : half, 0f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.flipX = i == 1;
                sr.sortingLayerName = BackgroundLayer;
                sr.sortingOrder = order;
                if (material != null) sr.sharedMaterial = material;
            }
        }

        private static void CreateLevelColliders(Transform parent)
        {
            var go = new GameObject("WalkAreaEdges");
            go.transform.SetParent(parent, false);
            var edge = go.AddComponent<EdgeCollider2D>();
            edge.points = new[]
            {
                new Vector2(-WalkHalfWidth, WalkTop),
                new Vector2(WalkHalfWidth, WalkTop),
                new Vector2(WalkHalfWidth, WalkBottom),
                new Vector2(-WalkHalfWidth, WalkBottom),
                new Vector2(-WalkHalfWidth, WalkTop),
            };
        }

        private static GameObject CreatePlayer(Material material)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = PlayerStart;

            var body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            body.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

            var feet = player.AddComponent<CapsuleCollider2D>();
            feet.direction = CapsuleDirection2D.Horizontal;
            feet.size = new Vector2(0.6f, 0.28f);
            feet.offset = new Vector2(0f, 0.12f);

            var input = player.AddComponent<PlayerInputReader>();
            var movement = player.AddComponent<PlayerMovement>();
            Configure(movement, so => so.FindProperty("input").objectReferenceValue = input);

            var facing = player.AddComponent<FacingDirection>();
            Configure(facing, so => so.FindProperty("movement").objectReferenceValue = movement);

            var interactor = player.AddComponent<PlayerInteractor>();
            Configure(interactor, so => so.FindProperty("input").objectReferenceValue = input);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            if (material != null) sr.sharedMaterial = material;

            var set = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>(SpriteSetPath);
            sr.sprite = set.GetIdle(Facing.Front);

            var animator = visual.AddComponent<CharacterSpriteAnimator>();
            Configure(animator, so =>
            {
                so.FindProperty("sprites").objectReferenceValue = set;
                so.FindProperty("movement").objectReferenceValue = movement;
                so.FindProperty("facing").objectReferenceValue = facing;
            });

            AddYSort(visual, sr, isStatic: false);
            return player;
        }

        private static void CreateBook(Transform parent, int index, Vector2 position, CollectibleCounter counter, Material material)
        {
            var go = new GameObject($"Book_{index}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(BookPath);
            if (material != null) sr.sharedMaterial = material;

            Bounds b = sr.sprite.bounds;
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = b.size * 0.8f;
            trigger.offset = b.center;

            var collectible = go.AddComponent<Collectible>();
            Configure(collectible, so => so.FindProperty("counter").objectReferenceValue = counter);

            AddYSort(go, sr, isStatic: true);
        }

        private static void CreateRock(Transform parent, Vector2 position, MessageView messageView, Font font, Material material)
        {
            var go = new GameObject("Rock");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(RockPath);
            if (material != null) sr.sharedMaterial = material;

            Bounds b = sr.sprite.bounds;
            float width = b.size.x;
            float height = b.max.y;

            var solid = go.AddComponent<CapsuleCollider2D>();
            solid.direction = CapsuleDirection2D.Horizontal;
            solid.size = new Vector2(width * 0.85f, height * 0.35f);
            solid.offset = new Vector2(0f, height * 0.18f);

            var zone = go.AddComponent<CircleCollider2D>();
            zone.isTrigger = true;
            zone.radius = width * 0.85f;
            zone.offset = new Vector2(0f, height * 0.25f);

            GameObject prompt = CreatePrompt(go.transform, new Vector3(0f, height + 0.45f, 0f), font);

            var interactable = go.AddComponent<Interactable>();
            Configure(interactable, so =>
            {
                so.FindProperty("prompt").objectReferenceValue = prompt;
                so.FindProperty("messageView").objectReferenceValue = messageView;
                SerializedProperty messages = so.FindProperty("messages");
                messages.arraySize = RockMessages.Length;
                for (int i = 0; i < RockMessages.Length; i++)
                    messages.GetArrayElementAtIndex(i).stringValue = RockMessages[i];
            });

            AddYSort(go, sr, isStatic: true);
        }

        private static GameObject CreatePrompt(Transform parent, Vector3 localPosition, Font font)
        {
            var go = new GameObject("Prompt_E", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = OverlayLayer;
            go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(100f, 100f);
            rt.localScale = Vector3.one * 0.006f;
            rt.localPosition = localPosition;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            Stretch((RectTransform)bg.transform);
            var image = bg.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            image.color = new Color(0.06f, 0.07f, 0.08f, 0.85f);

            Text label = CreateText(go.transform, "Label", "E", font, 64, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform);

            go.AddComponent<FloatingPrompt>();
            go.SetActive(false);
            return go;
        }

        private static void CreateHud(Font font, CollectibleCounter counter, out MessageView messageView)
        {
            var hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Text counterText = CreateText(hud.transform, "Counter", "Книги: 0 / 0", font, 46, TextAnchor.UpperLeft, Color.white);
            counterText.fontStyle = FontStyle.Bold;
            Place(counterText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(700f, 80f));
            counterText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
            var counterView = counterText.gameObject.AddComponent<CounterView>();
            Configure(counterView, so =>
            {
                so.FindProperty("counter").objectReferenceValue = counter;
                so.FindProperty("label").objectReferenceValue = counterText;
            });

            Text help = CreateText(hud.transform, "Controls", "WASD / стрелки — движение\nE — взаимодействие\nEsc — выход",
                font, 26, TextAnchor.UpperRight, new Color(1f, 1f, 1f, 0.7f));
            Place(help.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(600f, 120f));
            help.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

            var panel = new GameObject("Message", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panel.transform.SetParent(hud.transform, false);
            Place((RectTransform)panel.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1200f, 110f));
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            Text messageText = CreateText(panel.transform, "Text", "", font, 36, TextAnchor.MiddleCenter, Color.white);
            Stretch(messageText.rectTransform);

            messageView = panel.AddComponent<MessageView>();
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            Configure(messageView, so =>
            {
                so.FindProperty("label").objectReferenceValue = messageText;
                so.FindProperty("group").objectReferenceValue = group;
            });
        }

        private static void CreateCamera(Transform target, LevelBounds bounds)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.06f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            go.AddComponent<AudioListener>();

            float halfH = cam.orthographicSize;
            float halfW = halfH * 16f / 9f;
            Rect area = new Rect(-LevelHalfWidth, -StripHeight, LevelHalfWidth * 2f, StripHeight * 2f);
            go.transform.position = new Vector3(
                Mathf.Clamp(target.position.x, area.xMin + halfW, area.xMax - halfW),
                Mathf.Clamp(target.position.y + 1f, area.yMin + halfH, area.yMax - halfH),
                -10f);

            var follow = go.AddComponent<CameraFollow>();
            Configure(follow, so =>
            {
                so.FindProperty("target").objectReferenceValue = target;
                so.FindProperty("bounds").objectReferenceValue = bounds;
            });
        }

        private static void AddYSort(GameObject go, Renderer renderer, bool isStatic)
        {
            var sorter = go.AddComponent<YSortRenderer>();
            Configure(sorter, so =>
            {
                so.FindProperty("targetRenderer").objectReferenceValue = renderer;
                so.FindProperty("isStatic").boolValue = isStatic;
            });
            renderer.sortingOrder = YSortRenderer.CalculateOrder(go.transform.position.y, 100, 0);
        }

        private static Text CreateText(Transform parent, string name, string text, Font font, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Configure(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new Exception("Sprite not found: " + path);
            return sprite;
        }

        private static TextureImporter Importer(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) throw new Exception("Texture not found: " + path);
            return ti;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
