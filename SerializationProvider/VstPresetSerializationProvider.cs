// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SerializationProvider;

using System.Text;

/// <summary>
/// An <see cref="ISerializationProvider"/> that wraps another provider and packages its output inside a
/// VST3 <c>.vstpreset</c> container.
/// </summary>
/// <remarks>
/// The inner provider produces the logical state (for example JSON); this provider stores that state as
/// the component chunk of a <see cref="VstPreset"/> tagged with a configured class id. Because the
/// <see cref="ISerializationProvider"/> contract is string-based, the binary preset is returned
/// Base64-encoded. For direct file interop — writing bytes a host can load, or reading a host-written
/// <c>.vstpreset</c> — use <see cref="VstPresetFile"/> directly.
/// </remarks>
/// <param name="innerProvider">The provider that serializes the logical state stored inside the preset.</param>
/// <param name="presetClassId">The VST3 plugin class id (32-character ASCII FUID) to tag presets with.</param>
public sealed class VstPresetSerializationProvider(ISerializationProvider innerProvider, string presetClassId) : ISerializationProvider
{
	private readonly ISerializationProvider inner = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
	private readonly string classId = presetClassId ?? throw new ArgumentNullException(nameof(presetClassId));

	/// <inheritdoc/>
	public string ProviderName => "VST3 Preset";

	/// <inheritdoc/>
	public string ContentType => "application/vnd.steinberg.vstpreset";

	/// <inheritdoc/>
	public string Serialize<T>(T obj) => Wrap(inner.Serialize(obj));

	/// <inheritdoc/>
	public string Serialize(object obj, Type type) => Wrap(inner.Serialize(obj, type));

	/// <inheritdoc/>
	public T Deserialize<T>(string data) => inner.Deserialize<T>(Unwrap(data));

	/// <inheritdoc/>
	public object Deserialize(string data, Type type) => inner.Deserialize(Unwrap(data), type);

	/// <inheritdoc/>
	public Task<string> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(Serialize(obj));
	}

	/// <inheritdoc/>
	public Task<string> SerializeAsync(object obj, Type type, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(Serialize(obj, type));
	}

	/// <inheritdoc/>
	public Task<T> DeserializeAsync<T>(string data, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(Deserialize<T>(data));
	}

	/// <inheritdoc/>
	public Task<object> DeserializeAsync(string data, Type type, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(Deserialize(data, type));
	}

	private string Wrap(string state)
	{
		VstPreset preset = new(classId, Encoding.UTF8.GetBytes(state));
		return Convert.ToBase64String(VstPresetFile.ToBytes(preset));
	}

	private static string Unwrap(string data)
	{
		VstPreset preset = VstPresetFile.FromBytes(Convert.FromBase64String(data));
		return Encoding.UTF8.GetString(preset.ComponentState);
	}
}
