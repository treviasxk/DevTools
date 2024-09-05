using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DevTools {
    [BurstCompile]
    public class DevToolsService : MonoBehaviour {
        public struct DevToolsData {
            public int id;
            public GameObject gameObject;
            public List<Component> Components;
        }

        public struct DrawTextData{
            public string text;
            public Vector3 position;
            public Color color;
            public Vector2 positionOff;
            public Texture2D texture2D;
            public float timer;
        }

        public struct DrawObjectData{
            public ObjectType objectType;
            public Vector3 position;
            public Vector3 position2;
            public Color color;
            public float opacity;
            public float radius;
            public float timer;
            public Quaternion rotation;
            public Vector3 scale;
            public float height;
        }

        // LogType Unity values
        // error = 0, assets = 1, warnings = 2, log = 3, execption = 4
        enum LogsType {Log = 3, Warning = 2, Error = 0&1&4, Success = 5, Result = 6}
        struct LogContent{
            public LogsType type;
            public string text;
        }

        public InputActionAsset inputActionsAssets;
        public VisualTreeAsset visualTreeAsset;
        public PanelSettings panelSettings;
        public static GameObject SelectedObject;
        public static Component CurrentComponent;
        PlayerInput playerInput;
        UIDocument uIDocument;
        RenderParams renderParams;
        MaterialPropertyBlock materialPropertyBlock;
        public static Mesh Capsule, Sphere, Cube, Cylinder;
        public enum ObjectType {Capsule, Sphere, Cube, Cylinder, Line}

        void Awake(){
            SceneManager.sceneLoaded -= sceneLoaded;
            SceneManager.sceneLoaded += sceneLoaded;
            Application.logMessageReceived -= Log;
            Application.logMessageReceived += Log;

            materialPropertyBlock = new MaterialPropertyBlock();
            renderParams = new RenderParams(new Material(Shader.Find("DevTools/Debug"))){matProps = materialPropertyBlock};
            renderParams.material.enableInstancing = true;
            DontDestroyOnLoad(gameObject);
        }

        private void sceneLoaded(Scene arg0, LoadSceneMode arg1){
            var listObjects = DevTools.ListGameObjects.Where(item => item.id == -1).ToArray();
            if(uIDocument.rootVisualElement != null){
                var root = uIDocument.rootVisualElement.Q<ScrollView>("ListObjects");
                for(int i = 0; i < listObjects.Length; i++){
                    if(listObjects[i].id == -1){
                        var itemObject = root.Q<Button>(listObjects[i].id.ToString());
                        if(itemObject != null)
                            root.Remove(itemObject);
                        DevTools.ListGameObjects.Remove(listObjects[i]);
                    }
                }
            }
        }

        List<LogContent> Logs = new();

        public void Log(string logString, string stackTrace, LogType type){
            Logs.Add(new LogContent(){type = (LogsType)type, text = logString});
        }

        void Start(){
            playerInput = GetComponent<PlayerInput>();
            playerInput.actions = inputActionsAssets;
            playerInput.currentActionMap = inputActionsAssets.actionMaps[0];
            uIDocument = GetComponent<UIDocument>();
            uIDocument.visualTreeAsset = visualTreeAsset;
            uIDocument.panelSettings = panelSettings;

            uIDocument.rootVisualElement.Q<VisualElement>("DevTools").visible = false;
            uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible = false;
            uIDocument.rootVisualElement.Q<ScrollView>("Components").visible = false;
            uIDocument.rootVisualElement.Q<VisualElement>("BarTitleComponents").visible = false;
            uIDocument.rootVisualElement.Q<VisualElement>("Terminal").visible = false;
            uIDocument.rootVisualElement.Q<ScrollView>("Logs").verticalScrollerVisibility = ScrollerVisibility.Hidden;

            uIDocument.rootVisualElement.Q<Label>("title").text = $"{Application.productName} - {Application.companyName}";
            uIDocument.rootVisualElement.Q<Label>("api").text = $"API: {SystemInfo.graphicsDeviceType}";
            uIDocument.rootVisualElement.Q<Label>("gpu").text = $"GPU: {SystemInfo.graphicsDeviceName}";
            uIDocument.rootVisualElement.Q<Label>("platform").text = $"Platform: {Application.platform}";
            uIDocument.rootVisualElement.Q<Label>("version").text = $"Version: {Application.version}";
            uIDocument.rootVisualElement.Q<Label>("unityversion").text = $"Unity Version: {Application.unityVersion}";
            uIDocument.rootVisualElement.Q<VisualElement>("ListOptions").Add(new Button(ShowScenes){text = "Scenes"});
            uIDocument.rootVisualElement.Q<VisualElement>("ListOptions").Add(new Button(ShowGraphic){text = "Graphic"});
            uIDocument.rootVisualElement.Q<VisualElement>("ListOptions").Add(new Button(ShowResolutions){text = "Resolutions"});
            
            if(playerInput.currentActionMap != null)
                uIDocument.rootVisualElement.Q<Label>("Overlay-Label").text = "Press " + playerInput.currentActionMap.FindAction("DevTools").GetBindingDisplayString() +" to open/close DevTools." + (!DevTools.CurrentComponent.Equals(new Component()) ? "\nPress " + playerInput.currentActionMap.FindAction("Inspector").GetBindingDisplayString() + " to open/close current Inspector." : "") + "\nPress " + playerInput.currentActionMap.FindAction("Overlays").GetBindingDisplayString()  + " to show/hide Overlays. \nPress " + playerInput.currentActionMap.FindAction("Terminal").GetBindingDisplayString() + " to show/hide Terminal.";
            uIDocument.rootVisualElement.Q<VisualElement>("InspectorContent").Clear();
            uIDocument.rootVisualElement.Q<ScrollView>("Components").Clear();
            uIDocument.rootVisualElement.Q<ScrollView>("ListObjects").Clear();
            uIDocument.rootVisualElement.Q<ScrollView>("Logs").Clear();
            uIDocument.rootVisualElement.Q<Label>("Overlay-Label").text = "Press F1 to show/hide Terminal.\n" + "Press F2 to open/close DevTools." + "\nPress F3 to show/hide Overlays." + (!DevTools.CurrentComponent.Equals(new Component()) ? "\nPress F4 to open/close current Inspector." : "");


            DevTools.AddCommand("Devtools", "prefix", (string[] cmd) => {ShowPrefix();}, "Shows all command prefix.");
            DevTools.AddCommand("Devtools", "clear", (string[] cmd) => {ClearTerminal();}, "Clear all Terminal logs.");
            DevTools.AddCommand("Devtools", "quit", (string[] cmd) => {Application.Quit();}, "Quit game.");
        }

        void ClearTerminal(){
            if(uIDocument.rootVisualElement != null && uIDocument.rootVisualElement.Q<ScrollView>("Logs") is var Terminal && Terminal != null)
                Terminal.Clear();
        }

        void ShowPrefix(){
            if(DevTools.ListCommandLine.Count > 0){
                string text = "============ Available Prefix ============\n";
                var list = DevTools.ListCommandLine.GroupBy(item => item.Item1);
                for(int i = 0; i < list.Count(); i++)
                    text += list.ElementAt(i).Key + "\n";
                text += "=====================================\n";
                Logs.Add(new LogContent(){text = text, type = LogsType.Result});
            }else{
                Logs.Add(new LogContent(){text = "No prefix available!", type = LogsType.Result});
            }
        }

        void ShowGraphic(){
            SelectedObject = gameObject;

            var root = uIDocument.rootVisualElement.Q<ScrollView>("Components");
            root.Clear();

            string[] names = QualitySettings.names;
            root.Add(new Label(){text = "Quality: " + (name.Length > 0 ? names[QualitySettings.GetQualityLevel()] : "None")});

            for(int i = 0; i < names.Length; i++){
                int index = i;
                root.Add(new Button(()=>{
                    QualitySettings.SetQualityLevel(index, true);
                    ShowGraphic();
                }){text = names[i]});
            }


            root.Add(new SliderInt(){name = "TargetFrameRate", label = "FrameRate: " + Application.targetFrameRate, lowValue = -1, highValue = 500, value = Application.targetFrameRate});
            root.Q<SliderInt>("TargetFrameRate").RegisterCallback<ChangeEvent<int>>((evt) => {Application.targetFrameRate = evt.newValue; ((SliderInt)evt.currentTarget).label = $"FrameRate: {Application.targetFrameRate}";});
            
            
            root.Add(new SliderInt(){name = "vSyncCount", label = "VSync: " + QualitySettings.vSyncCount, lowValue = 0, highValue = 4, value = QualitySettings.vSyncCount});
            root.Q<SliderInt>("vSyncCount").RegisterCallback<ChangeEvent<int>>((evt) => {QualitySettings.vSyncCount = evt.newValue; ((SliderInt)evt.currentTarget).label = $"vSyncCount: {QualitySettings.vSyncCount}";});
           
           
            root.Add(new SliderInt(){name = "antiAliasing", label = "AntiAliasing: " + QualitySettings.antiAliasing, lowValue = 0, highValue = 4, value = QualitySettings.antiAliasing,});
            root.Q<SliderInt>("antiAliasing").RegisterCallback<ChangeEvent<int>>((evt) => {QualitySettings.antiAliasing = evt.newValue; ((SliderInt)evt.currentTarget).label = $"AntiAliasing: {QualitySettings.antiAliasing}";});
        
        
            root.Add(new EnumField("AnisotropicFiltering:", QualitySettings.anisotropicFiltering){name = "anisotropicFiltering"});
            root.Q<EnumField>("anisotropicFiltering").RegisterCallback<ChangeEvent<int>>((evt) => {QualitySettings.anisotropicFiltering = (AnisotropicFiltering)evt.newValue; ((EnumField)evt.currentTarget).label = $"AnisotropicFiltering: {QualitySettings.anisotropicFiltering}";});
        

            root.Add(new Toggle(){name = "enableLODCrossFade", text = "EnableLODCrossFade: ", value = QualitySettings.enableLODCrossFade});
            root.Q<Toggle>("enableLODCrossFade").RegisterCallback<ChangeEvent<bool>>((evt) => {QualitySettings.enableLODCrossFade = evt.newValue;});
        
            
            root.Add(new Slider(){name = "LodBias", label = "LodBias: " + QualitySettings.lodBias, lowValue = 0f, highValue = 2f, value = QualitySettings.lodBias});
            root.Q<Slider>("LodBias").RegisterCallback<ChangeEvent<float>>((evt) => {QualitySettings.lodBias = evt.newValue; ((Slider)evt.currentTarget).label = "LodBias: " + QualitySettings.lodBias;});


            root.Add(new SliderInt(){name = "maximumLODLevel", label = "MaximumLODLevel: " + QualitySettings.maximumLODLevel, lowValue = 0, highValue = 20, value = QualitySettings.maximumLODLevel,});
            root.Q<SliderInt>("maximumLODLevel").RegisterCallback<ChangeEvent<int>>((evt) => {QualitySettings.maximumLODLevel = evt.newValue; ((SliderInt)evt.currentTarget).label = $"MaximumLODLevel: {QualitySettings.maximumLODLevel}";});
        
            root.Add(new Slider(){name = "Shadow", label = "Shadow: " + QualitySettings.shadows, lowValue = 0f, highValue = 2f, value = (float)QualitySettings.shadows});
            root.Q<Slider>("Shadow").RegisterCallback<ChangeEvent<float>>((evt) => {QualitySettings.shadows = (ShadowQuality)evt.newValue; ((Slider)evt.currentTarget).label = "Shadow: " + QualitySettings.shadows;});


            root.Add(new Slider(){name = "ShadowDistance", label = "ShadowDistance: " + QualitySettings.shadowDistance, lowValue = 0f, highValue = 200f, value = (float)QualitySettings.shadowDistance});
            root.Q<Slider>("ShadowDistance").RegisterCallback<ChangeEvent<float>>((evt) => {QualitySettings.shadowDistance = evt.newValue; ((Slider)evt.currentTarget).label = "ShadowDistance: " + QualitySettings.shadowDistance;});

            root.Add(new Slider(){name = "ShadowResolution", label = "ShadowResolution: " + QualitySettings.shadowResolution, lowValue = 0f, highValue = 3f, value = (float)QualitySettings.shadowResolution});
            root.Q<Slider>("ShadowResolution").RegisterCallback<ChangeEvent<float>>((evt) => {QualitySettings.shadowResolution = (ShadowResolution)evt.newValue; ((Slider)evt.currentTarget).label = "ShadowResolution: " + QualitySettings.shadowResolution;});

            root.Add(new Toggle(){name = "SoftParticles", text = "SoftParticles: ", value = QualitySettings.softParticles});
            root.Q<Toggle>("SoftParticles").RegisterCallback<ChangeEvent<bool>>((evt) => {QualitySettings.softParticles = evt.newValue;});
        
            root.Add(new Toggle(){name = "SoftVegetation", text = "SoftVegetation: ", value = QualitySettings.softVegetation});
            root.Q<Toggle>("SoftVegetation").RegisterCallback<ChangeEvent<bool>>((evt) => {QualitySettings.softVegetation = evt.newValue;});
     
            root.Add(new Slider(){name = "TerrainDetailDensityScale", label = "TerrainDetailDensityScale: " + QualitySettings.terrainDetailDensityScale, lowValue = 0f, highValue = 5000f, value = QualitySettings.terrainDetailDensityScale});
            root.Q<Slider>("TerrainDetailDensityScale").RegisterCallback<ChangeEvent<float>>((evt) => {QualitySettings.terrainDetailDensityScale = evt.newValue; ((Slider)evt.currentTarget).label = "TerrainDetailDensityScale: " + QualitySettings.terrainDetailDensityScale;});

            root.Add(new Slider(){name = "TerrainDetailDistance", label = "TerrainDetailDistance: " + QualitySettings.terrainDetailDistance, lowValue = 0f, highValue = 5000f, value = QualitySettings.terrainDetailDistance});
            root.Q<Slider>("TerrainDetailDistance").RegisterCallback<ChangeEvent<float>>((evt) => {QualitySettings.terrainDetailDistance = evt.newValue; ((Slider)evt.currentTarget).label = "TerrainDetailDistance: " + QualitySettings.terrainDetailDistance;});

            root.Add(new Slider(){name = "TerrainTreeDistance", label = "TerrainTreeDistance: " + QualitySettings.terrainTreeDistance, lowValue = 0f, highValue = 5000f, value = QualitySettings.terrainTreeDistance});
            root.Q<Slider>("TerrainTreeDistance").RegisterCallback<ChangeEvent<float>>((evt) => {QualitySettings.terrainTreeDistance = evt.newValue; ((Slider)evt.currentTarget).label = "TerrainTreeDistance: " + QualitySettings.terrainTreeDistance;});

            root.Add(new Slider(){name = "TerrainMaxTrees", label = "TerrainMaxTrees: " + QualitySettings.terrainMaxTrees, lowValue = 0f, highValue = 50000f, value = QualitySettings.terrainMaxTrees});
            root.Q<Slider>("TerrainMaxTrees").RegisterCallback<ChangeEvent<float>>((evt) => {QualitySettings.terrainMaxTrees = evt.newValue; ((Slider)evt.currentTarget).label = "TerrainMaxTrees: " + QualitySettings.terrainMaxTrees;});

            root.visible = true;
            uIDocument.rootVisualElement.Q<VisualElement>("BarTitleComponents").visible = true;
        }


        void ShowScenes(){
            SelectedObject = gameObject;

            var components = uIDocument.rootVisualElement.Q<ScrollView>("Components");
            components.Clear();

            var regex = new Regex(@"([^/]*/)*([\w\d\-]*)\.unity");
            for(int i = 0; i < SceneManager.sceneCountInBuildSettings; i++){
                int index = i;
                components.Add(new Button(()=>{
                    SceneManager.LoadScene(index);
                    ShowScenes();
                }){text = regex.Replace(SceneUtility.GetScenePathByBuildIndex(i), "$2")});
            }


            components.visible = true;
            uIDocument.rootVisualElement.Q<VisualElement>("BarTitleComponents").visible = true;
        }

        void ShowResolutions(){
            SelectedObject = gameObject;

            var components = uIDocument.rootVisualElement.Q<ScrollView>("Components");
            components.Clear();

            components.Add(new Label(){text = "Resolution: " + Screen.width + "x" + Screen.height});

            Resolution[] resolutions = Screen.resolutions;
            for(int i = 0; i < resolutions.Length; i++){
                int index = i;
                components.Add(new Button(()=>{
                    Screen.SetResolution(resolutions[index].width, resolutions[index].height, Screen.fullScreenMode);
                    ShowResolutions();
                }){text = resolutions[i].width + "x" + resolutions[i].height});
            }

            components.visible = true;
            uIDocument.rootVisualElement.Q<VisualElement>("BarTitleComponents").visible = true;

        }

        void ShowComponents(){
            var components = uIDocument.rootVisualElement.Q<ScrollView>("Components");
            components.Clear();
            if(DevTools.ListGameObjects.First(item => item.gameObject == DevTools.SelectedObject) is var itemObject)
            foreach(var itemComponent in itemObject.Components){
                // check button exite and add or remove
                components.Add(new Button(()=>{
                    CurrentComponent = itemComponent;
                    ShowInspector(itemComponent);
                }){text = itemComponent.name});
            }

            components.visible = true;
            uIDocument.rootVisualElement.Q<VisualElement>("BarTitleComponents").visible = true;
        }

        int LogCount;
        void RenderLog(){
            if(uIDocument != null && uIDocument.rootVisualElement != null && uIDocument.rootVisualElement.Q<ScrollView>("Logs") is ScrollView Terminal){
                while(Logs.Count > LogCount){
                    if(Terminal.childCount >= 999)
                        Terminal.RemoveAt(0);

                    var Log = Logs.ElementAt(LogCount);
                    Label label = new Label(Log.type == LogsType.Result ? Log.text : (Log.type == LogsType.Success ? "<color=green>[OK] " : (Log.type == LogsType.Warning ? "<color=orange>[WARNING] " : (Log.type == LogsType.Error ? "<color=red>[ERROR] " : "<color=white>[DEBUG] "))) + Log.text + "</color>");
                    Terminal.Add(label);
                    LogCount++;
                }

                uIDocument.rootVisualElement.Q<Label>("Log-Count").text = Logs.Where(item => item.type == LogsType.Log).Count().ToString();
                uIDocument.rootVisualElement.Q<Label>("Log-Warning-Count").text = Logs.Where(item => item.type == LogsType.Warning).Count().ToString();
                uIDocument.rootVisualElement.Q<Label>("Log-Error-Count").text = Logs.Where(item => item.type == LogsType.Error).Count().ToString();
            }
        }

        void ShowInspector(Component devToolsComponent){
            uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible = true;
            DevTools.isOpenInspector = true;
            var root = uIDocument.rootVisualElement.Q<VisualElement>("InspectorContent");
            root.Clear();
            root.Add(devToolsComponent.templateContainer);
        }

        void FixedUpdate(){
            if(uIDocument.rootVisualElement != null){

                // Lock scroll
                if(uIDocument.rootVisualElement.Q<ScrollView>("Logs") is ScrollView Terminal && Terminal != null)
                    Terminal.verticalScroller.value = Terminal.verticalScroller.value > Terminal.verticalScroller.highValue % 20 || uIDocument.rootVisualElement.Q<ScrollView>("Logs").verticalScrollerVisibility == ScrollerVisibility.Hidden ? Terminal.verticalScroller.highValue : Terminal.verticalScroller.value;

                uIDocument.rootVisualElement.Q<Label>("fps").text = $"FPS : {fps} ({(fpsTimerCount * 1000).ToString("0.00")}ms)";

                if(totalReservedMemoryRecorder.Valid)
                    uIDocument.rootVisualElement.Q<Label>("memory").text = $"Total Reserved Memory: {BytesToString(totalReservedMemoryRecorder.LastValue)}";

                if(gcReservedMemoryRecorder.Valid)
                    uIDocument.rootVisualElement.Q<Label>("memorygc").text = $"GC Reserved Memory: {BytesToString(gcReservedMemoryRecorder.LastValue)}";

                if(gcReservedMemoryRecorder.Valid)
                    uIDocument.rootVisualElement.Q<Label>("memorysystem").text = $"System Used Memory: {BytesToString(systemUsedMemoryRecorder.LastValue)}";


                var listObjects = uIDocument.rootVisualElement.Q<ScrollView>("ListObjects");

                // update ListObjects registed, usage For loop because conflit in changed scene
                for(int i = 0; i < DevTools.ListGameObjects.Count; i++){
                    var itemObject = DevTools.ListGameObjects[i];
                    if(itemObject.id != -1 && !itemObject.gameObject){
                        listObjects.Remove(uIDocument.rootVisualElement.Q<Button>(itemObject.id.ToString()));
                        DevTools.ListGameObjects.Remove(itemObject);
                    }else{
                        if(listObjects.childCount == 0 || !listObjects.Children().Any(item => item.name == itemObject.id.ToString())){
                            listObjects.Add(new Button(()=>{SelectedObject = itemObject.gameObject; ShowComponents();}){name = itemObject.id.ToString(), text = itemObject.id != -1 ? itemObject.gameObject.name : "System"});
                        }else
                            if(uIDocument.rootVisualElement.Q<Button>(itemObject.id.ToString()) is var button)
                                button.text = itemObject.id != -1 ? itemObject.gameObject.name : "System";
                    }
                }
            }
        }

        ProfilerRecorder totalReservedMemoryRecorder, gcReservedMemoryRecorder, systemUsedMemoryRecorder;

        void StartCount(){
            totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
            gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            systemUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
        }

        void StopCount(){
            totalReservedMemoryRecorder.Dispose();
            gcReservedMemoryRecorder.Dispose();
            systemUsedMemoryRecorder.Dispose();
        }

        bool isOverlaysTmp = false, isInspectorTmp = false, isTerminalTmp = false;
        CursorLockMode cursorLockMode;
        int fps, fpsCount;
        float fpsTimerCount, fpsTimerTmp, timerFps;
        void Update(){
            // Renders
            RenderObjects();
            RenderLog();

            fpsTimerCount = Time.time - fpsTimerTmp;
            fpsTimerTmp = Time.time;
            
            if(timerFps < Time.time){
                timerFps = Time.time + 0.5f;
                fps = fpsCount * 2;
                fpsCount = 0;
                fpsTimerCount = Time.time - fpsTimerTmp;
            }else
                fpsCount++;

            // Command line Terminal
            if(playerInput.currentActionMap != null && playerInput.currentActionMap.FindAction("Enter").triggered){
                if(uIDocument.rootVisualElement.Q<TextField>("CommandLine") is var TextField && TextField != null && uIDocument.rootVisualElement.Q<VisualElement>("Terminal").visible){
                    if(TextField.value != ""){
                        TextField.value = TextField.value.ToLower();
                        string[] commands = GetWords(TextField.value);
                        switch(commands[0]){
                            case "commands":
                                if(DevTools.ListCommandLine.Count > 0){
                                    string text = "============== Available commands ==============\nPrefix:\t\t\tCommand:\t\t\tDescription:\n";
                                    var list = DevTools.ListCommandLine.OrderBy(item => item.Item2).OrderBy(item => item.Item1).ToList();
                                    for(int i = 0; i < list.Count; i++)
                                        text += list.ElementAt(i).Item2 +"\t\t\t" + (list.ElementAt(i).Item2.Length < 7 ? "\t" : "") + list.ElementAt(i).Item1 +"\t\t\t" + (list.ElementAt(i).Item1.Length < 7 ? "\t" : "") + list.ElementAt(i).Item4 + "\n";
                                    text += "=============================================";
                                    Logs.Add(new LogContent(){text = text, type = LogsType.Result});
                                }else{
                                    Logs.Add(new LogContent(){text = "No command available!", type = LogsType.Result});
                                }
                            break;
                            default:
                                if(commands.Length > 1 && DevTools.ListCommandLine.Any(item => item.Item2 == commands[0] && item.Item1 == commands[1])){
                                    DevTools.ListCommandLine.First(item => item.Item2 == commands[0] && item.Item1 == commands[1])?.Item3?.Invoke(commands.Length > 2 ? commands.Skip(2).ToArray() : new string[]{""});
                                    Logs.Add(new LogContent(){text = TextField.value, type = LogsType.Success});

                                }else
                                if(commands.Length >= 1 && DevTools.ListCommandLine.Any(item => item.Item1 == commands[0])){
                                    var list = DevTools.ListCommandLine.Where(item => item.Item1 == commands[0]);
                                    if(list.Count() == 1)
                                        DevTools.ListCommandLine.First(item => item.Item1 == commands[0])?.Item3?.Invoke(commands.Length > 1 ? commands.Skip(1).ToArray() : new string[]{""});
                                    else
                                        Logs.Add(new LogContent(){text = "There is more than 1 '" + commands[0] + "' command, use the prefix to specify the command.", type = LogsType.Warning});
                                }else
                                if(commands.Length == 1 && DevTools.ListCommandLine.Any(item => item.Item2 == commands[0])){
                                    // show commands from a Prefix
                                    var list = DevTools.ListCommandLine.Where(item => item.Item2 == commands[0]);
                                    string text = "============== Available commands ==============\nPrefix:\t\t\tCommand:\t\t\tDescription:\n";
                                    for(int i = 0; i < list.Count(); i++)
                                        text += list.ElementAt(i).Item2 +"\t\t\t" + (list.ElementAt(i).Item2.Length < 7 ? "\t" : "") + list.ElementAt(i).Item1 + "\t\t\t" + (list.ElementAt(i).Item1.Length < 7 ? "\t" : "") + list.ElementAt(i).Item4 + "\n";
                                    text += "=============================================";
                                    Logs.Add(new LogContent(){text = text, type = LogsType.Result});
                                }else
                                    Logs.Add(new LogContent(){text ="'" + TextField.value + "' is Invalid!, use 'commands' to see command lists!", type = LogsType.Result});
                            break;
                        }
                        TextField.value = "";
                    }
                }
            }

            // Open DevTools
            if(playerInput.currentActionMap != null && playerInput.currentActionMap.FindAction("DevTools").triggered){
                uIDocument.rootVisualElement.Q<VisualElement>("DevTools").visible = !uIDocument.rootVisualElement.Q<VisualElement>("DevTools").visible;
                DevTools.isOpenDevTools = uIDocument.rootVisualElement.Q<VisualElement>("DevTools").visible || uIDocument.rootVisualElement.Q<VisualElement>("Terminal").visible;
                
                if(DevTools.isOpenDevTools){
                    StartCount();
                    cursorLockMode = UnityEngine.Cursor.lockState;
                    isOverlaysTmp = DevTools.isOverlays;
                    DevTools.isOverlays = true;
                    isInspectorTmp = uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible;
                    isTerminalTmp = uIDocument.rootVisualElement.Q<VisualElement>("Terminal").visible;
                    uIDocument.rootVisualElement.Q<VisualElement>("Inspector").enabledSelf = true;
                    uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible = DevTools.isOpenInspector;
                    uIDocument.rootVisualElement.Q<VisualElement>("Terminal").visible = DevTools.isOpenTerminal;
                    uIDocument.rootVisualElement.Q<Label>("Overlay-Label").text = "";
                }else{
                    StopCount();
                    UnityEngine.Cursor.lockState = cursorLockMode;
                    DevTools.isOverlays = isOverlaysTmp;
                    uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible = isInspectorTmp;
                    uIDocument.rootVisualElement.Q<VisualElement>("Inspector").enabledSelf = false;
                    uIDocument.rootVisualElement.Q<ScrollView>("Components").visible = false;
                    uIDocument.rootVisualElement.Q<VisualElement>("Terminal").visible = isTerminalTmp;
                    uIDocument.rootVisualElement.Q<VisualElement>("BarTitleComponents").visible = false;
                    uIDocument.rootVisualElement.Q<Label>("Overlay-Label").text = "Press F1 to show/hide Terminal.\n" + "Press F2 to open/close DevTools." + "\nPress F3 to show/hide Overlays." + (!DevTools.CurrentComponent.Equals(new Component()) ? "\nPress F4 to open/close current Inspector." : "");
                }
                SelectedObject = null;
            }

            if(playerInput.currentActionMap != null && playerInput.currentActionMap.FindAction("Terminal").triggered && uIDocument.rootVisualElement.Q<VisualElement>("Terminal") is var Terminal && Terminal != null){
                Terminal.visible = !Terminal.visible;
                DevTools.isOpenTerminal = Terminal.visible;
                DevTools.isOpenDevTools = uIDocument.rootVisualElement.Q<VisualElement>("DevTools").visible || uIDocument.rootVisualElement.Q<VisualElement>("Terminal").visible;

                if(Terminal.visible){
                    uIDocument.rootVisualElement.Q<TextField>("CommandLine").Focus();
                    Terminal.style.height = Length.Percent(50);
                }else
                    Terminal.style.height = Length.Percent(25);

                if(Terminal.visible)
                    uIDocument.rootVisualElement.Q<ScrollView>("Logs").verticalScrollerVisibility = ScrollerVisibility.Auto;
                else
                    uIDocument.rootVisualElement.Q<ScrollView>("Logs").verticalScrollerVisibility = ScrollerVisibility.Hidden;
            }

            if(DevTools.isOpenTerminal)
                uIDocument.rootVisualElement.Q<TextField>("CommandLine").Focus();

            if(playerInput.currentActionMap != null && playerInput.currentActionMap.FindAction("Inspector").triggered){
                uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible = !uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible;
            }

            if(playerInput.currentActionMap != null && playerInput.currentActionMap.FindAction("Overlays").triggered)
                DevTools.isOverlays = !DevTools.isOverlays;

            if(DevTools.isOpenDevTools)
                UnityEngine.Cursor.lockState = CursorLockMode.None;
        }

        private static string[] GetWords(string text){
            List<string> lstreturn = new List<string>();
            List<string> lst = text.Split(new[]{' '}).ToList();
            foreach(string str in lst){
                lstreturn.Add(str.Replace(" ", ""));
            }

            if(lstreturn.Count == 0)
                lstreturn.Add(text);
            return lstreturn.ToArray();
        }

        static GUIStyle style = new GUIStyle();
        void RenderText(string text, Vector3 target, Color color, Texture2D texture2D, Vector2 positionOff = new Vector2()){
            var position = Camera.main.WorldToScreenPoint(target);
            var textSize = GUI.skin.label.CalcSize(new GUIContent(text));
            style.normal.textColor = color;
            style.normal.background = texture2D;
            style.alignment = TextAnchor.MiddleCenter;

            if(position.z > 0)  // Hide label off border camera
                GUI.Label(new Rect(position.x - (textSize.x + 10) / 2 + positionOff.x, Screen.height - position.y +  positionOff.y, textSize.x + 10, textSize.y), text, style);
        }

        void OnGUI(){
            if(Debug.isDebugBuild){
                // Draw text objects
                if(DevTools.isOverlays)
                foreach(var item in DevTools.ListGameObjects)
                    if(item.gameObject)
                        RenderText(item.gameObject.name, item.gameObject.transform.position, Color.white, Texture2D.grayTexture);
                
                // Renders
                DrawText();
            }
        }

        void RenderObjects(){
            for(int i = 0; i < DevTools.ListObjectsData.Count; i++){
                var objectData = DevTools.ListObjectsData[i];

                if(DevTools.isOverlays){
                    objectData.color.a = Mathf.Clamp(objectData.opacity, 0f, 1f);
                    materialPropertyBlock.SetColor("_Color", objectData.color);
                    
                    switch(objectData.objectType){
                        case ObjectType.Sphere:
                            Graphics.RenderMesh(renderParams, Sphere, 0, Matrix4x4.TRS(objectData.position, Quaternion.identity, Vector3.one * objectData.radius * 2));
                        break;
                        case ObjectType.Cube:
                            Graphics.RenderMesh(renderParams, Cube, 0, Matrix4x4.TRS(objectData.position, objectData.rotation, objectData.scale * 2));
                        break;
                        case ObjectType.Capsule:
                            Graphics.RenderMesh(renderParams, Capsule, 0, Matrix4x4.TRS(objectData.position, objectData.rotation, Vector3.one * objectData.radius + Vector3.up * objectData.height * 2));
                        break;
                        case ObjectType.Cylinder:
                            Graphics.RenderMesh(renderParams, Cylinder, 0, Matrix4x4.TRS(objectData.position, objectData.rotation, Vector3.one * objectData.radius + Vector3.up * objectData.height * 2));
                        break;
                        case ObjectType.Line:
                            Vector3 point = objectData.position - objectData.position2;
                            float distance = Vector3.Distance(objectData.position, objectData.position2);
                            Graphics.RenderMesh(renderParams, Cube, 0, Matrix4x4.TRS(objectData.position2 + point.normalized * (distance / 2), Quaternion.LookRotation(point, Vector3.up), new Vector3(objectData.radius, objectData.radius, distance)));
                        break;
                    }
                }

                if(objectData.timer <= Time.time)
                    DevTools.ListObjectsData.RemoveAt(i);
            }
        }        

        void DrawText(){
            for(int i = 0; i < DevTools.ListTextData.Count; i++){
                var textData = DevTools.ListTextData[i];

                if(DevTools.isOverlays)
                    RenderText(textData.text, textData.position, textData.color, textData.texture2D, textData.positionOff);

                if(textData.timer < Time.time)
                    DevTools.ListTextData.RemoveAt(i);
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