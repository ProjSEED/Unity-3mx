using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3MXB
{
    public struct RawMesh
    {
        public Vector3[] Vertices;
        public Vector2[] UVList;
        public Vector3[] Normals;
        public int[] Triangles;
        public Vector3 BBMin;
        public Vector3 BBMax;
    }

    public struct RawTexture
    {
        public int Width;
        public int Height;
        public byte[] ImgData;
    }

    public struct RawTexMesh
    {
        public RawMesh Mesh;
        public RawTexture Texture;
    }

    public struct RawPointCloud
    {
        public Vector3[] Vertices;
        public Color[] Colors;
        public Vector3 BBMin;
        public Vector3 BBMax;
    }

    public class RawPagedLOD
    {
        public string dir;
        public string id;

        public Vector3 BBMin;
        public Vector3 BBMax;
        public TileBoundingSphere BoundingSphere;
        public float MaxScreenDiameter;

        public List<string> ChildrenFiles;

        public bool IsPointCloud = false;
        public List<RawTexMesh> TexMeshs = new List<RawTexMesh>();
        public List<RawPointCloud> PointClouds = new List<RawPointCloud>();
    }

    public class CamState
    {
        public Vector4 pixelSizeVector;
        public Plane[] planes;
    }

    public class PagedLOD
    {
        public enum ChildrenStatus
        {
            Unstaged = 0,   // CommitedChildren.Count = 0
            Staging,        // CommitedChildren.Count = Unknown
            Staged,         // CommitedChildren.Count = Known
            Commited        // CommitedChildren.Count = Known
        };

        public Unity3MXBComponent unity3MXBComponent = null;

        public string dir;
        private GameObject Go;  // one node could contains more than one mesh, use this GameObject as a group, insert each mesh to a child GameObject
        private bool HasColliders = false;
        public bool IsPointCloud = false;

        public Vector3 BBMin;
        public Vector3 BBMax;
        public TileBoundingSphere BoundingSphere;
        public float MaxScreenDiameter;

        public ChildrenStatus childrenStatus;      // pass to thread, atomic
        public List<string> ChildrenFiles;          // pass to thread

        public List<PagedLOD> CommitedChildren;

        public int FrameNumberOfLastTraversal;

        public int Depth;

        public PagedLOD(string name, string dir, PagedLOD parent)
        {
            this.dir = dir;

            this.Go = new GameObject();
            this.Go.name = name;
            this.Go.transform.SetParent(parent.Go.transform, false);
            //this.Go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            this.childrenStatus = ChildrenStatus.Unstaged;

            this.CommitedChildren = new List<PagedLOD>();

            this.FrameNumberOfLastTraversal = -1;

            this.Depth = parent.Depth + 1;
        }

        public PagedLOD(string name, string dir, Transform parent, int depth)
        {
            this.dir = dir;

            this.Go = new GameObject();
            this.Go.name = name;
            this.Go.transform.SetParent(parent, false);
            //this.Go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            this.childrenStatus = ChildrenStatus.Unstaged;

            this.CommitedChildren = new List<PagedLOD>();

            this.FrameNumberOfLastTraversal = -1;

            this.Depth = depth;
        }

        public void AddTextureMesh(RawTexMesh rawMesh)
        {
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            GameObject goSingleMesh = new GameObject();
            //goSingleMesh.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            goSingleMesh.transform.SetParent(this.Go.transform, false);

            UnityEngine.Mesh um = new UnityEngine.Mesh();
            um.vertices = rawMesh.Mesh.Vertices;
            um.triangles = rawMesh.Mesh.Triangles;
            if (rawMesh.Mesh.UVList != null)
            {
                um.uv = rawMesh.Mesh.UVList;
            }
            if (rawMesh.Mesh.Normals != null)
            {
                um.normals = rawMesh.Mesh.Normals;
            }
            else
            {
                um.RecalculateNormals();
            }
            um.bounds.SetMinMax(rawMesh.Mesh.BBMin, rawMesh.Mesh.BBMax);

            MeshFilter mf = goSingleMesh.AddComponent<MeshFilter>();
            mf.mesh = um;

            MeshRenderer mr = goSingleMesh.AddComponent<MeshRenderer>();
            mr.enabled = false;
            if (rawMesh.Texture.ImgData != null)
            {
                //Texture2D texture = Texture2D.whiteTexture;
                Texture2D texture = new Texture2D(rawMesh.Texture.Width, rawMesh.Texture.Height, TextureFormat.RGB24, false);
                texture.LoadRawTextureData(rawMesh.Texture.ImgData);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                // After we conduct the Apply(), then we can make the texture non-readable and never create a CPU copy
                texture.Apply(true, true);
                mr.material.SetTexture("_MainTex", texture);
            }
            //sw.Stop();
            //UnityEngine.Debug.Log(string.Format("AddTextureMeshs: {0} ms", sw.ElapsedMilliseconds));
        }

        public void AddPointCloud(RawPointCloud rawPointCloud)
        {
            GameObject goSingleMesh = new GameObject();
            //goSingleMesh.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            goSingleMesh.transform.SetParent(this.Go.transform, false);

            UnityEngine.Mesh um = new UnityEngine.Mesh();
            um.vertices = rawPointCloud.Vertices;
            um.colors = rawPointCloud.Colors;

            um.bounds.SetMinMax(rawPointCloud.BBMin, rawPointCloud.BBMax);

            MeshFilter mf = goSingleMesh.AddComponent<MeshFilter>();
            mf.mesh = um;

            MeshRenderer mr = goSingleMesh.AddComponent<MeshRenderer>();
            mr.enabled = false;

            int[] indecies = new int[rawPointCloud.Vertices.Length];
            for (int i = 0; i < rawPointCloud.Vertices.Length; ++i)
            {
                indecies[i] = i;
            }

            um.SetIndices(indecies, MeshTopology.Points, 0);
        }

        private void EnableRenderer(bool enabled)
        {
            foreach(MeshRenderer mr in this.Go.GetComponentsInChildren<MeshRenderer>())
            {
                if(enabled != mr.enabled)
                {
                    mr.enabled = enabled;
                }
                if(mr.enabled == true)
                {
                    if ((this.unity3MXBComponent.MaterialOverride != null) && (mr.sharedMaterial != this.unity3MXBComponent.MaterialOverride))
                    {
                        mr.sharedMaterial = this.unity3MXBComponent.MaterialOverride;
                    }
                    if (mr.receiveShadows != this.unity3MXBComponent.ReceiveShadows)
                    {
                        mr.receiveShadows = this.unity3MXBComponent.ReceiveShadows;
                    }
                    if (this.unity3MXBComponent.AddColliders)
                    {
                        if(this.HasColliders == false)
                        {
                            MeshCollider collider = mr.gameObject.AddComponent<MeshCollider>();
                            collider.sharedMesh = mr.gameObject.GetComponent<MeshFilter>().mesh;
                            this.HasColliders = true;
                        }
                    }
                    else
                    {
                        if(this.HasColliders)
                        {
                            GameObject.Destroy(mr.gameObject.GetComponent<MeshCollider>());
                            this.HasColliders = false;
                        }
                    }
                }
            }
        }

        bool MarkStagingChildren(ref int stagingCount)
        {
            if(stagingCount == 0)
            {
                return false;
            }
            // recursively check every child's children's status to see if they are staging, if true, do NOT destory this child
            if (this.childrenStatus == ChildrenStatus.Unstaged)
            {
                return false;
            }
            else if (this.childrenStatus == ChildrenStatus.Staging)
            {
                this.unity3MXBComponent.LRUCache.MarkUsed(this);
                return true;
            }
            else if (this.childrenStatus == ChildrenStatus.Staged)
            {
                this.childrenStatus = ChildrenStatus.Unstaged;
                --stagingCount;
                return false;
            }

            bool hasStagingChid = false;
            foreach (PagedLOD child in this.CommitedChildren)
            {
                if (child.childrenStatus == ChildrenStatus.Staging)
                {
                    hasStagingChid = true;
                }
                else
                {
                    hasStagingChid = hasStagingChid | child.MarkStagingChildren(ref stagingCount);
                }
            }
            if(hasStagingChid)
            {
                this.unity3MXBComponent.LRUCache.MarkUsed(this);
            }
            return hasStagingChid;
        }

        public void UnloadChildren()
        {       
            foreach (PagedLOD child in this.CommitedChildren)
            {
                GameObject.Destroy(child.Go);
            }
            this.CommitedChildren.Clear();
            this.childrenStatus = ChildrenStatus.Unstaged;
        }

        public void Traverse(int frameCount, CamState[] camStates, ref int loadCount, ref int stagingCount)
        {
            if(camStates.Length == 0)
            {
                return;
            }
            this.FrameNumberOfLastTraversal = frameCount;

            // TODO: optimize run speed

            // cull by bounding sphere
            bool isInSide = false;
            float screenDiameter = 0;
            foreach (CamState camState in camStates)
            {
                PlaneClipMask mask = this.BoundingSphere.IntersectPlanes(camState.planes, PlaneClipMask.GetDefaultMask());
                if (mask.Intersection != IntersectionType.OUTSIDE)
                {
                    isInSide = true;
                    screenDiameter = Mathf.Max(screenDiameter, this.BoundingSphere.ScreenDiameter(camState.pixelSizeVector));
                }
            }
            if (isInSide == false)
            {
                this.EnableRenderer(false);
                MarkStagingChildren(ref stagingCount);
                return;
            }

            // traverse based on screenDiameter
            if (screenDiameter < MaxScreenDiameter || this.ChildrenFiles.Count == 0)
            {
                this.EnableRenderer(true);
                MarkStagingChildren(ref stagingCount);
            }
            else
            {
                // commit
                if (this.childrenStatus == ChildrenStatus.Staged)
                {
                    this.EnableRenderer(true);
                    this.childrenStatus = ChildrenStatus.Commited;
                    this.unity3MXBComponent.LRUCache.Add(this);
                    --stagingCount;
                    ++loadCount;
                }
                // commited
                if (this.childrenStatus == ChildrenStatus.Commited)
                {
                    this.EnableRenderer(false);
                    this.unity3MXBComponent.LRUCache.MarkUsed(this);
                    foreach (PagedLOD pagedLOD in this.CommitedChildren)
                    {
                        pagedLOD.Traverse(Time.frameCount, camStates, ref loadCount, ref stagingCount);
                    }
                }
                else
                {
                    this.EnableRenderer(true);
                    if (this.childrenStatus == ChildrenStatus.Unstaged)
                    {
                        this.childrenStatus = ChildrenStatus.Staging;
                        ++stagingCount;
                        StageTask stageTask = new StageTask(this);
                        PCQueue.Current.EnqueueItem(stageTask);
                    }
                }
            }
        }
    }
}