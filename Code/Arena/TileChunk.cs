using System;
using System.Collections.Generic;

/// <summary>
/// One square of a floor's tiles drawn as a single model: every tile's slab copied into one mesh per
/// material, so the square costs one draw call per material however many looks its tiles wear. With
/// a model per tile the engine drew almost every tile on its own (about 5,000 draws inside a room),
/// because neighbouring tiles wear different looks and its sort interleaves them.
///
/// The tiles themselves stay GameObjects with their colliders (CLAUDE.md: disabled, never destroyed);
/// a chunk only mirrors which of them stand. A hidden tile keeps its vertices, and its triangles are
/// collapsed to index 0 in the index buffer — one small upload, nothing rebuilt.
/// </summary>
public sealed class TileChunk
{
	sealed class Part
	{
		public readonly List<Vertex> Vertices = new();
		public readonly List<int> Indices = new();

		/// <summary>Where each tile's triangles sit in <see cref="Indices"/>: start and count, by the tile's slot in the chunk.</summary>
		public readonly List<(int Slot, int Start, int Count)> Ranges = new();

		public Mesh Mesh;
		public List<int> Live;
	}

	readonly Dictionary<Material, Part> _parts = new();
	readonly bool[] _shown;
	BBox _bounds;
	bool _hasBounds;

	public ModelRenderer Renderer { get; private set; }

	public TileChunk( int slots )
	{
		_shown = new bool[slots];
		Array.Fill( _shown, true );
	}

	/// <summary>
	/// Copies one tile's slab into the chunk at <paramref name="offset"/>, its materials swapped through
	/// <paramref name="material"/> (the tile's tinted copies of the floor's surfaces).
	/// </summary>
	public void Add( int slot, MeshKit slab, Vector3 offset, Func<Material, Material> material )
	{
		foreach ( var (source, vertices, indices) in slab.Batches )
		{
			var target = material( source );
			if ( !_parts.TryGetValue( target, out var part ) )
				_parts[target] = part = new Part();

			var first = part.Vertices.Count;
			foreach ( var vertex in vertices )
			{
				var moved = vertex;
				moved.Position += offset;
				part.Vertices.Add( moved );
				_bounds = _hasBounds ? _bounds.AddPoint( moved.Position ) : new BBox( moved.Position, moved.Position );
				_hasBounds = true;
			}

			part.Ranges.Add( (slot, part.Indices.Count, indices.Count) );
			foreach ( var index in indices )
				part.Indices.Add( first + index );
		}
	}

	/// <summary>Makes the chunk's model and its renderer under <paramref name="parent"/>. Main thread only.</summary>
	public void Build( GameObject parent, string name )
	{
		var builder = Model.Builder.WithName( name );
		foreach ( var (material, part) in _parts )
		{
			part.Mesh = new Mesh( material );
			part.Mesh.CreateVertexBuffer( part.Vertices.Count, part.Vertices );
			part.Live = new List<int>( part.Indices );
			ApplyShown( part );
			part.Mesh.CreateIndexBuffer( part.Live.Count, part.Live );
			part.Mesh.Bounds = _bounds;
			builder.AddMesh( part.Mesh );
		}

		var go = new GameObject( parent, true, name );
		ArenaGenerator.MakeLocalOnly( go );
		Renderer = go.AddComponent<ModelRenderer>();
		Renderer.Model = builder.Create();
	}

	/// <summary>
	/// Shows or hides the tile in <paramref name="slot"/>; true when that changed anything. Before
	/// <see cref="Build"/> it only records the state, which the build then starts from.
	/// </summary>
	public bool SetShown( int slot, bool shown )
	{
		if ( _shown[slot] == shown ) return false;
		_shown[slot] = shown;
		return true;
	}

	/// <summary>Uploads the index buffers after <see cref="SetShown"/> changed them. Main thread only.</summary>
	public void Upload()
	{
		foreach ( var part in _parts.Values )
		{
			if ( part.Mesh is null ) return;
			ApplyShown( part );
			part.Mesh.SetIndexBufferData( part.Live, 0 );
		}
	}

	void ApplyShown( Part part )
	{
		foreach ( var (slot, start, count) in part.Ranges )
		{
			var shown = _shown[slot];
			for ( var i = start; i < start + count; i++ )
				part.Live[i] = shown ? part.Indices[i] : 0;
		}
	}
}
