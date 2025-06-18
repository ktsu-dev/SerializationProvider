// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace SerializationProvider.Exceptions;

/// <summary>
/// Represents errors that occur during serialization operations.
/// </summary>
public class SerializationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SerializationException"/> class.
	/// </summary>
	public SerializationException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SerializationException"/> class
	/// with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public SerializationException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SerializationException"/> class
	/// with a specified error message and a reference to the inner exception
	/// that is the cause of this exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	public SerializationException(string message, Exception innerException) : base(message, innerException)
	{
	}
}

/// <summary>
/// Represents errors that occur during deserialization operations.
/// </summary>
public class DeserializationException : SerializationException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DeserializationException"/> class.
	/// </summary>
	public DeserializationException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DeserializationException"/> class
	/// with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public DeserializationException(string message) : base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DeserializationException"/> class
	/// with a specified error message and a reference to the inner exception
	/// that is the cause of this exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	public DeserializationException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
