using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Messaging;

// Sección "RabbitMq" (RabbitMq__Host, RabbitMq__User, ...). Sin defaults para credenciales.
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required] public string Host { get; init; } = string.Empty;
    [Range(1, 65535)] public int Port { get; init; } = 5672;
    [Required] public string User { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
    public string VirtualHost { get; init; } = "/";

    [Range(1, 60)] public int PublishConfirmTimeoutSeconds { get; init; } = 5;
}
