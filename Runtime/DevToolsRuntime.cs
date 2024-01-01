using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace DevTools {
    [InitializeOnLoad]
    public class DevToolsRuntime {
        public GameObject test;
        public static Dictionary<string, GUI.WindowFunction> ListWindows = new Dictionary<string, GUI.WindowFunction>();
        public static Dictionary<string, GameObject> ListGameObjects = new Dictionary<string, GameObject>();
        public static KeyValuePair<string, GUI.WindowFunction> CurrentWindow;
        public static GameObject SelectedObject;
        public static bool isOpenDevTools = false;
        public static bool isOpenDeveloperTools = false;
        public static bool isOverlays = false;
        static GUIStyle style = new GUIStyle();
        static Material mat;
        static Mesh Capsule, Sphere;

        static DevToolsRuntime(){
            #if UBuild
                UBuild.UBuildEditor.PackageConfigBuild.Add("com.treviasxk.devtools", UBuild.UBuildEditor.ConfigBuild.PlayerDevelopment);
            #endif
        }

        [RuntimeInitializeOnLoadMethod]
        static void InitRuntime(){
            SelectedObject = null;
            isOpenDeveloperTools = false;
            isOpenDevTools = false;
            isOverlays = false;
            ListWindows.Clear();
            ListGameObjects.Clear();
            CurrentWindow = new KeyValuePair<string, GUI.WindowFunction>();
            
            mat = new Material(Shader.Find("Hidden/Internal-Colored"));
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Capsule = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Sphere = obj.GetComponent<MeshFilter>().mesh;
            GameObject.Destroy(obj);

            GameObject service = new GameObject("[DevTools Service]");
            service.AddComponent<DevToolsService>();
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

        public class DrawData{
            public Transform transform;
            public Mesh mesh;
            public Color color;
        }

        public static void DrawSphere(Vector3 position, float radius, Color color){
            if(isOverlays){
                color.a = 0.5f;
                mat.color = color;
                Graphics.DrawMesh(Sphere, Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * radius * 2), mat, 0);
            }
        }

        public static void DrawCube(Vector3 position, float radius, Color color){
            if(isOverlays){
                color.a = 0.5f;
                mat.color = color;
                //Graphics.DrawMesh(Sphere, Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * radius * 2), mat, 0);
            }
        }


        public static void DrawCapsule(Vector3 start, Vector3 end, float radius, Color color){
            if(isOverlays){
                color.a = 0.5f;
                mat.color = color;
                //Graphics.DrawMesh(Capsule, Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * radius * 2), mat, 0);
            }
        }

        public static void DrawString(string text, Vector3 target, Color textColor, Texture2D backColor){
            var position = Camera.main.WorldToScreenPoint(target);
            var textSize = GUI.skin.label.CalcSize(new GUIContent(text));
            style.normal.textColor = textColor;
            style.normal.background = backColor;
            style.alignment = TextAnchor.MiddleCenter;
            if(position.z > 0)
                GUI.Label(new Rect(position.x - (textSize.x + 10) / 2, Screen.height - position.y, textSize.x + 10, textSize.y), text, style);
        }
    }
}