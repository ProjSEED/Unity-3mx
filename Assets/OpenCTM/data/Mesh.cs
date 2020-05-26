using System;
using System.Diagnostics;

namespace OpenCTM
{
	public class Mesh
	{
		public const int CTM_ATTR_ELEMENT_COUNT = 4;
	    public const int CTM_NORMAL_ELEMENT_COUNT = 3;
	    public const int CTM_POSITION_ELEMENT_COUNT = 3;
	    public const int CTM_UV_ELEMENT_COUNT = 2;
	    //
	    public readonly float[] vertices, normals;
	    public readonly int[] indices;
	    // Multiple sets of UV coordinate maps (optional)
	    public readonly AttributeData[] texcoordinates;
	    // Multiple sets of custom vertex attribute maps (optional)
	    public readonly AttributeData[] attributs;
	
	    public Mesh(float[] vertices, float[] normals, int[] indices, AttributeData[] texcoordinates, AttributeData[] attributs) {
	        Debug.Assert(vertices != null);
			Debug.Assert(indices != null);
			Debug.Assert(texcoordinates != null);
			Debug.Assert(attributs != null);
			
			this.vertices = vertices;
	        this.normals = normals;
	        this.indices = indices;
	        this.texcoordinates = texcoordinates;
	        this.attributs = attributs;
	    }
	
	    public int getVertexCount() {
	        return vertices.Length / CTM_POSITION_ELEMENT_COUNT;
	    }
	
	    public int getUVCount() {
	        return texcoordinates.Length;
	    }
	
	    public int getAttrCount() {
	        return attributs.Length;
	    }
	
	    public int getTriangleCount() {
	        return indices.Length / 3;
	    }
	
	    public bool hasNormals() {
	        return normals != null;
	    }
	
	    public float getAverageEdgeLength() {
	        // Calculate the average edge length (Note: we actually sum up all the half-
	        // edges, so in a proper solid mesh all connected edges are counted twice)
	
	        float totalLength = 0;
	        int edgeCount = 0;
	
	        for (int i = 0; i < getTriangleCount(); ++i) {
	            int p1, p2;
	            p1 = indices[i * 3 + 2] * 3;
	            for (int j = 0; j < 3; ++j) {
	                p2 = indices[i * 3 + j] * 3;
	                float length = (vertices[p2] - vertices[p1]) * (vertices[p2] - vertices[p1]);
	                length += (vertices[p2 + 1] - vertices[p1 + 1]) * (vertices[p2 + 1] - vertices[p1 + 1]);
	                length += (vertices[p2 + 2] - vertices[p1 + 2]) * (vertices[p2 + 2] - vertices[p1 + 2]);
	                totalLength += (float)Math.Sqrt(length);
	                p1 = p2;
	                ++edgeCount;
	            }
	        }
	
	        return totalLength / edgeCount;
	    }
	
	    public void checkIntegrity(){
	        // Check that we have all the mandatory data
	        if (vertices == null || indices == null || vertices.Length < 1
	            || getTriangleCount() < 1) {
	            throw new InvalidDataException("The vertice or indice array is NULL"
	                                           + " or empty!");
	        }
	
	        if (indices.Length % 3 != 0) {
	            throw new InvalidDataException("The indice array size is not a multible of three!");
	        }
	
	        // Check that all indices are within range
	        foreach (int ind in indices) {
	            if (ind >= vertices.Length) {
	                throw new InvalidDataException("One element of the indice array "
	                                               + "points to a none existing vertex(id: " + ind + ")");
	            }
	        }
	
	        // Check that all vertices are finite (non-NaN, non-inf)
	        foreach (float v in vertices) {
	            if (isNotFinit(v)) {
	                throw new InvalidDataException("One of the vertice values is not finit!");
	            }
	        }
	
	        // Check that all normals are finite (non-NaN, non-inf)
	        if (normals != null) {
	            foreach (float n in normals) {
	                if (isNotFinit(n)) {
	                    throw new InvalidDataException("One of the normal values is not finit!");
	                }
	            }
	        }
	
	        // Check that all UV maps are finite (non-NaN, non-inf)
	        foreach (AttributeData map in texcoordinates) {
	            foreach (float v in map.values) {
	                if (isNotFinit(v)) {
	                    throw new InvalidDataException("One of the texcoord values is not finit!");
	                }
	            }
	        }
	
	        // Check that all attribute maps are finite (non-NaN, non-inf)
	        foreach (AttributeData map in attributs) {
	            foreach (float v in map.values) {
	                if (isNotFinit(v)) {
	                    throw new InvalidDataException("One of the attribute values is not finit!");
	                }
	            }
	        }
	    }
	
	    private bool isNotFinit(float val) {
	        return float.IsNaN(val) || float.IsInfinity(val);
	    }
	
	    public override int GetHashCode() {
	        int hash = 3;
	        hash = 67 * hash + GetArrayHash(this.vertices);
	        hash = 67 * hash + GetArrayHash(this.normals);
	        hash = 67 * hash + Triangle.GetArrayHash(this.indices);
	        hash = 67 * hash + GetArrayHash(this.texcoordinates);
	        hash = 67 * hash + GetArrayHash(this.attributs);
	        return hash;
	    }
		
		public static int GetArrayHash(float[] array)
		{
			if(array == null)
				return 0;
			int hc=array.Length;
			for(int i=0;i<array.Length;++i)
			{
				CtmInputStream.IntFloat a = new CtmInputStream.IntFloat();
				a.FloatValue = array[i];
			     hc=unchecked(hc*31 + a.IntValue);
			}
			return hc;
		}
		
		public static int GetArrayHash(object[] array)
		{
			if(array == null)
				return 0;
			int hc=array.Length;
			for(int i=0;i<array.Length;++i)
			{
			     hc=unchecked(hc*31 + array[i].GetHashCode());
			}
			return hc;
		}
	
	    public override bool Equals(Object obj) {
	        
	        Mesh other = obj as Mesh;
			if (other == null) {
	            return false;
	        }			
	        if (!ArrayEquals(this.vertices, other.vertices)) {
	            return false;
	        }
	        if (!ArrayEquals(this.normals, other.normals)) {
	            return false;
	        }
	        if (!ArrayEquals(this.indices, other.indices)) {
	            return false;
	        }
	        if (!DeepEquals(this.texcoordinates, other.texcoordinates)) {
	            return false;
	        }
	        if (!DeepEquals(this.attributs, other.attributs)) {
	            return false;
	        }
	        return true;
	    }
		
		public static bool ArrayEquals<T>(T[] a, T[]b)
		{
			if(a==null)
			{
				return b==null;
			}
			
			if(b==null)
				return false;
			
			for(int i=0; i<a.Length; ++i)
			{
				if(!a[i].Equals(b[i]))
					return false;
			}
			return true;
		}
		
		public static bool DeepEquals(object[] a, object[] b)
		{
			if(a.Length != b.Length)
				return false;
			for(int i=0; i<a.Length; ++i)
			{
				if(!a[i].Equals(b[i]))
					return false;
			}
			return true;
		}
	}
}

