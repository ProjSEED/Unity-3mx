using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3MXB
{
    public class PagedLODInfo : MonoBehaviour
    {
        
    }

    public class PagedLOD
    {
        private GameObject Go;
        private MeshFilter mf;
        private MeshRenderer mr;
        private PagedLODInfo info;

        public Vector3 BBMin;
        public Vector3 BBMax;
        public TileBoundingSphere BoundingSphere;
        public float MaxScreenDiameter;

        public List<string> Children;
        public List<string> LoadedChildren { get; set; }
        public Dictionary<string, PagedLOD> LoadedChildNode { get; set; }

        private string dir;

        public int FrameNumberOfLastTraversal;

        private bool Loaded;

        public PagedLOD(string name, Transform parent, string dir)
        {
            this.Go = new GameObject();
            this.Go.name = name;

            this.Go.transform.SetParent(parent, false);
            this.Go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            this.info = this.Go.AddComponent<PagedLODInfo>();
            this.mf = this.Go.AddComponent<MeshFilter>();
            this.mr = this.Go.AddComponent<MeshRenderer>();
            this.mr.enabled = false;

            this.FrameNumberOfLastTraversal = -1;
            this.dir = dir;

            this.LoadedChildNode = new Dictionary<string, PagedLOD>();
            this.LoadedChildren = new List<string>();

            this.Loaded = false;
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
            foreach (PagedLOD pagedLOD in this.LoadedChildNode.Values)
            {
                pagedLOD.Go.SetActive(false);
                pagedLOD.mr.material.SetTexture("_MainTex", null);
                GameObject.Destroy(pagedLOD.Go);
            }
            this.LoadedChildNode.Clear();
            this.LoadedChildren.Clear();
            this.Loaded = false;
        }

        public void Traverse(int frameCount, Vector4 pixelSizeVector, Plane[] planes)
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
            if (screenDiameter < MaxScreenDiameter || this.Children.Count == 0)
            {
                this.EnableRenderer(true);
                this.UnloadChildren();
            }
            else
            {
                if (this.LoadedChildren.Count == this.Children.Count)
                {
                    this.EnableRenderer(false);
                    foreach (PagedLOD pagedLOD in this.LoadedChildNode.Values)
                    {
                        pagedLOD.Traverse(Time.frameCount, pixelSizeVector, planes);
                    }
                }
                else
                {
                    this.EnableRenderer(true);
                    if (this.Loaded == false)
                    {
                        this.Loaded = true;
                        for (int j = 0; j < this.Children.Count; ++j)
                        {
                            string file = this.Children[j];
                            Unity3MXBLoader loaderChild = new Unity3MXBLoader(dir, this);
                            this.info.StartCoroutine(loaderChild.LoadStream(file));
                        }
                    }
                }
            }
        }
    }
}