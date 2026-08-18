using Godot;
using Godot.Collections;

namespace GodotUtilities;

public static class World2DExtension
{
    public static bool Raycast(this World2D world, Vector2 from, Vector2 to, out RaycastHit2D hit,
        uint mask = uint.MaxValue, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        var query = PhysicsRayQueryParameters2D.Create(from, to, mask);
        query.CollideWithAreas = collideWithAreas;
        query.CollideWithBodies = collideWithBodies;

        var result = world.DirectSpaceState.IntersectRay(query);

        if (result.Count == 0)
        {
            hit = default;
            return false;
        }

        hit = RaycastHit2D.Create(result);
        return true;
    }

    public static bool Raycast(this World2D world, Vector2 origin, Vector2 direction, float distance,
        out RaycastHit2D hit, uint mask = uint.MaxValue, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        return Raycast(world, origin, origin + direction * distance, out hit, mask, collideWithBodies, collideWithAreas);
    }

    public static bool CheckShape(this World2D world, Shape2D shape, Vector2 position,
        uint mask = uint.MaxValue, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        var query = GetShapeQuery(shape, position, mask, collideWithBodies, collideWithAreas);
        var result = world.DirectSpaceState.IntersectShape(query, 1);
        return result.Count != 0;
    }
    
    public static bool CheckShape(this World2D world, Shape2D shape, Vector2 position, out CollisionObject2D collider,
        uint mask = uint.MaxValue, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        var query = GetShapeQuery(shape, position, mask, collideWithBodies, collideWithAreas);
        var result = world.DirectSpaceState.IntersectShape(query, 1);

        if (result.Count == 0)
        {
            collider = null;
            return false;
        }
    
        collider = result[0]["collider"].As<CollisionObject2D>();
        return true;
    }

    public static CollisionObject2D[] OverlapShape(this World2D world, Shape2D shape, Vector2 position,
        uint mask = uint.MaxValue, int maxResults = 8, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        var query = GetShapeQuery(shape, position, mask, collideWithBodies, collideWithAreas);
        var result = world.DirectSpaceState.IntersectShape(query, maxResults);

        if (result.Count == 0)
            return [];

        var colliders = new CollisionObject2D[result.Count];

        for (int i = 0; i < result.Count; i++)
            colliders[i] = result[i]["collider"].As<CollisionObject2D>();

        return colliders;
    }

    private static PhysicsShapeQueryParameters2D GetShapeQuery(Shape2D shape, Vector2 position,
        uint mask, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        return new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            CollisionMask = mask,
            Transform = new Transform2D(0, position),
            CollideWithAreas = collideWithAreas,
            CollideWithBodies = collideWithBodies
        };
    }
}

public readonly struct RaycastHit2D
{
    public Rid ColliderRid { get; init; }
    
    public int ColliderId { get; init; }
    public int Shape { get; init; }
    
    public Vector2 Normal { get; init; }
    public Vector2 Position { get; init; }
    
    public CollisionObject2D Collider { get; init; }

    internal static RaycastHit2D Create(Dictionary result) => new()
    {
        Position = result["position"].AsVector2(),
        Normal = result["normal"].AsVector2(),
        Collider = result["collider"].As<CollisionObject2D>(),
        ColliderRid = result["rid"].AsRid(),
        ColliderId = result["collider_id"].AsInt32(),
        Shape = result["shape"].AsInt32()
    };
}
