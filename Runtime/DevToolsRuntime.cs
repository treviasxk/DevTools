using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DevTools {
    public class DevToolsRuntime {
        public static Dictionary<string, GUI.WindowFunction> ListWindows = new Dictionary<string, GUI.WindowFunction>();
        public static Dictionary<string, GameObject> ListGameObjects = new Dictionary<string, GameObject>();
        public static List<DrawLineData> ListLineData = new List<DrawLineData>();
        public static List<DrawTextData> ListTextData = new List<DrawTextData>();
        public static List<DrawShpereData> ListSphereData = new List<DrawShpereData>();
        public static List<DrawCubeData> ListCubeData = new List<DrawCubeData>();
        public static List<DrawCylinderData> ListCylinderData = new List<DrawCylinderData>();
        public static List<DrawCapsuleData> ListCapsuleData = new List<DrawCapsuleData>();
        public static KeyValuePair<string, GUI.WindowFunction> CurrentWindow;
        public static GameObject SelectedObject;
        public static bool isOpenDevTools = false;
        public static bool isOpenDeveloperTools = false;
        public static bool isOverlays = false;
        
        #if UNITY_EDITOR && UBuild
        [InitializeOnLoadMethod]
        static void Init() => UBuild.UBuildEditor.PackagePreConfigBuild.Add("com.treviasxk.devtools", UBuild.PreConfigBuild.Player | UBuild.PreConfigBuild.Development);
        #endif

        [RuntimeInitializeOnLoadMethod]
        static void InitRuntime(){
            SelectedObject = null;
            isOpenDeveloperTools = false;
            isOpenDevTools = false;
            isOverlays = false;
            ListWindows.Clear();
            ListGameObjects.Clear();
            CurrentWindow = new KeyValuePair<string, GUI.WindowFunction>();
            
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            DevToolsService.Capsule = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DevToolsService.Sphere = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DevToolsService.Cube = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DevToolsService.Cylinder = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            GameObject service = new GameObject("[DevTools Service]");
            service.AddComponent<DevToolsService>();
            service.AddComponent<PlayerInput>();
        }


        public static void Add(string name, GameObject gameObject, GUI.WindowFunction windowFunction){
            int x = ListWindows.Count;
            for(int i = 0; i <= x; i++){
                var key = name + " #" + i;
                if(!ListWindows.ContainsKey(key)){
                    ListWindows.Add(key, windowFunction);
                    ListGameObjects.Add(key, gameObject);
                    return;
                }
            }
        }

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