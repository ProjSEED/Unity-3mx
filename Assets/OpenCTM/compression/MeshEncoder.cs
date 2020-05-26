using System;

namespace OpenCTM
{
	public interface MeshEncoder
	{
		void encode(Mesh m, CtmOutputStream output);

	    int getTag();
	
	    int getFormatVersion();
	}
}

