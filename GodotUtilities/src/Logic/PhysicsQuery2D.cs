using Godot;
using Godot.Collections;

namespace GodotUtilities.Logic;

/// <summary>
/// The result of a successful <see cref="PhysicsQuery2D.Raycast"/>.
/// </summary>
public readonly struct RaycastHit
{
    public Vector2 Position { get; init; }
    public Vector2 Normal { get; init; }
    public CollisionObject2D Collider { get; init; }
    public Rid ColliderRid { get; init; }

    internal static RaycastHit Create(Dictionary result)
    {
        return new RaycastHit
        {
            Position = result["position"].AsVector2(),
            Normal = result["normal"].AsVector2(),
            Collider = result["collider"].AsGodotObject() as CollisionObject2D,
            ColliderRid = result["rid"].AsRid()
        };
    }
}

/// <summary>
/// Static helpers for 2D physics-space queries through
/// <see cref="PhysicsDirectSpaceState2D"/>.
/// </summary>
public static class PhysicsQuery2D
{
    private static World2D _world;

    /// <summary>
    /// The physics space used for queries.
    /// Uses the configured world if one was provided; otherwise,
    /// falls back to the main scene tree's root viewport world.
    /// </summary>
    private static PhysicsDirectSpaceState2D CurrentSpace
    {
        get
        {
            if (_world != null)
                return _world.DirectSpaceState;

            if (Engine.GetMainLoop() is not SceneTree tree)
            {
                throw new InvalidOperationException(
                    $"Main loop isn't a {nameof(SceneTree)}. " +
                    $"Call {nameof(SetWorld)}() before performing physics queries.");
            }

            return tree.Root.GetViewport().World2D.DirectSpaceState;
        }
    }

    /// <summary>
    /// Sets the 2D world used for physics queries.
    /// Useful for SubViewport-based worlds or when avoiding main-tree lookup.
    /// Pass <c>null</c> to restore the default main viewport world.
    /// </summary>
    public static void SetWorld(World2D world)
    {
        _world = world;
    }

    /// <summary>
    /// Resets the configured world and restores the default main viewport world.
    /// </summary>
    public static void ResetWorld()
    {
        _world = null;
    }

    #region Raycast

    /// <summary>
    /// Casts a ray from <paramref name="from"/> to <paramref name="to"/>
    /// and returns the closest collision, if any.
    /// </summary>
    public static bool Raycast(Vector2 from, Vector2 to,
        out RaycastHit hit, uint collisionMask = uint.MaxValue)
    {
        var query = PhysicsRayQueryParameters2D.Create(from, to, collisionMask);
        query.CollideWithAreas = true;

        var result = CurrentSpace.IntersectRay(query);

        if (result.Count == 0)
        {
            hit = default;
            return false;
        }

        hit = RaycastHit.Create(result);
        return true;
    }

    #endregion

    #region Shape

    /// <summary>
    /// Checks whether a shape overlaps anything at the specified position.
    /// Returns the first collider found.
    /// </summary>
    public static bool Check(Shape2D shape, Vector2 position, out CollisionObject2D collider, uint collisionMask = uint.MaxValue)
    {
        var overlaps = Overlap(shape, position, collisionMask, 1);

        if (overlaps.Length == 0)
        {
            collider = null;
            return false;
        }

        collider = overlaps[0];
        return true;
    }

    /// <summary>
    /// Checks whether a shape overlaps anything at the specified position.
    /// </summary>
    public static bool Check(Shape2D shape, Vector2 position, uint collisionMask = uint.MaxValue)
    {
        return Check(shape, position, out _, collisionMask);
    }

    /// <summary>
    /// Returns all colliders overlapping a shape at the specified position.
    /// </summary>
    public static CollisionObject2D[] Overlap(Shape2D shape, Vector2 position,
        uint collisionMask = uint.MaxValue, int maxResults = 16)
    {
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(0, position),
            CollisionMask = collisionMask,
            CollideWithAreas = true,
            CollideWithBodies = true
        };

        var results = CurrentSpace.IntersectShape(query, maxResults);

        if (results.Count == 0)
            return [];

        var colliders = new CollisionObject2D[results.Count];

        for (int i = 0; i < results.Count; i++)
            colliders[i] = results[i]["collider"].AsGodotObject() as CollisionObject2D;

        return colliders;
    }

    /// <summary>
    /// Performs a raw shape intersection query.
    /// Use this when information beyond the collider is required,
    /// such as shape index or RID.
    /// </summary>
    public static Array<Dictionary> IntersectShape(Shape2D shape, Vector2 position,
        uint collisionMask = uint.MaxValue, int maxResults = 16)
    {
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(0, position),
            CollisionMask = collisionMask,
            CollideWithAreas = true,
            CollideWithBodies = true
        };

        return CurrentSpace.IntersectShape(query, maxResults);
    }

    #endregion

    #region Circle

    /// <summary>
    /// Checks whether a circle overlaps anything at the specified position.
    /// </summary>
    public static bool CheckCircle(Vector2 position, float radius,
        out CollisionObject2D collider, uint collisionMask = uint.MaxValue)
    {
        var shape = new CircleShape2D { Radius = radius };
        return Check(shape, position, out collider, collisionMask);
    }

    /// <summary>
    /// Checks whether a circle overlaps anything at the specified position.
    /// </summary>
    public static bool CheckCircle(Vector2 position, float radius, uint collisionMask = uint.MaxValue)
    {
        return CheckCircle(position, radius, out _, collisionMask);
    }

    /// <summary>
    /// Checks whether the supplied circle shape overlaps anything.
    /// </summary>
    public static bool CheckCircle(CircleShape2D shape, Vector2 position,
        out CollisionObject2D collider, uint collisionMask = uint.MaxValue)
    {
        return Check(shape, position, out collider, collisionMask);
    }

    /// <summary>
    /// Checks whether the supplied circle shape overlaps anything.
    /// </summary>
    public static bool CheckCircle(CircleShape2D shape, Vector2 position, uint collisionMask = uint.MaxValue)
    {
        return Check(shape, position, collisionMask);
    }

    /// <summary>
    /// Returns all colliders overlapping a circle.
    /// </summary>
    public static CollisionObject2D[] OverlapCircle(Vector2 position, float radius,
        uint collisionMask = uint.MaxValue, int maxResults = 16)
    {
        var shape = new CircleShape2D
        {
            Radius = radius
        };

        return Overlap(shape, position, collisionMask, maxResults);
    }

    /// <summary>
    /// Returns all colliders overlapping the supplied circle shape.
    /// </summary>
    public static CollisionObject2D[] OverlapCircle(CircleShape2D shape, Vector2 position,
        uint collisionMask = uint.MaxValue, int maxResults = 16)
    {
        return Overlap(shape, position, collisionMask, maxResults);
    }

    #endregion
}
