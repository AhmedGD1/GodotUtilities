using Godot;
using Godot.Collections;

namespace GodotUtilities;

public static class World3DExtension
{
    public static bool Raycast(this World3D world, Vector3 from, Vector3 to, out RaycastHit3D hit,
        uint mask = uint.MaxValue, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        var query = PhysicsRayQueryParameters3D.Create(from, to, mask);
        query.CollideWithAreas = collideWithAreas;
        query.CollideWithBodies = collideWithBodies;

        var result = world.DirectSpaceState.IntersectRay(query);

        if (result.Count == 0)
        {
            hit = default;
            return false;
        }

        hit = RaycastHit3D.Create(result);
        return true;
    }

    public static bool Raycast(this World3D world, Vector3 origin, Vector3 direction, float distance,
        out RaycastHit3D hit, uint mask = uint.MaxValue, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        return Raycast(world, origin, origin + direction * distance, out hit, mask, collideWithBodies, collideWithAreas);
    }

    public static bool CheckShape(this World3D world, Shape3D shape, Vector3 position,
        uint mask = uint.MaxValue, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        var query = GetShapeQuery(shape, position, mask, collideWithBodies, collideWithAreas);
        var result = world.DirectSpaceState.IntersectShape(query, 1);
        return result.Count != 0;
    }
    
    public static bool CheckShape(this World3D world, Shape3D shape, Vector3 position, out CollisionObject3D collider,
        uint mask = uint.MaxValue, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        var query = GetShapeQuery(shape, position, mask, collideWithBodies, collideWithAreas);
        var result = world.DirectSpaceState.IntersectShape(query, 1);

        if (result.Count == 0)
        {
            collider = null;
            return false;
        }
    
        collider = result[0]["collider"].As<CollisionObject3D>();
        return true;
    }

    public static CollisionObject3D[] OverlapShape(this World3D world, Shape3D shape, Vector3 position,
        uint mask = uint.MaxValue, int maxResults = 8, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        var query = GetShapeQuery(shape, position, mask, collideWithBodies, collideWithAreas);
        var result = world.DirectSpaceState.IntersectShape(query, maxResults);

        if (result.Count == 0)
            return [];

        var colliders = new CollisionObject3D[result.Count];

        for (int i = 0; i < result.Count; i++)
            colliders[i] = result[i]["collider"].As<CollisionObject3D>();

        return colliders;
    }

    private static PhysicsShapeQueryParameters3D GetShapeQuery(Shape3D shape, Vector3 position,
        uint mask, bool collideWithBodies = true, bool collideWithAreas = false)
    {
        return new PhysicsShapeQueryParameters3D
        {
            Shape = shape,
            Transform = new Transform3D(Basis.Identity, position),
            CollisionMask = mask,
            CollideWithAreas = collideWithAreas,
            CollideWithBodies = collideWithBodies
        };
    }
}

public readonly struct RaycastHit3D
{
    public Rid ColliderRid { get; init; }
    
    public int ColliderId { get; init; }
    public int Shape { get; init; }
    
    public Vector3 Normal { get; init; }
    public Vector3 Position { get; init; }
    
    public CollisionObject3D Collider { get; init; }

    internal static RaycastHit3D Create(Dictionary result) => new()
    {
        Position = result["position"].AsVector3(),
        Normal = result["normal"].AsVector3(),
        Collider = result["collider"].As<CollisionObject3D>(),
        ColliderRid = result["rid"].AsRid(),
        ColliderId = result["collider_id"].AsInt32(),
        Shape = result["shape"].AsInt32()
    };
}
