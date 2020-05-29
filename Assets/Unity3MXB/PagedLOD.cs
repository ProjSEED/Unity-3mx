using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3MXB
{
    public class RawMesh
    {
        public List<Vector3> Vertices;
        public List<Vector2> UVList;
        public List<Vector3> Normals;
        public List<int> Triangles;
        public Vector3 BBMin;
        public Vector3 BBMax;
    }

    public class RawTexture
    {
        public int Width;
        public int Height;
        public byte[] ImgData;
    }

    public class RawTexMesh
    {
        public RawMesh Mesh;
        public RawTexture Texture;
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

        public List<RawTexMesh> TexMeshs = new List<RawTexMesh>();
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
            Unstaged = 0,   // StagedChildren.Count = 0         , CommitedChildren.Count = 0
            Staging,        // StagedChildren.Count = Unknown   , CommitedChildren.Count = 0
            Staged,         // StagedChildren.Count = Known     , CommitedChildren.Count = 0
            Commited        // StagedChildren.Count = 0         , CommitedChildren.Count = Known
        };

        private string dir;
        private GameObject Go;  // one node could contains more than one mesh, use this GameObject as a group, insert each mesh to a child GameObject

        public Vector3 BBMin;
        public Vector3 BBMax;
        public TileBoundingSphere BoundingSphere;
        public float MaxScreenDiameter;

        public ChildrenStatus childrenStatus;      // pass to thread, atomic
        public List<string> ChildrenFiles;          // pass to thread
        public List<RawPagedLOD> StagedChildren;    // pass to thread

        public List<PagedLOD> CommitedChildren;

        public int FrameNumberOfLastTraversal;

        public PagedLOD(string name, Transform parent, string dir)
        {
            this.dir = dir;

            this.Go = new GameObject();
            this.Go.name = name;
            this.Go.transform.SetParent(parent, false);
            this.Go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            this.childrenStatus = ChildrenStatus.Unstaged;
            this.StagedChildren = new List<RawPagedLOD>();

            this.CommitedChildren = new List<PagedLOD>();

            this.FrameNumberOfLastTraversal = -1;
        }

        public void AddMeshTexture(List<RawTexMesh> rawMeshs)
        {
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            foreach (RawTexMesh rawMesh in rawMeshs)
            {
                GameObject goSingleMesh = new GameObject();
                goSingleMesh.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                goSingleMesh.transform.SetParent(this.Go.transform, false);
                
                UnityEngine.Mesh um = new UnityEngine.Mesh();
                um.vertices = rawMesh.Mesh.Vertices.ToArray();
                um.triangles = rawMesh.Mesh.Triangles.ToArray();
                if(rawMesh.Mesh.UVList != null)
                {
                    um.uv = rawMesh.Mesh.UVList.ToArray();
                }
                if (rawMesh.Mesh.Normals != null)
                {
                    um.normals = rawMesh.Mesh.Normals.ToArray();
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
                if(rawMesh.Texture != null)
                {
                    Texture2D texture;
                    texture = new Texture2D(rawMesh.Texture.Width, rawMesh.Texture.Height, TextureFormat.RGB24, false);
                    texture.LoadRawTextureData(rawMesh.Texture.ImgData);
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Repeat;
                    // After we conduct the Apply(), then we can make the texture non-readable and never create a CPU copy
                    texture.Apply(true, true);

                    mr.material.SetTexture("_MainTex", texture);
                }
            }
            //sw.Stop();
            //UnityEngine.Debug.Log(string.Format("AddMeshTexture: {0} ms", sw.ElapsedMilliseconds));
        }

        private void EnableRenderer(bool enabled)
        {
            // TODO: support change material
            foreach(MeshRenderer mr in this.Go.GetComponentsInChildren<MeshRenderer>())
            {
                if(enabled != mr.enabled)
                {
                    mr.enabled = enabled;
                }
            }
        }

        public bool UnloadChildren()
        {
            // recursively check every child's children's status to see if they are staging, if true, do NOT destory this child
            if(this.childrenStatus == ChildrenStatus.Unstaged)
            {
                return false;
            }
            else if(this.childrenStatus == ChildrenStatus.Staging)
            {
                return true;
            }
            else if(this.childrenStatus == ChildrenStatus.Staged)
            {
                this.StagedChildren.Clear();
                this.childrenStatus = ChildrenStatus.Unstaged;
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
                    hasStagingChid = hasStagingChid | child.UnloadChildren();
                }
            }
            if(hasStagingChid)
            {
                return true;
            }
            
            foreach (PagedLOD child in this.CommitedChildren)
            {
                foreach (MeshRenderer mr in child.Go.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.material.SetTexture("_MainTex", null);
                }
                GameObject.Destroy(child.Go);
            }
            this.CommitedChildren.Clear();
            this.childrenStatus = ChildrenStatus.Unstaged;
            return false;
        }

        public static void StageChildren(string dir, List<string> childrenFiles, List<RawPagedLOD> stagedChildren)
        {
            for (int j = 0; j < childrenFiles.Count; ++j)
            {
                string file = childrenFiles[j];
                Unity3MXBLoader loaderChild = new Unity3MXBLoader(dir);
                loaderChild.StagedChildren = stagedChildren;
                loaderChild.LoadStream(file);
            }
        }

        public void Traverse(int frameCount, CamState[] camStates, ref int loadCount)
        {
            if(camStates.Length == 0)
            {
                return;
            }
            this.FrameNumberOfLastTraversal = frameCount;

            // TODO: add cache

            // TODO: add shadow

            // TODO: add collider

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
                this.UnloadChildren();
                return;
            }

            // traverse based on screenDiameter
            if (screenDiameter < MaxScreenDiameter || this.ChildrenFiles.Count == 0)
            {
                this.EnableRenderer(true);
                this.UnloadChildren();
            }
            else
            {
                // commit
                if (this.childrenStatus == ChildrenStatus.Staged)
                {
                    this.EnableRenderer(true);
                    if (loadCount >= 5) // TODO: export 5 as a global option
                    {
                        return;
                    }
                    foreach (RawPagedLOD stagedChild in this.StagedChildren)
                    {
                        PagedLOD commitedChild = new PagedLOD(stagedChild.id, this.Go.transform, stagedChild.dir);
                        commitedChild.BBMin = stagedChild.BBMin;
                        commitedChild.BBMax = stagedChild.BBMax;
                        commitedChild.BoundingSphere = stagedChild.BoundingSphere;
                        commitedChild.MaxScreenDiameter = stagedChild.MaxScreenDiameter;
                        commitedChild.ChildrenFiles = stagedChild.ChildrenFiles;
                        commitedChild.AddMeshTexture(stagedChild.TexMeshs);
                        this.CommitedChildren.Add(commitedChild);
                    }
                    this.StagedChildren.Clear();
                    this.childrenStatus = ChildrenStatus.Commited;
                    ++loadCount;
                }
                // commited
                if (this.childrenStatus == ChildrenStatus.Commited)
                {
                    this.EnableRenderer(false);
                    foreach (PagedLOD pagedLOD in this.CommitedChildren)
                    {
                        pagedLOD.Traverse(Time.frameCount, camStates, ref loadCount);
                    }
                }
                else
                {
                    this.EnableRenderer(true);
                    if (this.childrenStatus == ChildrenStatus.Unstaged)
                    {
                        this.childrenStatus = ChildrenStatus.Staging;
                        PCQueue.Current.EnqueueItem(() =>
                        {
                            // stage
                            StageChildren(this.dir, this.ChildrenFiles, this.StagedChildren);
                            this.childrenStatus = ChildrenStatus.Staged;
                        });
                    }
                }
            }
        }
    }
}