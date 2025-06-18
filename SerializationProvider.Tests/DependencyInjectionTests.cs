// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SerializationProvider.Tests;
using FluentAssertions;
using ktsu.SerializationProvider.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DependencyInjectionTests
{
	private ServiceCollection _services = null!;

	[TestInitialize]
	public void SetUp()
	{
		_services = new ServiceCollection();
	}

	[TestMethod]
	public void AddSerializationProvider_ShouldRegisterCustomProvider_ByType()
	{
		// Act
		_services.AddSerializationProvider<TestSerializationProvider>();
		ServiceProvider serviceProvider = _services.BuildServiceProvider();

		// Assert
		ISerializationProvider provider = serviceProvider.GetRequiredService<ISerializationProvider>();
		provider.Should().NotBeNull();
		provider.Should().BeOfType<TestSerializationProvider>();
	}

	[TestMethod]
	public void AddSerializationProvider_ShouldRegisterCustomProvider_ByInstance()
	{
		// Arrange
		TestSerializationProvider customProvider = new();

		// Act
		_services.AddSerializationProvider(customProvider);
		ServiceProvider serviceProvider = _services.BuildServiceProvider();

		// Assert
		ISerializationProvider provider = serviceProvider.GetRequiredService<ISerializationProvider>();
		provider.Should().NotBeNull();
		provider.Should().BeSameAs(customProvider);
	}

	[TestMethod]
	public void AddSerializationProvider_ShouldRegisterCustomProvider_ByFactory()
	{
		// Act
		_services.AddSerializationProvider(serviceProvider => new TestSerializationProvider());
		ISerializationProvider provider = _services.BuildServiceProvider().GetRequiredService<ISerializationProvider>();

		// Assert
		provider.Should().NotBeNull();
		provider.Should().BeOfType<TestSerializationProvider>();
	}

	[TestMethod]
	public void SerializationProvider_ShouldWork_InDependencyInjectionContext()
	{
		// Arrange
		_services.AddSerializationProvider<TestSerializationProvider>();
		_services.AddTransient<TestService>();
		ServiceProvider serviceProvider = _services.BuildServiceProvider();

		// Act
		TestService testService = serviceProvider.GetRequiredService<TestService>();
		string result = testService.SerializeTestData();

		// Assert
		result.Should().NotBeNullOrEmpty();
		result.Should().Contain("TestValue");
	}

	[TestMethod]
	public void MultipleProviders_ShouldUseLastRegistered()
	{
		// Act
		_services.AddSerializationProvider<TestSerializationProvider>();
		_services.AddSerializationProvider<AnotherTestSerializationProvider>(); // This should be the one resolved
		ServiceProvider serviceProvider = _services.BuildServiceProvider();

		// Assert
		ISerializationProvider provider = serviceProvider.GetRequiredService<ISerializationProvider>();
		provider.Should().BeOfType<AnotherTestSerializationProvider>();
	}

	/// <summary>
	/// Test implementation of ISerializationProvider for testing purposes.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "This class is instantiated through dependency injection")]
	private sealed class TestSerializationProvider : ISerializationProvider
	{
		public string ProviderName => "Test Provider";
		public string ContentType => "application/json";

		public string Serialize<T>(T obj) => $"{{\"TestProperty\":\"TestValue\",\"Data\":\"{obj}\"}}";
		public string Serialize(object obj, Type type) => $"{{\"TestProperty\":\"TestValue\",\"Data\":\"{obj}\"}}";
		public T Deserialize<T>(string data) => (T)(object)data;
		public object Deserialize(string data, Type type) => data;
		public Task<string> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default) => Task.FromResult(Serialize(obj));
		public Task<string> SerializeAsync(object obj, Type type, CancellationToken cancellationToken = default) => Task.FromResult(Serialize(obj, type));
		public Task<T> DeserializeAsync<T>(string data, CancellationToken cancellationToken = default) => Task.FromResult(Deserialize<T>(data));
		public Task<object> DeserializeAsync(string data, Type type, CancellationToken cancellationToken = default) => Task.FromResult(Deserialize(data, type));
	}

	/// <summary>
	/// Another test implementation of ISerializationProvider for testing multiple registrations.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "This class is instantiated through dependency injection")]
	private sealed class AnotherTestSerializationProvider : ISerializationProvider
	{
		public string ProviderName => "Another Test Provider";
		public string ContentType => "application/json";

		public string Serialize<T>(T obj) => $"{{\"AnotherTestProperty\":\"AnotherTestValue\",\"Data\":\"{obj}\"}}";
		public string Serialize(object obj, Type type) => $"{{\"AnotherTestProperty\":\"AnotherTestValue\",\"Data\":\"{obj}\"}}";
		public T Deserialize<T>(string data) => (T)(object)data;
		public object Deserialize(string data, Type type) => data;
		public Task<string> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default) => Task.FromResult(Serialize(obj));
		public Task<string> SerializeAsync(object obj, Type type, CancellationToken cancellationToken = default) => Task.FromResult(Serialize(obj, type));
		public Task<T> DeserializeAsync<T>(string data, CancellationToken cancellationToken = default) => Task.FromResult(Deserialize<T>(data));
		public Task<object> DeserializeAsync(string data, Type type, CancellationToken cancellationToken = default) => Task.FromResult(Deserialize(data, type));
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "This class is instantiated through dependency injection")]
	private sealed class TestService(ISerializationProvider serializationProvider)
	{
		private readonly ISerializationProvider _serializationProvider = serializationProvider;

		public string SerializeTestData()
		{
			var data = new { TestProperty = "TestValue", Number = 42 };
			return _serializationProvider.Serialize(data);
		}
	}
}
