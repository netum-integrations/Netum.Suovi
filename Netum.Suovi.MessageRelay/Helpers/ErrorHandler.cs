using System;
using Netum.Suovi.MessageRelay.Definitions;

namespace Netum.Suovi.MessageRelay.Helpers;

internal static class ErrorHandler
{
    internal static Result Handle(this Exception exception, Options options)
    {
        var message = string.IsNullOrWhiteSpace(options?.ErrorMessageOnFailure)
            ? exception.Message
            : options.ErrorMessageOnFailure;

        if (options?.ThrowErrorOnFailure != false)
            throw new Exception(message, exception);

        return new Result
        {
            Success = false,
            Error = new Error { Message = message, AdditionalInfo = exception },
        };
    }
}
