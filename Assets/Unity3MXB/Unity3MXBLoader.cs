//#define DEBUG_TIME

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KeyJ;

namespace Unity3MXB
{
    public class Unity3MXBLoader : Loader.ILoader
    {
        public List<RawPagedLOD> StagedChildren;

        private Loader.ILoader loader;

        private string dir;

        public Unity3MXBLoader(string dir)
        {
            this.loader = Loader.AbstractWebRequestLoader.CreateDefaultRequestLoader(dir);
            this.dir = dir;
            _rawTextureCache = new Dictionary<string, RawTexture>();
            _rawMeshCache = new Dictionary<string, RawMesh>();
            _rawPointCloudCache = new Dictionary<string, RawPointCloud>();
            _meshTextureIdCache = new Dictionary<string, string>();
        }

        public Stream LoadedStream { get; private set; }

        protected Dictionary<string, RawTexture> _rawTextureCache { get; set; }

        protected Dictionary<string, RawMesh> _rawMeshCache { get; set; }

        protected Dictionary<string, RawPointCloud> _rawPointCloudCache { get; set; }

        protected Dictionary<string, string> _meshTextureIdCache { get; set; }

        public void Dispose()
        {
            _rawTextureCache = null;
            _rawMeshCache = null;
            _meshTextureIdCache = null;
        }

        protected void ConstructRawTexture(string id, BinaryReader br, int size)
        {
            if (_rawTextureCache.ContainsKey(id) == false)
            {
                byte[] data = br.ReadBytes(size);
                NanoJPEG nanoJPEG = new NanoJPEG();
                nanoJPEG.njDecode(data);
                byte[] rawPixels = nanoJPEG.njGetImage();

                if(rawPixels != null && rawPixels.Length > 0 && rawPixels.Length == nanoJPEG.njGetWidth() * nanoJPEG.njGetHeight() * 3)
                {
                    RawTexture texture = new RawTexture();
                    texture.ImgData = rawPixels;
                    texture.Width = nanoJPEG.njGetWidth();
                    texture.Height = nanoJPEG.njGetHeight();
                    _rawTextureCache.Add(id, texture);
                }
            }
        }

        protected void ConstructRawMesh(string id, BinaryReader br, int size, Vector3 bbMin, Vector3 bbMax)
        {
            // TODO: solve the issue that OpenCTM does NOT support multi-threading
            lock (PCQueue.Current.LockerForUser)
            if (_rawMeshCache.ContainsKey(id) == false)
            {
                OpenCTM.CtmFileReader reader = new OpenCTM.CtmFileReader(br.BaseStream);
                OpenCTM.Mesh mesh = reader.decode();

                RawMesh rawMesh = new RawMesh();

                mesh.checkIntegrity();

                {
                    List<Vector3> Vertices = new List<Vector3>();
                    for (int j = 0; j < mesh.getVertexCount(); j++)
                        Vertices.Add(new Vector3(mesh.vertices[(j * 3)], mesh.vertices[(j * 3) + 2], mesh.vertices[(j * 3) + 1]));
                    rawMesh.Vertices = Vertices.ToArray();
                }

                {
                    List<int> Triangles = new List<int>();
                    for (int j = 0; j < mesh.indices.Length / 3; j++)
                    {
                        Triangles.Add(mesh.indices[(j * 3)]);
                        Triangles.Add(mesh.indices[(j * 3) + 2]);
                        Triangles.Add(mesh.indices[(j * 3) + 1]);
                    }
                    rawMesh.Triangles = Triangles.ToArray();
                    }

                if (mesh.getUVCount() > 0)
                {
                    List<Vector2> UVList = new List<Vector2>();
                    for (int j = 0; j < mesh.texcoordinates[0].values.Length / 2; j++)
                        UVList.Add(new Vector2(mesh.texcoordinates[0].values[(j * 2)], 1 - mesh.texcoordinates[0].values[(j * 2) + 1]));
                    rawMesh.UVList = UVList.ToArray();
                    }

                if (mesh.hasNormals())
                {
                    List<Vector3> Normals = new List<Vector3>();
                    for (int j = 0; j < mesh.getVertexCount(); j++)
                        Normals.Add(new Vector3(mesh.normals[(j * 3)], mesh.normals[(j * 3) + 2], mesh.normals[(j * 3) + 1]));
                    rawMesh.Normals = Normals.ToArray();
                    }
                rawMesh.BBMin = bbMin;
                rawMesh.BBMax = bbMax;

                _rawMeshCache.Add(id, rawMesh);
            }
        }

        protected void ConstructRawPointCloud(string id, BinaryReader br, int size, Vector3 bbMin, Vector3 bbMax)
        {
            // TODO: pointcloud
            if (_rawPointCloudCache.ContainsKey(id) == false)
            {
                RawPointCloud rawPointCloud = new RawPointCloud();
                Int32 vertNum = br.ReadInt32();
                byte[] vertData = br.ReadBytes(vertNum * 3 * 4);
                byte[] colorData = br.ReadBytes(vertNum * 4);
                List<Vector3> Vertices = new List<Vector3>();
                List<Color> Colors = new List<Color>();
                for (int i = 0; i < vertNum; ++i)
                {
                    Vector3 vert;
                    vert.x = BitConverter.ToSingle(vertData, i * 3 * 4);
                    vert.y = BitConverter.ToSingle(vertData, i * 3 * 4 + 4);
                    vert.z = BitConverter.ToSingle(vertData, i * 3 * 4 + 8);
                    Vertices.Add(vert);
                    Color color;
                    color.r = colorData[i * 4] / 255.0f;
                    color.g = colorData[i * 4 + 1] / 255.0f;
                    color.b = colorData[i * 4 + 2] / 255.0f;
                    color.a = colorData[i * 4 + 3] / 255.0f;
                    Colors.Add(color);
                }
                rawPointCloud.Vertices = Vertices.ToArray();
                rawPointCloud.Colors = Colors.ToArray();
                _rawPointCloudCache.Add(id, rawPointCloud);
            }
        }

        public void LoadStream(string relativeFilePath)
        {
            try
            {
                this.loader.LoadStream(relativeFilePath);
            }
            catch(Exception ex)
            {
                return;
            }

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
#if DEBUG_TIME
                    System.Diagnostics.Stopwatch swHeader = new System.Diagnostics.Stopwatch();
                    swHeader.Start();
#endif

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

#if DEBUG_TIME
                    swHeader.Stop();
                    System.Diagnostics.Stopwatch swTexture = new System.Diagnostics.Stopwatch();
                    System.Diagnostics.Stopwatch swGeometry = new System.Diagnostics.Stopwatch();
#endif
                    // resources
                    for (int i = 0; i < header3MXB.Resources.Count; ++i)
                    {
                        Schema.Resource resource = header3MXB.Resources[i];
                        if (resource.Type == "textureBuffer" && resource.Format == "jpg")
                        {
#if DEBUG_TIME
                            swTexture.Start();
#endif
                            ConstructRawTexture(resource.Id, br, resource.Size);
#if DEBUG_TIME
                            swTexture.Stop();
#endif
                        }
                        else if (resource.Type == "geometryBuffer" && resource.Format == "ctm")
                        {
#if DEBUG_TIME
                            swGeometry.Start();
#endif
                            ConstructRawMesh(resource.Id, br, resource.Size,
                                new Vector3(resource.BBMin[0], resource.BBMin[2], resource.BBMin[1]),
                                new Vector3(resource.BBMax[0], resource.BBMax[2], resource.BBMax[1]));

                            _meshTextureIdCache.Add(resource.Id, resource.Texture);
#if DEBUG_TIME
                            swGeometry.Stop();
#endif
                        }
                        else if (resource.Type == "geometryBuffer" && resource.Format == "xyz")
                        {
#if DEBUG_TIME
                            swGeometry.Start();
#endif
                            ConstructRawPointCloud(resource.Id, br, resource.Size,
                                new Vector3(resource.BBMin[0], resource.BBMin[2], resource.BBMin[1]),
                                new Vector3(resource.BBMax[0], resource.BBMax[2], resource.BBMax[1]));

                            //_meshTextureIdCache.Add(resource.Id, resource.Texture);
#if DEBUG_TIME
                            swGeometry.Stop();
#endif
                        }
                        else
                        {
                            Debug.LogError("Unexpected buffer type in 3mxb file: " + relativeFilePath);
                        }
                    }
#if DEBUG_TIME
                    System.Diagnostics.Stopwatch swNodes = new System.Diagnostics.Stopwatch();
                    swNodes.Start();
#endif
                    // nodes
                    for (int i = 0; i < header3MXB.Nodes.Count; ++i)
                    {
                        string url = UrlUtils.ReplaceDataProtocol(this.dir + relativeFilePath);
                        string childDir = UrlUtils.GetBaseUri(url);

                        Schema.Node node = header3MXB.Nodes[i];
                        RawPagedLOD rawPagedLOD = new RawPagedLOD(); // (node.Id, this.parent.GetTransform(), childDir);
                        rawPagedLOD.dir = childDir;
                        rawPagedLOD.id = node.Id;
                        rawPagedLOD.BBMin = new Vector3(node.BBMin[0], node.BBMin[2], node.BBMin[1]);
                        rawPagedLOD.BBMax = new Vector3(node.BBMax[0], node.BBMax[2], node.BBMax[1]);
                        rawPagedLOD.BoundingSphere = new TileBoundingSphere((rawPagedLOD.BBMax + rawPagedLOD.BBMin) / 2, (rawPagedLOD.BBMax - rawPagedLOD.BBMin).magnitude / 2);
                        rawPagedLOD.MaxScreenDiameter = node.MaxScreenDiameter;
                        rawPagedLOD.ChildrenFiles = node.Children;

                        for (int j = 0; j < node.Resources.Count; ++j)
                        {
                            RawMesh mesh;
                            RawPointCloud pointCloud;
                            if (_rawMeshCache.TryGetValue(node.Resources[j], out mesh))
                            {
                                RawTexMesh texMesh = new RawTexMesh();
                                texMesh.Mesh = mesh;

                                RawTexture texture;
                                texture.Width = 0;
                                texture.Height = 0;
                                texture.ImgData = null;
                                string textureId;
                                if (_meshTextureIdCache.TryGetValue(node.Resources[j], out textureId))
                                {
                                    if (textureId != null)
                                    {
                                        _rawTextureCache.TryGetValue(textureId, out texture);
                                    }
                                }
                                texMesh.Texture = texture;
                                rawPagedLOD.TexMeshs.Add(texMesh);
                                rawPagedLOD.IsPointCloud = false;
                            }
                            else if(_rawPointCloudCache.TryGetValue(node.Resources[j], out pointCloud))
                            {
                                rawPagedLOD.PointClouds.Add(pointCloud);
                                rawPagedLOD.IsPointCloud = true;
                            }
                        }
                        this.StagedChildren.Add(rawPagedLOD);
                    }
#if DEBUG_TIME
                    swNodes.Stop();
                    UnityEngine.Debug.Log(string.Format("Header: {0} ms, Texture: {1} ms, Geometry: {2} ms, Nodes: {3} m", 
                        swHeader.ElapsedMilliseconds, swTexture.ElapsedMilliseconds, swGeometry.ElapsedMilliseconds, swNodes.ElapsedMilliseconds));
#endif
                }
            }
            Dispose();
        }
    }
}