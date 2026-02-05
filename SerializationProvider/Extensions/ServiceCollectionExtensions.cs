// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.SerializationProvider.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering serialization providers with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers a custom serialization provider.
	/// </summary>
	/// <typeparam name="TProvider">The type of the serialization provider.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddSerializationProvider<TProvider>(this IServiceCollection services)
		where TProvider : class, ISerializationProvider => services.AddSingleton<ISerializationProvider, TProvider>();

	/// <summary>
	/// Registers a custom serialization provider instance.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="provider">The serialization provider instance.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddSerializationProvider(
		this IServiceCollection services,
		ISerializationProvider provider) => services.AddSingleton(provider);

	/// <summary>
	/// Registers a custom serialization provider factory.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="providerFactory">A factory function to create the serialization provider.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddSerializationProvider(
		this IServiceCollection services,
		Func<IServiceProvider, ISerializationProvider> providerFactory) => services.AddSingleton(providerFactory);
}
