using System.ComponentModel;
using System.Reflection;
using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using Helix.Orchestration.Devices.Abstractions;

namespace Helix.Orchestration.Devices.Transports.CDll;

/// <summary>
/// Native DLL transport that loads vendor libraries at runtime and invokes exported functions through unmanaged delegates.
/// </summary>
public sealed class CDllDeviceTransport : IDeviceTransport
{
    private readonly CDllTransportOptions _options;
    private readonly ILogger<CDllDeviceTransport> _logger;
    private readonly Subject<TransportStatus> _statusSubject = new();
    private readonly Subject<byte[]> _dataReceivedSubject = new();
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly Dictionary<string, Delegate> _resolvedFunctions = new(StringComparer.OrdinalIgnoreCase);

    private SafeLibraryHandle? _libraryHandle;
    private TransportStatus _status = TransportStatus.Disconnected;
    private bool _disposed;

    public CDllDeviceTransport(CDllTransportOptions options, ILogger<CDllDeviceTransport> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public TransportStatus Status => _status;

    /// <inheritdoc />
    public IObservable<TransportStatus> StatusChanged => _statusSubject;

    /// <inheritdoc />
    public IObservable<byte[]> DataReceived => _dataReceivedSubject;

    /// <summary>
    /// Gets a value indicating whether the native library is currently loaded.
    /// </summary>
    public bool IsConnected => _libraryHandle is not null && !_libraryHandle.IsInvalid && IsHandleValid();

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();

            if (IsConnected)
            {
                return;
            }

            SetStatus(_status == TransportStatus.Faulted ? TransportStatus.Reconnecting : TransportStatus.Connecting);

            var resolvedPath = ResolveLibraryPath();
            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException($"Native library '{resolvedPath}' was not found.", resolvedPath);
            }

            var nativeHandle = NativeLibrary.Load(resolvedPath);
            _libraryHandle = new SafeLibraryHandle(nativeHandle);
            _resolvedFunctions.Clear();

            foreach (var mapping in _options.FunctionMap)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _resolvedFunctions[mapping.Key] = ResolveFunction(mapping.Value);
            }

            _logger.LogInformation("Loaded native device library from {LibraryPath}", resolvedPath);
            SetStatus(TransportStatus.Connected);
        }
        catch (Exception ex) when (ex is not TransportFaultException)
        {
            SetStatus(TransportStatus.Faulted);
            throw new TransportFaultException("Failed to load native device library.", "C DLL", TransportStatus.Connecting, ex);
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _resolvedFunctions.Clear();

            if (_libraryHandle is not null)
            {
                _libraryHandle.Dispose();
                _libraryHandle = null;
            }

            SetStatus(TransportStatus.Disconnected);
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> SendReceiveAsync(byte[] command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!IsConnected)
        {
            throw new TransportFaultException("Native transport is not connected.", "C DLL", _status);
        }

        await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_resolvedFunctions.TryGetValue("SendReceive", out var del))
            {
                throw new InvalidOperationException("FunctionMap must contain a 'SendReceive' mapping.");
            }

            if (del is not NativeSendReceive sendReceive)
            {
                throw new InvalidOperationException("The SendReceive export does not match the expected signature.");
            }

            var responseBuffer = new byte[Math.Max(command.Length * 4, 4096)];
            var bytesWritten = 0;

            try
            {
                bytesWritten = sendReceive(command, command.Length, responseBuffer, responseBuffer.Length);
            }
            catch (SEHException ex)
            {
                SetStatus(TransportStatus.Faulted);
                throw new TransportFaultException("Native transport raised an SEH exception.", "C DLL", TransportStatus.Connected, ex);
            }
            catch (AccessViolationException ex)
            {
                SetStatus(TransportStatus.Faulted);
                throw new TransportFaultException("Native transport raised an access violation.", "C DLL", TransportStatus.Connected, ex);
            }

            if (bytesWritten < 0)
            {
                SetStatus(TransportStatus.Faulted);
                throw new TransportFaultException("Native transport returned a failure code.", "C DLL", TransportStatus.Connected);
            }

            var response = responseBuffer.AsSpan(0, bytesWritten).ToArray();
            _dataReceivedSubject.OnNext(response);
            return response;
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await DisconnectAsync().ConfigureAwait(false);
        _statusSubject.Dispose();
        _dataReceivedSubject.Dispose();
        _sync.Dispose();
    }

    private Delegate ResolveFunction(string exportName)
    {
        if (_libraryHandle is null || _libraryHandle.IsInvalid)
        {
            throw new InvalidOperationException("The native library is not loaded.");
        }

        if (!NativeLibrary.TryGetExport(_libraryHandle.DangerousGetHandle(), exportName, out var export) || export == IntPtr.Zero)
        {
            throw new MissingMethodException($"The export '{exportName}' could not be found in '{ResolveLibraryPath()}'.");
        }

        return string.Equals(exportName, _options.FunctionMap.GetValueOrDefault("SendReceive"), StringComparison.OrdinalIgnoreCase)
            ? Marshal.GetDelegateForFunctionPointer<NativeSendReceive>(export)
            : Marshal.GetDelegateForFunctionPointer(export, typeof(NativeNoop));
    }

    private string ResolveLibraryPath()
    {
        var baseDirectory = string.IsNullOrWhiteSpace(_options.DllPath)
            ? AppContext.BaseDirectory
            : _options.DllPath;

        if (_options.UseArchitectureSubfolder)
        {
            var architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                Architecture.Arm => "win-arm",
                _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.ProcessArchitecture}")
            };

            baseDirectory = Path.Combine(baseDirectory, "runtimes", architecture, "native");
        }

        return Path.Combine(baseDirectory, _options.DllName);
    }

    private bool IsHandleValid()
    {
        if (_libraryHandle is null || _libraryHandle.IsInvalid)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.HandleValidationFunction))
        {
            return _options.MaxValidHandle <= 0 || _libraryHandle.DangerousGetHandle().ToInt64() <= _options.MaxValidHandle;
        }

        if (!_resolvedFunctions.TryGetValue(_options.HandleValidationFunction, out var del))
        {
            return true;
        }

        if (del is NativeHandleValidation handleValidation)
        {
            return handleValidation() != 0;
        }

        return true;
    }

    private void SetStatus(TransportStatus newStatus)
    {
        if (_status == newStatus)
        {
            return;
        }

        _status = newStatus;
        _statusSubject.OnNext(newStatus);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeSendReceive(
        [In] byte[] requestBuffer,
        int requestLength,
        [Out] byte[] responseBuffer,
        int responseBufferLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeHandleValidation();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NativeNoop();

    /// <summary>
    /// SafeHandle wrapper for a loaded native library.
    /// </summary>
    public sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeLibraryHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            NativeLibrary.Free(handle);
            return true;
        }
    }
}