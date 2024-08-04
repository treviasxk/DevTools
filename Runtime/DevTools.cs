using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static DevTools.DevToolsService;
namespace DevTools {
    public struct Component{
        public int id;
        public string name;
        public TemplateContainer templateContainer;
    }

    [BurstCompile]
    public class DevTools {
        public static List<DevToolsData> ListGameObjects = new();
        public static List<DrawTextData> ListTextData = new List<DrawTextData>();
        public static List<DrawObjectData> ListObjectsData = new List<DrawObjectData>();
        public static ConcurrentDictionary<string, System.Action> ListCommandLine = new();
        public static Component CurrentComponent {get{return DevToolsService.CurrentComponent;}}
        public static GameObject SelectedObject {get{return DevToolsService.SelectedObject;}}
        public static bool isOpenDevTools {get;set;} = false;
        public static bool isOpenInspector {get;set;} = false;
        public static bool isOpenConsole {get;set;} = false;
        public static bool isOverlays {get;set;} = false;
        
        #if UNITY_EDITOR && UBuild
        [InitializeOnLoadMethod]
        static void Init() => UBuild.UBuildEditor.PackagePreConfigBuild.Add("com.treviasxk.devtools", UBuild.PreConfigBuild.Player | UBuild.PreConfigBuild.Development);
        #endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Clear(){
            ListCommandLine.Clear();
            ListTextData.Clear();
            ListObjectsData.Clear();
            ListGameObjects.Clear();
        }

        [RuntimeInitializeOnLoadMethod]
        static void Start(){
            DevToolsService.SelectedObject = null;
            DevToolsService.CurrentComponent = new Component();
            isOpenInspector = false;
            isOpenConsole = false;
            isOpenDevTools = false;
            isOverlays = false;

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


        /// <summary>
        /// Create a devtools component, when the component is selected, the interface is created with Unity's UI Toolkit.
        /// If the GameObject is destroyed at runtime, this component will also be destroyed.
        /// </summary>
        /// <param name="name">Component name.</param>
        /// <param name="gameObject">Bind gameobject to component.</param>
        /// <param name="templateContainer">UI Toolkit to be displayed in inspector.</param>
        public static void AddComponent(string name, TemplateContainer templateContainer, GameObject gameObject){
            if(!ListGameObjects.Any(item => item.gameObject == gameObject))
                ListGameObjects.Add(new(){ id = gameObject ? gameObject.GetHashCode() : -1, gameObject = gameObject, Components = new(){{new Component(){ id = gameObject ? gameObject.GetHashCode() : -1, name = name, templateContainer = templateContainer}}}});
            else{
                var itemObject = ListGameObjects.First(item => item.gameObject == gameObject);
                itemObject.Components.Add(new (){id = gameObject ? gameObject.GetHashCode() : -1, name = name, templateContainer = templateContainer});
            }
        }

        /// <summary>
        /// Create a devtools component, when the component is selected, the interface is created with Unity's UI Toolkit.
        /// </summary>
        /// <param name="name">Component name.</param>
        /// <param name="templateContainer">UI Toolkit to be displayed in inspector.</param>
        public static void AddComponent(string name, TemplateContainer templateContainer) => AddComponent(name, templateContainer, null);

        /// <summary>
        /// Create commands to be executed when entered into the console.
        /// </summary>
        /// <param name="command">Command Line</param>
        /// <param name="action">Action to be executed</param>
        public static void AddCommandLine(string command, System.Action action) => ListCommandLine.TryAdd(command, action);

        /// <summary>
        /// Remove an existing command from the console
        /// </summary>
        /// <param name="command">Command Line</param>
        public static void RemoveCommandLine(string command) => ListCommandLine.TryRemove(command, out _);

        /// <summary>   
        /// Add an overlay Label to your game world.
        /// </summary>
        /// <param name="text">Text Label.</param>
        /// <param name="position">Set World Position.</param>
        /// <param name="textColor">Text Color.</param>
        /// <param name="backColor">Background Color.</param>
        /// <param name="offset">Position Offset.</param>
        /// <param name="timer">Time to destroy the text. (If the value is 0, the text will only appear in 1 frame.)</param>
        public static void DrawText(string text, Vector3 position, Color textColor, Texture2D backColor, Vector2 offset = new Vector2(), float timer = 0){
            ListTextData.Add(new DrawTextData{text = text, position = position, color = textColor, texture2D = backColor, positionOff = offset, timer = Time.time + timer});
        }
        
        /// <summary>
        /// Add an overlay Line to your game world.
        /// </summary>
        /// <param name="from">World position of the first point.</param>
        /// <param name="to">world position of the second point.</param>
        /// <param name="color">Line Color.</param>
        /// <param name="opacity">Opacity value between 0f and 1f.</param>
        /// <param name="Size">Line Size.</param>
        /// <param name="timer">Time to destroy the line. (If the value is 0, the text will only appear in 1 frame.)</param>
        public static void DrawLine(Vector3 from, Vector3 to, Color color, float opacity = 1f, float Size = 0.0025f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Line, position = from, radius = Size, position2 = to, color = color, opacity = opacity, timer = Time.time + timer});
        }


        /// <summary>
        /// Add an overlay Sphere to your game world.
        /// </summary>
        /// <param name="position">World position of the Sphere.</param>
        /// <param name="radius">Radius of the Sphere.</param>
        /// <param name="color">Sphere Color.</param>
        /// <param name="opacity">Opacity value between 0f and 1f.</param>
        /// <param name="timer">Time to destroy the line. (If the value is 0, the text will only appear in 1 frame.)</param>
        public static void DrawSphere(Vector3 position, float radius, Color color, float opacity = 0.5f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Sphere, position = position, radius = radius, color = color, opacity = opacity, timer = Time.time + timer});
        }


        /// <summary>
        /// Add an overlay Cube to your game world.
        /// </summary>
        /// <param name="position">World position of the Cube.</param>
        /// <param name="rotation">World rotation of the Cube.</param>
        /// <param name="scale">Cube Scale</param>
        /// <param name="color">Cube Color</param>
        /// <param name="opacity">Opacity value between 0f and 1f.</param>
        /// <param name="timer">Time to destroy the line. (If the value is 0, the text will only appear in 1 frame.)</param>
        public static void DrawCube(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float opacity = 0.5f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Cube, position = position, rotation = rotation, scale = scale, color = color, opacity = opacity, timer = Time.time + timer});
        }

        /// <summary>
        /// Add an overlay Capsule to your game world.
        /// </summary>
        /// <param name="position">World position of the Capsule.</param>
        /// <param name="rotation">World rotation of the Capsule.</param>
        /// <param name="radius">Radius of the Capsule.</param>
        /// <param name="height">Height of the Capsule.</param>
        /// <param name="color">Capsule Color</param>
        /// <param name="opacity">Opacity value between 0f and 1f.</param>
        /// <param name="timer">Time to destroy the line. (If the value is 0, the text will only appear in 1 frame.)</param>
        public static void DrawCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color, float opacity = 0.5f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Capsule, position = position, rotation = rotation, height = height, radius = radius, color = color, opacity = opacity, timer = Time.time + timer});
        }

        /// <summary>
        /// Add an overlay Cylinder to your game world.
        /// </summary>
        /// <param name="position">World position of the Cylinder.</param>
        /// <param name="rotation">World rotation of the Cylinder.</param>
        /// <param name="radius">Radius of the Cylinder.</param>
        /// <param name="height">Height of the Cylinder.</param>
        /// <param name="color">Cylinder Color</param>
        /// <param name="opacity">Opacity value between 0f and 1f.</param>
        /// <param name="timer">Time to destroy the line. (If the value is 0, the text will only appear in 1 frame.)</param>
        public static void DrawCylinder(Vector3 position, Quaternion rotation, float radius, float height, Color color, float opacity = 0.5f, float timer = 0){
            ListObjectsData.Add(new DrawObjectData{objectType = ObjectType.Cylinder, position = position, rotation = rotation, height = height, radius = radius, color = color, opacity = opacity, timer = Time.time + timer});
        }
    }
}