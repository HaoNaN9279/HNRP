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
    [BurstCompile(FloatMode = FloatMode.Default, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    struct TilingJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<VisibleLight> lights;

        [ReadOnly]
        public NativeArray<VisibleReflectionProbe> reflectionProbes;

        [NativeDisableParallelForRestriction]
        public NativeArray<InclusiveRange> tileRanges;

        public int itemsPerTile;
        public int rangesPerItem;

        public float4x4 worldToView;

        public float2 tileScale;
        public float2 tileScaleInv;
        public float viewPlaneBottom;
        public float viewPlaneTop;
        public float4 viewToViewportScaleBias;
        public int2 tileCount;
        public float near;
        public bool isOrthographic;

        InclusiveRange m_TileYRange;
        int m_Offset;
        float2 m_CenterOffset;

        public void Execute(int jobIndex)
        {
            var index = jobIndex % itemsPerTile;
            m_Offset = jobIndex * rangesPerItem;

            m_TileYRange = new InclusiveRange(short.MaxValue, short.MinValue);

            for (var i = 0; i < rangesPerItem; i++)
            {
                tileRanges[m_Offset + i] = new InclusiveRange(short.MaxValue, short.MinValue);
            }


            if (index < lights.Length)
            {
                if (isOrthographic) { TileLightOrthographic(index); }
                else { TileLight(index); }
            }
            else { TileReflectionProbe(index); }
        }

        void TileLight(int lightIndex)
        {
            var light = lights[lightIndex];
            var lightToWorld = (float4x4)light.localToWorldMatrix;
            var lightPositionVS = math.mul(worldToView, math.float4(lightToWorld.c3.xyz, 1)).xyz;
            lightPositionVS.z *= -1;
            if (lightPositionVS.z >= near) ExpandY(lightPositionVS);
            var lightDirectionVS = math.normalize(math.mul(worldToView, math.float4(lightToWorld.c2.xyz, 0)).xyz);
            lightDirectionVS.z *= -1;

            var halfAngle = math.radians(light.spotAngle * 0.5f);
            var range = light.range;
            var rangesq = ClusterCullingJobCommon.square(range);
            var cosHalfAngle = math.cos(halfAngle);
            var coneHeight = cosHalfAngle * range;

            // Radius of circle formed by intersection of sphere and near plane.
            // Found using Pythagoras with a right triangle formed by three points:
            // (a) light position
            // (b) light position projected to near plane
            // (c) a point on the near plane at a distance `range` from the light position
            //     (i.e. lies both on the sphere and the near plane)
            // Thus the hypotenuse is formed by (a) and (c) with length `range`, and the known side is formed
            // by (a) and (b) with length equal to the distance between the near plane and the light position.
            // The remaining unknown side is formed by (b) and (c) with length equal to the radius of the circle.
            // m_ClipCircleRadius = sqrt(sq(light.range) - sq(m_Near - m_LightPosition.z));
            var sphereClipRadius = math.sqrt(rangesq - ClusterCullingJobCommon.square(near - lightPositionVS.z));

            // Assumes a point on the sphere, i.e. at distance `range` from the light position.
            // If spot light, we check the angle between the direction vector from the light position and the light direction vector.
            // Note that division by range is to normalize the vector, as we know that the resulting vector will have length `range`.
            bool SpherePointIsValid(float3 p) => light.lightType == LightType.Point ||
                math.dot(math.normalize(p - lightPositionVS), lightDirectionVS) >= cosHalfAngle;

            // Project light sphere onto YZ plane, find the horizon points, and re-construct view space position of found points.
            // CalculateSphereYBounds(lightPositionVS, range, near, sphereClipRadius, out var sphereBoundY0, out var sphereBoundY1);
            ClusterCullingJobCommon.GetSphereHorizon(lightPositionVS.yz, range, near, sphereClipRadius, out var sphereBoundYZ0, out var sphereBoundYZ1);
            var sphereBoundY0 = math.float3(lightPositionVS.x, sphereBoundYZ0);
            var sphereBoundY1 = math.float3(lightPositionVS.x, sphereBoundYZ1);
            if (SpherePointIsValid(sphereBoundY0)) ExpandY(sphereBoundY0);
            if (SpherePointIsValid(sphereBoundY1)) ExpandY(sphereBoundY1);

            // Project light sphere onto XZ plane, find the horizon points, and re-construct view space position of found points.
            ClusterCullingJobCommon.GetSphereHorizon(lightPositionVS.xz, range, near, sphereClipRadius, out var sphereBoundXZ0, out var sphereBoundXZ1);
            var sphereBoundX0 = math.float3(sphereBoundXZ0.x, lightPositionVS.y, sphereBoundXZ0.y);
            var sphereBoundX1 = math.float3(sphereBoundXZ1.x, lightPositionVS.y, sphereBoundXZ1.y);
            if (SpherePointIsValid(sphereBoundX0)) ExpandY(sphereBoundX0);
            if (SpherePointIsValid(sphereBoundX1)) ExpandY(sphereBoundX1);

            if (light.lightType == LightType.Spot)
            {
                // Cone base
                var baseRadius = math.sqrt(range * range - coneHeight * coneHeight);
                var baseCenter = lightPositionVS + lightDirectionVS * coneHeight;

                // Project cone base (a circle) into the YZ plane, find the horizon points, and re-construct view space position of found points.
                // When projecting a circle to a plane, it becomes an ellipse where the major axis is parallel to the line
                // of intersection of the projection plane and the circle plane. We can get this by taking the cross product
                // of the two plane normals, as the line of intersection will have to be a vector in both planes, and thus
                // orthogonal to both normals.
                // If the two plane normals are parallel, the cross product would return 0. In that case, the circle will
                // project to a line segment, so we pick a vector in the plane pointing in the direction we're interested
                // in finding horizon points in.
                var baseUY = math.abs(math.abs(lightDirectionVS.x) - 1) < 1e-6f ? math.float3(0, 1, 0) : math.normalize(math.cross(lightDirectionVS, math.float3(1, 0, 0)));
                var baseVY = math.cross(lightDirectionVS, baseUY);
                ClusterCullingJobCommon.GetProjectedCircleHorizon(baseCenter.yz, baseRadius, baseUY.yz, baseVY.yz, out var baseY1UV, out var baseY2UV);
                var baseY1 = baseCenter + baseY1UV.x * baseUY + baseY1UV.y * baseVY;
                var baseY2 = baseCenter + baseY2UV.x * baseUY + baseY2UV.y * baseVY;
                if (baseY1.z >= near) ExpandY(baseY1);
                if (baseY2.z >= near) ExpandY(baseY2);

                // Project cone base into the XZ plane, find the horizon points, and re-construct view space position of found points.
                // See comment for YZ plane for details.
                var baseUX = math.abs(math.abs(lightDirectionVS.y) - 1) < 1e-6f ? math.float3(1, 0, 0) : math.normalize(math.cross(lightDirectionVS, math.float3(0, 1, 0)));
                var baseVX = math.cross(lightDirectionVS, baseUX);
                ClusterCullingJobCommon.GetProjectedCircleHorizon(baseCenter.xz, baseRadius, baseUX.xz, baseVX.xz, out var baseX1UV, out var baseX2UV);
                var baseX1 = baseCenter + baseX1UV.x * baseUX + baseX1UV.y * baseVX;
                var baseX2 = baseCenter + baseX2UV.x * baseUX + baseX2UV.y * baseVX;
                if (baseX1.z >= near) ExpandY(baseX1);
                if (baseX2.z >= near) ExpandY(baseX2);

                // Handle base circle clipping by intersecting it with the near-plane if needed.
                if (ClusterCullingJobCommon.GetCircleClipPoints(baseCenter, lightDirectionVS, baseRadius, near, out var baseClip0, out var baseClip1))
                {
                    ExpandY(baseClip0);
                    ExpandY(baseClip1);
                }

                bool ConicPointIsValid(float3 p) =>
                    math.dot(math.normalize(p - lightPositionVS), lightDirectionVS) >= 0 &&
                    math.dot(p - lightPositionVS, lightDirectionVS) <= coneHeight;

                // Calculate Z bounds of cone and check if it's overlapping with the near plane.
                // From https://www.iquilezles.org/www/articles/diskbbox/diskbbox.htm
                var baseExtentZ = baseRadius * math.sqrt(1.0f - ClusterCullingJobCommon.square(lightDirectionVS.z));
                var coneIsClipping = near >= math.min(baseCenter.z - baseExtentZ, lightPositionVS.z) && near <= math.max(baseCenter.z + baseExtentZ, lightPositionVS.z);

                var coneU = math.cross(lightDirectionVS, lightPositionVS);
                // The cross product will be the 0-vector if the light-direction and camera-to-light-position vectors are parallel.
                // In that case, {1, 0, 0} is orthogonal to the light direction and we use that instead.
                coneU = math.csum(coneU) != 0f ? math.normalize(coneU) : math.float3(1, 0, 0);
                var coneV = math.cross(lightDirectionVS, coneU);

                if (coneIsClipping)
                {
                    var r = baseRadius / coneHeight;

                    // Find the Y bounds of the near-plane cone intersection, i.e. where y' = 0
                    var thetaY = ClusterCullingJobCommon.FindNearConicTangentTheta(lightPositionVS.yz, lightDirectionVS.yz, r, coneU.yz, coneV.yz);
                    var p0Y = ClusterCullingJobCommon.EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, thetaY.x);
                    var p1Y = ClusterCullingJobCommon.EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, thetaY.y);
                    if (ConicPointIsValid(p0Y)) ExpandY(p0Y);
                    if (ConicPointIsValid(p1Y)) ExpandY(p1Y);

                    // Find the X bounds of the near-plane cone intersection, i.e. where x' = 0
                    var thetaX = ClusterCullingJobCommon.FindNearConicTangentTheta(lightPositionVS.xz, lightDirectionVS.xz, r, coneU.xz, coneV.xz);
                    var p0X = ClusterCullingJobCommon.EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, thetaX.x);
                    var p1X = ClusterCullingJobCommon.EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, thetaX.y);
                    if (ConicPointIsValid(p0X)) ExpandY(p0X);
                    if (ConicPointIsValid(p1X)) ExpandY(p1X);
                }

                // Calculate the lines making up the sides of the cone as seen from the camera. `l1` and `l2` form lines
                // from the light position.
                ClusterCullingJobCommon.GetConeSideTangentPoints(lightPositionVS, lightDirectionVS, cosHalfAngle, baseRadius, coneHeight, range, coneU, coneV, out var l1, out var l2);

                {
                    var planeNormal = math.float3(0, 1, viewPlaneBottom);
                    var l1t = math.dot(-lightPositionVS, planeNormal) / math.dot(l1, planeNormal);
                    var l1x = lightPositionVS + l1 * l1t;
                    if (l1t >= 0 && l1t <= 1 && l1x.z >= near) ExpandY(l1x);
                }
                {
                    var planeNormal = math.float3(0, 1, viewPlaneTop);
                    var l1t = math.dot(-lightPositionVS, planeNormal) / math.dot(l1, planeNormal);
                    var l1x = lightPositionVS + l1 * l1t;
                    if (l1t >= 0 && l1t <= 1 && l1x.z >= near) ExpandY(l1x);
                }

                m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

                // Calculate tile plane ranges for cone.
                for (var planeIndex = m_TileYRange.start + 1; planeIndex <= m_TileYRange.end; planeIndex++)
                {
                    var planeRange = InclusiveRange.empty;

                    // Y-position on the view plane (Z=1)
                    var planeY = math.lerp(viewPlaneBottom, viewPlaneTop, planeIndex * tileScaleInv.y);

                    var planeNormal = math.float3(0, 1, -planeY);

                    // Intersect lines with y-plane and clip if needed.
                    var l1t = math.dot(-lightPositionVS, planeNormal) / math.dot(l1, planeNormal);
                    var l1x = lightPositionVS + l1 * l1t;
                    if (l1t >= 0 && l1t <= 1 && l1x.z >= near) planeRange.Expand((short)math.clamp(ViewToTileSpace(l1x).x, 0, tileCount.x - 1));

                    var l2t = math.dot(-lightPositionVS, planeNormal) / math.dot(l2, planeNormal);
                    var l2x = lightPositionVS + l2 * l2t;
                    if (l2t >= 0 && l2t <= 1 && l2x.z >= near) planeRange.Expand((short)math.clamp(ViewToTileSpace(l2x).x, 0, tileCount.x - 1));

                    if (ClusterCullingJobCommon.IntersectCircleYPlane(planeY, baseCenter, lightDirectionVS, baseUY, baseVY, baseRadius, out var circleTile0, out var circleTile1))
                    {
                        if (circleTile0.z >= near) planeRange.Expand((short)math.clamp(ViewToTileSpace(circleTile0).x, 0, tileCount.x - 1));
                        if (circleTile1.z >= near) planeRange.Expand((short)math.clamp(ViewToTileSpace(circleTile1).x, 0, tileCount.x - 1));
                    }

                    if (coneIsClipping)
                    {
                        var y = planeY * near;
                        var r = baseRadius / coneHeight;
                        var theta = ClusterCullingJobCommon.FindNearConicYTheta(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, y);
                        var p0 = math.float3(ClusterCullingJobCommon.EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, theta.x).x, y, near);
                        var p1 = math.float3(ClusterCullingJobCommon.EvaluateNearConic(near, lightPositionVS, lightDirectionVS, r, coneU, coneV, theta.y).x, y, near);
                        if (ConicPointIsValid(p0)) planeRange.Expand((short)math.clamp(ViewToTileSpace(p0).x, 0, tileCount.x - 1));
                        if (ConicPointIsValid(p1)) planeRange.Expand((short)math.clamp(ViewToTileSpace(p1).x, 0, tileCount.x - 1));
                    }

                    // Write to tile ranges above and below the plane. Note that at `m_Offset` we store Y-range.
                    var tileIndex = m_Offset + 1 + planeIndex;
                    tileRanges[tileIndex] = InclusiveRange.Merge(tileRanges[tileIndex], planeRange);
                    tileRanges[tileIndex - 1] = InclusiveRange.Merge(tileRanges[tileIndex - 1], planeRange);
                }
            }

            m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

            // Calculate tile plane ranges for sphere.
            for (var planeIndex = m_TileYRange.start + 1; planeIndex <= m_TileYRange.end; planeIndex++)
            {
                var planeRange = InclusiveRange.empty;

                var planeY = math.lerp(viewPlaneBottom, viewPlaneTop, planeIndex * tileScaleInv.y);
                ClusterCullingJobCommon.GetSphereYPlaneHorizon(lightPositionVS, range, near, sphereClipRadius, planeY, out var sphereTile0, out var sphereTile1);
                if (SpherePointIsValid(sphereTile0)) planeRange.Expand((short)math.clamp(ViewToTileSpace(sphereTile0).x, 0, tileCount.x - 1));
                if (SpherePointIsValid(sphereTile1)) planeRange.Expand((short)math.clamp(ViewToTileSpace(sphereTile1).x, 0, tileCount.x - 1));

                var tileIndex = m_Offset + 1 + planeIndex;
                tileRanges[tileIndex] = InclusiveRange.Merge(tileRanges[tileIndex], planeRange);
                tileRanges[tileIndex - 1] = InclusiveRange.Merge(tileRanges[tileIndex - 1], planeRange);
            }

            tileRanges[m_Offset] = m_TileYRange;
        }

        void TileLightOrthographic(int lightIndex)
        {
            var light = lights[lightIndex];
            var lightToWorld = (float4x4)light.localToWorldMatrix;
            var lightPosVS = math.mul(worldToView, math.float4(lightToWorld.c3.xyz, 1)).xyz;
            lightPosVS.z *= -1;
            ExpandOrthographic(lightPosVS);
            var lightDirVS = math.mul(worldToView, math.float4(lightToWorld.c2.xyz, 0)).xyz;
            lightDirVS.z *= -1;
            lightDirVS = math.normalize(lightDirVS);

            var halfAngle = math.radians(light.spotAngle * 0.5f);
            var range = light.range;
            var rangeSq = ClusterCullingJobCommon.square(range);
            var cosHalfAngle = math.cos(halfAngle);
            var coneHeight = cosHalfAngle * range;
            var coneHeightSq = ClusterCullingJobCommon.square(coneHeight);
            var coneHeightInv = 1f / coneHeight;
            var coneHeightInvSq = ClusterCullingJobCommon.square(coneHeightInv);

            bool SpherePointIsValid(float3 p) => light.lightType == LightType.Point ||
                math.dot(math.normalize(p - lightPosVS), lightDirVS) >= cosHalfAngle;

            var sphereBoundY0 = lightPosVS - math.float3(0, range, 0);
            var sphereBoundY1 = lightPosVS + math.float3(0, range, 0);
            var sphereBoundX0 = lightPosVS - math.float3(range, 0, 0);
            var sphereBoundX1 = lightPosVS + math.float3(range, 0, 0);

            if (SpherePointIsValid(sphereBoundY0)) ExpandOrthographic(sphereBoundY0);
            if (SpherePointIsValid(sphereBoundY1)) ExpandOrthographic(sphereBoundY1);
            if (SpherePointIsValid(sphereBoundX0)) ExpandOrthographic(sphereBoundX0);
            if (SpherePointIsValid(sphereBoundX1)) ExpandOrthographic(sphereBoundX1);

            var circleCenter = lightPosVS + lightDirVS * coneHeight;
            var circleRadius = math.sqrt(rangeSq - coneHeightSq);
            var circleRadiusSq = ClusterCullingJobCommon.square(circleRadius);
            var circleUp = math.normalize(math.float3(0, 1, 0) - lightDirVS * lightDirVS.y);
            var circleRight = math.normalize(math.float3(1, 0, 0) - lightDirVS * lightDirVS.x);
            var circleBoundY0 = circleCenter - circleUp * circleRadius;
            var circleBoundY1 = circleCenter + circleUp * circleRadius;

            if (light.lightType == LightType.Spot)
            {
                var circleBoundX0 = circleCenter - circleRight * circleRadius;
                var circleBoundX1 = circleCenter + circleRight * circleRadius;
                ExpandOrthographic(circleBoundY0);
                ExpandOrthographic(circleBoundY1);
                ExpandOrthographic(circleBoundX0);
                ExpandOrthographic(circleBoundX1);
            }

            m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

            // Find two lines in screen-space for the cone if the light is a spot.
            float coneDir0X = 0, coneDir0YInv = 0, coneDir1X = 0, coneDir1YInv = 0;
            if (light.lightType == LightType.Spot)
            {
                // Distance from light position to and radius of sphere fitted to the end of the cone.
                var sphereDistance = coneHeight + circleRadiusSq * coneHeightInv;
                var sphereRadius = math.sqrt(ClusterCullingJobCommon.square(circleRadiusSq) * coneHeightInvSq + circleRadiusSq);
                var directionXYSqInv = math.rcp(math.lengthsq(lightDirVS.xy));
                var polarIntersection = -circleRadiusSq * coneHeightInv * directionXYSqInv * lightDirVS.xy;
                var polarDir = math.sqrt((ClusterCullingJobCommon.square(sphereRadius) - math.lengthsq(polarIntersection)) * directionXYSqInv) * math.float2(lightDirVS.y, -lightDirVS.x);
                var conePBase = lightPosVS.xy + sphereDistance * lightDirVS.xy + polarIntersection;
                var coneP0 = conePBase - polarDir;
                var coneP1 = conePBase + polarDir;

                coneDir0X = coneP0.x - lightPosVS.x;
                coneDir0YInv = math.rcp(coneP0.y - lightPosVS.y);
                coneDir1X = coneP1.x - lightPosVS.x;
                coneDir1YInv = math.rcp(coneP1.y - lightPosVS.y);
            }

            // Tile plane ranges
            for (var planeIndex = m_TileYRange.start + 1; planeIndex <= m_TileYRange.end; planeIndex++)
            {
                var planeRange = InclusiveRange.empty;

                // Sphere
                var planeY = math.lerp(viewPlaneBottom, viewPlaneTop, planeIndex * tileScaleInv.y);
                var sphereX = math.sqrt(rangeSq - ClusterCullingJobCommon.square(planeY - lightPosVS.y));
                var sphereX0 = math.float3(lightPosVS.x - sphereX, planeY, lightPosVS.z);
                var sphereX1 = math.float3(lightPosVS.x + sphereX, planeY, lightPosVS.z);
                if (SpherePointIsValid(sphereX0)) { ExpandRangeOrthographic(ref planeRange, sphereX0.x); }
                if (SpherePointIsValid(sphereX1)) { ExpandRangeOrthographic(ref planeRange, sphereX1.x); }

                if (light.lightType == LightType.Spot)
                {
                    // Circle
                    if (planeY >= circleBoundY0.y && planeY <= circleBoundY1.y)
                    {
                        var intersectionDistance = (planeY - circleCenter.y) / circleUp.y;
                        var closestPointX = circleCenter.x + intersectionDistance * circleUp.x;
                        var intersectionDirX = -lightDirVS.z / math.length(math.float3(-lightDirVS.z, 0, lightDirVS.x));
                        var sideDistance = math.sqrt(ClusterCullingJobCommon.square(circleRadius) - ClusterCullingJobCommon.square(intersectionDistance));
                        var circleX0 = closestPointX - sideDistance * intersectionDirX;
                        var circleX1 = closestPointX + sideDistance * intersectionDirX;
                        ExpandRangeOrthographic(ref planeRange, circleX0);
                        ExpandRangeOrthographic(ref planeRange, circleX1);
                    }

                    // Cone
                    var deltaY = planeY - lightPosVS.y;
                    var coneT0 = deltaY * coneDir0YInv;
                    var coneT1 = deltaY * coneDir1YInv;
                    if (coneT0 >= 0 && coneT0 <= 1) { ExpandRangeOrthographic(ref planeRange, lightPosVS.x + coneT0 * coneDir0X); }
                    if (coneT1 >= 0 && coneT1 <= 1) { ExpandRangeOrthographic(ref planeRange, lightPosVS.x + coneT1 * coneDir1X); }
                }

                var tileIndex = m_Offset + 1 + planeIndex;
                tileRanges[tileIndex] = InclusiveRange.Merge(tileRanges[tileIndex], planeRange);
                tileRanges[tileIndex - 1] = InclusiveRange.Merge(tileRanges[tileIndex - 1], planeRange);
            }

            tileRanges[m_Offset] = m_TileYRange;
        }

        static readonly float3[] k_CubePoints =
        {
            new(-1, -1, -1),
            new(-1, -1, +1),
            new(-1, +1, -1),
            new(-1, +1, +1),
            new(+1, -1, -1),
            new(+1, -1, +1),
            new(+1, +1, -1),
            new(+1, +1, +1),
        };

        // Each item represents 3 lines, with x being the start index and yzw the end indices.
        static readonly int4[] k_CubeLineIndices =
        {
            // (-1, -1, -1) -> {(+1, -1, -1), (-1, +1, -1), (-1, -1, +1)}
            new(0, 4, 2, 1),

            // (-1, +1, +1) -> {(+1, +1, +1), (-1, -1, +1), (-1, +1, -1)}
            new(3, 7, 1, 2),

            // (+1, -1, +1) -> {(-1, -1, +1), (+1, +1, +1), (+1, -1, -1)}
            new(5, 1, 7, 4),

            // (+1, +1, -1) -> {(-1, +1, -1), (+1, -1, -1), (+1, +1, +1)}
            new(6, 2, 4, 7),
        };

        void TileReflectionProbe(int index)
        {
            // The algorithm used here works by clipping all the lines of the cube against the near-plane, and then
            // projects the resulting points to the view plane. These points are then used to construct a 2D convex
            // hull, which we can iterate linearly to get the lines on screen making up the cube.

            var reflectionProbe = reflectionProbes[index - lights.Length];
            var centerWS = (float3)reflectionProbe.bounds.center;
            var extentsWS = (float3)reflectionProbe.bounds.extents;

            // The vertices of the cube in view space.
            var points = new NativeArray<float3>(k_CubePoints.Length, Allocator.Temp);
            // This is initially filled with just the cube vertices that lie in front of the near plane.
            var clippedPoints = new NativeArray<float2>(k_CubePoints.Length + k_CubeLineIndices.Length * 3, Allocator.Temp);
            var clippedPointsCount = 0;
            var leftmostIndex = 0;
            for (var i = 0; i < k_CubePoints.Length; i++)
            {
                var point = math.mul(worldToView, math.float4(centerWS + extentsWS * k_CubePoints[i], 1)).xyz;
                point.z *= -1;
                points[i] = point;
                if (point.z >= near)
                {
                    var clippedPoint = isOrthographic ? point.xy : point.xy/point.z;
                    var clippedIndex = clippedPointsCount++;
                    clippedPoints[clippedIndex] = clippedPoint;
                    if (clippedPoint.x < clippedPoints[leftmostIndex].x) leftmostIndex = clippedIndex;
                }
            }

            // Clip the cube's line segments with the near plane, and add the new vertices to clippedPoints. Only lines
            // that are clipped will generate new vertices.
            for (var i = 0; i < k_CubeLineIndices.Length; i++)
            {
                var indices = k_CubeLineIndices[i];
                var p0 = points[indices.x];
                for (var j = 0; j < 3; j++)
                {
                    var p1 = points[indices[j+1]];
                    // The entire line is in front of the near plane.
                    if (p0.z < near && p1.z < near) continue;
                    // Check whether the line needs clipping.
                    if (p0.z < near || p1.z < near)
                    {
                        var d = (near - p0.z) / (p1.z - p0.z);
                        var p = math.lerp(p0, p1, d);
                        var clippedPoint = isOrthographic ? p.xy : p.xy/p.z;
                        var clippedIndex = clippedPointsCount++;
                        clippedPoints[clippedIndex] = clippedPoint;
                        if (clippedPoint.x < clippedPoints[leftmostIndex].x) leftmostIndex = clippedIndex;
                    }
                }
            }

            // Construct the convex hull. It is formed by the line loop consisting of the points in the array.
            var hullPoints = new NativeArray<float2>(clippedPointsCount, Allocator.Temp);
            var hullPointsCount = 0;

            if (clippedPointsCount > 0)
            {
                // Start with the leftmost point, as that is guaranteed to be on the hull.
                var hullPointIndex = leftmostIndex;

                // Find the remaining hull points until we end up back at the leftmost point.
                do
                {
                    var hullPoint = clippedPoints[hullPointIndex];
                    ExpandY(math.float3(hullPoint, 1));
                    hullPoints[hullPointsCount++] = hullPoint;

                    // Find the endpoint resulting in the leftmost turning line. This line will be a part of the hull.
                    var endpointIndex = 0;
                    var endpointLine = clippedPoints[endpointIndex] - hullPoint;
                    for (var i = 0; i < clippedPointsCount; i++)
                    {
                        var candidateLine = clippedPoints[i] - hullPoint;
                        var det = math.determinant(math.float2x2(endpointLine, candidateLine));

                        // Check if point i lies on the left side of the line to the current endpoint, or if it lies
                        // collinear to the current endpoint but farther away.
                        if (endpointIndex == hullPointIndex || det > 0 || (det == 0.0f && math.lengthsq(candidateLine) > math.lengthsq(endpointLine)))
                        {
                            endpointIndex = i;
                            endpointLine = candidateLine;
                        }
                    }

                    hullPointIndex = endpointIndex;
                } while (hullPointIndex != leftmostIndex && hullPointsCount < clippedPointsCount);

                m_TileYRange.Clamp(0, (short)(tileCount.y - 1));

                // Calculate tile plane ranges for sphere.
                for (var planeIndex = m_TileYRange.start + 1; planeIndex <= m_TileYRange.end; planeIndex++)
                {
                    var planeRange = InclusiveRange.empty;

                    var planeY = math.lerp(viewPlaneBottom, viewPlaneTop, planeIndex * tileScaleInv.y);

                    for (var i = 0; i < hullPointsCount; i++)
                    {
                        var hp0 = hullPoints[i];
                        var hp1 = hullPoints[(i + 1) % hullPointsCount];

                        // planeY = hp0 + t * (hp1 - hp0) => planeY - hp0 = t * (hp1 - hp0) => (planeY - hp0) / (hp1 - hp0) = t
                        var t = (planeY - hp0.y) / (hp1.y - hp0.y);
                        if (t < 0 || t > 1) continue;
                        var x = math.lerp(hp0.x, hp1.x, t);

                        var p = math.float3(x, planeY, 1);
                        var pTS = isOrthographic ? ViewToTileSpaceOrthographic(p) : ViewToTileSpace(p);
                        planeRange.Expand((short)math.clamp(pTS.x, 0, tileCount.x - 1));
                    }

                    var tileIndex = m_Offset + 1 + planeIndex;
                    tileRanges[tileIndex] = InclusiveRange.Merge(tileRanges[tileIndex], planeRange);
                    tileRanges[tileIndex - 1] = InclusiveRange.Merge(tileRanges[tileIndex - 1], planeRange);
                }

                tileRanges[m_Offset] = m_TileYRange;
            }

            hullPoints.Dispose();
            clippedPoints.Dispose();
            points.Dispose();
        }

        /// <summary>
        /// Project onto Z=1, scale and offset into [0, tileCount]
        /// </summary>
        float2 ViewToTileSpace(float3 positionVS)
        {
            return (positionVS.xy / positionVS.z * viewToViewportScaleBias.xy + viewToViewportScaleBias.zw) * tileScale;
        }

        /// <summary>
        /// Project onto Z=1, scale and offset into [0, tileCount]
        /// </summary>
        float2 ViewToTileSpaceOrthographic(float3 positionVS)
        {
            return (positionVS.xy * viewToViewportScaleBias.xy + viewToViewportScaleBias.zw) * tileScale;
        }

        /// <summary>
        /// Expands the tile Y range and the X range in the row containing the position.
        /// </summary>
        void ExpandY(float3 positionVS)
        {
            // var positionTS = math.clamp(ViewToTileSpace(positionVS), 0, tileCount - 1);
            var positionTS = ViewToTileSpace(positionVS);
            var tileY = (int)positionTS.y;
            var tileX = (int)positionTS.x;
            m_TileYRange.Expand((short)math.clamp(tileY, 0, tileCount.y - 1));
            if (tileY >= 0 && tileY < tileCount.y && tileX >= 0 && tileX < tileCount.x)
            {
                var rowXRange = tileRanges[m_Offset + 1 + tileY];
                rowXRange.Expand((short)tileX);
                tileRanges[m_Offset + 1 + tileY] = rowXRange;
            }
        }

        /// <summary>
        /// Expands the tile Y range and the X range in the row containing the position.
        /// </summary>
        void ExpandOrthographic(float3 positionVS)
        {
            // var positionTS = math.clamp(ViewToTileSpace(positionVS), 0, tileCount - 1);
            var positionTS = ViewToTileSpaceOrthographic(positionVS);
            var tileY = (int)positionTS.y;
            var tileX = (int)positionTS.x;
            m_TileYRange.Expand((short)math.clamp(tileY, 0, tileCount.y - 1));
            if (tileY >= 0 && tileY < tileCount.y && tileX >= 0 && tileX < tileCount.x)
            {
                var rowXRange = tileRanges[m_Offset + 1 + tileY];
                rowXRange.Expand((short)tileX);
                tileRanges[m_Offset + 1 + tileY] = rowXRange;
            }
        }

        void ExpandRangeOrthographic(ref InclusiveRange range, float xVS)
        {
            range.Expand((short)math.clamp(ViewToTileSpaceOrthographic(xVS).x, 0, tileCount.x - 1));
        }

    }
}
