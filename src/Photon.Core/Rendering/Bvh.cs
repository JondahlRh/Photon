// -----------------------------------------------------------------------------
// Author:        Jondahl Rhenius
// AI Assistance: Copilot for information & documentation
// -----------------------------------------------------------------------------

using System.Numerics;
using Photon.Core.Mathematics;
using Photon.Core.Scenes;

namespace Photon.Core.Rendering;

/// <summary>
/// Represents a flat, axis-aligned bounding volume hierarchy.
/// </summary>
public sealed class Bvh : IAccelerationStructure
{
    /// <summary>
    /// The largest number of primitives stored in a leaf node.
    /// </summary>
    private const int MaxPrimitivesPerLeaf = 4;

    /// <summary>
    /// Contains the primitives stored in this bounding volume hierarchy.
    /// </summary>
    private readonly IPrimitive[] _primitives;

    /// <summary>
    /// Stores the nodes of this bounding volume hierarchy.
    /// </summary>
    private readonly Node[] _nodes;

    /// <summary>
    /// Contains the index of the root node, or <c>-1</c> for an empty hierarchy.
    /// </summary>
    private int _rootNodeIndex = -1;

    /// <summary>
    /// Stores the bounds and either the child indices or primitive range for one BVH node.
    /// </summary>
    private readonly struct Node(
        Aabb bounds,
        int leftChildIndex,
        int rightChildIndex,
        int firstPrimitiveIndex,
        int primitiveCount
    )
    {
        /// <summary>
        /// Gets the bounding box enclosing this node.
        /// </summary>
        public Aabb Bounds { get; } = bounds;

        /// <summary>
        /// Gets the left child index, or -1 when this node is a leaf.
        /// </summary>
        public int LeftChildIndex { get; } = leftChildIndex;

        /// <summary>
        /// Gets the right child index, or -1 when this node is a leaf.
        /// </summary>
        public int RightChildIndex { get; } = rightChildIndex;

        /// <summary>
        /// Gets the first primitive index stored in this leaf.
        /// </summary>
        public int FirstPrimitiveIndex { get; } = firstPrimitiveIndex;

        /// <summary>
        /// Gets the number of primitives stored in this leaf; zero for an inner node.
        /// </summary>
        public int PrimitiveCount { get; } = primitiveCount;
    }

    /// <summary>
    /// Specifies the coordinate axis used to split a BVH node.
    /// </summary>
    private enum SplitAxis
    {
        /// <summary>
        /// The X axis.
        /// </summary>
        X,

        /// <summary>
        /// The Y axis.
        /// </summary>
        Y,

        /// <summary>
        /// The Z axis.
        /// </summary>
        Z,
    }

    /// <summary>
    /// Computes the primitive bounds and centroid bounds for a range of primitives.
    /// </summary>
    /// <param name="start">The index of the first primitive in the range.</param>
    /// <param name="count">The number of primitives in the range.</param>
    /// <returns>The combined primitive bounds and the bounds of their centers.</returns>
    private (Aabb PrimitiveBounds, Aabb CentroidBounds) ComputeRangeBounds(int start, int count)
    {
        Aabb primitiveBounds = Aabb.Empty;
        Aabb centroidBounds = Aabb.Empty;

        for (int i = start; i < start + count; i++)
        {
            primitiveBounds = primitiveBounds.Include(_primitives[i].Bounds);
            centroidBounds = centroidBounds.Include(_primitives[i].Bounds.Center);
        }

        return (primitiveBounds, centroidBounds);
    }

    /// <summary>
    /// Builds a node for the specified primitive range.
    /// </summary>
    /// <param name="nodes">The list of nodes being built.</param>
    /// <param name="start">The index of the first primitive in the range.</param>
    /// <param name="count">The number of primitives in the range.</param>
    /// <returns>The index of the newly built node.</returns>
    private int BuildNode(List<Node> nodes, int start, int count)
    {
        (Aabb bounds, Aabb centroidBounds) = ComputeRangeBounds(start, count);

        if (count <= MaxPrimitivesPerLeaf)
        {
            nodes.Add(new Node(bounds, -1, -1, start, count));
            return nodes.Count - 1;
        }

        // Split along the axis with the largest centroid distribution.
        var size = centroidBounds.Size;
        SplitAxis axis;
        if (size.X > size.Y && size.X > size.Z)
        {
            axis = SplitAxis.X;
        }
        else if (size.Y > size.Z)
        {
            axis = SplitAxis.Y;
        }
        else
        {
            axis = SplitAxis.Z;
        }

        Array.Sort(
            _primitives,
            start,
            count,
            Comparer<IPrimitive>.Create(
                (left, right) =>
                {
                    return axis switch
                    {
                        SplitAxis.X => left.Bounds.Center.X.CompareTo(right.Bounds.Center.X),
                        SplitAxis.Y => left.Bounds.Center.Y.CompareTo(right.Bounds.Center.Y),
                        SplitAxis.Z => left.Bounds.Center.Z.CompareTo(right.Bounds.Center.Z),
                        _ => throw new InvalidOperationException("Unknown BVH split axis."),
                    };
                }
            )
        );

        var leftCount = count / 2;
        var leftChild = BuildNode(nodes, start, leftCount);
        var rightChild = BuildNode(nodes, start + leftCount, count - leftCount);

        var node = new Node(bounds, leftChild, rightChild, -1, 0);
        nodes.Add(node);

        return nodes.Count - 1;
    }

    /// <summary>
    /// Initializes a new bounding volume hierarchy for the specified scene.
    /// </summary>
    /// <param name="scene">The scene whose objects are queried.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="scene"/> is <see langword="null"/>.</exception>
    public Bvh(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        _primitives = scene.Model.Primitives.ToArray();
        var nodes = new List<Node>();
        if (_primitives.Length > 0)
        {
            _rootNodeIndex = BuildNode(nodes, 0, _primitives.Length);
        }

        _nodes = nodes.ToArray();
    }

    /// <summary>
    /// Finds the closest primitive intersection within the specified distance range.
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <param name="minDistance">The minimum distance along the ray to consider.</param>
    /// <param name="maxDistance">The maximum distance along the ray to consider.</param>
    /// <param name="hit">Receives information about the closest hit, if one is found.</param>
    /// <returns><see langword="true"/> if the ray intersects the hierarchy; otherwise, <see langword="false"/>.</returns>
    public bool Intersect(in Ray ray, float minDistance, float maxDistance, out HitInfo hit)
    {
        hit = default;

        if (
            _rootNodeIndex == -1
            || !float.IsFinite(minDistance)
            || !float.IsFinite(maxDistance)
            || minDistance < 0f
            || maxDistance < 0f
            || minDistance > maxDistance
        )
        {
            return false;
        }

        var inverseDirection = Vector3.One / ray.Direction;
        var closestDistance = maxDistance;
        var foundHit = false;

        var nodesToVisit = new Stack<int>();
        nodesToVisit.Push(_rootNodeIndex);

        while (nodesToVisit.Count > 0)
        {
            var node = _nodes[nodesToVisit.Pop()];

            var intersectsBounds = node.Bounds.Intersects(
                ray,
                inverseDirection,
                minDistance,
                closestDistance
            );

            if (!intersectsBounds)
            {
                continue;
            }

            if (node.PrimitiveCount == 0)
            {
                if (node.LeftChildIndex != -1)
                {
                    nodesToVisit.Push(node.LeftChildIndex);
                }

                if (node.RightChildIndex != -1)
                {
                    nodesToVisit.Push(node.RightChildIndex);
                }

                continue;
            }

            var start = node.FirstPrimitiveIndex;
            var end = node.FirstPrimitiveIndex + node.PrimitiveCount;

            for (int i = start; i < end; i++)
            {
                var intersectsPrimitive = _primitives[i]
                    .Intersect(ray, minDistance, closestDistance, out HitInfo primitiveHit);

                if (!intersectsPrimitive)
                {
                    continue;
                }

                closestDistance = primitiveHit.Distance;
                hit = primitiveHit;
                foundHit = true;
            }
        }

        return foundHit;
    }
}
