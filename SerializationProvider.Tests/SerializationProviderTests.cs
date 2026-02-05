// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SerializationProvider.Tests;

using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SerializationProviderTests
{
	private sealed record TestObject(string Name, int Age, DateTime CreatedAt);

	[TestMethod]
	public void ISerializationProvider_ShouldHaveCorrectInterface()
	{
		// Arrange
		TestSerializationProvider provider = new();

		// Assert
		provider.ProviderName.Should().NotBeNullOrEmpty();
		provider.ContentType.Should().NotBeNullOrEmpty();
	}

	[TestMethod]
	public void SerializationProvider_ShouldSerializeAndDeserialize_Generic()
	{
		// Arrange
		TestSerializationProvider provider = new();
		TestObject testObject = new("John Doe", 30, new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));

		// Act
		string serialized = provider.Serialize(testObject);
		TestObject deserialized = provider.Deserialize<TestObject>(serialized);

		// Assert
		serialized.Should().NotBeNullOrEmpty();
		deserialized.Should().NotBeNull();
		deserialized.Name.Should().Be(testObject.Name);
		deserialized.Age.Should().Be(testObject.Age);
	}

	[TestMethod]
	public void SerializationProvider_ShouldSerializeWithType_UsingObjectParameter()
	{
		// Arrange
		TestSerializationProvider provider = new();
		TestObject testObject = new("Test", 100, DateTime.UtcNow);

		// Act
		string serialized = provider.Serialize(testObject);
		object deserialized = provider.Deserialize<TestObject>(serialized);

		// Assert
		serialized.Should().NotBeNullOrEmpty();
		deserialized.Should().BeOfType<TestObject>();
		((TestObject)deserialized).Name.Should().Be(testObject.Name);
	}

	[TestMethod]
	public async Task SerializationProvider_ShouldSupportAsyncOperations()
	{
		// Arrange
		TestSerializationProvider provider = new();
		TestObject testObject = new("Async Test", 50, DateTime.UtcNow);

		// Act
		string serialized = await provider.SerializeAsync(testObject).ConfigureAwait(false);
		TestObject deserialized = await provider.DeserializeAsync<TestObject>(serialized).ConfigureAwait(false);

		// Assert
		serialized.Should().NotBeNullOrEmpty();
		deserialized.Should().NotBeNull();
		deserialized.Name.Should().Be(testObject.Name);
	}

	[TestMethod]
	public async Task SerializationProvider_ShouldSupportAsyncOperationsWithType()
	{
		// Arrange
		TestSerializationProvider provider = new();
		TestObject testObject = new("Async Type Test", 75, DateTime.UtcNow);

		// Act
		string serialized = await provider.SerializeAsync(testObject).ConfigureAwait(false);
		object deserialized = await provider.DeserializeAsync(serialized, typeof(TestObject)).ConfigureAwait(false);

		// Assert
		serialized.Should().NotBeNullOrEmpty();
		deserialized.Should().BeOfType<TestObject>();
		((TestObject)deserialized).Name.Should().Be(testObject.Name);
	}

	[TestMethod]
	public async Task SerializationProvider_ShouldSupportCancellationToken()
	{
		// Arrange
		TestSerializationProvider provider = new();
		TestObject testObject = new("Cancellation Test", 25, DateTime.UtcNow);
		using CancellationTokenSource cts = new();

		// Act
		string serialized = await provider.SerializeAsync(testObject, cts.Token).ConfigureAwait(false);
		TestObject deserialized = await provider.DeserializeAsync<TestObject>(serialized, cts.Token).ConfigureAwait(false);

		// Assert
		serialized.Should().NotBeNullOrEmpty();
		deserialized.Name.Should().Be(testObject.Name);
	}

	[TestMethod]
	public void SerializationProvider_ShouldHandleNullObjectSerialization()
	{
		// Arrange
		TestSerializationProvider provider = new();
		TestObject? nullObject = null;

		// Act
		string serialized = provider.Serialize(nullObject);

		// Assert
		serialized.Should().NotBeNullOrEmpty();
	}

	/// <summary>
	/// Test implementation of ISerializationProvider for testing purposes.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "This class is used for testing interface implementation")]
	private sealed class TestSerializationProvider : ISerializationProvider
	{
		public string ProviderName => "Test Provider";
		public string ContentType => "application/json";

		public string Serialize<T>(T obj)
		{
			if (obj is null)
			{
				return "null";
			}

			return System.Text.Json.JsonSerializer.Serialize(obj);
		}

		public string Serialize(object obj, Type type)
		{
			if (obj is null)
			{
				return "null";
			}

			return System.Text.Json.JsonSerializer.Serialize(obj, type);
		}

		public T Deserialize<T>(string data)
		{
			if (string.IsNullOrWhiteSpace(data))
			{
				throw new ArgumentException("Data cannot be null or empty", nameof(data));
			}

			return System.Text.Json.JsonSerializer.Deserialize<T>(data)!;
		}

		public object Deserialize(string data, Type type)
		{
			if (string.IsNullOrWhiteSpace(data))
			{
				throw new ArgumentException("Data cannot be null or empty", nameof(data));
			}

			return System.Text.Json.JsonSerializer.Deserialize(data, type)!;
		}

		public Task<string> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Serialize(obj));
		}

		public Task<string> SerializeAsync(object obj, Type type, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Serialize(obj, type));
		}

		public Task<T> DeserializeAsync<T>(string data, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Deserialize<T>(data));
		}

		public Task<object> DeserializeAsync(string data, Type type, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Deserialize(data, type));
		}
	}
}
