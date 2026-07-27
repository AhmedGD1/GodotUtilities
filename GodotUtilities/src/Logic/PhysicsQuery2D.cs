using Godot;
using Godot.Collections;

namespace GodotUtilities.Logic;

/// <summary>
/// Static helpers for 2D physics-space queries (raycasts, circle checks/overlaps, and
/// general shape intersection) via <see cref="PhysicsDirectSpaceState2D"/>, without
/// needing a <see cref="Node"/> reference at the call site.
/// </summary>
public static class PhysicsQuery2D
{
    private static PhysicsDirectSpaceState2D _state;

    /// <summary>
    /// The physics space queries are run against: the space set via <see cref="SetWorldSpace"/>,
    /// or the main scene tree's root viewport space if none was set.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No space was set via <see cref="SetWorldSpace"/> and <see cref="Engine.GetMainLoop"/>
    /// is not a <see cref="SceneTree"/> (e.g. queried too early, or outside a running game tree).
    /// </exception>
    private static PhysicsDirectSpaceState2D CurrentSpace
    {
        get
        {
            if (_state != null)
                return _state;

            if (Engine.GetMainLoop() is not SceneTree tree)
                throw new InvalidOperationException(
                    $"Current Main Loop isn't assigned to be {nameof(SceneTree)}, Call {nameof(SetWorldSpace)}()");

            var viewport = tree.Root.GetViewport();
            return viewport.World2D.DirectSpaceState;
        }
    }

    /// <summary>
    /// Overrides the physics space used for queries. Useful when the default (the main
    /// scene tree's root viewport space) isn't the correct 2D world — e.g. a
    /// <see cref="SubViewport"/> with its own physics space — or to avoid the
    /// <see cref="Engine.GetMainLoop"/> lookup entirely.
    /// </summary>
    /// <param name="state">The space to query against, or <c>null</c> to revert to the default.</param>
    public static void SetWorldSpace(PhysicsDirectSpaceState2D state)
    {
        _state = state;
    }

    #region Raycast

    /// <summary>
    /// Casts a ray from <paramref name="from"/> to <paramref name="to"/> and returns the
    /// closest collision, if any. Includes <see cref="Area2D"/> colliders.
    /// </summary>
    /// <param name="from">The ray's start position, in world space.</param>
    /// <param name="to">The ray's end position, in world space.</param>
    /// <param name="hit">The collision result if the ray hit something; otherwise <c>default</c>.</param>
    /// <param name="collisionMask">The physics layers to test against.</param>
    /// <returns><c>true</c> if the ray hit something; otherwise <c>false</c>.</returns>
    public static bool Raycast(Vector2 from, Vector2 to, out RaycastHit hit, uint collisionMask = uint.MaxValue)
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

    #region Check Circle

    /// <summary>
    /// Checks whether <paramref name="shape"/> overlaps anything at <paramref name="position"/>,
    /// returning the first collider found.
    /// </summary>
    /// <param name="shape">The circle shape to test with.</param>
    /// <param name="position">The world-space position to test at.</param>
    /// <param name="collider">The first overlapping collider, if any; otherwise <c>default</c>.</param>
    /// <param name="collisionMask">The physics layers to test against.</param>
    /// <returns><c>true</c> if an overlap was found; otherwise <c>false</c>.</returns>
    public static bool CheckCircle(CircleShape2D shape, Vector2 position,
        out GodotObject collider, uint collisionMask = uint.MaxValue)
    {
        var result = IntersectShape(shape, position, collisionMask, maxResults: 1);

        if (result.Count == 0)
        {
            collider = default;
            return false;
        }

        collider = result[0]["collider"].AsGodotObject();
        return true;
    }

    /// <summary>
    /// Checks whether a circle of the given <paramref name="radius"/> overlaps anything at
    /// <paramref name="position"/>, returning the first collider found.
    /// </summary>
    /// <param name="position">The world-space position to test at.</param>
    /// <param name="radius">The radius of the circle to test with.</param>
    /// <param name="collider">The first overlapping collider, if any; otherwise <c>default</c>.</param>
    /// <param name="collisionMask">The physics layers to test against.</param>
    /// <returns><c>true</c> if an overlap was found; otherwise <c>false</c>.</returns>
    public static bool CheckCircle(Vector2 position, float radius, out GodotObject collider, uint collisionMask = uint.MaxValue)
    {
        var shape = new CircleShape2D { Radius = radius };
        return CheckCircle(shape, position, out collider, collisionMask);
    }

    /// <inheritdoc cref="CheckCircle(CircleShape2D, Vector2, out GodotObject, uint)"/>
    public static bool CheckCircle(CircleShape2D shape, Vector2 position, uint collisionMask = uint.MaxValue)
    {
        return CheckCircle(shape, position, out var _, collisionMask);
    }

    /// <inheritdoc cref="CheckCircle(Vector2, float, out GodotObject, uint)"/>
    public static bool CheckCircle(Vector2 position, float radius, uint collisionMask = uint.MaxValue)
    {
        return CheckCircle(position, radius, out var _, collisionMask);
    }

    #endregion

    #region Overlap Circle

    /// <summary>
    /// Returns every collider overlapping <paramref name="shape"/> at <paramref name="position"/>,
    /// up to <paramref name="maxResults"/>.
    /// </summary>
    /// <param name="shape">The circle shape to test with.</param>
    /// <param name="position">The world-space position to test at.</param>
    /// <param name="collisionMask">The physics layers to test against.</param>
    /// <param name="maxResults">The maximum number of overlaps to return.</param>
    /// <returns>The overlapping colliders, in no guaranteed order. Empty if none overlap.</returns>
    public static GodotObject[] OverlapCircle(CircleShape2D shape, Vector2 position,
        uint collisionMask = uint.MaxValue, int maxResults = 16)
    {
        var overlaps = IntersectShape(shape, position, collisionMask, maxResults);
        var result = new GodotObject[overlaps.Count];

        for (int i = 0; i < overlaps.Count; i++)
            result[i] = overlaps[i]["collider"].AsGodotObject();
        return result;
    }

    /// <summary>
    /// Returns every collider overlapping a circle of the given <paramref name="radius"/> at
    /// <paramref name="position"/>, up to <paramref name="maxResults"/>.
    /// </summary>
    /// <param name="position">The world-space position to test at.</param>
    /// <param name="radius">The radius of the circle to test with.</param>
    /// <param name="collisionMask">The physics layers to test against.</param>
    /// <param name="maxResults">The maximum number of overlaps to return.</param>
    /// <returns>The overlapping colliders, in no guaranteed order. Empty if none overlap.</returns>
    public static GodotObject[] OverlapCircle(Vector2 position, float radius,
        uint collisionMask = uint.MaxValue, int maxResults = 16)
    {
        var shape = new CircleShape2D { Radius = radius };
        return OverlapCircle(shape, position, collisionMask, maxResults);
    }

    #endregion

    #region Shape Intersect

    /// <summary>
    /// Runs a raw shape-intersection query at <paramref name="position"/>, returning each
    /// overlap's raw result dictionary (as provided by <see cref="PhysicsDirectSpaceState2D.IntersectShape"/>).
    /// Lower-level than <see cref="CheckCircle(CircleShape2D, Vector2, out GodotObject, uint)"/>/
    /// <see cref="OverlapCircle(CircleShape2D, Vector2, uint, int)"/>; use those unless you
    /// need fields beyond the collider (e.g. shape index, RID).
    /// </summary>
    public static Array<Dictionary> IntersectShape(Shape2D shape, Vector2 position, uint collisionMask, int maxResults)
    {
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(0, position),
            CollisionMask = collisionMask,
            CollideWithAreas = true
        };

        var overlaps = CurrentSpace.IntersectShape(query, maxResults);
        return overlaps;
    }

    #endregion
}

/// <summary>
/// The result of a successful <see cref="PhysicsQuery2D.Raycast"/>.
/// </summary>
public readonly struct RaycastHit
{
    public Vector2 Position { get; init; }
    public Vector2 Normal { get; init; }
    public GodotObject Collider { get; init; }
    public Rid ColliderRid { get; init; }

    public static RaycastHit Create(Dictionary result)
    {
        return new RaycastHit
        {
            Normal = result["normal"].AsVector2(),
            Position = result["position"].AsVector2(),
            Collider = result["collider"].AsGodotObject(),
            ColliderRid = result["rid"].AsRid(),
        };
    }
}
