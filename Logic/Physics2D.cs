using Godot;
using System.Collections.Generic;

namespace Utilities.Logic;

public partial class Physics2D : Node
{
    private const int MaxPoolSize = 32;

    private static PhysicsDirectSpaceState2D spaceState;

    private static readonly Stack<PhysicsRayQueryParameters2D> queryPool = new();
    private static readonly object poolLock = new();

    public static void UpdateSpaceState(Viewport viewport)
    {
        spaceState = viewport.World2D.DirectSpaceState;
    }

    public static bool Raycast(Vector2 from, Vector2 to, out RaycastHit hit, uint collisionMask = uint.MaxValue, Godot.Collections.Array<Rid> exclude = null)
    {
        var query           = GetQuery(from, to);
        query.CollisionMask = collisionMask;
        query.HitFromInside = true;

        if (exclude != null)
            query.Exclude = exclude;
        
        var result = spaceState.IntersectRay(query);

        ReturnQuery(query);

        if (result.Count > 0)
        {
            hit = new RaycastHit
            {
                Position    = (Vector2)result["position"],
                Normal      = (Vector2)result["normal"],
                Collider    = (Node2D)(GodotObject)result["collider"],
                ColliderRid = (Rid)result["rid"],
                Shape       = (int)result["shape"],
                Distance    = from.DistanceTo((Vector2)result["position"])
            };

            return true;
        }

        hit = default;
        return false;
    }

    public static bool Raycast(Vector2 origin, Vector2 direction, float maxDistance, out RaycastHit hit, params int[] layers)
    {
        uint collisionMasks = GetLayerMask(layers);
        return Raycast(origin, origin + direction.Normalized() * maxDistance, out hit, collisionMasks);
    }

    private static PhysicsRayQueryParameters2D GetQuery(Vector2 from, Vector2 to)
    {
        lock(poolLock)
        {
            if (queryPool.Count > 0)
            {
                var query  = queryPool.Pop();
                query.From = from;
                query.To   = to;
                
                query.CollisionMask     = uint.MaxValue;
                query.Exclude           = null;
                query.CollideWithAreas  = false;
                query.CollideWithBodies = true;
                query.HitFromInside     = false;

                return query;
            }
        }

        return PhysicsRayQueryParameters2D.Create(from , to);
    }

    private static void ReturnQuery(PhysicsRayQueryParameters2D query)
    {
        lock(poolLock)
        {
            if (queryPool.Count < MaxPoolSize)
                queryPool.Push(query);
        }
    }

    public static uint GetLayerMask(params int[] layers)
    {
        uint mask = 0;
        foreach (int layer in layers)
        {
            if (layer < 1 || layer > 32)
            {
                GD.PushWarning($"Layer {layer} is out of range (1-32) and will be ignored.");
                continue;
            }
            mask |= 1u << (layer - 1);
        }
        return mask;
    }
}

public readonly struct RaycastHit
{
    public Vector2 Position { get; init; }
    public Vector2 Normal   { get; init; }
    public Node2D Collider  { get; init; }
    public Rid ColliderRid  { get; init; }
    public int Shape        { get; init; }
    public float Distance   { get; init; }

    public readonly T GetCollider<T>() where T : Node2D => Collider as T;
}