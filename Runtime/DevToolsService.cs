using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DevTools {
    public class DevToolsService : MonoBehaviour {
        public struct DevToolsData {
            public int id;
            public GameObject gameObject;
            public List<DevToolsComponent> Components;
        }

        public struct DevToolsComponent{
            public int id;
            public string name;
            public TemplateContainer templateContainer;
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


        public InputActionAsset inputActionsAssets;
        public VisualTreeAsset visualTreeAsset;
        public PanelSettings panelSettings;
        PlayerInput playerInput;
        UIDocument uIDocument;
        RenderParams renderParams;
        MaterialPropertyBlock materialPropertyBlock;
        public static Mesh Capsule, Sphere, Cube, Cylinder;
        public enum ObjectType {Capsule, Sphere, Cube, Cylinder, Line}
        void Awake(){
            SceneManager.sceneLoaded -= sceneLoaded;
            SceneManager.sceneLoaded += sceneLoaded;

            materialPropertyBlock = new MaterialPropertyBlock();
            renderParams = new RenderParams(new Material(Shader.Find("DevTools/Debug"))){matProps = materialPropertyBlock};
            //mat.enableInstancing = true;
            DontDestroyOnLoad(gameObject);
        }

        private void sceneLoaded(Scene arg0, LoadSceneMode arg1){
            var listObjects = DevToolsRuntime.ListGameObjects.Where(item => item.id == -1).ToArray();
            if(uIDocument.rootVisualElement != null){
                var root = uIDocument.rootVisualElement.Q<ScrollView>("ListObjects");
                for(int i = 0; i < listObjects.Length; i++){
                    if(listObjects[i].id == -1){
                        var itemObject = root.Q<Button>(listObjects[i].id.ToString());
                        if(itemObject != null)
                            root.Remove(itemObject);
                        DevToolsRuntime.ListGameObjects.Remove(listObjects[i]);
                    }
                }
            }
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
                uIDocument.rootVisualElement.Q<Label>("Overlay-Label").text = "Press " + playerInput.currentActionMap.FindAction("DevTools").GetBindingDisplayString() +" to open/close DevTools." + (!DevToolsRuntime.CurrentComponent.Equals(new DevToolsComponent()) ? "\nPress " + playerInput.currentActionMap.FindAction("Inspector").GetBindingDisplayString() + " to open/close current Inspector." : "") + "\nPress " + playerInput.currentActionMap.FindAction("Overlays").GetBindingDisplayString() + " to show/hide Overlays.";
            uIDocument.rootVisualElement.Q<VisualElement>("InspectorContent").Clear();
            uIDocument.rootVisualElement.Q<ScrollView>("Components").Clear();
            uIDocument.rootVisualElement.Q<ScrollView>("ListObjects").Clear();
        }


        void ShowGraphic(){
            DevToolsRuntime.SelectedObject = gameObject;

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
            DevToolsRuntime.SelectedObject = gameObject;

            var components = uIDocument.rootVisualElement.Q<ScrollView>("Components");
            components.Clear();

            var regex = new System.Text.RegularExpressions.Regex(@"([^/]*/)*([\w\d\-]*)\.unity");
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
            DevToolsRuntime.SelectedObject = gameObject;

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
            if(DevToolsRuntime.ListGameObjects.First(item => item.gameObject == DevToolsRuntime.SelectedObject) is var itemObject)
            foreach(var itemComponent in itemObject.Components){
                // check button exite and add or remove
                components.Add(new Button(()=>{
                    DevToolsRuntime.CurrentComponent = itemComponent;
                    ShowInspector(itemComponent);
                }){text = itemComponent.name});
            }

            components.visible = true;
            uIDocument.rootVisualElement.Q<VisualElement>("BarTitleComponents").visible = true;
        }

        void ShowInspector(DevToolsComponent devToolsComponent){
            uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible = true;
            DevToolsRuntime.isOpenInspector = true;
            var root = uIDocument.rootVisualElement.Q<VisualElement>("InspectorContent");
            root.Clear();
            root.Add(devToolsComponent.templateContainer);
        }

        void FixedUpdate(){
            if(uIDocument.rootVisualElement != null){
                uIDocument.rootVisualElement.Q<Label>("fps").text = $"FPS : {fps} ({(fpsTimerCount * 1000).ToString("0.00")}ms)";

                if(totalReservedMemoryRecorder.Valid)
                    uIDocument.rootVisualElement.Q<Label>("memory").text = $"Total Reserved Memory: {BytesToString(totalReservedMemoryRecorder.LastValue)}";

                if(gcReservedMemoryRecorder.Valid)
                    uIDocument.rootVisualElement.Q<Label>("memorygc").text = $"GC Reserved Memory: {BytesToString(gcReservedMemoryRecorder.LastValue)}";

                if(gcReservedMemoryRecorder.Valid)
                    uIDocument.rootVisualElement.Q<Label>("memorysystem").text = $"System Used Memory: {BytesToString(systemUsedMemoryRecorder.LastValue)}";


                var listObjects = uIDocument.rootVisualElement.Q<ScrollView>("ListObjects");

                // update ListObjects registed, usage For loop because conflit in changed scene
                for(int i = 0; i < DevToolsRuntime.ListGameObjects.Count; i++){
                    var itemObject = DevToolsRuntime.ListGameObjects[i];
                    if(itemObject.id != -1 && !itemObject.gameObject){
                        listObjects.Remove(uIDocument.rootVisualElement.Q<Button>(itemObject.id.ToString()));
                        DevToolsRuntime.ListGameObjects.Remove(itemObject);
                    }else{
                        if(listObjects.childCount == 0 || !listObjects.Children().Any(item => item.name == itemObject.id.ToString())){
                            listObjects.Add(new Button(()=>{DevToolsRuntime.SelectedObject = itemObject.gameObject; ShowComponents();}){name = itemObject.id.ToString(), text = itemObject.id != -1 ? itemObject.gameObject.name : "System"});
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

        bool isOverlaysTmp = false, isInspectorTmp = false;
        CursorLockMode cursorLockMode;
        int fps, fpsCount;
        float fpsTimerCount, fpsTimerTmp, timerFps;
        void Update(){
            // Renders
            RenderObjects();

            fpsTimerCount = Time.time - fpsTimerTmp;
            fpsTimerTmp = Time.time;
            
            if(timerFps < Time.time){
                timerFps = Time.time + 0.5f;
                fps = fpsCount * 2;
                fpsCount = 0;
                fpsTimerCount = Time.time - fpsTimerTmp;
            }else
                fpsCount++;

            

            if(playerInput.currentActionMap != null && playerInput.currentActionMap.FindAction("DevTools").triggered){
                uIDocument.rootVisualElement.Q<VisualElement>("DevTools").visible = !uIDocument.rootVisualElement.Q<VisualElement>("DevTools").visible;
                DevToolsRuntime.isOpenDevTools = uIDocument.rootVisualElement.Q<VisualElement>("DevTools").visible;
                
                if(DevToolsRuntime.isOpenDevTools){
                    StartCount();
                    cursorLockMode = UnityEngine.Cursor.lockState;
                    isOverlaysTmp = DevToolsRuntime.isOverlays;
                    DevToolsRuntime.isOverlays = true;
                    isInspectorTmp = uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible;
                    uIDocument.rootVisualElement.Q<VisualElement>("Inspector").enabledSelf = true;
                    uIDocument.rootVisualElement.Q<Label>("Overlay-Label").text = "";
                }else{
                    StopCount();
                    UnityEngine.Cursor.lockState = cursorLockMode;
                    DevToolsRuntime.isOverlays = isOverlaysTmp;
                    uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible = isInspectorTmp;
                    uIDocument.rootVisualElement.Q<VisualElement>("Inspector").enabledSelf = false;
                    uIDocument.rootVisualElement.Q<ScrollView>("Components").visible = false;
                    uIDocument.rootVisualElement.Q<VisualElement>("BarTitleComponents").visible = false;
                    uIDocument.rootVisualElement.Q<Label>("Overlay-Label").text = "Press F1 to open/close DevTools." + (!DevToolsRuntime.CurrentComponent.Equals(new DevToolsComponent()) ? "\nPress F2 to open/close current Inspector." : "") + "\nPress F3 to show/hide Overlays.";
                }
                DevToolsRuntime.SelectedObject = null;
            }

            if(playerInput.currentActionMap != null && playerInput.currentActionMap.FindAction("Inspector").triggered){
                uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible = !uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible;
                DevToolsRuntime.isOpenInspector = uIDocument.rootVisualElement.Q<VisualElement>("Inspector").visible;
            }

            if(playerInput.currentActionMap != null && playerInput.currentActionMap.FindAction("Overlays").triggered)
                DevToolsRuntime.isOverlays = !DevToolsRuntime.isOverlays;

            if(DevToolsRuntime.isOpenDevTools)
                UnityEngine.Cursor.lockState = CursorLockMode.None;
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
                if(DevToolsRuntime.isOverlays)
                foreach(var item in DevToolsRuntime.ListGameObjects)
                    if(item.gameObject)
                        RenderText(item.gameObject.name, item.gameObject.transform.position, Color.white, Texture2D.grayTexture);
                
                // Renders
                DrawText();
            }
        }

        void RenderObjects(){
            for(int i = 0; i < DevToolsRuntime.ListObjectsData.Count; i++){
                var objectData = DevToolsRuntime.ListObjectsData[i];

                if(DevToolsRuntime.isOverlays){
                    objectData.color.a = Mathf.Clamp(objectData.opacity, 0f, 1f);
                    materialPropertyBlock.SetColor("_Color", objectData.color);
                    
                    switch(objectData.objectType){
                        case ObjectType.Sphere:
                            Graphics.RenderMesh(renderParams, Sphere, 0, Matrix4x4.TRS(objectData.position, Quaternion.identity, Vector3.one * objectData.radius));
                        break;
                        case ObjectType.Cube:
                            Graphics.RenderMesh(renderParams, Cube, 0, Matrix4x4.TRS(objectData.position, objectData.rotation, objectData.scale));
                        break;
                        case ObjectType.Capsule:
                            Graphics.RenderMesh(renderParams, Capsule, 0, Matrix4x4.TRS(objectData.position, objectData.rotation, Vector3.one * objectData.radius + Vector3.up * objectData.height));
                        break;
                        case ObjectType.Cylinder:
                            Graphics.RenderMesh(renderParams, Cylinder, 0, Matrix4x4.TRS(objectData.position, objectData.rotation, Vector3.one * objectData.radius + Vector3.up * objectData.height));
                        break;
                        case ObjectType.Line:
                            Vector3 point = objectData.position - objectData.position2;
                            float distance = Vector3.Distance(objectData.position, objectData.position2);
                            Graphics.RenderMesh(renderParams, Cube, 0, Matrix4x4.TRS(objectData.position2 + point.normalized * (distance / 2), Quaternion.LookRotation(point, Vector3.up), new Vector3(objectData.radius, objectData.radius, distance)));
                        break;
                    }
                }

                if(objectData.timer <= Time.time)
                    DevToolsRuntime.ListObjectsData.RemoveAt(i);
            }
        }        

        void DrawText(){
            for(int i = 0; i < DevToolsRuntime.ListTextData.Count; i++){
                var textData = DevToolsRuntime.ListTextData[i];

                if(DevToolsRuntime.isOverlays)
                    RenderText(textData.text, textData.position, textData.color, textData.texture2D, textData.positionOff);

                if(textData.timer < Time.time)
                    DevToolsRuntime.ListTextData.RemoveAt(i);
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