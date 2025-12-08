using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace HN.HNRP
{
    public struct ClusterCullingJobCommon
    {
        public static uint EncodeHeader(uint min, uint max)
        {
            return (min & 0xFFFF) | ((max & 0xFFFF) << 16);
        }

        public static (uint, uint) DecodeHeader(uint zBin)
        {
            return (zBin & 0xFFFF, (zBin >> 16) & 0xFFFF);
        }

        public static void FillZBins(ref NativeArray<uint> bins, ref NativeArray<float2> minMaxZs, bool isOrthographic, float zBinScale, float zBinOffset, int headerLength, int wordsPerTile, int binStart, int binEnd, int itemStart, int itemEnd, int headerIndex)
        {
            for (var index = itemStart; index < itemEnd; index++)
            {
                var minMax = minMaxZs[index];
                var minBin = math.max((int)((isOrthographic ? minMax.x : math.log2(minMax.x)) * zBinScale + zBinOffset), binStart);
                var maxBin = math.min((int)((isOrthographic ? minMax.y : math.log2(minMax.y)) * zBinScale + zBinOffset), binEnd);

                var wordIndex = index / 32;
                var bitMask = 1u << (index % 32);

                for (var binIndex = minBin; binIndex <= maxBin; binIndex++)
                {
                    var baseIndex = binIndex * (headerLength + wordsPerTile);
                    var (minIndex, maxIndex) = DecodeHeader(bins[baseIndex + headerIndex]);
                    minIndex = math.min(minIndex, (uint)index);
                    maxIndex = math.max(maxIndex, (uint)index);
                    bins[baseIndex + headerIndex] = EncodeHeader(minIndex, maxIndex);
                    bins[baseIndex + headerLength + wordIndex] |= bitMask;
                }
            }
        }

        public static float square(float x) => x * x;

        /// <summary>
        /// Finds the two horizon points seen from (0, 0) of a sphere projected onto either XZ or YZ. Takes clipping into account.
        /// </summary>
        public static void GetSphereHorizon(float2 center, float radius, float near, float clipRadius, out float2 p0, out float2 p1)
        {
            var direction = math.normalize(center);

            // Distance from camera to center of sphere
            var d = math.length(center);

            // Distance from camera to sphere horizon edge
            var l = math.sqrt(d * d - radius * radius);

            // Height of circle horizon
            var h = l * radius / d;

            // Center of circle horizon
            var c = direction * (l * h / radius);

            p0 = math.float2(float.MinValue, 1f);
            p1 = math.float2(float.MaxValue, 1f);

            // Handle clipping
            if (center.y - radius < near)
            {
                p0 = math.float2(center.x + clipRadius, near);
                p1 = math.float2(center.x - clipRadius, near);
            }

            // Circle horizon points
            var c0 = c + math.float2(-direction.y, direction.x) * h;
            if (square(d) >= square(radius) && c0.y >= near)
            {
                if (c0.x > p0.x) { p0 = c0; }
                if (c0.x < p1.x) { p1 = c0; }
            }

            var c1 = c + math.float2(direction.y, -direction.x) * h;
            if (square(d) >= square(radius) && c1.y >= near)
            {
                if (c1.x > p0.x) { p0 = c1; }
                if (c1.x < p1.x) { p1 = c1; }
            }
        }

        public static void GetSphereYPlaneHorizon(float3 center, float sphereRadius, float near, float clipRadius, float y, out float3 left, out float3 right)
        {
            // Note: The y-plane is the plane that is determined by `y` in that it contains the vector (1, 0, 0)
            // and goes through the points (0, y, 1) and (0, 0, 0). This would become a straight line in screen-space, and so it
            // represents the boundary between two rows of tiles.

            // Near-plane clipping - will get overwritten if no clipping is needed.
            // `y` is given for the view plane (Z=1), scale it so that it is on the near plane instead.
            var yNear = y * near;
            // Find the two points of intersection between the clip circle of the sphere and the y-plane.
            // Found using Pythagoras with a right triangle formed by three points:
            // (a) center of the clip circle
            // (b) a point straight above the clip circle center on the y-plane
            // (c) a point that is both on the circle and the y-plane (this is the point we want to find in the end)
            // The hypotenuse is formed by (a) and (c) with length equal to the clip radius. The known side is
            // formed by (a) and (b) and is simply the distance from the center to the y-plane along the y-axis.
            // The remaining side gives us the x-displacement needed to find the intersection points.
            var clipHalfWidth = math.sqrt(square(clipRadius) - square(yNear - center.y));
            left = math.float3(center.x - clipHalfWidth, yNear, near);
            right = math.float3(center.x + clipHalfWidth, yNear, near);

            // Basis vectors in the y-plane for being able to parameterize the plane.
            var planeU = math.normalize(math.float3(0, y, 1));
            var planeV = math.float3(1, 0, 0);

            // Calculate the normal of the y-plane. Found from: (0, y, 1) × (1, 0, 0) = (0, 1, -y)
            // This is used to represent the plane along with the origin, which is just 0 and thus doesn't show up
            // in the calculations.
            var normal = math.normalize(math.float3(0, 1, -y));

            // We want to first find the circle from the intersection of the y-plane and the sphere.

            // The shortest distance from the sphere center and the y-plane. The sign determines which side of the plane
            // the center is on.
            var signedDistance = math.dot(normal, center);

            // Unsigned shortest distance from the sphere center to the plane.
            var distanceToPlane = math.abs(signedDistance);

            // The center of the intersection circle in the y-plane, which is the point on the plane closest to the
            // sphere center. I.e. this is at `distanceToPlane` from the center.
            var centerOnPlane = math.float2(math.dot(center, planeU), math.dot(center, planeV));

            // Distance from origin to the circle center.
            var distanceInPlane = math.length(centerOnPlane);

            // Direction from origin to the circle center.
            var directionPS = centerOnPlane / distanceInPlane;

            // Calculate the radius of the circle using Pythagoras. We know that any point on the circle is a point on
            // the sphere. Thus we can construct a triangle with the sphere center, circle center, and a point on the
            // circle. We then want to find its distance to the circle center, as that will be equal to the radius. As
            // the point is on the sphere, it must be `sphereRadius` from the sphere center, forming the hypotenuse. The
            // other side is between the sphere and circle centers, which we've already calculated to be
            // `distanceToPlane`.
            var circleRadius = math.sqrt(square(sphereRadius) - square(distanceToPlane));

            // Now that we have the circle, we can find the horizon points. Since we've parametrized the plane, we can
            // just do this in 2D.

            // Any of these conditions will yield NaN due to negative square roots. They are signs that clipping is needed,
            // so we fallback on the already calculated values in that case.
            if (square(distanceToPlane) <= square(sphereRadius) && square(circleRadius) <= square(distanceInPlane))
            {
                // Distance from origin to circle horizon edge.
                var l = math.sqrt(square(distanceInPlane) - square(circleRadius));

                // Height of circle horizon.
                var h = l * circleRadius / distanceInPlane;

                // Center of circle horizon.
                var c = directionPS * (l * h / circleRadius);

                // Calculate the horizon points in the plane.
                var leftOnPlane = c + math.float2(directionPS.y, -directionPS.x) * h;
                var rightOnPlane = c + math.float2(-directionPS.y, directionPS.x) * h;

                // Transform horizon points to view space and use if not clipped.
                var leftCandidate = leftOnPlane.x * planeU + leftOnPlane.y * planeV;
                if (leftCandidate.z >= near) left = leftCandidate;

                var rightCandidate = rightOnPlane.x * planeU + rightOnPlane.y * planeV;
                if (rightCandidate.z >= near) right = rightCandidate;
            }
        }

        /// <summary>
        /// Finds the two points of intersection of a 3D circle and the near plane.
        /// </summary>
        public static bool GetCircleClipPoints(float3 circleCenter, float3 circleNormal, float circleRadius, float near, out float3 p0, out float3 p1)
        {
            // The intersection of two planes is a line where the direction is the cross product of the two plane normals.
            // In this case, it is the plane containing the circle, and the near plane.
            var lineDirection = math.normalize(math.cross(circleNormal, math.float3(0, 0, 1)));

            // Find a direction on the circle plane towards the nearest point on the intersection line.
            // It has to be perpendicular to the circle normal to be in the circle plane. The direction to the closest
            // point on a line is perpendicular to the line direction. Thus this is given by the cross product of the
            // line direction and the circle normal, as this gives us a vector that is perpendicular to both of those.
            var nearestDirection = math.cross(lineDirection, circleNormal);

            // Distance from circle center to the intersection line along `nearestDirection`.
            // This is done using a ray-plane intersection, where the plane is the near plane.
            // ({0, 0, near} - circleCenter) . {0, 0, 1} / (nearestDirection . {0, 0, 1})
            var distance = (near - circleCenter.z) / nearestDirection.z;

            // The point on the line nearest to the circle center when traveling only in the circle plane.
            var nearestPoint = circleCenter + nearestDirection * distance;

            // Any line through a circle makes a chord where the endpoints are the intersections with the circle.
            // The half length of the circle chord can be found by constructing a right triangle from three points:
            // (a) The circle center.
            // (b) The nearest point.
            // (c) A point that is on circle and the intersection line.
            // The hypotenuse is formed by (a) and (c) and will have length `circleRadius` as it is on the circle.
            // The known side if formed by (a) and (b), which we have already calculated the distance of in `distance`.
            // The unknown side formed by (b) and (c) is then found using Pythagoras.
            var chordHalfLength = math.sqrt(square(circleRadius) - square(distance));
            p0 = nearestPoint + lineDirection * chordHalfLength;
            p1 = nearestPoint - lineDirection * chordHalfLength;

            return math.abs(distance) <= circleRadius;
        }

        public static (float, float) IntersectEllipseLine(float a, float b, float3 line)
        {
            // The line is represented as a homogenous 2D line {u, v, w} such that ux + vy + w = 0.
            // The ellipse is represented by the implicit equation x^2/a^2 + y^2/b^2 = 1.
            // We solve the line equation for y:  y = (ux + w) / v
            // We then substitute this into the ellipse equation and expand and re-arrange a bit:
            //   x^2/a^2 + ((ux + w) / v)^2/b^2 = 1 =>
            //   x^2/a^2 + ((ux + w)^2 / v^2)/b^2 = 1 =>
            //   x^2/a^2 + (ux + w)^2/(v^2 b^2) = 1 =>
            //   x^2/a^2 + (u^2 x^2 + w^2 + 2 u x w)/(v^2 b^2) = 1 =>
            //   x^2/a^2 + x^2 u^2 / (v^2 b^2) + w^2/(v^2 b^2) + x 2 u w / (v^2 b^2) = 1 =>
            //   x^2 (1/a^2 + u^2 / (v^2 b^2)) + x 2 u w / (v^2 b^2) + w^2 / (v^2 b^2) - 1 = 0
            // We now have a quadratic equation with:
            //   a = 1/a^2 + u^2 / (v^2 b^2)
            //   b = 2 u w / (v^2 b^2)
            //   c = w^2 / (v^2 b^2) - 1
            var div = math.rcp(square(line.y) * square(b));
            var qa = 1f / square(a) + square(line.x) * div;
            var qb = 2f * line.x * line.z * div;
            var qc = square(line.z) * div - 1f;
            var sqrtD = math.sqrt(qb * qb - 4f * qa * qc);
            var x1 = (-qb + sqrtD) / (2f * qa);
            var x2 = (-qb - sqrtD) / (2f * qa);
            return (x1, x2);
        }

        /// <summary>
        /// Calculates the horizon of a circle orthogonally projected to a plane as seen from the origin on the plane.
        /// </summary>
        /// <param name="center">The center of the circle projected onto the plane.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="U">The major axis of the ellipse formed by the projection of the circle.</param>
        /// <param name="V">The minor axis of the ellipse formed by the projection of the circle.</param>
        /// <param name="uv1">The first horizon point expressed as factors of <paramref name="U"/> and <paramref name="V"/>.</param>
        /// <param name="uv2">The second horizon point expressed as factors of <paramref name="U"/> and <paramref name="V"/>.</param>
        public static void GetProjectedCircleHorizon(float2 center, float radius, float2 U, float2 V, out float2 uv1, out float2 uv2)
        {
            // U is assumed to be constructed such that it is never 0, but V can be if the circle projects to a line segment.
            // In that case, the solution can be trivially found using U only.
            var vl = math.length(V);
            if (vl < 1e-6f)
            {
                uv1 = math.float2(radius, 0);
                uv2 = math.float2(-radius, 0);
            }
            else
            {
                var ul = math.length(U);
                var ulinv = math.rcp(ul);
                var vlinv = math.rcp(vl);

                // Normalize U and V in the plane.
                var u = U * ulinv;
                var v = V * vlinv;

                // Major and minor axis of the ellipse.
                var a = ul * radius;
                var b = vl * radius;

                // Project the camera position into a 2D coordinate system with the circle at (0, 0) and
                // the ellipse major and minor axes as the coordinate system axes. This allows us to use the standard
                // form of the ellipse equation, greatly simplifying the calculations.
                var cameraUV = math.float2(math.dot(-center, u), math.dot(-center, v));

                // Find the polar line of the camera position in the normalized UV coordinate system.
                var polar = math.float3(cameraUV.x / square(a), cameraUV.y / square(b), -1);
                var (t1, t2) = IntersectEllipseLine(a, b, polar);

                // Find Y by putting polar into line equation and solving. Denormalize by dividing by U and V lengths.
                uv1 = math.float2(t1 * ulinv, (-polar.x / polar.y * t1 - polar.z / polar.y) * vlinv);
                uv2 = math.float2(t2 * ulinv, (-polar.x / polar.y * t2 - polar.z / polar.y) * vlinv);
            }
        }

        public static bool IntersectCircleYPlane(
            float y, float3 circleCenter, float3 circleNormal, float3 circleU, float3 circleV, float circleRadius,
            out float3 p1, out float3 p2)
        {
            p1 = p2 = 0;

            // Intersecting a circle with a plane yields 2 points, or the whole circle if the plane and the plane of the
            // circle are the same, or nothing if the planes are parallel but offset. We're only interested in the first
            // case. Our other tests will catch the other cases.

            // The two points will be on the line of intersection of the two planes. Thus we first have to find that line.

            // Shoot 2 rays along the y-plane and intersect the circle plane. We then transform them into the circle
            // plane, so that we can work in 2D.
            var CdotN = math.dot(circleCenter, circleNormal);
            var h1v = math.float3(1, y, 1) * CdotN / math.dot(math.float3(1, y, 1), circleNormal) - circleCenter;
            var h1 = math.float2(math.dot(h1v, circleU), math.dot(h1v, circleV));
            var h2v = math.float3(-1, y, 1) * CdotN / math.dot(math.float3(-1, y, 1), circleNormal) - circleCenter;
            var h2 = math.float2(math.dot(h2v, circleU), math.dot(h2v, circleV));

            var lineDirection = math.normalize(h2 - h1);
            // We now have the direction of the line, and would like to find the point on it that is closest to the
            // circle center. A line in 2D is similar to a plane in 3D. So we can calculate a normal, which is just a
            // perpendicular/orthogonal direction, and then take the dot product to find the distance. This is similar
            // to when calculating the d-term for a plane in 3D, which is also just calculating the closest distance
            // from the origin to the plane.
            var lineNormal = math.float2(lineDirection.y, -lineDirection.x);
            var distToLine = math.dot(h1, lineNormal);
            // We can then get that point on the line by following our normal with the distance we just calculated.
            var lineCenter = lineNormal * distToLine;

            // Avoid negative square roots, as this means we've hit one of the cases that we do not care about.
            if (distToLine > circleRadius) return false;

            // What's left now is to intersect the line with the circle. We can do so with Pythagoras. Our triangle
            // is made up of `lineCenter`, the circle center and one of the intersection points.
            // We know the distance from `lineCenter` to the circle center (`distToLine`), and the distance from
            // the circle center to one of the intersection points must be the circle radius, as it lies on the
            // circle, forming the hypotenuse.
            var l = math.sqrt(circleRadius * circleRadius - distToLine * distToLine);

            // What we found above is the distance from `lineCenter` to each of the intersection points. So we just
            // scrub along the line in both directions using the found distance, and then transform back into view
            // space.
            var x1 = lineCenter + l * lineDirection;
            var x2 = lineCenter - l * lineDirection;
            p1 = circleCenter + x1.x * circleU + x1.y * circleV;
            p2 = circleCenter + x2.x * circleU + x2.y * circleV;

            return true;
        }

        public static void GetConeSideTangentPoints(float3 vertex, float3 axis, float cosHalfAngle, float circleRadius, float coneHeight, float range, float3 circleU, float3 circleV, out float3 l1, out float3 l2)
        {
            l1 = l2 = 0;

            if (math.dot(math.normalize(-vertex), axis) >= cosHalfAngle)
            {
                return;
            }

            var d = -math.dot(vertex, axis);
            // If d is zero, this leads to a numerical instability in the code later on. This is why we make the value
            // an epsilon if it is zero.
            if (d == 0f) d = 1e-6f;
            var sign = d < 0 ? -1f : 1f;
            // sign *= vertex.z < 0 ? -1f : 1f;
            // `origin` is the center of the circular slice we're about to calculate at distance `d` from the `vertex`.
            var origin = vertex + axis * d;
            // Get the radius of the circular slice of the cone at the `origin`.
            var radius = math.abs(d) * circleRadius / coneHeight;
            // `circleU` and `circleV` are the two vectors perpendicular to the cone's axis. `cameraUV` is thus the
            // position of the camera projected onto the plane of the circular slice. This basically creates a new
            // 2D coordinate space, with (0, 0) located at the center of the circular slice, which why this variable
            // is called `origin`.
            var cameraUV = math.float2(math.dot(circleU, -origin), math.dot(circleV, -origin));
            // Use homogeneous coordinates to find the tangents.
            var polar = math.float3(cameraUV, -square(radius));
            var p1 = math.float2(-1, -polar.x / polar.y * (-1) - polar.z / polar.y);
            var p2 = math.float2(1, -polar.x / polar.y * 1 - polar.z / polar.y);
            var lineDirection = math.normalize(p2 - p1);
            var lineNormal = math.float2(lineDirection.y, -lineDirection.x);
            var distToLine = math.dot(p1, lineNormal);
            var lineCenter = lineNormal * distToLine;
            var l = math.sqrt(radius * radius - distToLine * distToLine);
            var x1UV = lineCenter + l * lineDirection;
            var x2UV = lineCenter - l * lineDirection;
            var dir1 = math.normalize((origin + x1UV.x * circleU + x1UV.y * circleV) - vertex) * sign;
            var dir2 = math.normalize((origin + x2UV.x * circleU + x2UV.y * circleV) - vertex) * sign;
            l1 = dir1 * range;
            l2 = dir2 * range;
        }

        public static float3 EvaluateNearConic(float near, float3 o, float3 d, float r, float3 u, float3 v, float theta)
        {
            var h = (near - o.z) / (d.z + r * u.z * math.cos(theta) + r * v.z * math.sin(theta));
            return math.float3(o.xy + h * (d.xy + r * u.xy * math.cos(theta) + r * v.xy * math.sin(theta)), near);
        }

        // o, d, u and v are expected to contain {x or y, z}. I.e. pass in x values to find tangents where x' = 0
        // Returns the two theta values as a float2.
        public static float2 FindNearConicTangentTheta(float2 o, float2 d, float r, float2 u, float2 v)
        {
            var sqrt = math.sqrt(square(d.x) * square(u.y) + square(d.x) * square(v.y) - 2f * d.x * d.y * u.x * u.y - 2f * d.x * d.y * v.x * v.y + square(d.y) * square(u.x) + square(d.y) * square(v.x) - square(r) * square(u.x) * square(v.y) + 2f * square(r) * u.x * u.y * v.x * v.y - square(r) * square(u.y) * square(v.x));
            var denom = d.x * v.y - d.y * v.x - r * u.x * v.y + r * u.y * v.x;
            return 2 * math.atan((-d.x * u.y + d.y * u.x + math.float2(1, -1) * sqrt) / denom);
        }

        public static float2 FindNearConicYTheta(float near, float3 o, float3 d, float r, float3 u, float3 v, float y)
        {
            var sqrt = math.sqrt(-square(d.y) * square(o.z) + 2 * square(d.y) * o.z * near - square(d.y) * square(near) + 2 * d.y * d.z * o.y * o.z - 2 * d.y * d.z * o.y * near - 2 * d.y * d.z * o.z * y + 2 * d.y * d.z * y * near - square(d.z) * square(o.y) + 2 * square(d.z) * o.y * y - square(d.z) * square(y) + square(o.y) * square(r) * square(u.z) + square(o.y) * square(r) * square(v.z) - 2 * o.y * o.z * square(r) * u.y * u.z - 2 * o.y * o.z * square(r) * v.y * v.z - 2 * o.y * y * square(r) * square(u.z) - 2 * o.y * y * square(r) * square(v.z) + 2 * o.y * square(r) * u.y * u.z * near + 2 * o.y * square(r) * v.y * v.z * near + square(o.z) * square(r) * square(u.y) + square(o.z) * square(r) * square(v.y) + 2 * o.z * y * square(r) * u.y * u.z + 2 * o.z * y * square(r) * v.y * v.z - 2 * o.z * square(r) * square(u.y) * near - 2 * o.z * square(r) * square(v.y) * near + square(y) * square(r) * square(u.z) + square(y) * square(r) * square(v.z) - 2 * y * square(r) * u.y * u.z * near - 2 * y * square(r) * v.y * v.z * near + square(r) * square(u.y) * square(near) + square(r) * square(v.y) * square(near));
            var denom = d.y * o.z - d.y * near - d.z * o.y + d.z * y + o.y * r * u.z - o.z * r * u.y - y * r * u.z + r * u.y * near;
            return 2 * math.atan((r * (o.y * v.z - o.z * v.y - y * v.z + v.y * near) + math.float2(1, -1) * sqrt) / denom);
        }

    }
}
