using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Unity3MX
{

    public enum IntersectionType
    {
        OUTSIDE,
        INSIDE,
        INTERSECTING,
    }


    /// <summary>
    /// This strucutre is used as an optimization when culling a bounding volume with a view frustum
    /// If a bounding volume is enclosed within a parent volume, then we only need to check planes 
    /// that intersected the parent volume.  We can skip a plane if the volume is completly inside.
    /// We also track if the volume is completly outside any of the planes, in which case we know the
    /// volume is not inside the frustum.
    /// See https://cesium.com/blog/2015/08/04/fast-hierarchical-culling/
    /// </summary>
    public struct PlaneClipMask
    {
        /// <summary>
        /// Represents a code where all planes are marked as intersecting
        /// </summary>
        const int MASK_INTERSECTING = 0;
        const int MASK_INSIDE = (0x3F << 6);

        /// <summary>
        /// Bitwise mask where each bit corresponds to a clipping plane
        /// bit i is 0 iff the node MIGHT intersect plane[i]
        /// bit i is 1 if the node is DEFINITELY inside plane[i]
        /// There are 6 bits in the order: far (MSB), near, top, bottom, right, left (LSB)
        /// </summary>
        int code;

        /// <summary>
        /// True if any the volume being checked is outside of any plane, in which case it is fully outside the frustum
        /// </summary>
        bool anyOutside;

        public IntersectionType Intersection
        {
            get
            {
                if (anyOutside)
                {
                    // If the volume is fully outside any plane, then it is outside
                    return IntersectionType.OUTSIDE;
                }
                else if (code == MASK_INSIDE)
                {
                    // If the volume is inside all 6 planes then it is fully inside the volume
                    return IntersectionType.INSIDE;
                }
                else
                {
                    // Otherwise we are intersecting or don't have enough information to know for sure
                    return IntersectionType.INTERSECTING;
                }
            }
        }

        /// <summary>
        /// Returns a default mask which indicates that all planes must be checked for intersection with the frustum
        /// </summary>
        /// <returns></returns>
        public static PlaneClipMask GetDefaultMask()
        {
            return new PlaneClipMask();
        }

        /// <summary>
        /// Returns true if this node might intersect the given frustum plane (i.e. needs to be checked)
        /// Returns false if this node is fully inside the given frustum plane and can be skipped
        /// </summary>
        /// <param name="planeIdx"></param>
        /// <returns></returns>
        public bool Intersecting(int planeIdx)
        {
            // Return true if we are not inside this plane
            return (code & (1 << planeIdx)) == 0;
        }

        /// <summary>
        /// Mark this node as being completly inside the plane, subsequent intersection checks will not use this plane
        /// </summary>
        /// <param name="planeIdx"></param>
        public void Set(int planeIdx, IntersectionType intersection)
        {
            if (intersection == IntersectionType.OUTSIDE)
            {
                anyOutside = true;
            }
            else if (intersection == IntersectionType.INSIDE)
            {
                // Set the bit i to 1 for inside
                code |= (1 << planeIdx);
            }
            else
            {
                // Set bit i to 0 for intersecting
                code &= ~(1 << planeIdx);
            }
        }

    }

    public abstract class Unity3MXBBoundingVolume
    {

        public abstract IntersectionType IntersectPlane(Plane plane);

        public abstract float DistanceTo(Vector3 point);

        public abstract void DebugDraw(Color c, Transform t);

        public abstract BoundingSphere BoundingSphere();

        public abstract float Volume();

        public abstract float ScreenDiameter(Vector4 pixelSizeVector);

        public abstract string SizeString();

        public PlaneClipMask IntersectPlanes(Plane[] planes)
        {
            return IntersectPlanes(planes, PlaneClipMask.GetDefaultMask());
        }

        public PlaneClipMask IntersectPlanes(Plane[] planes, PlaneClipMask mask)
        {
            if (mask.Intersection != IntersectionType.INTERSECTING)
            {
                return mask;
            }

            for (var i = 0; i < planes.Length; ++i)
            {
                if (mask.Intersecting(i))
                {
                    IntersectionType value = this.IntersectPlane(planes[i]);
                    mask.Set(i, value);
                    if(value == IntersectionType.OUTSIDE)
                    {
                        break;
                    }
                }
            }
            return mask;
        }
    }

    public class TileBoundingSphere : Unity3MXBBoundingVolume
    {
        public Vector3 Center;
        public float Radius;

        public TileBoundingSphere(Vector3 center, float radius)
        {
            this.Center = center;
            this.Radius = radius;
        }

        public void Transform(Matrix4x4 transform)
        {
            this.Center = transform.MultiplyPoint(this.Center);
            //var scale = transform.lossyScale;   // Change to lossyScale in future versions of unity
            Vector3 scale = new Vector3(
                transform.GetColumn(0).magnitude,
                transform.GetColumn(1).magnitude,
                transform.GetColumn(2).magnitude
            );
            float maxScale = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            this.Radius *= maxScale;
        }

        public override IntersectionType IntersectPlane(Plane plane)
        {
            float distanceToPlane = Vector3.Dot(plane.normal, this.Center) + plane.distance;

            if (distanceToPlane < -this.Radius)
            {
                // The center point is negative side of the plane normal
                return IntersectionType.OUTSIDE;
            }
            else if (distanceToPlane < this.Radius)
            {
                // The center point is positive side of the plane, but radius extends beyond it; partial overlap
                return IntersectionType.INTERSECTING;
            }
            return IntersectionType.INSIDE;
        }

        public override float DistanceTo(Vector3 point)
        {
            return Mathf.Max(0.0f, Vector3.Distance(this.Center, point) - this.Radius);
        }

        public override BoundingSphere BoundingSphere()
        {
            return new BoundingSphere(Center, Radius);
        }

        public override float Volume()
        {
            return (4.0f / 3.0f) * Mathf.PI * Radius * Radius * Radius;
        }
        public override float ScreenDiameter(Vector4 pixelSizeVector)
        {
            return Mathf.Abs(this.Radius / (Vector4.Dot(this.Center, pixelSizeVector) + pixelSizeVector.w));
        }

        public override string SizeString()
        {
            return string.Format("d={0:f3}", Radius);
        }

        public override void DebugDraw(Color c, Transform t)
        {
            throw new NotImplementedException();
        }
    }
}
