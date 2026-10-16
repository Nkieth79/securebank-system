using System;
using System.Collections.Generic;
using System.Text;
namespace SecureBank.Shared.Protocol;

public static class MessageProtocol
{
    public const int Port = 5000;

    public const string Separator = "|";

    public const string ResponseOk = "OK";
    public const string ResponseError = "ERROR";

    public static string Build(string command, params string[] args)
    {
        return command + Separator + string.Join(Separator, args);
    }

    public static string[] Parse(string message)
    {
        return message.Split(Separator);
    }
}