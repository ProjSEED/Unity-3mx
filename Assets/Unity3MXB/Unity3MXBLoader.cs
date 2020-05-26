using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unity3MXB
{
    public class Unity3MXBLoader : Loader.ILoader
    {
        public PagedLOD parent { get; private set; }

        private Loader.ILoader loader;

        private string dir;

        public Unity3MXBLoader(string dir, PagedLOD parent)
        {
            this.loader = Loader.AbstractWebRequestLoader.CreateDefaultRequestLoader(dir); //.glb, .gltf
            this.dir = dir;
            this.parent = parent;
            _textureCache = new Dictionary<string, Texture2D>();
            _meshCache = new Dictionary<string, Mesh>();
            _meshTextureIdCache = new Dictionary<string, string>();
        }

        public Stream LoadedStream { get; private set; }

        protected Dictionary<string, Texture2D> _textureCache { get; set; }

        protected Dictionary<string, Mesh> _meshCache { get; set; }

        protected Dictionary<string, string> _meshTextureIdCache { get; set; }

        public void Dispose()
        {
            _textureCache = null;
            _meshCache = null;
            _meshTextureIdCache = null;
        }

        protected void ConstructImage(string id, BinaryReader br, int size, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Repeat, bool markGpuOnly = true)
        {
            if (_textureCache.ContainsKey(id) == false)
            {
                byte[] data = br.ReadBytes(size);

                Texture2D texture;

                {
                    texture = new Texture2D(0, 0);
                    //	NOTE: the second parameter of LoadImage() marks non-readable, but we can't mark it until after we call Apply()
                    texture.LoadImage(data, false);
                }
                texture.filterMode = filterMode;
                texture.wrapMode = wrapMode;

                // After we conduct the Apply(), then we can make the texture non-readable and never create a CPU copy
                texture.Apply(true, markGpuOnly);
                _textureCache.Add(id, texture);
            }
        }

        protected void ConstructMesh(string id, BinaryReader br, int size, Vector3 bbMin, Vector3 bbMax)
        {
            if(_meshCache.ContainsKey(id) == false)
            {
                OpenCTM.CtmFileReader reader = new OpenCTM.CtmFileReader(br.BaseStream);
                OpenCTM.Mesh mesh = reader.decode();

                UnityEngine.Mesh um = new UnityEngine.Mesh();

                mesh.checkIntegrity();

                {
                    List<Vector3> Vertices = new List<Vector3>();
                    for (int j = 0; j < mesh.getVertexCount(); j++)
                        Vertices.Add(new Vector3(mesh.vertices[(j * 3)], mesh.vertices[(j * 3) + 2], mesh.vertices[(j * 3) + 1]));
                    um.vertices = Vertices.ToArray();
                }

                {
                    List<int> Triangles = new List<int>();
                    for (int j = 0; j < mesh.indices.Length / 3; j++)
                    {
                        Triangles.Add(mesh.indices[(j * 3)]);
                        Triangles.Add(mesh.indices[(j * 3) + 2]);
                        Triangles.Add(mesh.indices[(j * 3) + 1]);
                    }
                    um.triangles = Triangles.ToArray();
                }

                if(mesh.getUVCount() > 0)
                {
                    List<Vector2> UVList = new List<Vector2>();
                    for (int j = 0; j < mesh.texcoordinates[0].values.Length / 2; j++)
                        UVList.Add(new Vector2(mesh.texcoordinates[0].values[(j * 2)], mesh.texcoordinates[0].values[(j * 2) + 1]));
                    um.uv = UVList.ToArray();
                }

                if (mesh.hasNormals())
                {
                    List<Vector3> Normals = new List<Vector3>();
                    for (int j = 0; j < mesh.getVertexCount(); j++)
                        Normals.Add(new Vector3(mesh.normals[(j * 3)], mesh.normals[(j * 3) + 2], mesh.normals[(j * 3) + 1]));
                    um.normals = Normals.ToArray();
                }
                else
                {
                    um.RecalculateNormals();
                }

                um.bounds.SetMinMax(bbMin, bbMax);

                _meshCache.Add(id, um);
            }
        }

        public IEnumerator LoadStream(string relativeFilePath)
        {
            yield return this.loader.LoadStream(relativeFilePath);

            if (this.loader.LoadedStream.Length == 0)
            {
                LoadedStream = new MemoryStream(0);
            }
            else
            {
                // We need to read the header info off of the .3mxb file
                // Using statment will ensure this.loader.LoadedStream is disposed
                using (BinaryReader br = new BinaryReader(this.loader.LoadedStream))
                {
                    // Remove query parameters if there are any
                    string filename = relativeFilePath.Split('?')[0];
			            const int magicNumberLen = 5;
                    // magic number
                    string magicNumber = new String(br.ReadChars((int)magicNumberLen));
                    if (magicNumber != "3MXBO")
                    {
                        Debug.LogError("Unsupported magic number in 3mxb file: " + magicNumber + " " + relativeFilePath);
                    }

                    // header size
                    UInt32 headerSize = br.ReadUInt32();
                    if (headerSize == 0)
                    {
                        Debug.LogError("Unexpected zero length header in 3mxb file: " + relativeFilePath);
                    }

                    // header
                    string headerJson = new String(br.ReadChars((int)headerSize));
                    Schema.Header3MXB header3MXB = JsonConvert.DeserializeObject<Schema.Header3MXB>(headerJson);

                    // resources
                    for(int i = 0; i < header3MXB.Resources.Count; ++i)
                    {
                        Schema.Resource resource = header3MXB.Resources[i];
			            if (resource.Type == "textureBuffer" && resource.Format == "jpg")
                        {
                            ConstructImage(resource.Id, br, resource.Size);
                        }
                        else if (resource.Type == "geometryBuffer" && resource.Format == "ctm")
                        {
                            ConstructMesh(resource.Id, br, resource.Size, 
                                new Vector3(resource.BBMin[0], resource.BBMin[2], resource.BBMin[1]),
                                new Vector3(resource.BBMax[0], resource.BBMax[2], resource.BBMax[1]));

                            _meshTextureIdCache.Add(resource.Id, resource.Texture);
                        }
                        else
                        {
                            Debug.LogError("Unexpected buffer type in 3mxb file: " + relativeFilePath);
                        }
                    }

                    // nodes
                    for (int i = 0; i < header3MXB.Nodes.Count; ++i)
                    {
                        string url = UrlUtils.ReplaceDataProtocol(this.dir + relativeFilePath);
                        string childDir = UrlUtils.GetBaseUri(url);

                        Schema.Node node= header3MXB.Nodes[i];
                        PagedLOD pagedLOD = new PagedLOD(node.Id, this.parent.GetTransform(), childDir);
                        pagedLOD.BBMin = new Vector3(node.BBMin[0], node.BBMin[2], node.BBMin[1]);
                        pagedLOD.BBMax = new Vector3(node.BBMax[0], node.BBMax[2], node.BBMax[1]);
                        pagedLOD.BoundingSphere = new TileBoundingSphere((pagedLOD.BBMax + pagedLOD.BBMin) / 2, (pagedLOD.BBMax - pagedLOD.BBMin).magnitude / 2);
                        pagedLOD.MaxScreenDiameter = node.MaxScreenDiameter;
                        pagedLOD.Children = node.Children;

                        for (int j = 0; j < node.Resources.Count; ++j)
                        {
                            UnityEngine.Mesh um;
                            if(_meshCache.TryGetValue(node.Resources[j], out um))
                            {
                                pagedLOD.SetMesh(um);
                                string textureId;
                                if(_meshTextureIdCache.TryGetValue(node.Resources[j], out textureId))
                                {
                                    Texture2D texture;
                                    if(_textureCache.TryGetValue(textureId, out texture))
                                    {
                                        pagedLOD.SetTexture(texture);
                                    }
                                }
                            }
                        }
                        this.parent.LoadedChildNode.Add(node.Id, pagedLOD);
                    }

                    this.parent.LoadedChildren.Add(relativeFilePath);
                }
            }
            Dispose();
        }
    }
}