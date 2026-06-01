using System.Runtime.CompilerServices;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the OceanView NIR spectrometer. Facade over IOceanViewDriver.
/// The IDeviceTransport parameter satisfies the base class requirement but is never used for I/O.
/// StreamTrendsAsync not implemented — spectrum data is non-scalar (Matrix); use GetSpectrum capability instead.
/// </summary>
public sealed class OceanViewNir : DeviceAdapterBase
{
    private readonly IOceanViewDriver _driver;
    private int _deviceId;

    public OceanViewNir(
        IOceanViewDriver driver,
        IDeviceTransport transport,
        ILogger<OceanViewNir> logger)
        : base(transport, logger)
    {
        _driver = driver;
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Open: sets the device ID for subsequent calls
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Open",
                "Open the NIR spectrometer by device ID",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"deviceId\":{\"type\":\"integer\"}},\"required\":[\"deviceId\"]}"),
            (parameters, _, _) =>
            {
                _deviceId = parameters.GetProperty("deviceId").GetInt32();
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // IsDevice: opens and initializes the device
        builder.RegisterSimple(
            "IsDevice",
            "Initialize and verify the NIR spectrometer is connected",
            _ =>
            {
                var result = _driver.OpenDevice(_deviceId);
                if (result.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed(result.Reason));
                _driver.ApplyElectricDarkCorrection(_deviceId, true);
                _driver.SetIntegrationTime(_deviceId, 1000);
                _driver.SetScansToAverage(_deviceId, 4);
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // Close: closes the device
        builder.RegisterSimple(
            "Close",
            "Close the NIR spectrometer connection",
            _ =>
            {
                var result = _driver.CloseDevice(_deviceId);
                return Task.FromResult(result.State == DeviceResultState.Failure
                    ? CapabilityInvocationResult.Failed(result.Reason)
                    : CapabilityInvocationResult.Successful());
            });

        // GetSpectrum: returns the current spectrum as a JSON array
        builder.RegisterSimple(
            "GetSpectrum",
            "Acquire a spectrum from the NIR spectrometer",
            _ =>
            {
                var (result, spectrum) = _driver.GetSpectrum(_deviceId);
                if (result.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed(result.Reason));
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(spectrum)));
            });

        // GetWavelengths: returns the wavelength axis as a JSON array
        builder.RegisterSimple(
            "GetWavelengths",
            "Get the wavelength axis for the NIR spectrometer",
            _ =>
            {
                var (result, wavelengths) = _driver.GetWavelengths(_deviceId);
                if (result.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed(result.Reason));
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(wavelengths)));
            });

        // TurnOnElectricDark
        builder.RegisterSimple(
            "TurnOnElectricDark",
            "Enable electric dark correction on the NIR spectrometer",
            _ =>
            {
                var result = _driver.ApplyElectricDarkCorrection(_deviceId, true);
                return Task.FromResult(result.State == DeviceResultState.Failure
                    ? CapabilityInvocationResult.Failed(result.Reason)
                    : CapabilityInvocationResult.Successful());
            });

        // TurnOffElectricDark
        builder.RegisterSimple(
            "TurnOffElectricDark",
            "Disable electric dark correction on the NIR spectrometer",
            _ =>
            {
                var result = _driver.ApplyElectricDarkCorrection(_deviceId, false);
                return Task.FromResult(result.State == DeviceResultState.Failure
                    ? CapabilityInvocationResult.Failed(result.Reason)
                    : CapabilityInvocationResult.Successful());
            });

        // TurnOnNonLinearCorrection
        builder.RegisterSimple(
            "TurnOnNonLinearCorrection",
            "Enable non-linearity correction on the NIR spectrometer",
            _ =>
            {
                var result = _driver.ApplyNonLinearityCorrection(_deviceId, true);
                return Task.FromResult(result.State == DeviceResultState.Failure
                    ? CapabilityInvocationResult.Failed(result.Reason)
                    : CapabilityInvocationResult.Successful());
            });

        // TurnOffNonLinearCorrection
        builder.RegisterSimple(
            "TurnOffNonLinearCorrection",
            "Disable non-linearity correction on the NIR spectrometer",
            _ =>
            {
                var result = _driver.ApplyNonLinearityCorrection(_deviceId, false);
                return Task.FromResult(result.State == DeviceResultState.Failure
                    ? CapabilityInvocationResult.Failed(result.Reason)
                    : CapabilityInvocationResult.Successful());
            });

        // SetScansToAverage
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetScansToAverage",
                "Set the number of scans to average on the NIR spectrometer",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"numberOfScans\":{\"type\":\"integer\",\"minimum\":1}},\"required\":[\"numberOfScans\"]}"),
            (parameters, _, _) =>
            {
                var n = parameters.GetProperty("numberOfScans").GetInt32();
                var result = _driver.SetScansToAverage(_deviceId, n);
                return Task.FromResult(result.State == DeviceResultState.Failure
                    ? CapabilityInvocationResult.Failed(result.Reason)
                    : CapabilityInvocationResult.Successful());
            });

        // GetScansToAverage
        builder.RegisterSimple(
            "GetScansToAverage",
            "Get the current scans-to-average setting from the NIR spectrometer",
            _ =>
            {
                var (result, scans) = _driver.GetScansToAverage(_deviceId);
                if (result.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed(result.Reason));
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(scans)));
            });

        // SetBoxcarWidth
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetBoxcarWidth",
                "Set the boxcar averaging width on the NIR spectrometer",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"width\":{\"type\":\"integer\",\"minimum\":0}},\"required\":[\"width\"]}"),
            (parameters, _, _) =>
            {
                var width = parameters.GetProperty("width").GetInt32();
                var result = _driver.SetBoxcarWidth(_deviceId, width);
                return Task.FromResult(result.State == DeviceResultState.Failure
                    ? CapabilityInvocationResult.Failed(result.Reason)
                    : CapabilityInvocationResult.Successful());
            });

        // GetBoxcarWidth
        builder.RegisterSimple(
            "GetBoxcarWidth",
            "Get the current boxcar width from the NIR spectrometer",
            _ =>
            {
                var (result, width) = _driver.GetBoxcarWidth(_deviceId);
                if (result.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed(result.Reason));
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(width)));
            });

        // SetIntegrationTime
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetIntegrationTime",
                "Set the integration time in milliseconds on the NIR spectrometer",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"milliseconds\":{\"type\":\"integer\",\"minimum\":1}},\"required\":[\"milliseconds\"]}"),
            (parameters, _, _) =>
            {
                var ms = parameters.GetProperty("milliseconds").GetInt32();
                var result = _driver.SetIntegrationTime(_deviceId, ms);
                return Task.FromResult(result.State == DeviceResultState.Failure
                    ? CapabilityInvocationResult.Failed(result.Reason)
                    : CapabilityInvocationResult.Successful());
            });

        // GetIntegrationTime
        builder.RegisterSimple(
            "GetIntegrationTime",
            "Get the current integration time from the NIR spectrometer",
            _ =>
            {
                var (result, t) = _driver.GetIntegrationTime(_deviceId);
                if (result.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed(result.Reason));
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(t)));
            });

        // GetMinIntegrationTime
        builder.RegisterSimple(
            "GetMinIntegrationTime",
            "Get the minimum integration time for the NIR spectrometer",
            _ =>
            {
                var (result, t) = _driver.GetMinIntegrationTime(_deviceId);
                if (result.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed(result.Reason));
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(t)));
            });

        // GetMaxIntegrationTime
        builder.RegisterSimple(
            "GetMaxIntegrationTime",
            "Get the maximum integration time for the NIR spectrometer",
            _ =>
            {
                var (result, t) = _driver.GetMaxIntegrationTime(_deviceId);
                if (result.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed(result.Reason));
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(t)));
            });

        // GetIntegrationStepSize
        builder.RegisterSimple(
            "GetIntegrationStepSize",
            "Get the integration time step size for the NIR spectrometer",
            _ =>
            {
                var (result, step) = _driver.GetIntegrationStepSize(_deviceId);
                if (result.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed(result.Reason));
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(step)));
            });

        // FindAllDevices
        builder.RegisterSimple(
            "FindAllDevices",
            "Find all active OceanView devices",
            _ =>
            {
                var devices = _driver.FindActiveDevices();
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(devices.Select(d => new { d.Id, d.Name }))));
            });
    }

    // StreamTrendsAsync not implemented — spectrum data is non-scalar (Matrix); use GetSpectrum capability instead.
}
