using Godot;
using Godot.Collections;

namespace GodotUtilities;

public static class Viewport3DExtension
{
    public static bool Raycast(this Viewport viewport, Vector3 from, Vector3 to, out RaycastHit3D hit, uint mask = uint.MaxValue)
    {
        var query = PhysicsRayQueryParameters3D.Create(from, to, mask);
        query.CollideWithAreas = true;

        var result = viewport.World3D.DirectSpaceState.IntersectRay(query);

        if (result.Count == 0)
        {
            hit = default;
            return false;
        }

        hit = RaycastHit3D.Create(result);
        return true;
    }

    public static bool CheckShape(this Viewport viewport, Shape3D shape, Vector3 position, uint mask = uint.MaxValue)
    {
        var query = GetShapeQuery(shape, position, mask);
        var result = viewport.World3D.DirectSpaceState.IntersectShape(query, 1);
        return result.Count != 0;
    }

    public static CollisionObject3D[] OverlapShape(this Viewport viewport, Shape3D shape, Vector3 position,
        uint mask = uint.MaxValue, int maxResults = 8)
    {
        var query = GetShapeQuery(shape, position, mask);
        var result = viewport.World3D.DirectSpaceState.IntersectShape(query, maxResults);

        if (result.Count == 0)
            return [];

        var colliders = new CollisionObject3D[result.Count];

        for (int i = 0; i < result.Count; i++)
            colliders[i] = result[i]["collider"].As<CollisionObject3D>();

        return colliders;
    }

    private static PhysicsShapeQueryParameters3D GetShapeQuery(Shape3D shape, Vector3 position, uint mask)
    {
        return new PhysicsShapeQueryParameters3D
        {
            Shape = shape,
            Transform = new Transform3D(Basis.Identity, position),
            CollisionMask = mask,
            CollideWithAreas = true,
            CollideWithBodies = true
        };
    }
}

public readonly struct RaycastHit3D
{
    public Rid ColliderRid { get; init; }
    
    public Vector3 Normal { get; init; }
    public Vector3 Position { get; init; }
    
    public CollisionObject3D Collider { get; init; }

    internal static RaycastHit3D Create(Dictionary result)
    {
        return new RaycastHit3D
        {
            Position = result["position"].AsVector3(),
            Normal = result["normal"].AsVector3(),
            Collider = result["collider"].As<CollisionObject3D>(),
            ColliderRid = result["rid"].AsRid()
        };
    }
}
