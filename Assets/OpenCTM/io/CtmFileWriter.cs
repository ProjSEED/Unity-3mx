using System;
using System.IO;

namespace OpenCTM
{
	public class CtmFileWriter
	{
		private readonly CtmOutputStream output;
	    private readonly MeshEncoder encoder;
	
	    public CtmFileWriter(Stream o, MeshEncoder e) {
	        output = new CtmOutputStream(o);
	        encoder = e;
	    }
	
	    public CtmFileWriter(Stream o, MeshEncoder e, int compressionLevel) {
	        output = new CtmOutputStream(compressionLevel, o);
	        encoder = e;
	    }
	
	    public void encode(Mesh m, String comment){
	        // Check mesh integrity
	        m.checkIntegrity();
	
	        // Determine flags
	        int flags = 0;
	        if (m.normals != null) {
	            flags |= MeshInfo.HAS_NORMAL_BIT;
	        }
	
	        // Write header to stream
	        output.writeLittleInt(CtmFileReader.OCTM);
	        output.writeLittleInt(encoder.getFormatVersion());
	        output.writeLittleInt(encoder.getTag());
	
	        output.writeLittleInt(m.getVertexCount());
	        output.writeLittleInt(m.getTriangleCount());
	        output.writeLittleInt(m.getUVCount());
	        output.writeLittleInt(m.getAttrCount());
	        output.writeLittleInt(flags);
	        output.writeString(comment);
	
	        // Compress to stream
	        encoder.encode(m, output);
	    }
	}
}

