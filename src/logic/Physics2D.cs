using System.Collections.Generic;
using Godot;

namespace Utilities.Logic;

/// <summary>
/// Main Thread only
/// </summary>
public partial class Physics2D : Node
{
    private const uint ALL_LAYERS = 0xFFFFFFFF;

    private static Physics2D instance;

    private PhysicsDirectSpaceState2D spaceState;

    private readonly Stack<PhysicsRayQueryParameters2D>   queryRayPool    = new();
    private readonly Stack<PhysicsShapeQueryParameters2D> queryShapePool  = new();
    private readonly Stack<CircleShape2D>                 sphereShapePool = new();

    public override void _EnterTree()
    {
        instance = this;
    }

    public override void _Ready()
    {
        UpdateSpaceState();
    }

    public static void UpdateSpaceState()
    {
        if (!instance.IsInsideTree())
            return;
        
        instance.spaceState = instance.GetViewport().World2D.DirectSpaceState;
    }

    private static PhysicsShapeQueryParameters2D GetShapeQuery()
    {
        return instance.queryShapePool.Count > 0 ? instance.queryShapePool.Pop() : new PhysicsShapeQueryParameters2D();
    }

    private static CircleShape2D GetSphereShape()
    {
        return instance.sphereShapePool.Count > 0 ? instance.sphereShapePool.Pop() : new();
    }

    private static bool ValidateState()
    {
        var state = instance.spaceState;

        if (state != null && IsInstanceValid(state))
            return true;
        
        GD.PushError("Direct Space state 2D is corrupted, make sure to handle casting carefully or call UpdateSpaceState() to get one");
        return false;
    }

    #region Raycast

    public static bool Raycast(Vector2 origin, Vector2 direction, float distance)
    {
        return Raycast(origin, direction, distance, out var _);
    }

    public static bool Raycast(Vector2 origin, Vector2 direction, float distance, out RaycastHit hit)
    {
        return Raycast(origin, direction, distance, out hit, ALL_LAYERS);
    }

    public static bool Raycast(Vector2 origin, Vector2 direction, float distance, out RaycastHit hit, uint collisionMask)
    {
        return Raycast(origin, origin + direction.Normalized() * distance, out hit, collisionMask);
    }

    public static bool Raycast(Vector2 from, Vector2 to)
    {
        return Raycast(from, to, out var _);
    }

    public static bool Raycast(Vector2 from, Vector2 to, out RaycastHit hit)
    {
        return Raycast(from, to, out hit, ALL_LAYERS);
    }

    public static bool Raycast(Vector2 from, Vector2 to, out RaycastHit hit, uint collisionMask)
    {
        if (!ValidateState()) { hit = default; return false; }
        
        var query = GetRaycastQuery(from, to);

        query.CollisionMask = collisionMask;

        var result = instance.spaceState.IntersectRay(query);
        instance.queryRayPool.Push(query);

        if (result.Count <= 0)
        {
            hit = default;
            return false;
        }
        
        hit = new RaycastHit
        {
            Position    = result["position"].AsVector2(),
            Normal      = result["normal"].AsVector2(),
            Collider    = result["collider"].AsGodotObject(),
            ColliderRid = result["rid"].AsRid()
        };

        return true;
    }

    private static PhysicsRayQueryParameters2D GetRaycastQuery(Vector2 from, Vector2 to)
    {
        if (instance.queryRayPool.Count == 0)
            return PhysicsRayQueryParameters2D.Create(from, to);
        
        var query = instance.queryRayPool.Pop();

        query.From = from;
        query.To   = to;

        return query;
    }

    #endregion

    #region Check Sphere

    public static bool CheckSphere(Vector2 position, float radius)
    {
        return CheckSphere(position, radius, ALL_LAYERS);
    }

    public static bool CheckSphere(Vector2 position, float radius, uint collisionMask)
    {
        if (!ValidateState()) return false;

        var query = GetShapeQuery();
        var shape = GetSphereShape();

        shape.Radius                = radius;
        query.Shape                 = shape;
        query.Transform             = new Transform2D(0f, position);
        query.CollisionMask         = collisionMask;

        var overlaps = instance.spaceState.IntersectShape(query, maxResults: 1);
        instance.queryShapePool.Push(query);
        instance.sphereShapePool.Push(shape);

        return overlaps.Count > 0;
    }

    #endregion

    #region Overlap Sphere

    public static GodotObject[] OverlapSphere(Vector2 position, float radius)
    {
        return OverlapSphere(position, radius, ALL_LAYERS);
    }

    public static GodotObject[] OverlapSphere(Vector2 position, float radius, uint collisionMask)
    {
        return OverlapSphere(position, radius, collisionMask, 16);
    }

    public static GodotObject[] OverlapSphere(Vector2 position, float radius, int maxResults)
    {
        return OverlapSphere(position, radius, ALL_LAYERS, maxResults);
    }

    public static GodotObject[] OverlapSphere(Vector2 position, float radius, uint collisionMask, int maxResults)
    {
        if (!ValidateState()) return [];

        var query = GetShapeQuery();
        var shape = GetSphereShape();
        
        shape.Radius        = radius;
        query.Shape         = shape;
        query.Transform     = new Transform2D(0f, position);
        query.CollisionMask = collisionMask;

        var overlaps = instance.spaceState.IntersectShape(query, maxResults);
        instance.queryShapePool.Push(query);
        instance.sphereShapePool.Push(shape);

        var result = new GodotObject[overlaps.Count];
        
        for (int i = 0; i < overlaps.Count; i++)
            result[i] = overlaps[i]["collider"].AsGodotObject();

        return result;
    }

    #endregion
}

public readonly struct RaycastHit
{
    public Vector2 Position     { get; init; }
    public Vector2 Normal       { get; init; }
    public GodotObject Collider { get; init; }
    public Rid ColliderRid      { get; init; }

    public readonly T GetCollider<T>() where T : Node2D => Collider as T;
}

