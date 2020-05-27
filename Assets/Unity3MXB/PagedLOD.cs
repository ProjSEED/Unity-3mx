using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3MXB
{
    public class PagedLODBehaviour : MonoBehaviour
    {
        
    }

    public class RawPagedLOD
    {
        // mesh
        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector2> UVList = new List<Vector2>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<int> Triangles = new List<int>();
        public Vector3 BBMin = new Vector3();
        public Vector3 BBMax = new Vector3();

        // texture
        public List<byte> JpgData = new List<byte>();
    }

    public class PagedLOD
    {
        enum ChildrenStatus
        {
            Unstaged = 0,   // StagedChildren.Count = 0         , CommitedChildren.Count = 0
            Staging,        // StagedChildren.Count = Unknown   , CommitedChildren.Count = 0
            Staged,         // StagedChildren.Count = Known     , CommitedChildren.Count = 0
            Commited        // StagedChildren.Count = 0         , CommitedChildren.Count = Known
        };

        private string dir;
        private GameObject Go;
        private MeshFilter mf;
        private MeshRenderer mr;
        private PagedLODBehaviour behaviour;

        public Vector3 BBMin;
        public Vector3 BBMax;
        public TileBoundingSphere BoundingSphere;
        public float MaxScreenDiameter;

        public List<string> ChildrenFiles;
        public int LoadedChildrenFilesCount;

        public List<RawPagedLOD> StagedChildren;
        public List<PagedLOD> CommitedChildren;

        private ChildrenStatus childrenStatus;

        public int FrameNumberOfLastTraversal;

        public PagedLOD(string name, Transform parent, string dir)
        {
            this.dir = dir;

            this.Go = new GameObject();
            this.Go.name = name;
            this.Go.transform.SetParent(parent, false);
            this.Go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            this.behaviour = this.Go.AddComponent<PagedLODBehaviour>();
            this.mf = this.Go.AddComponent<MeshFilter>();
            this.mr = this.Go.AddComponent<MeshRenderer>();
            this.mr.enabled = false;

            this.LoadedChildrenFilesCount = 0;

            this.CommitedChildren = new List<PagedLOD>();
            this.StagedChildren = new List<RawPagedLOD>();
            this.childrenStatus = ChildrenStatus.Unstaged;

            this.FrameNumberOfLastTraversal = -1;
        }

        public Transform GetTransform()
        {
            return this.Go.transform;
        }

        public void SetMesh(Mesh mesh)
        {
            this.mf.mesh = mesh;
        }

        public void SetTexture(Texture2D texture)
        {
            this.mr.material.SetTexture("_MainTex", texture);
        }

        public void EnableRenderer(bool enabled)
        {
            if (enabled != this.mr.enabled)
            {
                this.mr.enabled = enabled;
            }
        }

        void UnloadChildren()
        {
            this.Go.SetActive(false);
            this.Go.SetActive(true);
            foreach (PagedLOD pagedLOD in this.CommitedChildren)
            {
                pagedLOD.Go.SetActive(false);
                pagedLOD.mr.material.SetTexture("_MainTex", null);
                GameObject.Destroy(pagedLOD.Go);
            }
            this.CommitedChildren.Clear();
            this.LoadedChildrenFilesCount = 0;
            this.childrenStatus = ChildrenStatus.Unstaged;
        }

        public void Traverse(int frameCount, Vector4 pixelSizeVector, Plane[] planes, ref int loadCount)
        {
            // TODO: add cache
            this.FrameNumberOfLastTraversal = frameCount;

            // TODO: add shadow

            // TODO: add collider

            // TODO: optimize run speed

            // cull by bounding sphere
            PlaneClipMask mask = this.BoundingSphere.IntersectPlanes(planes, PlaneClipMask.GetDefaultMask());
            if (mask.Intersection == IntersectionType.OUTSIDE)
            {
                this.EnableRenderer(false);
                this.UnloadChildren();
                return;
            }

            // traverse based on screenDiameter
            float screenDiameter = this.BoundingSphere.ScreenDiameter(pixelSizeVector);
            if (screenDiameter < MaxScreenDiameter || this.ChildrenFiles.Count == 0)
            {
                this.EnableRenderer(true);
                this.UnloadChildren();
            }
            else
            {
                if (this.LoadedChildrenFilesCount == this.ChildrenFiles.Count)
                {
                    this.EnableRenderer(false);
                    foreach (PagedLOD pagedLOD in this.CommitedChildren)
                    {
                        pagedLOD.Traverse(Time.frameCount, pixelSizeVector, planes, ref loadCount);
                    }
                }
                else
                {
                    this.EnableRenderer(true);
                    if (this.childrenStatus == ChildrenStatus.Unstaged)
                    {
                        if (loadCount >= 5)
                        {
                            return;
                        }
                        this.childrenStatus = ChildrenStatus.Staging;
                        for (int j = 0; j < this.ChildrenFiles.Count; ++j)
                        {
                            loadCount++;
                            string file = this.ChildrenFiles[j];
                            Unity3MXBLoader loaderChild = new Unity3MXBLoader(dir, this);
                            this.behaviour.StartCoroutine(loaderChild.LoadStream(file));
                        }
                    }
                }
            }
        }
    }
}