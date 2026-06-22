namespace AlongJourney.Core;

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// 四向网格 A*（Manhattan 代价与启发式）。
/// </summary>
public static class GridPathfinder
{
    private static readonly Vector2I[] Neighbors =
    {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
    };

    public static List<Vector2I> FindPath(
        Vector2I start,
        Vector2I goal,
        Func<Vector2I, bool> isWalkable)
    {
        if (!isWalkable(start))
        {
            return null;
        }

        Vector2I resolvedGoal = ResolveGoal(goal, start, isWalkable);
        if (resolvedGoal == InvalidCell)
        {
            return null;
        }

        if (start == resolvedGoal)
        {
            return new List<Vector2I> { start };
        }

        var openSet = new PriorityQueue<Vector2I, int>();
        var cameFrom = new Dictionary<Vector2I, Vector2I>();
        var gScore = new Dictionary<Vector2I, int> { [start] = 0 };

        openSet.Enqueue(start, Manhattan(start, resolvedGoal));

        while (openSet.Count > 0)
        {
            Vector2I current = openSet.Dequeue();
            if (current == resolvedGoal)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (Vector2I offset in Neighbors)
            {
                Vector2I neighbor = current + offset;
                if (!isWalkable(neighbor))
                {
                    continue;
                }

                int tentativeG = gScore[current] + 1;
                if (gScore.TryGetValue(neighbor, out int knownG) && tentativeG >= knownG)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                openSet.Enqueue(neighbor, tentativeG + Manhattan(neighbor, resolvedGoal));
            }
        }

        return null;
    }

    public static Vector2I ResolveGoal(
        Vector2I goal,
        Vector2I start,
        Func<Vector2I, bool> isWalkable)
    {
        if (isWalkable(goal))
        {
            return goal;
        }

        var queue = new Queue<Vector2I>();
        var visited = new HashSet<Vector2I> { goal };
        queue.Enqueue(goal);

        Vector2I best = InvalidCell;
        int bestDistance = int.MaxValue;

        while (queue.Count > 0)
        {
            Vector2I cell = queue.Dequeue();
            foreach (Vector2I offset in Neighbors)
            {
                Vector2I neighbor = cell + offset;
                if (!visited.Add(neighbor))
                {
                    continue;
                }

                if (isWalkable(neighbor))
                {
                    int distance = Manhattan(neighbor, start);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = neighbor;
                    }

                    continue;
                }

                queue.Enqueue(neighbor);
            }
        }

        return best;
    }

    private static Vector2I InvalidCell => new(int.MinValue, int.MinValue);

    private static int Manhattan(Vector2I a, Vector2I b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static List<Vector2I> ReconstructPath(
        Dictionary<Vector2I, Vector2I> cameFrom,
        Vector2I current)
    {
        var path = new List<Vector2I> { current };
        while (cameFrom.TryGetValue(current, out Vector2I previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
