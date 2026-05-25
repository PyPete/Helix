namespace Helix.Orchestration.Devices.Model;

public sealed record DeviceInfoComponent(string Name, string Value);

public sealed record CommandComponent(string Name, string Command);

public sealed record ConnectionComponent(string Name, string Value);

public sealed record DeviceDetails(
    string DeviceType,
    IReadOnlyList<DeviceInfoComponent> DeviceInfo,
    IReadOnlyList<ConnectionComponent> Connection,
    IReadOnlyList<CommandComponent> Commands
);
