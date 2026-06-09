// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SerializationProvider;

using System.IO;
using System.Text;

/// <summary>
/// Reads and writes the Steinberg VST3 <c>.vstpreset</c> container format.
/// </summary>
/// <remarks>
/// The format is a header followed by the state blobs and a trailing chunk list:
/// <list type="number">
///   <item>Header: the ASCII tag <c>VST3</c>, an <see cref="int"/> version, a 32-byte ASCII class id, and an <see cref="long"/> offset to the chunk list.</item>
///   <item>Data: the component state, optional controller state, and optional metadata, written back to back.</item>
///   <item>Chunk list: the ASCII tag <c>List</c>, an <see cref="int"/> entry count, then for each entry a 4-byte id (<c>Comp</c>, <c>Cont</c>, <c>Info</c>), an <see cref="long"/> offset and an <see cref="long"/> size.</item>
/// </list>
/// All integers are little-endian, matching the reference implementation in the VST3 SDK, so files
/// written here can be loaded by hosts and host-written presets can be read back.
/// </remarks>
public static class VstPresetFile
{
	/// <summary>The current preset format version.</summary>
	public const int FormatVersion = 1;

	private const string HeaderTag = "VST3";
	private const string ListTag = "List";
	private const string ComponentChunkId = "Comp";
	private const string ControllerChunkId = "Cont";
	private const string MetaInfoChunkId = "Info";
	private const int ClassIdLength = 32;

	/// <summary>
	/// Writes a preset to a seekable, writable stream.
	/// </summary>
	/// <param name="stream">The destination stream; must support seeking and writing.</param>
	/// <param name="preset">The preset to write.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="preset"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="stream"/> is not seekable or writable.</exception>
	public static void Write(Stream stream, VstPreset preset)
	{
		Ensure.NotNull(stream);
		Ensure.NotNull(preset);
		if (!stream.CanSeek || !stream.CanWrite)
		{
			throw new ArgumentException("Stream must be seekable and writable.", nameof(stream));
		}

		using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

		writer.Write(Encoding.ASCII.GetBytes(HeaderTag));
		writer.Write(preset.Version);
		writer.Write(EncodeClassId(preset.ClassId));
		long listOffsetField = stream.Position;
		writer.Write(0L); // Placeholder for the chunk-list offset; patched once the list position is known.

		List<(string Id, long Offset, long Size)> entries = [];

		AppendChunk(writer, stream, entries, ComponentChunkId, preset.ComponentState);
		if (preset.ControllerState is not null)
		{
			AppendChunk(writer, stream, entries, ControllerChunkId, preset.ControllerState);
		}

		if (preset.MetaInfo is not null)
		{
			AppendChunk(writer, stream, entries, MetaInfoChunkId, Encoding.UTF8.GetBytes(preset.MetaInfo));
		}

		long listOffset = stream.Position;
		writer.Write(Encoding.ASCII.GetBytes(ListTag));
		writer.Write(entries.Count);
		foreach ((string id, long offset, long size) in entries)
		{
			writer.Write(Encoding.ASCII.GetBytes(id));
			writer.Write(offset);
			writer.Write(size);
		}

		writer.Flush();

		// Patch the header's chunk-list offset now that we know where the list landed.
		stream.Seek(listOffsetField, SeekOrigin.Begin);
		writer.Write(listOffset);
		writer.Flush();
		stream.Seek(0, SeekOrigin.End);
	}

	/// <summary>
	/// Serializes a preset to a new byte array.
	/// </summary>
	/// <param name="preset">The preset to serialize.</param>
	/// <returns>The encoded <c>.vstpreset</c> bytes.</returns>
	public static byte[] ToBytes(VstPreset preset)
	{
		using MemoryStream stream = new();
		Write(stream, preset);
		return stream.ToArray();
	}

	/// <summary>
	/// Reads a preset from a seekable, readable stream.
	/// </summary>
	/// <param name="stream">The source stream; must support seeking and reading.</param>
	/// <returns>The decoded <see cref="VstPreset"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="stream"/> is not seekable or readable.</exception>
	/// <exception cref="InvalidDataException">Thrown when the stream is not a valid VST3 preset.</exception>
	public static VstPreset Read(Stream stream)
	{
		Ensure.NotNull(stream);
		if (!stream.CanSeek || !stream.CanRead)
		{
			throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
		}

		using BinaryReader reader = new(stream, Encoding.ASCII, leaveOpen: true);

		stream.Seek(0, SeekOrigin.Begin);
		if (ReadTag(reader) != HeaderTag)
		{
			throw new InvalidDataException("Not a VST3 preset: missing 'VST3' header tag.");
		}

		int version = reader.ReadInt32();
		string classId = DecodeClassId(ReadExact(reader, ClassIdLength));
		long listOffset = reader.ReadInt64();

		stream.Seek(listOffset, SeekOrigin.Begin);
		if (ReadTag(reader) != ListTag)
		{
			throw new InvalidDataException("Corrupt VST3 preset: missing 'List' chunk tag.");
		}

		int entryCount = reader.ReadInt32();
		if (entryCount < 0)
		{
			throw new InvalidDataException("Corrupt VST3 preset: negative chunk count.");
		}

		List<(string Id, long Offset, long Size)> entries = new(entryCount);
		for (int i = 0; i < entryCount; i++)
		{
			string id = ReadTag(reader);
			long offset = reader.ReadInt64();
			long size = reader.ReadInt64();
			entries.Add((id, offset, size));
		}

		byte[]? component = null;
		byte[]? controller = null;
		string? metaInfo = null;

		foreach ((string id, long offset, long size) in entries)
		{
			stream.Seek(offset, SeekOrigin.Begin);
			byte[] data = ReadExact(reader, checked((int)size));
			switch (id)
			{
				case ComponentChunkId:
					component = data;
					break;
				case ControllerChunkId:
					controller = data;
					break;
				case MetaInfoChunkId:
					metaInfo = Encoding.UTF8.GetString(data);
					break;
				default:
					// Unknown chunk: preserved by hosts but not modelled here; ignore.
					break;
			}
		}

		return component is null
			? throw new InvalidDataException("Corrupt VST3 preset: no component ('Comp') chunk.")
			: new VstPreset(classId, component, controller, metaInfo, version);
	}

	/// <summary>
	/// Deserializes a preset from a byte array.
	/// </summary>
	/// <param name="bytes">The encoded <c>.vstpreset</c> bytes.</param>
	/// <returns>The decoded <see cref="VstPreset"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is null.</exception>
	public static VstPreset FromBytes(byte[] bytes)
	{
		Ensure.NotNull(bytes);
		using MemoryStream stream = new(bytes, writable: false);
		return Read(stream);
	}

	private static void AppendChunk(BinaryWriter writer, Stream stream, List<(string Id, long Offset, long Size)> entries, string id, byte[] data)
	{
		long offset = stream.Position;
		writer.Write(data);
		entries.Add((id, offset, data.Length));
	}

	private static byte[] EncodeClassId(string classId)
	{
		byte[] buffer = new byte[ClassIdLength];
		byte[] source = Encoding.ASCII.GetBytes(classId);
		Array.Copy(source, buffer, Math.Min(source.Length, ClassIdLength));
		return buffer;
	}

	private static string DecodeClassId(byte[] raw) => Encoding.ASCII.GetString(raw).TrimEnd('\0');

	private static string ReadTag(BinaryReader reader) => Encoding.ASCII.GetString(ReadExact(reader, 4));

	private static byte[] ReadExact(BinaryReader reader, int count)
	{
		byte[] data = reader.ReadBytes(count);
		return data.Length != count
			? throw new InvalidDataException("Corrupt VST3 preset: unexpected end of stream.")
			: data;
	}
}
