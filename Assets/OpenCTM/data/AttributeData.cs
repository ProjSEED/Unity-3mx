using System;

namespace OpenCTM
{
	public class AttributeData
	{
		public static readonly float STANDARD_UV_PRECISION = 1f / 4096f;
	    public static readonly float STANDARD_PRECISION = 1f / 256;
	    public readonly String name;         // Unique name
	    public readonly String materialName;     // File name reference (used only for UV maps)
	    public readonly float precision;
	    public readonly float[] values;   // Attribute/UV coordinate values (per vertex)
	
	    public AttributeData(String name, String materialName, float precision, float[] values) {
	        this.name = name;
	        this.materialName = materialName;
	        this.precision = precision;
	        this.values = values;
	    }
	
	    public bool checkIntegrity() {
	        return precision > 0;
	    }
	
	    public override int GetHashCode() {
	        int hash = 3;
	        hash = 67 * hash + this.name.GetHashCode();
	        hash = 67 * hash + this.materialName.GetHashCode();
	        hash = 67 * hash + this.precision.GetHashCode();
	        hash = 67 * hash + this.values.GetHashCode();
	        return hash;
	    }
	
	    public override bool Equals(Object obj) {
	        
	        AttributeData other = obj as AttributeData;
			if (other == null) {
	            return false;
	        }
	        if (!name.Equals(other.name)) {
	            return false;
	        }
	        if (!materialName.Equals(other.materialName)) {
	            return false;
	        }
	        if (precision != other.precision) {
	            return false;
	        }
	        if (!Mesh.ArrayEquals(this.values, other.values)) {
	            return false;
	        }
	        return true;
	    }
	}
}

