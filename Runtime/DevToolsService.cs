using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DevTools {


    public struct DrawLineData{
        public Vector3 from, to;
        public Color color;
        public float timer;
    }

    public struct DrawTextData{
        public string text;
        public Vector3 position;
        public Color color;
        public Vector2 positionOff;
        public Texture2D texture2D;
        public float timer;
    }

    public struct DrawShpereData{
        public Vector3 position;
        public Color color;
        public float radius;
        public float timer;
    }

    public struct DrawCubeData{
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color color;
        public float timer;
    }

    public struct DrawCylinderData{
        public Vector3 position;
        public Quaternion rotation;
        public Color color;
        public float height;
        public float radius;
        public float timer;
    }

    public struct DrawCapsuleData{
        public Vector3 position;
        public Quaternion rotation;
        public Color color;
        public float height;
        public float radius;
        public float timer;
    }

    public class DevToolsService : MonoBehaviour {
        static List<ChartGraph> ListGraph;
        ChartGraph FPS;
        public static Material mat, mat2;
        public InputActionAsset inputActionsAssets;
        PlayerInput playerInput;
        public static Mesh Capsule, Sphere, Cube, Cylinder;
        void Awake(){
            DevToolsRuntime.ListWindows.Clear();
            DevToolsRuntime.ListGameObjects.Clear();
            DevToolsRuntime.ListLineData.Clear();
            DevToolsRuntime.ListTextData.Clear();
            DevToolsRuntime.ListSphereData.Clear();
            DevToolsRuntime.ListCapsuleData.Clear();
            DevToolsRuntime.ListCubeData.Clear();
            DevToolsRuntime.ListCylinderData.Clear();
            
            ListGraph = new List<ChartGraph>();
            mat = new Material(Shader.Find("Hidden/Internal-Colored"));
            mat2 = new Material(Shader.Find("Hidden/Internal-Colored"));
            DontDestroyOnLoad(gameObject);
            FPS = CreateGraph(500, "FPS", new Rect(1, 17, 198, 50));
        }

        void Start(){
            playerInput = GetComponent<PlayerInput>();
            playerInput.actions = inputActionsAssets;
            playerInput.currentActionMap = inputActionsAssets.actionMaps[0];
        }

        void FixedUpdate(){
            for(int i = 0; i < DevToolsRuntime.ListGameObjects.Count; i++){
                if(!DevToolsRuntime.ListGameObjects.ElementAt(i).Value){
                    DevToolsRuntime.ListWindows.Remove(DevToolsRuntime.ListGameObjects.ElementAt(i).Key);
                    DevToolsRuntime.ListGameObjects.Remove(DevToolsRuntime.ListGameObjects.ElementAt(i).Key);
                }
            }
        }

        float tmp, count;

        string statsText;
        ProfilerRecorder totalReservedMemoryRecorder;
        ProfilerRecorder gcReservedMemoryRecorder;
        ProfilerRecorder systemUsedMemoryRecorder;


        void OnEnable(){
            totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
            gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            systemUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
        }

        void OnDisable(){
            totalReservedMemoryRecorder.Dispose();
            gcReservedMemoryRecorder.Dispose();
            systemUsedMemoryRecorder.Dispose();
        }


        bool isOverlaysTmp = false;
        CursorLockMode cursorLockMode;
        void Update(){
            // Renders
            DrawSpheres();
            DrawCubes();
            DrawCapsules();
            DrawCylinders();
            
            var sb = new StringBuilder(500);
            sb.AppendLine($"Platform: {Application.platform}");
            if (totalReservedMemoryRecorder.Valid)
                sb.AppendLine($"Total Reserved Memory: {BytesToString(totalReservedMemoryRecorder.LastValue)}");
            if (gcReservedMemoryRecorder.Valid)
                sb.AppendLine($"GC Reserved Memory: {BytesToString(gcReservedMemoryRecorder.LastValue)}");
            if (systemUsedMemoryRecorder.Valid)
                sb.AppendLine($"System Used Memory: {BytesToString(systemUsedMemoryRecorder.LastValue)}");

            statsText = sb.ToString();

            if(playerInput.currentActionMap.FindAction("DevTools").triggered){
                DevToolsRuntime.isOpenDevTools = !DevToolsRuntime.isOpenDevTools;
                if(DevToolsRuntime.isOpenDevTools){
                    cursorLockMode = Cursor.lockState;
                    isOverlaysTmp = DevToolsRuntime.isOverlays;
                    DevToolsRuntime.isOverlays = true;
                }else{
                    Cursor.lockState = cursorLockMode;
                    DevToolsRuntime.isOverlays = isOverlaysTmp;
                }
                DevToolsRuntime.SelectedObject = null;
            }

            if(playerInput.currentActionMap.FindAction("Inspector").triggered)
                DevToolsRuntime.isOpenDeveloperTools = !DevToolsRuntime.isOpenDeveloperTools;

            if(playerInput.currentActionMap.FindAction("Overlays").triggered)
                DevToolsRuntime.isOverlays = !DevToolsRuntime.isOverlays;

            if(DevToolsRuntime.isOpenDevTools)
                Cursor.lockState = CursorLockMode.None;

            if(tmp + 1 < Time.time){ 
                tmp = Time.time;
                AddValueGraph(count, FPS);
                count = 0;
            }
            count++;
        }


        Vector2 SizeComponents = new(250, 0);
        Vector2 PaddingScreen = new(10, 10);
        Vector2 SizePerformance = new(200, 70);
        Vector2 SizeManager = new(200, 112);

        static GUIStyle style = new GUIStyle();
        void DrawText(string text, Vector3 target, Color color, Texture2D texture2D, Vector2 positionOff = new Vector2()){
            var position = Camera.main.WorldToScreenPoint(target);
            var textSize = GUI.skin.label.CalcSize(new GUIContent(text));
            style.normal.textColor = color;
            style.normal.background = texture2D;
            style.alignment = TextAnchor.MiddleCenter;
            if(position.z > 0)
                GUI.Label(new Rect(position.x - (textSize.x + 10) / 2 + positionOff.x, Screen.height - position.y +  positionOff.y, textSize.x + 10, textSize.y), text, style);
        }

        void OnGUI(){
            if(Debug.isDebugBuild){
                   
                // Draw text objects
                if(DevToolsRuntime.isOverlays)
                foreach(var item in DevToolsRuntime.ListGameObjects.Values.Distinct().Where(item => item))
                    DrawText(item.name, item.transform.position, Color.white, Texture2D.grayTexture);
                
                DrawText();
                DrawLines();

                GUI.backgroundColor = Color.black;
                GUI.color = Color.white;
                // Layout size
                Vector2 SizeDevTools = new(200, Screen.height - (PaddingScreen.y * 4 + SizePerformance.y + SizeManager.y));
                Vector2 SizeInspector = new(275, Screen.height - PaddingScreen.y * 2);

                if(DevToolsRuntime.isOpenDevTools){
                    GUI.Label(new Rect(SizePerformance.x + PaddingScreen.x * 2, PaddingScreen.y, 250, SizePerformance.y), statsText, new GUIStyle(GUI.skin.textArea){fontSize = 12, padding = new RectOffset(10, 0,5,0)});
                    GUILayout.Window(0, new Rect(PaddingScreen.x, PaddingScreen.y, SizePerformance.x, SizePerformance.y), DevToolsWindow, "Performance");
                    GUILayout.Window(1, new Rect(PaddingScreen.x, PaddingScreen.y * 2 + SizePerformance.y, SizeManager.x, SizeManager.y), DevToolsWindow, "Manager");
                    GUILayout.Window(2, new Rect(PaddingScreen.x, PaddingScreen.y * 3 + SizePerformance.y + SizeManager.y, SizeDevTools.x, SizeDevTools.y), DevToolsWindow, "DevTools");
                    if(DevToolsRuntime.SelectedObject)
                        GUILayout.Window(3, new Rect(SizePerformance.x + PaddingScreen.x * 2,  PaddingScreen.y * 2 + SizePerformance.y, SizeComponents.x, SizeComponents.y), DevToolsWindow, DevToolsRuntime.SelectedObject.name == gameObject.name ? currentList.ToString() : DevToolsRuntime.SelectedObject.name);
                }else{
                    GUILayout.Label("  Press F1 to open/close DevTools." + (!DevToolsRuntime.CurrentWindow.Equals(new KeyValuePair<string, GUI.WindowFunction>()) ? "\n  Press F2 to open/close current Developer Tools." : "") + "\n  Press F3 to show/hide Overlays.");
                }
                if(DevToolsRuntime.isOpenDeveloperTools)
                if(!DevToolsRuntime.CurrentWindow.Equals(new KeyValuePair<string, GUI.WindowFunction>()))
                    GUILayout.Window(4, new Rect(Screen.width - SizeInspector.x - PaddingScreen.x, PaddingScreen.y, SizeInspector.x, SizeInspector.y), DevToolsRuntime.CurrentWindow.Value, DevToolsRuntime.CurrentWindow.Key);
            }
        }


        void DrawSpheres(){
            for(int i = 0; i < DevToolsRuntime.ListSphereData.Count; i++){
                var shpereData = DevToolsRuntime.ListSphereData[i];

                if(DevToolsRuntime.isOverlays){
                    mat2.color = shpereData.color;
                    Graphics.DrawMesh(Sphere, Matrix4x4.TRS(shpereData.position, Quaternion.identity, Vector3.one * shpereData.radius), mat2, 0);
                }

                if(shpereData.timer < Time.time)
                    DevToolsRuntime.ListSphereData.RemoveAt(i);
            }
        }

        void DrawCubes(){
            for(int i = 0; i < DevToolsRuntime.ListCubeData.Count; i++){
                var cubeData = DevToolsRuntime.ListCubeData[i];

                if(DevToolsRuntime.isOverlays){
                    mat.color = cubeData.color;
                    Graphics.DrawMesh(Cube, Matrix4x4.TRS(cubeData.position, cubeData.rotation, cubeData.scale), mat, 0);
                }

                if(cubeData.timer < Time.time)
                    DevToolsRuntime.ListCubeData.RemoveAt(i);
            } 
        }

        void DrawCapsules(){
            for(int i = 0; i < DevToolsRuntime.ListCapsuleData.Count; i++){
                var capsuleData = DevToolsRuntime.ListCapsuleData[i];

                if(DevToolsRuntime.isOverlays){
                    mat.color = capsuleData.color;
                    Graphics.DrawMesh(Capsule, Matrix4x4.TRS(capsuleData.position, capsuleData.rotation, Vector3.one * capsuleData.radius + Vector3.up * capsuleData.height), mat, 0);
                }

                if(capsuleData.timer < Time.time)
                    DevToolsRuntime.ListCapsuleData.RemoveAt(i);
            }
        }

        void DrawCylinders(){
            for(int i = 0; i < DevToolsRuntime.ListCylinderData.Count; i++){
                var cylinderData = DevToolsRuntime.ListCylinderData[i];

                if(DevToolsRuntime.isOverlays){
                    mat.color = cylinderData.color;
                    Graphics.DrawMesh(Cylinder, Matrix4x4.TRS(cylinderData.position, cylinderData.rotation, Vector3.one * cylinderData.radius + Vector3.up * cylinderData.height), mat, 0);
                }

                if(cylinderData.timer < Time.time)
                    DevToolsRuntime.ListCylinderData.RemoveAt(i);
            }
        }

        void DrawLines(){
            if(Event.current.type != EventType.Repaint)
            for(int i = 0; i < DevToolsRuntime.ListLineData.Count; i++){
                var line = DevToolsRuntime.ListLineData[i];

                if(DevToolsRuntime.isOverlays){
                    GL.PushMatrix();
                    mat.SetPass(0);
                    GL.Begin(GL.LINES);
                    GL.Color(line.color);
                    GL.Vertex(line.from);
                    GL.Vertex(line.to);
                    GL.End();
                    GL.PopMatrix();
                }
                if(line.timer < Time.time)
                    DevToolsRuntime.ListLineData.RemoveAt(i);
            }
        }

        void DrawText(){
            for(int i = 0; i < DevToolsRuntime.ListTextData.Count; i++){
                var textData = DevToolsRuntime.ListTextData[i];

                if(DevToolsRuntime.isOverlays)
                    DrawText(textData.text, textData.position, textData.color, textData.texture2D, textData.positionOff);

                if(textData.timer < Time.time)
                    DevToolsRuntime.ListTextData.RemoveAt(i);
            }
        }

        Vector2 scrollPosition = Vector2.zero;
        Vector2 scrollPosition2 = Vector2.zero;
        Vector2 scrollPosition3 = Vector2.zero;
        enum typeList {Components, Scenes, Graphic, Resolution}
        typeList currentList = typeList.Components;
        void DevToolsWindow(int id){
            GUILayout.Space(0);
            switch(id){
                case 0: //Performance
                    for(int i = 0; i < ListGraph.Count; i++)
                        ShowGraph(ListGraph[i]);
                break;
                case 1: // Manager
                    scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2);
                    if(GUILayout.Button("Scenes")){
                        currentList = typeList.Scenes;
                        DevToolsRuntime.SelectedObject = gameObject;
                    }
                    if(GUILayout.Button("Graphic")){
                        currentList = typeList.Graphic;
                        DevToolsRuntime.SelectedObject = gameObject;
                    }
                    if(GUILayout.Button("Resolution")){
                        currentList = typeList.Resolution;
                        DevToolsRuntime.SelectedObject = gameObject;
                    }
                    GUILayout.EndScrollView();
                break;
                case 2: //DevTools
                    scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                    foreach(var item in DevToolsRuntime.ListGameObjects.Values.Distinct().Where(item => item))
                        if(GUILayout.Button(item.name)){
                            currentList = typeList.Components;
                            DevToolsRuntime.SelectedObject = item;
                        }
                    GUILayout.EndScrollView();
                break;
                case 3: //Component
                    Draw();
                break;
            }       
        }

        void Draw(){
            int quantity = 0;
            switch(currentList){
                case typeList.Components:
                    quantity = DevToolsRuntime.ListGameObjects.Where(item => item.Value == DevToolsRuntime.SelectedObject).Count();
                break;
                case typeList.Scenes:
                    quantity = SceneManager.sceneCountInBuildSettings;
                break;
                case typeList.Graphic:
                    quantity = QualitySettings.names.Length + 30;
                break;
                case typeList.Resolution:
                    quantity = Screen.resolutions.Length;
                break;
            }

            if(quantity == 0){
                DevToolsRuntime.SelectedObject = null;
                return;
            }

            if((quantity * 26) >= Screen.height - (PaddingScreen.y * 5 + SizePerformance.y)){
                SizeComponents.y = Screen.height - (PaddingScreen.y * 3 + SizePerformance.y);
                scrollPosition3 = GUILayout.BeginScrollView(scrollPosition3);
            }else{
                SizeComponents.y = 0;
                GUILayout.BeginVertical();
            }

            switch(currentList){
                case typeList.Components:
                    var Components = DevToolsRuntime.ListGameObjects.Where(item => item.Value == DevToolsRuntime.SelectedObject);
                    foreach(var item in Components.Where(item => item.Value))
                        if(GUILayout.Button(item.Key)){
                            var value = DevToolsRuntime.ListWindows.First(item2 => item2.Key == item.Key);
                            if(value.Value != null){
                                DevToolsRuntime.isOpenDeveloperTools = true;
                                DevToolsRuntime.CurrentWindow = DevToolsRuntime.ListWindows.First(item2 => item2.Key == item.Key);
                            }
                        }
                break;
                case typeList.Scenes:
                    var regex = new System.Text.RegularExpressions.Regex(@"([^/]*/)*([\w\d\-]*)\.unity");
                    for(int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                        if(GUILayout.Button(regex.Replace(SceneUtility.GetScenePathByBuildIndex(i), "$2"))){
                            DevToolsRuntime.CurrentWindow = new KeyValuePair<string, GUI.WindowFunction>();
                            SceneManager.LoadScene(i);
                        }
                break;
                case typeList.Graphic:
                    string[] names = QualitySettings.names;
                    if(name.Length > 0)
                        GUILayout.Label("Quality: " + names[QualitySettings.GetQualityLevel()]); // 1
                    for(int i = 0; i < names.Length; i++)
                        if(GUILayout.Button(names[i]))
                            QualitySettings.SetQualityLevel(i, true);

                    GUILayout.Label("FrameRate: " + Application.targetFrameRate); // 2
                    Application.targetFrameRate = (int)GUILayout.HorizontalSlider(Application.targetFrameRate, -1, 500); // 3

                    GUILayout.Label("VSync: " + QualitySettings.vSyncCount); // 4
                    QualitySettings.vSyncCount = (int)GUILayout.HorizontalSlider(QualitySettings.vSyncCount, 0, 4); // 5

                    GUILayout.Label("AntiAliasing: " + QualitySettings.antiAliasing); // 6
                    QualitySettings.antiAliasing = (int)GUILayout.HorizontalSlider(QualitySettings.antiAliasing, 0, 4); // 7

                    GUILayout.Label("AnisotropicFiltering: " + QualitySettings.anisotropicFiltering); // 8
                    QualitySettings.anisotropicFiltering = (AnisotropicFiltering)GUILayout.HorizontalSlider((float)QualitySettings.anisotropicFiltering, 0, 2); // 9

                    QualitySettings.enableLODCrossFade = GUILayout.Toggle(QualitySettings.enableLODCrossFade, "EnableLODCrossFade"); // 10

                    GUILayout.Label("LodBias: " + QualitySettings.lodBias); // 11
                    QualitySettings.lodBias = GUILayout.HorizontalSlider(QualitySettings.lodBias, 0, 2f); // 12

                    GUILayout.Label("MaximumLODLevel: " + QualitySettings.maximumLODLevel); // 13
                    QualitySettings.maximumLODLevel = (int)GUILayout.HorizontalSlider(QualitySettings.maximumLODLevel, 0, 20); // 14

                    GUILayout.Label("Shadow: " + QualitySettings.shadows); // 15
                    QualitySettings.shadows = (ShadowQuality)GUILayout.HorizontalSlider((float)QualitySettings.shadows, 0, 2); // 16

                    GUILayout.Label("ShadowDistance: " + QualitySettings.shadowDistance); // 17
                    QualitySettings.shadowDistance = (int)GUILayout.HorizontalSlider(QualitySettings.shadowDistance, 0, 200); // 18

                    GUILayout.Label("ShadowResolution: " + QualitySettings.shadowResolution); // 19
                    QualitySettings.shadowResolution = (ShadowResolution)GUILayout.HorizontalSlider((float)QualitySettings.shadowResolution, 0, 3); // 20

                    QualitySettings.softParticles = GUILayout.Toggle(QualitySettings.softParticles, "SoftParticles"); // 21

                    QualitySettings.softVegetation = GUILayout.Toggle(QualitySettings.softVegetation, "SoftVegetation"); // 22

                    GUILayout.Label("TerrainDetailDensityScale: " + QualitySettings.terrainDetailDensityScale); // 23
                    QualitySettings.terrainDetailDensityScale = GUILayout.HorizontalSlider(QualitySettings.terrainDetailDensityScale, 0, 5000); // 24

                    GUILayout.Label("TerrainDetailDistance: " + QualitySettings.terrainDetailDistance); // 25
                    QualitySettings.terrainDetailDistance = GUILayout.HorizontalSlider(QualitySettings.terrainDetailDistance, 0, 5000); // 26

                    GUILayout.Label("TerrainTreeDistance: " + QualitySettings.terrainTreeDistance); // 27
                    QualitySettings.terrainTreeDistance = GUILayout.HorizontalSlider(QualitySettings.terrainTreeDistance, 0, 5000); // 28

                    GUILayout.Label("TerrainMaxTrees: " + QualitySettings.terrainMaxTrees); // 29
                    QualitySettings.terrainMaxTrees = GUILayout.HorizontalSlider(QualitySettings.terrainMaxTrees, 0, 50000); // 30
                break;
                case typeList.Resolution:
                    Resolution[] resolutions = Screen.resolutions;
                    for(int i = 0; i < resolutions.Length; i++)
                        if(GUILayout.Button(resolutions[i].width + "x" + resolutions[i].height))
                            Screen.SetResolution(resolutions[i].width, resolutions[i].height, Screen.fullScreenMode);
                break;
            }

            if((quantity * 26) >= Screen.height - (PaddingScreen.y * 5 + SizePerformance.y))
                GUILayout.EndScrollView();
            else
                GUILayout.EndVertical();
        }

        class ChartGraph {
            public Rect windowRect;
            public string Name;
            public float value;
            public float maxValue;
            public List<float> values = new();
        }
        
        void AddValueGraph(float value, ChartGraph graph){
            graph.value = value;
            float b = graph.windowRect.height / graph.maxValue;
            value = b*value;
            if(value > graph.windowRect.height)
                value = graph.windowRect.height;
            if(value < 0)
                value = 0;
            graph.values.Add(value);
        }

        ChartGraph CreateGraph(int maxValue, string name, Rect windowRect){
            ChartGraph graph = new(){
                windowRect = windowRect,
                maxValue = maxValue,
                Name = name
            };
            ListGraph.Add(graph);
            return graph;
        }

        void ShowGraph(ChartGraph graph){
            if(graph.values.Count > 0 && Event.current.type == EventType.Repaint){
                while(graph.windowRect.width < graph.values.Count)
                    graph.values.RemoveAt(0);

                GL.PushMatrix();
                GL.Clear(true, false, Color.black);
                mat.color = Color.white;
                mat.SetPass(0);

                GL.Begin(GL.LINES);
                GL.Color(new Color(1f,1f,1f,0.1f));
                for(int i = 1; i < 3; i++){
                    var xs = graph.windowRect.height / 3;
                    GL.Vertex3(graph.windowRect.x, graph.windowRect.y + xs * i, 0);
                    GL.Vertex3(graph.windowRect.x + graph.windowRect.width, graph.windowRect.y + xs * i, 0);
                }
                for(int i = 1; i < 4; i++){
                    var xs = graph.windowRect.width / 4;
                    GL.Vertex3(graph.windowRect.x + xs * i, graph.windowRect.y, 0);
                    GL.Vertex3(graph.windowRect.x + xs * i, graph.windowRect.y + graph.windowRect.height, 0);
                }
                GL.End();

                GL.Begin(GL.QUADS);
                GL.Color(new Color(0.0f, 0.0f, 0.0f,0.0f));
                GL.Vertex3(graph.windowRect.x, graph.windowRect.y, 0);
                GL.Vertex3(graph.windowRect.width + graph.windowRect.x, graph.windowRect.y, 0);
                GL.Vertex3(graph.windowRect.width + graph.windowRect.x, graph.windowRect.height + graph.windowRect.y, 0);
                GL.Vertex3(graph.windowRect.x, graph.windowRect.height + graph.windowRect.y, 0);
                GL.End();

                GL.Begin(GL.LINES);
                GL.Color(new Color(1f,1f,1f,1f));


                float max = graph.values.Max() + 1;
                float min = graph.values.Min();

                for(int i = 0; i < graph.values.Count; i++){
                    if(i > 1){
                        float y2 = Mathf.InverseLerp(max, min, graph.values[i]) * graph.windowRect.height + graph.windowRect.y;
                        float y1 = Mathf.InverseLerp(max, min, graph.values[i - 1]) * graph.windowRect.height + graph.windowRect.y;
                        GL.Vertex3(i + graph.windowRect.x, y2, 0);
                        GL.Vertex3(i - 1 + graph.windowRect.x, y1, 0);
                    }
                }

                GL.End();
                GL.PopMatrix();
                var style = new GUIStyle
                {
                    alignment = TextAnchor.UpperRight,
                    fontSize = 10
                };
                GUI.Label(new Rect(graph.windowRect.x,graph.windowRect.y,graph.windowRect.width,graph.windowRect.height),"<color=white>"+ graph.Name + ": " + graph.value + "</color>", style);
           
            }
        }

        string BytesToString(float PacketsReceived){
            if(PacketsReceived > 1024000000)
            return (PacketsReceived / 1024000000).ToString("0.00") + "GB";
            if(PacketsReceived > 1024000)
            return (PacketsReceived / 1024000).ToString("0.00") + "MB";
            if(PacketsReceived > 1024)
            return (PacketsReceived / 1024).ToString("0.00") + "KB";
            if(PacketsReceived < 1024)
            return PacketsReceived + "Bytes";
            return "";
        }
    }
}