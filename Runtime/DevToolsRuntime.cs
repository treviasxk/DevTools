using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static DevTools.DevToolsService;
namespace DevTools {
    public class DevToolsRuntime {
        public static List<DevToolsData> ListGameObjects = new();
        public static List<DrawTextData> ListTextData = new List<DrawTextData>();
        public static List<DrawObjectData> ListObjectsData = new List<DrawObjectData>();
        public static DevToolsComponent CurrentComponent;
        public static GameObject SelectedObject;
        public static bool isOpenDevTools = false;
        public static bool isOpenInspector = false;
        public static bool isOverlays = false;
        
        #if UNITY_EDITOR && UBuild
        [InitializeOnLoadMethod]
        static void Init() => UBuild.UBuildEditor.PackagePreConfigBuild.Add("com.treviasxk.devtools", UBuild.PreConfigBuild.Player | UBuild.PreConfigBuild.Development);
        #endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Clear(){
            ListTextData.Clear();
            ListObjectsData.Clear();
            ListGameObjects.Clear();
        }

        [RuntimeInitializeOnLoadMethod]
        static void Start(){
            SelectedObject = null;
            isOpenInspector = false;
            isOpenDevTools = false;
            isOverlays = false;
            CurrentComponent = new DevToolsComponent();
            
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Capsule = obj.GetComponent<MeshFilter>().mesh;
            Object.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Sphere = obj.GetComponent<MeshFilter>().mesh;
            Object.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Cube = obj.GetComponent<MeshFilter>().mesh;
            Object.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Cylinder = obj.GetComponent<MeshFilter>().mesh;
            Object.Destroy(obj);

            GameObject service = new GameObject("[DevTools]");
            service.AddComponent<DevToolsService>();
            service.AddComponent<PlayerInput>();
            service.AddComponent<UIDocument>();
            service.hideFlags = HideFlags.HideInHierarchy;
        }

        public static void Add(string name, GameObject gameObject, TemplateContainer templateContainer){
            if(!ListGameObjects.Any(item => item.gameObject == gameObject))
                ListGameObjects.Add(new (){id = gameObject ? gameObject.GetHashCode() : -1, gameObject = gameObject, Components = new(){{new DevToolsComponent(){id = gameObject ? gameObject.GetHashCode() : -1, name = name, templateContainer = templateContainer}}}});
            else{
                var itemObject = ListGameObjects.First(item => item.gameObject == gameObject);
                itemObject.Components.Add(new (){id = gameObject ? gameObject.GetHashCode() : -1, name = name, templateContainer = templateContainer});
            }
            return;
        }

        public static void Add(string name, TemplateContainer templateContainer) => Add(name, null, templateContainer);

        public static void DrawText(string text, Vector3 position, Color textColor, Texture2D backColor, Vector2 positionOff = new Vector2(), float timer = 0){
            ListTextData.Add(new DrawTextData{text = text, position = position, color = textColor, texture2D = backColor, positionOff = positionOff, timer = Time.time + timer});
        }
        
        public static void DrawText(string text, Vector3 position, Color textColor, Texture2D backColor, float timer = 0, Vector2 positionOff = new Vector2()){
            ListTextData.Add(new DrawTextData{text = text, position = position, color = textColor, texture2D = backColor, positionOff = positionOff, timer = Time.time + timer});
        }

        public static void DrawLine(Vector3 from, Vector3 to, Color color, float opacity = 1f, float Size = 0.0025f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Line, position = from, radius = Size, position2 = to, color = color, opacity = opacity, timer = Time.time + timer});
        }

        public static void DrawSphere(Vector3 position, float radius, Color color, float opacity = 0.5f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Sphere, position = position, radius = radius, color = color, opacity = opacity, timer = Time.time + timer});
        }

        public static void DrawCube(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float opacity = 0.5f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Cube, position = position, rotation = rotation, scale = scale, color = color, opacity = opacity, timer = Time.time + timer});
        }

        public static void DrawCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color, float opacity = 0.5f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Capsule, position = position, rotation = rotation, height = height, radius = radius, color = color, opacity = opacity, timer = Time.time + timer});
        }

        public static void DrawCylinder(Vector3 position, Quaternion rotation, float radius, float height, Color color, float opacity = 0.5f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Cylinder, position = position, rotation = rotation, height = height, radius = radius, color = color, opacity = opacity, timer = Time.time + timer});
        }
    }
}