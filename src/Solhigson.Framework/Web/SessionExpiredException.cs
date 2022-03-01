using System;

namespace Solhigson.Framework.Web;

public class SessionExpiredException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionExpiredException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SessionExpiredException(string message, Exception innerException)
        : base(message, innerException)
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionExpiredException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public SessionExpiredException(string message) : base(message)
    {
            
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionExpiredException"/> class.
    /// </summary>
    public SessionExpiredException()
    {
            
    }

}