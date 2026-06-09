// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SerializationProvider.Tests;

using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class VstPresetTests
{
	private const string SampleClassId = "565354416463416E6F746167666F6F62"; // 32 ASCII chars

	[TestMethod]
	public void ToBytes_ProducesValidVst3Header()
	{
		VstPreset preset = new(SampleClassId, [1, 2, 3, 4]);

		byte[] bytes = VstPresetFile.ToBytes(preset);

		Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("VST3");
		bytes.Length.Should().BeGreaterThan(48);
	}

	[TestMethod]
	public void RoundTrip_ComponentStateOnly_IsPreserved()
	{
		byte[] state = [10, 20, 30, 40, 50];
		VstPreset preset = new(SampleClassId, state);

		VstPreset decoded = VstPresetFile.FromBytes(VstPresetFile.ToBytes(preset));

		decoded.ClassId.Should().Be(SampleClassId);
		decoded.ComponentState.Should().Equal(state);
		decoded.ControllerState.Should().BeNull();
		decoded.MetaInfo.Should().BeNull();
		decoded.Version.Should().Be(VstPresetFile.FormatVersion);
	}

	[TestMethod]
	public void RoundTrip_AllChunks_ArePreserved()
	{
		byte[] component = [1, 2, 3];
		byte[] controller = [9, 8, 7, 6];
		const string meta = "<info><note>hello</note></info>";
		VstPreset preset = new(SampleClassId, component, controller, meta);

		VstPreset decoded = VstPresetFile.FromBytes(VstPresetFile.ToBytes(preset));

		decoded.ComponentState.Should().Equal(component);
		decoded.ControllerState.Should().Equal(controller);
		decoded.MetaInfo.Should().Be(meta);
	}

	[TestMethod]
	public void RoundTrip_EmptyComponentState_IsPreserved()
	{
		VstPreset preset = new(SampleClassId, []);

		VstPreset decoded = VstPresetFile.FromBytes(VstPresetFile.ToBytes(preset));

		decoded.ComponentState.Should().BeEmpty();
	}

	[TestMethod]
	public void Read_NonVstData_Throws()
	{
		byte[] garbage = Encoding.ASCII.GetBytes("NOPE this is not a preset file at all");

		Action act = () => VstPresetFile.FromBytes(garbage);

		act.Should().Throw<InvalidDataException>();
	}

	[TestMethod]
	public void Write_NonSeekableStream_Throws()
	{
		VstPreset preset = new(SampleClassId, [1]);

		Action act = () => VstPresetFile.Write(new NonSeekableStream(), preset);

		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void Provider_RoundTrips_ThroughInnerProvider()
	{
		VstPresetSerializationProvider provider = new(new JsonInner(), SampleClassId);
		Payload original = new("delay", 0.45);

		string serialized = provider.Serialize(original);
		Payload restored = provider.Deserialize<Payload>(serialized);

		provider.ProviderName.Should().Be("VST3 Preset");
		provider.ContentType.Should().Be("application/vnd.steinberg.vstpreset");
		restored.Should().Be(original);
	}

	[TestMethod]
	public void Provider_Output_IsBase64OfRealVstPreset()
	{
		VstPresetSerializationProvider provider = new(new JsonInner(), SampleClassId);

		string serialized = provider.Serialize(new Payload("gain", 1.0));
		byte[] presetBytes = Convert.FromBase64String(serialized);

		Encoding.ASCII.GetString(presetBytes, 0, 4).Should().Be("VST3");
		VstPreset decoded = VstPresetFile.FromBytes(presetBytes);
		decoded.ClassId.Should().Be(SampleClassId);
	}

	private sealed record Payload(string Name, double Value);

	private sealed class JsonInner : ISerializationProvider
	{
		public string ProviderName => "Json";
		public string ContentType => "application/json";
		public string Serialize<T>(T obj) => System.Text.Json.JsonSerializer.Serialize(obj);
		public string Serialize(object obj, Type type) => System.Text.Json.JsonSerializer.Serialize(obj, type);
		public T Deserialize<T>(string data) => System.Text.Json.JsonSerializer.Deserialize<T>(data)!;
		public object Deserialize(string data, Type type) => System.Text.Json.JsonSerializer.Deserialize(data, type)!;
		public Task<string> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default) => Task.FromResult(Serialize(obj));
		public Task<string> SerializeAsync(object obj, Type type, CancellationToken cancellationToken = default) => Task.FromResult(Serialize(obj, type));
		public Task<T> DeserializeAsync<T>(string data, CancellationToken cancellationToken = default) => Task.FromResult(Deserialize<T>(data));
		public Task<object> DeserializeAsync(string data, Type type, CancellationToken cancellationToken = default) => Task.FromResult(Deserialize(data, type));
	}

	private sealed class NonSeekableStream : MemoryStream
	{
		public override bool CanSeek => false;
	}
}
