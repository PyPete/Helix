namespace Helix.Orchestration.Devices.Transport;

/// <summary>
/// Abstracts serial-port communication for driver unit testing and protocol separation.
/// Drivers depend on this interface; transport implementations live in the Agent project.
/// </summary>
public interface ISerialTransport
{
    bool IsOpen { get; }

    /// <summary>Opens the port at the given COM path with an optional baud rate (defaults to 9600).</summary>
    void Open(string portName, int baudRate = 9600);

    void Close();

    /// <summary>Writes <paramref name="command"/> and returns the response after <paramref name="timeoutMs"/> ms.</summary>
    string SendCommand(string command, int timeoutMs = 200);

    /// <summary>Writes raw bytes and returns the response as a string.</summary>
    string SendCommand(byte[] command, int timeoutMs = 200);

    /// <summary>Writes raw bytes and reads exactly <paramref name="expectedBytes"/> bytes back.</summary>
    byte[] SendCommandFixedResponse(byte[] command, int expectedBytes, int timeoutMs = 200);
}
