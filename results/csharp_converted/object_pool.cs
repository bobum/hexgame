using Godot;
using Godot.Collections;


//# Generic object pool for reusing objects instead of creating/destroying.
//# Reduces garbage collection pressure for frequently used objects.

//# Matches web/src/utils/ObjectPool.ts
[GlobalClass]
public partial class ObjectPool : Godot.RefCounted
{
	protected Array _Available = new Array{};
	protected Dictionary _Active = new Dictionary{};
	// item -> true (used as Set)
	protected Callable _Factory;
	protected Callable _Reset;
	protected int _MaxSize;


	// Statistics
	protected int _Created = 0;
	protected int _Reused = 0;
	protected int _Peak = 0;


	//# Create an object pool.
	//# @param factory - Callable to create new objects: func() -> Object
	//# @param reset - Callable to reset an object before reuse: func(obj) -> void
	//# @param max_size - Maximum pool size (prevents unbounded growth)
	public override void _Init(Callable factory, Callable reset = new Callable(), int max_size = 1000)
	{
		_Factory = factory;
		_Reset = reset;
		_MaxSize = max_size;
	}


	//# Acquire an object from the pool (or create new if empty).
	public Godot.Variant Acquire()
	{
		var obj;

		if(_Available.Size() > 0)
		{
			obj = _Available.PopBack();
			_Reused += 1;
		}
		else
		{
			obj = _Factory.Call();
			_Created += 1;
		}

		_Active[obj] = true;
		_Peak = Mathf.Max(_Peak, _Active.Size());

		return obj;
	}


	//# Release an object back to the pool.
	public void Release(Godot.Variant obj)
	{
		if(!_Active.ContainsKey(obj))
		{
			GD.PushWarning("ObjectPool: releasing object not from this pool");
			return ;
		}

		_Active.Erase(obj);

		if(_Reset.IsValid())
		{
			_Reset.Call(obj);
		}


		// Only keep up to max_size in the pool
		if(_Available.Size() < _MaxSize)
		{
			_Available.Append(obj);
		}
	}


	//# Release all active objects back to the pool.
	public void ReleaseAll()
	{
		foreach(Variant obj in _Active.Keys())
		{
			if(_Reset.IsValid())
			{
				_Reset.Call(obj);
			}
			if(_Available.Size() < _MaxSize)
			{
				_Available.Append(obj);
			}
		}
		_Active.Clear();
	}


	//# Pre-warm the pool with objects.
	public void Prewarm(int count)
	{
		foreach(int i in GD.Range(count))
		{
			if(_Available.Size() >= _MaxSize)
			{
				break;
			}
			_Available.Append(_Factory.Call());
			_Created += 1;
		}
	}


	//# Clear the pool entirely.
	public void Clear()
	{
		_Available.Clear();
		_Active.Clear();
	}


	//# Get pool statistics.
	public Dictionary GetStats()
	{
		var total = _Created + _Reused;
		return new Dictionary{
					{"available", _Available.Size()},
					{"active", _Active.Size()},
					{"created", _Created},
					{"reused", _Reused},
					{"peak", _Peak},
					{"reuse_rate", ( total > 0 ? Float(_Reused) / total : 0.0 )},
					};
	}


	//# Get the number of active objects.
	public int GetActiveCount()
	{
		return _Active.Size();
	}


	//# Get the number of available objects.
	public int GetAvailableCount()
	{
		return _Available.Size();
	}


	//# Reset statistics counters.
	public void ResetStats()
	{
		_Created = 0;
		_Reused = 0;
		_Peak = 0;
	}


}