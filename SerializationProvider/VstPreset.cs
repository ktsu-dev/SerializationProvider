// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SerializationProvider;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The decoded contents of a VST3 <c>.vstpreset</c> file.
/// </summary>
/// <remarks>
/// A VST3 preset is a small binary container that pairs a plugin's class identifier with one or more
/// opaque state blobs: the processor (component) state, an optional controller state, and optional XML
/// metadata. See <see cref="VstPresetFile"/> for reading and writing the on-disk format.
/// </remarks>
public sealed record VstPreset
{
	/// <summary>
	/// Gets the plugin class identifier (the 32-character ASCII representation of the VST3 FUID).
	/// </summary>
	public string ClassId { get; }

	/// <summary>
	/// Gets the processor (component) state blob. This is the primary plugin state.
	/// </summary>
	[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "A preset state chunk is an opaque binary blob written verbatim to the file; a byte array is the natural representation.")]
	public byte[] ComponentState { get; }

	/// <summary>
	/// Gets the optional controller state blob, or <see langword="null"/> when the preset has none.
	/// </summary>
	[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "A preset state chunk is an opaque binary blob written verbatim to the file; a byte array is the natural representation.")]
	public byte[]? ControllerState { get; }

	/// <summary>
	/// Gets the optional metadata, typically an XML document describing the preset, or <see langword="null"/>.
	/// </summary>
	public string? MetaInfo { get; }

	/// <summary>
	/// Gets the preset format version stored in the file header.
	/// </summary>
	public int Version { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="VstPreset"/> class.
	/// </summary>
	/// <param name="classId">The plugin class identifier.</param>
	/// <param name="componentState">The processor (component) state blob.</param>
	/// <param name="controllerState">The optional controller state blob.</param>
	/// <param name="metaInfo">The optional XML metadata.</param>
	/// <param name="version">The preset format version.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="classId"/> or <paramref name="componentState"/> is null.</exception>
	public VstPreset(string classId, byte[] componentState, byte[]? controllerState = null, string? metaInfo = null, int version = VstPresetFile.FormatVersion)
	{
		ClassId = classId ?? throw new ArgumentNullException(nameof(classId));
		ComponentState = componentState ?? throw new ArgumentNullException(nameof(componentState));
		ControllerState = controllerState;
		MetaInfo = metaInfo;
		Version = version;
	}
}
