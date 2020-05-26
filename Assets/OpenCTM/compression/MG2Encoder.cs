using System;

namespace OpenCTM
{
	public class MG2Encoder : MG1Encoder
	{
		public const float STANDARD_NORMAL_PRECISION = 1f / 256f;
		public const float STANDARD_VERTEX_PRECISION = 1f / 1024f;
		public readonly float vertexPrecision, normalPrecision;
	
		public MG2Encoder (float vertexPrecision = STANDARD_VERTEX_PRECISION, float normalPrecision = STANDARD_NORMAL_PRECISION)
		{
			this.vertexPrecision = vertexPrecision;
			this.normalPrecision = normalPrecision;
		}
	
		public override int getTag ()
		{
			return MG2Decoder.MG2_Tag;
		}
	
		public override void encode (Mesh m, CtmOutputStream output)
		{
			Grid grid = setupGrid (m.vertices);
			SortableVertex[] sorted = sortVertices (grid, m.vertices);
			int[] vdeltas = makeVertexDeltas (m.vertices, sorted, grid);
	
			int[] gridIndicies = new int[m.getVertexCount ()];
			gridIndicies [0] = sorted [0].gridIndex;
			for (int i = 1; i < m.getVertexCount(); ++i) {
				gridIndicies [i] = sorted [i].gridIndex - sorted [i - 1].gridIndex;
			}
	
			output.writeLittleInt (MG2Decoder.MG2_HEADER_TAG);
	
			output.writeLittleFloat (vertexPrecision);
			output.writeLittleFloat (normalPrecision);
	
			grid.writeToStream (output);
	
			output.writeLittleInt (MeshDecoder.VERT);
			output.writePackedInts (vdeltas, m.getVertexCount (), Mesh.CTM_POSITION_ELEMENT_COUNT, false);
	
			output.writeLittleInt (MG2Decoder.GIDX);
			output.writePackedInts (gridIndicies, m.getVertexCount (), 1, false);
	
			output.writeLittleInt (MeshDecoder.INDX);
			int[] indices = reIndexIndices (sorted, m.indices);
			rearrangeTriangles (indices);
			
			//write indicies
			{
				int[] deltas = new int[indices.Length];
				Array.Copy(indices, deltas, indices.Length);
				makeIndexDeltas (deltas);
				output.writePackedInts (deltas, m.getTriangleCount (), 3, false);
			}
	
			if (m.hasNormals ()) {
	
				for (int i = 1; i < m.getVertexCount(); i++) {
					gridIndicies [i] += gridIndicies [i - 1];
				}
				float[] restoredv = CommonAlgorithm.restoreVertices (vdeltas, gridIndicies, grid, vertexPrecision);
	
				output.writeLittleInt (MeshDecoder.NORM);
				int[] intNormals = makeNormalDeltas (restoredv, m.normals, indices, sorted);
				output.writePackedInts (intNormals, m.getVertexCount (), Mesh.CTM_NORMAL_ELEMENT_COUNT, false);
			}
	
			foreach (AttributeData ad in m.texcoordinates) {
				output.writeLittleInt (MeshDecoder.TEXC);
				output.writeString (ad.name);
				output.writeString (ad.materialName);
				output.writeLittleFloat (ad.precision);
				int[] deltas = makeUVCoordDeltas (ad, sorted);
				output.writePackedInts (deltas, m.getVertexCount (), Mesh.CTM_UV_ELEMENT_COUNT, true);
			}
	
			foreach (AttributeData ad in m.attributs) {
				output.writeLittleInt (MeshDecoder.ATTR);
				output.writeString (ad.name);
				output.writeLittleFloat (ad.precision);
				int[] deltas = makeAttribDeltas (ad, sorted);
				output.writePackedInts (deltas, m.getVertexCount (), Mesh.CTM_ATTR_ELEMENT_COUNT, true);
			}
		}
	
		/**
	     * Setup the 3D space subdivision grid.
	     */
		public Grid setupGrid (float[] vertices)
		{
			int vc = vertices.Length / 3;
			//CTM_POSITION_ELEMENT_COUNT == 3
			// Calculate the mesh boundinggrid. box
			float[] min = new float[3];
			float[] max = new float[3];
			int[] division = new int[3];
	
			for (int i = 0; i < 3; ++i) {
				min [i] = max [i] = vertices [i];
	
			}
			for (int i = 1; i < vc; ++i) {
				for (int j = 0; j < 3; j++) {
					min [j] = Math.Min (min [j], vertices [i * 3 + j]);
					max [j] = Math.Max (max [j], vertices [i * 3 + j]);
				}
			}
	
			// Determine optimal grid resolution, based on the number of vertices and
			// the bounding box.
			// NOTE: This algorithm is quite crude, and could very well be optimized for
			// better compression levels in the future without affecting the file format
			// or backward compatibility at all.
	
			float[] factor = new float[3];
			for (int i = 0; i < 3; ++i) {
				factor [i] = max [i] - min [i];
			}
	
			float sum = factor [0] + factor [1] + factor [2];
	
			if (sum > 1e-30f) {
				sum = 1.0f / sum;
				for (int i = 0; i < 3; ++i) {
					factor [i] *= sum;
				}
				double wantedGrids = Math.Pow (100.0f * vc, 1.0f / 3.0f);
				for (int i = 0; i < 3; ++i) {
					division [i] = (int)Math.Ceiling (wantedGrids * factor [i]);
					if (division [i] < 1) {
						division [i] = 1;
					}
				}
			} else {
				division [0] = 4;
				division [1] = 4;
				division [2] = 4;
			}
	
			return new Grid (min, max, division);
		}
	
		/**
	     * Convert a point to a grid index.
	     */
		private int pointToGridIdx (Grid grid, float[] point)
		{
			int[] idx = new int[3];
			float[] size = grid.getSize ();
	
			for (int i = 0; i < 3; ++i) {
				idx [i] = (int)Math.Floor ((point [i] - grid.getMin () [i]) / size [i]);
				if (idx [i] >= grid.getDivision () [i]) {
					idx [i] = grid.getDivision () [i] - 1;
				}
			}
	
			return idx [0] + grid.getDivision () [0] * (idx [1] + grid.getDivision () [1] * idx [2]);
		}
	
		public SortableVertex[] sortVertices (Grid grid, float[] v)
		{
			// Prepare sort vertex array
			int vc = v.Length / Mesh.CTM_POSITION_ELEMENT_COUNT;
			SortableVertex[] sortVertices = new SortableVertex[vc];
			for (int i = 0; i < vc; ++i) {
				// Store vertex properties in the sort vertex array
				float[] point = new float[]{v [i * 3], v [i * 3 + 1], v [i * 3 + 2]};
				int p2g = pointToGridIdx (grid, point);
				sortVertices [i] = new SortableVertex (v [i * 3], p2g, i);
			}
	
			// Sort vertices. The elements are first sorted by their grid indices, and
			// scondly by their x coordinates.
			Array.Sort (sortVertices);
			return sortVertices;
		}
	
		/**
	     * Re-index all indices, based on the sorted vertices.
	     */
		private int[] reIndexIndices (SortableVertex[] sortVertices, int[] indices)
		{
			// Create temporary lookup-array, O(n)
			int[] indexLUT = new int[sortVertices.Length];
			int[] newIndices = new int[indices.Length];
	
			for (int i = 0; i < sortVertices.Length; ++i) {
				indexLUT [sortVertices [i].originalIndex] = i;
			}
	
			// Convert old indices to new indices, O(n)
			for (int i = 0; i < indices.Length; ++i) {
				newIndices [i] = indexLUT [indices [i]];
			}
	
	
			return newIndices;
		}
	
		/**
	     * Calculate various forms of derivatives in order to reduce data entropy.
	     */
		private int[] makeVertexDeltas (float[] vertices, SortableVertex[] sortVertices, Grid grid)
		{
			int vc = sortVertices.Length;
	
			// Vertex scaling factor
			float scale = 1.0f / vertexPrecision;
	
			float prevGridIndex = 0x7fffffff;
			int prevDeltaX = 0;
			int[] intVertices = new int[vc * Mesh.CTM_POSITION_ELEMENT_COUNT];
			for (int i = 0; i < vc; ++i) {
				// Get grid box origin
				int gridIdx = sortVertices [i].gridIndex;
				float[] gridOrigin = CommonAlgorithm.gridIdxToPoint (grid, gridIdx);
	
				// Get old vertex coordinate index (before vertex sorting)
				int oldIdx = sortVertices [i].originalIndex;
	
				// Store delta to the grid box origin in the integer vertex array. For the
				// X axis (which is sorted) we also do the delta to the previous coordinate
				// in the box.
				int deltaX = (int)Math.Floor (scale * (vertices [oldIdx * 3] - gridOrigin [0]) + 0.5f);
				if (gridIdx == prevGridIndex) {
					intVertices [i * 3] = deltaX - prevDeltaX;
				} else {
					intVertices [i * 3] = deltaX;
				}
	
				intVertices [i * 3 + 1] = (int)Math.Floor (scale * (vertices [oldIdx * 3 + 1] - gridOrigin [1]) + 0.5f);
				intVertices [i * 3 + 2] = (int)Math.Floor (scale * (vertices [oldIdx * 3 + 2] - gridOrigin [2]) + 0.5f);
	
				prevGridIndex = gridIdx;
				prevDeltaX = deltaX;
			}
	
			return intVertices;
		}
	
		/**
	     * Convert the normals to a new coordinate system: magnitude, phi, theta
	     * (relative to predicted smooth normals).
	     */
		private int[] makeNormalDeltas (float[] vertices, float[] normals, int[] indices, SortableVertex[] sortVertices)
		{
			// Calculate smooth normals (Note: aVertices and aIndices use the sorted
			// index space, so smoothNormals will too)
			float[] smoothNormals = CommonAlgorithm.calcSmoothNormals (vertices, indices);
	
			// Normal scaling factor
			float scale = 1.0f / normalPrecision;
	
			int vc = vertices.Length / Mesh.CTM_POSITION_ELEMENT_COUNT;
			int[] intNormals = new int[vc * Mesh.CTM_NORMAL_ELEMENT_COUNT];
			for (int i = 0; i < vc; ++i) {
				// Get old normal index (before vertex sorting)
				int oldIdx = sortVertices [i].originalIndex;
	
				// Calculate normal magnitude (should always be 1.0 for unit length normals)
				float magn = (float)Math.Sqrt (normals [oldIdx * 3] * normals [oldIdx * 3]
	                                      + normals [oldIdx * 3 + 1] * normals [oldIdx * 3 + 1]
	                                      + normals [oldIdx * 3 + 2] * normals [oldIdx * 3 + 2]);
				if (magn < 1e-10f) {
					magn = 1.0f;
				}
	
				// Invert magnitude if the normal is negative compared to the predicted
				// smooth normal
				if ((smoothNormals [i * 3] * normals [oldIdx * 3]
	                 + smoothNormals [i * 3 + 1] * normals [oldIdx * 3 + 1]
	                 + smoothNormals [i * 3 + 2] * normals [oldIdx * 3 + 2]) < 0.0f) {
					magn = -magn;
				}
	
				// Store the magnitude in the first element of the three normal elements
				intNormals [i * 3] = (int)Math.Floor (scale * magn + 0.5f);
	
				// Normalize the normal (1 / magn) - and flip it if magn < 0
				magn = 1.0f / magn;
				float[] n = new float[3];
				for (int j = 0; j < 3; ++j) {
					n [j] = normals [oldIdx * 3 + j] * magn;
				}
	
				// Convert the normal to angular representation (phi, theta) in a coordinate
				// system where the nominal (smooth) normal is the Z-axis
				float[] basisAxes = CommonAlgorithm.makeNormalCoordSys (smoothNormals, i * 3);
				float[] n2 = new float[3];
				for (int j = 0; j < 3; ++j) {
					n2 [j] = basisAxes [j * 3] * n [0]
	                        + basisAxes [j * 3 + 1] * n [1]
	                        + basisAxes [j * 3 + 2] * n [2];
				}
				double phi, theta, thetaScale;
				if (n2 [2] >= 1.0f) {
					phi = 0.0f;
				} else {
					phi = Math.Acos (n2 [2]);
				}
				theta = Math.Atan2 (n2 [1], n2 [0]);
	
				// Round phi and theta (spherical coordinates) to integers. Note: We let the
				// theta resolution vary with the x/y circumference (roughly phi).
				int intPhi = (int)Math.Floor (phi * (scale / (0.5 * Math.PI)) + 0.5);
				if (intPhi == 0) {
					thetaScale = 0.0;
				} else if (intPhi <= 4) {
					thetaScale = 2.0 / Math.PI;
				} else {
					thetaScale = intPhi / (2.0 * Math.PI);
				}
				intNormals [i * 3 + 1] = intPhi;
				intNormals [i * 3 + 2] = (int)Math.Floor ((theta + Math.PI) * thetaScale + 0.5f);
			}
			return intNormals;
		}
	
		/**
	     * Calculate various forms of derivatives in order to reduce data entropy.
	     */
		private int[] makeUVCoordDeltas (AttributeData map, SortableVertex[] sortVertices)
		{
			// UV coordinate scaling factor
			float scale = 1.0f / map.precision;
			int vc = sortVertices.Length;
			int prevU = 0, prevV = 0;
			int[] intUVCoords = new int[vc * Mesh.CTM_UV_ELEMENT_COUNT];
			for (int i = 0; i < vc; ++i) {
				// Get old UV coordinate index (before vertex sorting)
				int oldIdx = sortVertices [i].originalIndex;
	
				// Convert to fixed point
				int u = (int)Math.Floor (scale * map.values [oldIdx * 2] + 0.5f);
				int v = (int)Math.Floor (scale * map.values [oldIdx * 2 + 1] + 0.5f);
	
				// Calculate delta and store it in the converted array. NOTE: Here we rely
				// on the fact that vertices are sorted, and usually close to each other,
				// which means that UV coordinates should also be close to each other...
				intUVCoords [i * 2] = u - prevU;
				intUVCoords [i * 2 + 1] = v - prevV;
	
				prevU = u;
				prevV = v;
			}
			return intUVCoords;
		}
	
		/**
	     * Calculate various forms of derivatives in order to reduce data entropy.
	     */
		private int[] makeAttribDeltas (AttributeData map, SortableVertex[] sortVertices)
		{
			// Attribute scaling factor
			float scale = 1.0f / map.precision;
	
			int[] prev = new int[4];
	
			int vc = sortVertices.Length;
			int[] intAttribs = new int[vc * Mesh.CTM_ATTR_ELEMENT_COUNT];
	
			for (int i = 0; i < vc; ++i) {
				// Get old attribute index (before vertex sorting)
				int oldIdx = sortVertices [i].originalIndex;
	
				// Convert to fixed point, and calculate delta and store it in the converted
				// array. NOTE: Here we rely on the fact that vertices are sorted, and
				// usually close to each other, which means that attributes should also
				// be close to each other (and we assume that they somehow vary slowly with
				// the geometry)...
	
				for (int j = 0; j < 4; ++j) {
					int value = (int)Math.Floor (scale * map.values [oldIdx * 4 + j] + 0.5f);
					intAttribs [i * 4 + j] = value - prev [j];
					prev [j] = value;
				}
			}
			return intAttribs;
		}
	}
}

