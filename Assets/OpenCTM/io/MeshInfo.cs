using System;

namespace OpenCTM
{
	public class MeshInfo
	{
		public const int HAS_NORMAL_BIT = 1;

	    private readonly int vertexCount, triangleCount, uvMapCount, attrCount, flags;
	
	    public MeshInfo(int vertexCount, int triangleCount, int uvMapCount, int attrCount, int flags)
	    {
	        this.vertexCount = vertexCount;
	        this.triangleCount = triangleCount;
	        this.uvMapCount = uvMapCount;
	        this.attrCount = attrCount;
	        this.flags = flags;
	    }
	
	    public int getAttrCount()
	    {
	        return attrCount;
	    }
	
	    public int getTriangleCount()
	    {
	        return triangleCount;
	    }
	
	    public int getUvMapCount()
	    {
	        return uvMapCount;
	    }
	
	    public int getVertexCount()
	    {
	        return vertexCount;
	    }
	
	    public bool hasNormals()
	    {
	        return (flags & HAS_NORMAL_BIT) > 0;
	    }
	}
}

