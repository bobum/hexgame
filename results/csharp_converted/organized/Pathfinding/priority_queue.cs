using Godot;
using Godot.Collections;


//# Bucket-based priority queue optimized for A* pathfinding.
//# Uses integer buckets for fast O(1) enqueue and amortized O(1) dequeue.

//# Matches web/src/pathfinding/PriorityQueue.ts
[GlobalClass]
public partial class PriorityQueue : Godot.RefCounted
{
	public Dictionary Buckets = new Dictionary{};
	// int -> Array
	public int MinBucket = 999999999;
	// Use int to match bucket keys (NOT float)
	protected int _Size = 0;
	public int Precision = 10;


	// Multiplier for priorities to handle decimals
	public override void _Init(int p_precision = 10)
	{
		Precision = p_precision;
	}


	//# Add an item with the given priority (lower = higher priority)
	public void Enqueue(Godot.Variant item, double priority)
	{
		var bucket_key = Int(Mathf.Floor(priority * Precision));

		if(!Buckets.ContainsKey(bucket_key))
		{
			Buckets[bucket_key] = new Array{};
		}

		Buckets[bucket_key].Append(item);

		if(bucket_key < MinBucket)
		{
			MinBucket = bucket_key;
		}

		_Size += 1;
	}


	//# Remove and return the item with lowest priority
	public Godot.Variant Dequeue()
	{
		if(_Size == 0)
		{
			return null;
		}


		// Find the minimum bucket with items
		while(MinBucket < 999999999)
		{
			if(Buckets.ContainsKey(MinBucket) && Buckets[MinBucket].Size() > 0)
			{
				_Size -= 1;
				return Buckets[MinBucket].PopFront();
			}


			// Bucket is empty, clean it up and find next
			Buckets.Erase(MinBucket);
			MinBucket = _FindMinBucket();
		}

		return null;
	}


	//# Peek at the item with lowest priority without removing it
	public Godot.Variant Peek()
	{
		if(_Size == 0)
		{
			return null;
		}

		if(Buckets.ContainsKey(MinBucket) && Buckets[MinBucket].Size() > 0)
		{
			return Buckets[MinBucket][0];
		}

		return null;
	}


	//# Check if the queue is empty
	public bool IsEmpty()
	{
		return _Size == 0;
	}


	//# Clear all items from the queue
	public void Clear()
	{
		Buckets.Clear();
		MinBucket = 999999999;
		_Size = 0;
	}


	//# Get the number of items in the queue
	public int GetSize()
	{
		return _Size;
	}


	//# Find the minimum priority bucket key
	protected int _FindMinBucket()
	{
		var min_val = 999999999;
		foreach(Variant key in Buckets.Keys())
		{
			if(key < min_val)
			{
				min_val = key;
			}
		}
		return min_val;
	}


}