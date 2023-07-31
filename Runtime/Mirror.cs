#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Kuanmi
{
    [ExecuteAlways]
    [ExecuteInEditMode]
    public class Mirror : MonoBehaviour
    {
        public Camera originalCamera;
        public Camera mirrorCamera;
        public Transform plane;

        private string currentView;
        private static readonly int MirrorID = Shader.PropertyToID("_Mirror");
        private readonly string mirrorShaderName = "Shader Graphs/Mirror";

        private void LateUpdate()
        {
            MirrorCam();
        }

#if UNITY_EDITOR
        void OnRenderObject()
        {
            MirrorCam();
        }

        [ContextMenu("Init")]
        public void Init()
        {
            // 让用户选择一个Assets中的文件夹
            var path = EditorUtility.OpenFolderPanel("选择保存资源的文件夹", Application.dataPath, "");

            // 如果用户取消选择，返回
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            path = path.Replace(Application.dataPath, "Assets");

            originalCamera = Camera.main;
            mirrorCamera = new GameObject("MirrorCamera").AddComponent<Camera>();
            mirrorCamera.transform.parent = transform;

            var rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGBFloat)
            {
                name = "MirrorRT"
            };

            var rtPath = $"{path}/MirrorRT.renderTexture";
            var matPath = $"{path}/Mirror.mat";

            rtPath = AssetDatabase.GenerateUniqueAssetPath(rtPath);
            matPath = AssetDatabase.GenerateUniqueAssetPath(matPath);

            AssetDatabase.CreateAsset(rt, rtPath);
            AssetDatabase.SaveAssets();

            mirrorCamera.targetTexture = rt;

            var shader = Shader.Find(mirrorShaderName);
            if (shader == null)
            {
                Debug.LogError("shader not found");
                return;
            }

            var mat = new Material(shader)
            {
                name = "MirrorMat"
            };

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.parent = transform;
            quad.name = "MirrorPlane";
            quad.transform.localPosition = new Vector3(0, 0, 0);
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = new Vector3(10, 10, 1);

            var mirrorRenderer = quad.GetComponent<Renderer>();
            mirrorRenderer.material = mat;
            mirrorRenderer.sharedMaterial.SetTexture(MirrorID, rt);

            plane = quad.transform;
        }
#endif


        void MirrorCam()
        {
            if (mirrorCamera == null)
                return;


            // mirrorCamera.gameObject.hideFlags = HideFlags.DontSave;
            Camera usedCamera = originalCamera;

#if UNITY_EDITOR
            var focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow == null)
                return;

            var type = focusedWindow.GetType().Name;
            switch (type)
            {
                case "SceneView":
                case "GameView":
                    currentView = type;
                    break;
            }

            if (Application.isPlaying == false && currentView == "SceneView")
            {
                var sceneView = SceneView.lastActiveSceneView;
                usedCamera = sceneView.camera;
            }
#endif

            if (usedCamera != null)
            {
                mirrorCamera.fieldOfView = usedCamera.fieldOfView;
                mirrorCamera.aspect = usedCamera.aspect;
                if (plane == null)
                {
                    plane = transform;
                }

                MirrorTransformForPlane(mirrorCamera.transform, usedCamera.transform, plane);
            }
        }

        private static Quaternion mirrorRot(Transform plane, Quaternion cam)
        {
            var forward = plane.forward;
            var reflect = Vector3.Reflect(cam * Vector3.forward, forward);
            var reflectUp = Vector3.Reflect(cam * Vector3.up, forward);

            return Quaternion.LookRotation(reflect, reflectUp);
        }

        private static Vector3 mirrorPos(Transform plane, Vector3 oldPos)
        {
            var forward = plane.forward;
            var planPos = plane.position;
            var dis = planPos - oldPos;
            var cross = Vector3.Cross(dis, forward).normalized;
            cross = Vector3.Cross(cross, forward);
            var refDis = Vector3.Reflect(dis, cross);
            return refDis + planPos;
        }

        private static void MirrorTransformForPlane(Transform mirrorTransform, Transform originTransform,
            Transform plane)
        {
            var originPosition = originTransform.position;
            var originRotation = originTransform.rotation;

            var mirrorPosition = mirrorPos(plane, originPosition);
            var mirrorRotation = mirrorRot(plane, originRotation);

            mirrorTransform.position = mirrorPosition;
            mirrorTransform.rotation = mirrorRotation;
        }
    }
}