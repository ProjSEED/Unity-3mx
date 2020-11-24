using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RSG;

namespace Unity3MXB
{
    public class Unity3MXBComponent : MonoBehaviour
    {
        public string Url;
        public int MaximumLod = 1000;
        public Material MaterialOverride = null;
        public bool AddColliders = false;
        public bool ReceiveShadows = true;

        private float timeSinceLastCalled;

        private float delay = 1f;

        PagedLOD Root;

        private List<CamState> camStates;

        private List<Camera> cams;

        public LRUCache<PagedLOD> LRUCache = new LRUCache<PagedLOD>();

        private int stagingCount = 0;

        public void Start()
        {
            // TODO: support to set cameras
            camStates = new List<CamState>();
#if UNITY_EDITOR
            cams = UnityEditor.SceneView.GetAllSceneCameras().ToList();
#else
            cams = new List<Camera>();
#endif
            cams.Add(Camera.main);
            foreach (Camera cam in cams)
            {
                CamState camState = new CamState();
                Matrix4x4 cameraMatrix = cam.projectionMatrix * cam.worldToCameraMatrix * this.transform.localToWorldMatrix;
                camState.planes = GeometryUtility.CalculateFrustumPlanes(cameraMatrix);
                camState.pixelSizeVector = computePixelSizeVector(cam.scaledPixelWidth, cam.scaledPixelHeight, cam.projectionMatrix, cam.worldToCameraMatrix * this.transform.localToWorldMatrix);
                camStates.Add(camState);
            }

            // Download
            StartCoroutine(Download(null));
        }

        public void Update()
        {
            if(this.Root != null)
            {
                LRUCache.MarkAllUnused();
                for(int i = 0; i < cams.Count; ++i)
                {
                    Camera cam = cams[i];
                    CamState camState = camStates[i];
                    Matrix4x4 cameraMatrix = cam.projectionMatrix * cam.worldToCameraMatrix * this.transform.localToWorldMatrix;
                    camState.planes = GeometryUtility.CalculateFrustumPlanes(cameraMatrix);
                    camState.pixelSizeVector = computePixelSizeVector(cam.scaledPixelWidth, cam.scaledPixelHeight, cam.projectionMatrix, cam.worldToCameraMatrix * this.transform.localToWorldMatrix);
                }

                // All of our bounding boxes and tiles are using tileset coordinate frame so lets get our frustrum planes
                // in tileset frame.  This way we only need to transform our planes, not every bounding box we need to check against
                int loadCount = 0;
                this.Root.Traverse(Time.frameCount, camStates.ToArray(), ref loadCount, ref stagingCount);

                //if(loadCount > 0)
                //{
                //    UnityEngine.Debug.Log(string.Format("Need to load {0} files", loadCount));
                //}
                LRUCache.UnloadUnusedContent(this.MaximumLod, 0.2f, n => -n.Depth, t => t.UnloadChildren());
                //UnityEngine.Debug.Log(string.Format("Used {0} pagedLODs", LRUCache.Used));
                //UnityEngine.Debug.Log(string.Format("Staging {0} files", stagingCount));

                //timeSinceLastCalled += Time.deltaTime;
                //if (timeSinceLastCalled > delay)
                //{
                //    timeSinceLastCalled = 0f;
                //    Resources.UnloadUnusedAssets();
                //}
            }
        }

        public Vector4 computePixelSizeVector(int ScreenWidth, int ScreenHeight, Matrix4x4 P, Matrix4x4 M)
        {
            // pre adjust P00,P20,P23,P33 by multiplying them by the viewport window matrix.
            // here we do it in short hand with the knowledge of how the window matrix is formed
            // note P23,P33 are multiplied by an implicit 1 which would come from the window matrix.
            // Robert Osfield, June 2002.

            // scaling for horizontal pixels
            float P00 = P.m00 * ScreenWidth * 0.5f;
            float P20_00 = P.m02 * ScreenWidth * 0.5f + P.m32 * ScreenWidth * 0.5f;
            Vector3 scale_00 = new Vector3(M.m00*P00 + M.m20*P20_00,
                               M.m01*P00 + M.m21*P20_00,
                               M.m02*P00 + M.m22*P20_00);

            // scaling for vertical pixels
            float P10 = P.m11 * ScreenHeight * 0.5f;
            float P20_10 = P.m12 * ScreenHeight * 0.5f + P.m32 * ScreenHeight * 0.5f;
            Vector3 scale_10 = new Vector3(M.m10*P10 + M.m20*P20_10,
                               M.m11*P10 + M.m21*P20_10,
                               M.m12*P10 + M.m22*P20_10);

            float P23 = P.m32;
            float P33 = P.m33;
            Vector4 pixelSizeVector = new Vector4(M.m20*P23,
                                      M.m21*P23,
                                      M.m22*P23,
                                      M.m23*P23 + M.m33*P33);

            float scaleRatio = 0.7071067811f / Mathf.Sqrt(scale_00.sqrMagnitude + scale_10.sqrMagnitude);
            pixelSizeVector = pixelSizeVector * scaleRatio;

            return pixelSizeVector;
        }

    public IEnumerator Download(Promise<bool> loadComplete)
        {
            string url = UrlUtils.ReplaceDataProtocol(Url);
            string dir = UrlUtils.GetBaseUri(url);

            string file = UrlUtils.GetLastPathSegment(url);

            if (file.EndsWith(".3mxb", StringComparison.OrdinalIgnoreCase))
            {
                this.Root = new PagedLOD("root", dir, this.transform, 0);
                this.Root.unity3MXBComponent = this;
                this.Root.MaxScreenDiameter = 0;
                this.Root.BoundingSphere = new TileBoundingSphere(new Vector3(0, 0, 0), 1e30f);
                this.Root.ChildrenFiles = new List<string>();
                this.Root.ChildrenFiles.Add(file);
                yield return null;
            }         
        }
    }
}
