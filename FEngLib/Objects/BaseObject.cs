using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using FEngLib.Messaging;
using FEngLib.Packages;
using FEngLib.Scripts;
using FEngLib.Structures;
using FEngLib.Utils;

namespace FEngLib.Objects;

/// <summary>
/// This represents all common data found in an object's 'ObjD' chunk.
/// For objects where the ObjD chunk contains extra data (e.g. images),
/// inherit from this class to represent the extra values in that chunk. 
/// </summary>
public class ObjectData : IBinaryAccess, ICloneable
{
	public Color4 Color { get; set; }
	public Vector3 Pivot { get; set; }
	public Vector3 Position { get; set; }
	public Quaternion Rotation { get; set; }
	public Vector3 Size { get; set; }

	protected void InternalClone(ObjectData @object)
	{
		this.Color = @object.Color;
		this.Pivot = @object.Pivot;
		this.Position = @object.Position;
		this.Rotation = @object.Rotation;
		this.Size = @object.Size;
	}

	public virtual object Clone()
	{
		var result = new ObjectData();

		result.InternalClone(this);

		return result;
	}

	public virtual void Read(BinaryReader br)
	{
		Color = br.ReadColor();
		Pivot = br.ReadVector3();
		Position = br.ReadVector3();
		Rotation = br.ReadQuaternion();
		Size = br.ReadVector3();
	}

	public virtual void Write(BinaryWriter bw)
	{
		bw.Write(Color);
		bw.Write(Pivot);
		bw.Write(Position);
		bw.Write(Rotation);
		bw.Write(Size);
	}
}

/// <summary>
/// Common interface for all object types and their properties.
/// Extend this if your object type has extra ObjD attributes,
/// and if there are other object types that inherit from your object type (to ensure type safety).
/// </summary>
/// <typeparam name="TData">
/// A type inheriting from ObjectData,
/// representing the contents of an ObjD chunk for this object.
/// </typeparam>
public interface IObject<out TData> : IScriptedObject, ICloneable, IHaveMessageResponses where TData : ObjectData
{
	TData Data { get; }
	ObjectType Type { get; set; }
	ObjectFlags Flags { get; set; }
	ResourceRequest ResourceRequest { get; set; }
	string Name { get; set; }
	uint NameHash { get; set; }
	uint Guid { get; set; }
	IObject<ObjectData> Parent { get; set; }

	void InitializeData();

	void SetFlag(ObjectFlags flag, bool value)
	{
		if (value)
			Flags |= flag;
		else
			Flags &= ~flag;
	}
}

public interface IScriptedObject : ICloneable
{
	IEnumerable<Script> GetScripts();

	Script CreateScript();

	Script FindScript(uint id);
}

public interface IScriptedObject<out TScript> : IScriptedObject where TScript : Script
{
	new IEnumerable<TScript> GetScripts();

	new TScript CreateScript();

	new TScript FindScript(uint id);
}

public class BaseObjectScript : Script<ScriptTracks>
{
	public override object Clone()
	{
		var result = new BaseObjectScript();

		result.InternalClone(this);

		return result;
	}
}

/// <summary>
/// All objects that don't have any extra attributes in their ObjD chunk's SA tag should inherit from this.
/// </summary>
public abstract class BaseObject : BaseObject<ObjectData, BaseObjectScript>
{
	protected BaseObject(ObjectData data) : base(data)
	{
	}
}

/// <summary>
/// All objects that have extra data in their ObjD chunk's SA tag should inherit from this base class.
/// </summary>
/// <typeparam name="TData">
/// A type inheriting from ObjectData that represents any additional attributes relevant for this object type.
/// </typeparam>
/// <typeparam name="TScript"></typeparam>
public abstract class BaseObject<TData, TScript> : IObject<TData>, IScriptedObject<TScript>
	where TData : ObjectData where TScript : Script, new()
{
	protected BaseObject(TData data)
	{
		Scripts = new List<TScript>();
		MessageResponses = new List<MessageResponse>();
		Data = data;
	}

	public abstract object Clone();

	protected void InternalClone(BaseObject<TData, TScript> @object)
	{
		foreach (var script in @object.Scripts)
		{
			this.Scripts.Add(script?.Clone() as TScript);
		}

		this.Data = @object.Data?.Clone() as TData;
		this.Type = @object.Type;
		this.Flags = @object.Flags;
		this.ResourceRequest = @object.ResourceRequest?.Clone() as ResourceRequest;

		if ((this.ResourceRequest is null) != (@object.ResourceRequest is null))
		{
			Debugger.Break();
		}

		this.Name = @object.Name;
		this.NameHash = @object.NameHash;
		this.Guid = @object.Guid;
		this.Parent = @object.Parent?.Clone() as IObject<ObjectData>;

		foreach (var response in @object.MessageResponses)
		{
			this.MessageResponses.Add(response?.Clone() as MessageResponse);
		}
	}

	public List<TScript> Scripts { get; }

	public TData Data { get; protected set; }
	public ObjectType Type { get; set; }
	public ObjectFlags Flags { get; set; }
	public ResourceRequest ResourceRequest { get; set; }
	public string Name { get; set; }
	public uint NameHash { get; set; }
	public uint Guid { get; set; }
	public IObject<ObjectData> Parent { get; set; }
	public List<MessageResponse> MessageResponses { get; }

	public abstract void InitializeData();

	Script IScriptedObject.FindScript(uint id)
	{
		return FindScript(id);
	}

	Script IScriptedObject.CreateScript()
	{
		return CreateScript();
	}

	IEnumerable<Script> IScriptedObject.GetScripts()
	{
		return GetScripts();
	}

	public IEnumerable<TScript> GetScripts()
	{
		return Scripts;
	}

	public TScript CreateScript()
	{
		var script = new TScript();
		Scripts.Add(script);
		return script;
	}

	public TScript FindScript(uint id)
	{
		return Scripts.Find(s => s.Id == id);
	}
}
