using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static DevTools.DevToolsService;
namespace DevTools {
    public class DevToolsRuntime {
        public static List<DevToolsData> ListGameObjects = new();
        public static List<DrawLineData> ListLineData = new List<DrawLineData>();
        public static List<DrawTextData> ListTextData = new List<DrawTextData>();
        public static List<DrawShpereData> ListSphereData = new List<DrawShpereData>();
        public static List<DrawCubeData> ListCubeData = new List<DrawCubeData>();
        public static List<DrawCylinderData> ListCylinderData = new List<DrawCylinderData>();
        public static List<DrawCapsuleData> ListCapsuleData = new List<DrawCapsuleData>();
        public static DevToolsComponent CurrentComponent;
        public static GameObject SelectedObject;
        public static bool isOpenDevTools = false;
        public static bool isOpenInspector = false;
        public static bool isOverlays = false;
        
        #if UNITY_EDITOR && UBuild
        DevToolsRuntime() => UBuild.UBuildEditor.PackagePreConfigBuild.Add("com.treviasxk.devtools", UBuild.PreConfigBuild.Player | UBuild.PreConfigBuild.Development);
        #endif

        [RuntimeInitializeOnLoadMethod]
        static void Init(){
            SelectedObject = null;
            isOpenInspector = false;
            isOpenDevTools = false;
            isOverlays = false;
            CurrentComponent = new DevToolsComponent();
            
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Capsule = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Sphere = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Cube = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Cylinder = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            GameObject service = new GameObject("[DevTools]");
            service.AddComponent<DevToolsService>();
            service.AddComponent<PlayerInput>();
            service.AddComponent<UIDocument>();
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

        public static void DrawLine(Vector3 from, Vector3 to, Color color, float timer = 0){
            ListLineData.Add(new DrawLineData{from = from, to = to, color = color, timer = Time.time + timer});
        }

        public static void DrawText(string text, Vector3 position, Color textColor, Texture2D backColor, Vector2 positionOff = new Vector2(), float timer = 0){
            ListTextData.Add(new DrawTextData{text = text, position = position, color = textColor, texture2D = backColor, positionOff = positionOff, timer = Time.time + timer});
        }
        
        public static void DrawText(string text, Vector3 position, Color textColor, Texture2D backColor, float timer = 0, Vector2 positionOff = new Vector2()){
            ListTextData.Add(new DrawTextData{text = text, position = position, color = textColor, texture2D = backColor, positionOff = positionOff, timer = Time.time + timer});
        }

        public static void DrawSphere(Vector3 position, float radius, Color color, float timer = 0){
            ListSphereData.Add(new DrawShpereData{position = position, radius = radius, color = color, timer = Time.time + timer});
        }

        public static void DrawCube(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float timer = 0){
            ListCubeData.Add(new DrawCubeData{position = position, rotation = rotation, scale = scale, color = color, timer = Time.time + timer});
        }

        public static void DrawCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color, float timer = 0){
            ListCapsuleData.Add(new DrawCapsuleData{position = position, rotation = rotation, height = height, radius = radius, color = color, timer = Time.time + timer});
        }

        public static void DrawCylinder(Vector3 position, Quaternion rotation, float radius, float height, Color color, float timer = 0){
            ListCylinderData.Add(new DrawCylinderData{position = position, rotation = rotation, height = height, radius = radius, color = color, timer = Time.time + timer});
        }
    }
}