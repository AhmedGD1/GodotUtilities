using Godot;
using Godot.Collections;

namespace GodotUtilities;

public static class Viewport2DExtension
{
    public static bool Raycast(this Viewport viewport, Vector2 from, Vector2 to, out RaycastHit2D hit, uint mask = uint.MaxValue)
    {
        var query = PhysicsRayQueryParameters2D.Create(from, to, mask);
        query.CollideWithAreas = true;

        var result = viewport.World2D.DirectSpaceState.IntersectRay(query);

        if (result.Count == 0)
        {
            hit = default;
            return false;
        }

        hit = RaycastHit2D.Create(result);
        return true;
    }

    public static bool CheckShape(this Viewport viewport, Shape2D shape, Vector2 position, uint mask = uint.MaxValue)
    {
        var query = GetShapeQuery(shape, position, mask);
        var result = viewport.World2D.DirectSpaceState.IntersectShape(query, 1);
        return result.Count != 0;
    }

    public static CollisionObject2D[] OverlapShape(this Viewport viewport, Shape2D shape, Vector2 position,
        uint mask = uint.MaxValue, int maxResults = 8)
    {
        var query = GetShapeQuery(shape, position, mask);
        var result = viewport.World2D.DirectSpaceState.IntersectShape(query, maxResults);

        if (result.Count == 0)
            return [];

        var colliders = new CollisionObject2D[result.Count];

        for (int i = 0; i < result.Count; i++)
            colliders[i] = result[i]["collider"].As<CollisionObject2D>();

        return colliders;
    }

    private static PhysicsShapeQueryParameters2D GetShapeQuery(Shape2D shape, Vector2 position, uint mask)
    {
        return new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(0, position),
            CollisionMask = mask,
            CollideWithAreas = true,
            CollideWithBodies = true
        };
    }
}

public readonly struct RaycastHit2D
{
    public Rid ColliderRid { get; init; }

    public Vector2 Position { get; init; }
    public Vector2 Normal { get; init; }
    
    public CollisionObject2D Collider { get; init; }

    internal static RaycastHit2D Create(Dictionary result)
    {
        return new RaycastHit2D
        {
            Position = result["position"].AsVector2(),
            Normal = result["normal"].AsVector2(),
            Collider = result["collider"].As<CollisionObject2D>(),
            ColliderRid = result["rid"].AsRid()
        };
    }
}
