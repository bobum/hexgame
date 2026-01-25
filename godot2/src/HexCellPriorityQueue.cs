using System.Collections.Generic;

/// <summary>
/// A priority queue optimized for pathfinding on hex grids.
/// Tutorial 15: Uses linked lists for efficient priority management.
/// </summary>
public class HexCellPriorityQueue
{
    private List<HexCell?> _list = new List<HexCell?>();
    private int _count;
    private int _minimum = int.MaxValue;

    public int Count => _count;

    /// <summary>
    /// Adds a cell to the queue at its search priority.
    /// </summary>
    public void Enqueue(HexCell cell)
    {
        _count++;
        int priority = cell.SearchPriority;
        if (priority < _minimum)
        {
            _minimum = priority;
        }
        // Expand list if needed
        while (priority >= _list.Count)
        {
            _list.Add(null);
        }
        // Add to linked list at this priority
        cell.NextWithSamePriority = _list[priority];
        _list[priority] = cell;
    }

    /// <summary>
    /// Removes and returns the cell with the lowest priority.
    /// </summary>
    public HexCell Dequeue()
    {
        _count--;
        // Find first non-empty bucket starting from minimum
        for (; _minimum < _list.Count; _minimum++)
        {
            HexCell? cell = _list[_minimum];
            if (cell != null)
            {
                _list[_minimum] = cell.NextWithSamePriority;
                return cell;
            }
        }
        return null!; // Should never happen if Count > 0
    }

    /// <summary>
    /// Updates a cell's position in the queue after its priority changed.
    /// Tutorial 15: Called when we find a shorter path to a cell.
    /// </summary>
    public void Change(HexCell cell, int oldPriority)
    {
        HexCell? current = _list[oldPriority];
        HexCell? next = current!.NextWithSamePriority;

        if (current == cell)
        {
            _list[oldPriority] = next;
        }
        else
        {
            while (next != cell)
            {
                current = next;
                next = current!.NextWithSamePriority;
            }
            current!.NextWithSamePriority = cell.NextWithSamePriority;
        }
        // Re-add at new priority
        Enqueue(cell);
        _count--; // Enqueue increments, so decrement to maintain count
    }

    /// <summary>
    /// Clears the queue for reuse.
    /// </summary>
    public void Clear()
    {
        _list.Clear();
        _count = 0;
        _minimum = int.MaxValue;
    }
}
