# ktsu.SerializationProvider

A dependency injection interface for pluggable serialization providers. Define serialization contracts once and swap implementations without changing application code.

## Features

- **Unified Interface**: Single interface for all serialization operations
- **Dependency Injection Ready**: Easy registration with Microsoft.Extensions.DependencyInjection
- **Zero Dependencies**: Pure interface library with no specific serialization library dependencies
- **Async Support**: Full async/await support for all operations
- **Type Safety**: Generic methods provide compile-time type safety
- **Extensible**: Easy to create custom serialization providers
- **Well Tested**: Comprehensive unit test coverage

## Installation

```bash
dotnet add package SerializationProvider
```

## Quick Start

### 1. Create a Serialization Provider

First, implement the `ISerializationProvider` interface:

```csharp
public class JsonSerializationProvider : ISerializationProvider
{
    public string ProviderName => "System.Text.Json";
    public string ContentType => "application/json";

    public string Serialize<T>(T obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj);
    }

    public T Deserialize<T>(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        
        return System.Text.Json.JsonSerializer.Deserialize<T>(data)!;
    }

    public string Serialize(object obj, Type type)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, type);
    }

    public object Deserialize(string data, Type type)
    {
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        
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
```

### 2. Register the Serialization Provider

```csharp
using SerializationProvider.Extensions;

// In your Startup.cs or Program.cs
services.AddSerializationProvider<JsonSerializationProvider>();

// Or register by instance
services.AddSerializationProvider(new JsonSerializationProvider());

// Or register by factory
services.AddSerializationProvider(serviceProvider => 
    new JsonSerializationProvider());
```

### 3. Inject and Use the Serialization Provider

```csharp
public class MyService
{
    private readonly ISerializationProvider _serializationProvider;

    public MyService(ISerializationProvider serializationProvider)
    {
        _serializationProvider = serializationProvider;
    }

    public async Task<string> SerializeDataAsync<T>(T data)
    {
        return await _serializationProvider.SerializeAsync(data);
    }

    public async Task<T> DeserializeDataAsync<T>(string json)
    {
        return await _serializationProvider.DeserializeAsync<T>(json);
    }
}
```

## API Reference

### ISerializationProvider Interface

```csharp
public interface ISerializationProvider
{
    string ProviderName { get; }
    string ContentType { get; }

    // Synchronous methods
    string Serialize<T>(T obj);
    string Serialize(object obj, Type type);
    T Deserialize<T>(string data);
    object Deserialize(string data, Type type);

    // Asynchronous methods
    Task<string> SerializeAsync<T>(T obj, CancellationToken cancellationToken = default);
    Task<string> SerializeAsync(object obj, Type type, CancellationToken cancellationToken = default);
    Task<T> DeserializeAsync<T>(string data, CancellationToken cancellationToken = default);
    Task<object> DeserializeAsync(string data, Type type, CancellationToken cancellationToken = default);
}
```

## Usage Examples

### Basic Serialization

```csharp
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public DateTime BirthDate { get; set; }
}

// Serialize
var person = new Person { Name = "John Doe", Age = 30, BirthDate = DateTime.Now };
string json = serializationProvider.Serialize(person);

// Deserialize
var deserializedPerson = serializationProvider.Deserialize<Person>(json);
```

### Async Operations

```csharp
// Async serialize
string json = await serializationProvider.SerializeAsync(person);

// Async deserialize
var person = await serializationProvider.DeserializeAsync<Person>(json);
```

### Type-based Operations

```csharp
// When you need to work with Type objects
Type personType = typeof(Person);
string json = serializationProvider.Serialize(person, personType);
object deserializedObject = serializationProvider.Deserialize(json, personType);
```

## Dependency Injection Registration Options

### Register by Type

```csharp
services.AddSerializationProvider<MyCustomSerializationProvider>();
```

### Register by Instance

```csharp
services.AddSerializationProvider(new MyCustomSerializationProvider());
```

### Register by Factory

```csharp
services.AddSerializationProvider(serviceProvider => 
    new MyCustomSerializationProvider(
        serviceProvider.GetRequiredService<ILogger<MyCustomSerializationProvider>>()
    ));
```

## Creating Custom Serialization Providers

### JSON Provider with Newtonsoft.Json

```csharp
public class NewtonsoftJsonSerializationProvider : ISerializationProvider
{
    private readonly JsonSerializerSettings _settings;

    public NewtonsoftJsonSerializationProvider(JsonSerializerSettings? settings = null)
    {
        _settings = settings ?? new JsonSerializerSettings();
    }

    public string ProviderName => "Newtonsoft.Json";
    public string ContentType => "application/json";

    public string Serialize<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj, _settings);
    }

    public T Deserialize<T>(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        
        return JsonConvert.DeserializeObject<T>(data, _settings)!;
    }

    // Implement other interface methods...
}
```

### XML Provider

```csharp
public class XmlSerializationProvider : ISerializationProvider
{
    public string ProviderName => "System.Xml";
    public string ContentType => "application/xml";

    public string Serialize<T>(T obj)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, obj);
        return stringWriter.ToString();
    }

    public T Deserialize<T>(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        var serializer = new XmlSerializer(typeof(T));
        using var stringReader = new StringReader(data);
        return (T)serializer.Deserialize(stringReader)!;
    }

    // Implement other interface methods...
}
```

## Best Practices

1. **Use Async Methods**: For I/O operations, prefer async methods to avoid blocking threads
2. **Handle Exceptions**: Wrap serialization operations in try-catch blocks for production code
3. **Validate Input**: Always validate input data in your serialization provider implementations
4. **Single Provider per Application**: Register only one serialization provider per application to avoid confusion
5. **Provider Naming**: Use descriptive provider names to help with debugging and logging

## Testing

The library includes comprehensive unit tests. Run them using:

```bash
dotnet test
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE.md file for details.

## Changelog

See CHANGELOG.md for a list of changes and version history.