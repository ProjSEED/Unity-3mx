using System;

namespace OpenCTM
{
	public class MG2Decoder : MG1Decoder
	{
		public static readonly int MG2_Tag = CtmFileReader.getTagInt("MG2\0");
	    public static readonly int MG2_HEADER_TAG = CtmFileReader.getTagInt("MG2H");
	    public static readonly int GIDX = CtmFileReader.getTagInt("GIDX");
	
	    public override bool isFormatSupported(int tag, int version) {
	        return tag == MG2_Tag && version == RawDecoder.FORMAT_VERSION;
	    }
	
	    public override Mesh decode(MeshInfo minfo, CtmInputStream input){
	        int vc = minfo.getVertexCount();
	
	        checkTag(input.readLittleInt(), MG2_HEADER_TAG);
	        float vertexPrecision = input.readLittleFloat();
	        float normalPrecision = input.readLittleFloat();
	
	        Grid grid = Grid.fromStream(input);
	        if(!grid.checkIntegrity()) {
	            throw new InvalidDataException("The vertex size grid is corrupt!");
	        }
	
	        float[] vertices = readVertices(input, grid, vc, vertexPrecision);
	
	        int[] indices = readIndices(input, minfo.getTriangleCount(), vc);
	
	        float[] normals = null;
	        if (minfo.hasNormals()) {
	            normals = readNormals(input, vertices, indices, normalPrecision, vc);
	        }
	
	        AttributeData[] uvData = new AttributeData[minfo.getUvMapCount()];
	        for (int i = 0; i < uvData.Length; i++) {
	            uvData[i] = readUvData(input, vc);
	        }
	
	        AttributeData[] attributs = new AttributeData[minfo.getAttrCount()];
	        for (int i = 0; i < attributs.Length; i++) {
	            attributs[i] = readAttribute(input, vc);
	        }
	
	        return new Mesh(vertices, normals, indices, uvData, attributs);
	    }
	
	    private float[] readVertices(CtmInputStream input, Grid grid, int vcount, float precision){
	        checkTag(input.readLittleInt(), VERT);
	        int[] intVertices = input.readPackedInts(vcount, Mesh.CTM_POSITION_ELEMENT_COUNT, false);
	
	        checkTag(input.readLittleInt(), GIDX);
	        int[] gridIndices = input.readPackedInts(vcount, 1, false);
	        for (int i = 1; i < vcount; i++) {
	            gridIndices[i] += gridIndices[i - 1];
	        }
	
	        return CommonAlgorithm.restoreVertices(intVertices, gridIndices, grid, precision);
	    }
	
	    private int[] readIndices(CtmInputStream input, int triCount, int vcount){
	        checkTag(input.readLittleInt(), INDX);
	        int[] indices = input.readPackedInts(triCount, 3, false);
	        restoreIndices(triCount, indices);
	        foreach(int i in indices) {
	            if (i > vcount) {
	                throw new InvalidDataException("One element of the indice array "
	                                               + "points to a none existing vertex(id: " + i + ")");
	            }
	        }
	        return indices;
	    }
	
	    private float[] readNormals(CtmInputStream input, float[] vertices, int[] indices,
	                                float normalPrecision, int vcount){
	        checkTag(input.readLittleInt(), NORM);
	        int[] intNormals = input.readPackedInts(vcount, Mesh.CTM_NORMAL_ELEMENT_COUNT, false);
	        return restoreNormals(intNormals, vertices, indices, normalPrecision);
	    }
	
	    private AttributeData readUvData(CtmInputStream input, int vcount){
	        checkTag(input.readLittleInt(), TEXC);
	        String name = input.readString();
	        String material = input.readString();
	        float precision = input.readLittleFloat();
	        if (precision <= 0f) {
	            throw new InvalidDataException("A uv precision value <= 0.0 was read");
	        }
	
	        int[] intCoords = input.readPackedInts(vcount, Mesh.CTM_UV_ELEMENT_COUNT, true);
	        float[] data = restoreUVCoords(precision, intCoords);
	
	        return new AttributeData(name, material, precision, data);
	    }
	
	    private AttributeData readAttribute(CtmInputStream input, int vc){
	        checkTag(input.readLittleInt(), ATTR);
	
	        String name = input.readString();
	        float precision = input.readLittleFloat();
	        if (precision <= 0f) {
	            throw new InvalidDataException("An attribute precision value <= 0.0 was read");
	        }
	
	        int[] intData = input.readPackedInts(vc, Mesh.CTM_ATTR_ELEMENT_COUNT, true);
	        float[] data = restoreAttribs(precision, intData);
	
	        return new AttributeData(name, null, precision, data);
	    }
	
	    /**
	     * Calculate inverse derivatives of the vertex attributes.
	     */
	    private float[] restoreAttribs(float precision, int[] intAttribs) {
	        int ae = Mesh.CTM_ATTR_ELEMENT_COUNT;
	        int vc = intAttribs.Length / ae;
	        float[] values = new float[intAttribs.Length];
	        int[] prev = new int[ae];
	        for (int i = 0; i < vc; ++i) {
	            // Calculate inverse delta, and convert to floating point
	            for (int j = 0; j < ae; ++j) {
	                int value = intAttribs[i * ae + j] + prev[j];
	                values[i * ae + j] = value * precision;
	                prev[j] = value;
	            }
	        }
	        return values;
	    }
	
	    /**
	     * Calculate inverse derivatives of the UV coordinates.
	     */
	    private float[] restoreUVCoords(float precision, int[] intUVCoords) {
	        int vc = intUVCoords.Length / Mesh.CTM_UV_ELEMENT_COUNT;
	        float[] values = new float[intUVCoords.Length];
	        int prevU = 0, prevV = 0;
	        for (int i = 0; i < vc; ++i) {
	            // Calculate inverse delta
	            int u = intUVCoords[i * Mesh.CTM_UV_ELEMENT_COUNT] + prevU;
	            int v = intUVCoords[i * Mesh.CTM_UV_ELEMENT_COUNT + 1] + prevV;
	
	            // Convert to floating point
	            values[i * Mesh.CTM_UV_ELEMENT_COUNT] = u * precision;
	            values[i * Mesh.CTM_UV_ELEMENT_COUNT + 1] = v * precision;
	
	            prevU = u;
	            prevV = v;
	        }
	        return values;
	    }
	
	    /**
	     * Convert the normals back to cartesian coordinates.
	     */
	    private float[] restoreNormals(int[] intNormals, float[] vertices, int[] indices, float normalPrecision) {
	
	        // Calculate smooth normals (nominal normals)
	        float[] smoothNormals = CommonAlgorithm.calcSmoothNormals(vertices, indices);
	        float[] normals = new float[vertices.Length];
	
	        int vc = vertices.Length / Mesh.CTM_POSITION_ELEMENT_COUNT;
	        int ne = Mesh.CTM_NORMAL_ELEMENT_COUNT;
	
	        for (int i = 0; i < vc; ++i) {
	            // Get the normal magnitude from the first of the three normal elements
	            float magn = intNormals[i * ne] * normalPrecision;
	
	            // Get phi and theta (spherical coordinates, relative to the smooth normal).
	            double thetaScale, theta;
	            int intPhi = intNormals[i * ne + 1];
	            double phi = intPhi * (0.5 * Math.PI) * normalPrecision;
	            if (intPhi == 0) {
	                thetaScale = 0.0f;
	            } else if (intPhi <= 4) {
	                thetaScale = Math.PI / 2.0f;
	            } else {
	                thetaScale = (2.0f * Math.PI) / intPhi;
	            }
	            theta = intNormals[i * ne + 2] * thetaScale - Math.PI;
	
	            // Convert the normal from the angular representation (phi, theta) back to
	            // cartesian coordinates
	            double[] n2 = new double[3];
	            n2[0] = Math.Sin(phi) * Math.Cos(theta);
	            n2[1] = Math.Sin(phi) * Math.Sin(theta);
	            n2[2] = Math.Cos(phi);
	            float[] basisAxes = CommonAlgorithm.makeNormalCoordSys(smoothNormals, i * ne);
	            double[] n = new double[3];
	            for (int j = 0; j < 3; ++j) {
	                n[j] = basisAxes[j] * n2[0]
	                       + basisAxes[3 + j] * n2[1]
	                       + basisAxes[6 + j] * n2[2];
	            }
	
	            // Apply normal magnitude, and output to the normals array
	            for (int j = 0; j < 3; ++j) {
	                normals[i * ne + j] = (float) (n[j] * magn);
	            }
	        }
	
	        return normals;
	    }
	}
}

