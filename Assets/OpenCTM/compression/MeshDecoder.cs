using System;

namespace OpenCTM
{
	public abstract class MeshDecoder
	{
		public static readonly int INDX = CtmFileReader.getTagInt("INDX");
	    public static readonly int VERT = CtmFileReader.getTagInt("VERT");
	    public static readonly int NORM = CtmFileReader.getTagInt("NORM");
	    public static readonly int TEXC = CtmFileReader.getTagInt("TEXC");
	    public static readonly int ATTR = CtmFileReader.getTagInt("ATTR");
	
	    public abstract Mesh decode(MeshInfo minfo, CtmInputStream input);
	
	    public abstract bool isFormatSupported(int tag, int version);

	}
}

