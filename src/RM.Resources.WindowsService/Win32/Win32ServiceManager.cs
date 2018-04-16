﻿
namespace RM.Resources.WindowsService.Win32
{
    using System;
    using System.ComponentModel;

    public sealed class Win32ServiceManager
    {
        private readonly string machineName;
        private readonly string databaseName;
        private readonly INativeInterop nativeInterop;

        public Win32ServiceManager(string machineName = null, string databaseName = null)
            : this(machineName, databaseName, Win32Interop.Wrapper)
        {
        }

        internal Win32ServiceManager(string machineName, string databaseName, INativeInterop nativeInterop)
        {
            this.machineName = machineName;
            this.databaseName = databaseName;
            this.nativeInterop = nativeInterop;
        }

        public void CreateService(string serviceName, string displayName, string description, string binaryPath, Win32ServiceCredentials credentials, bool autoStart = false, bool startImmediately = false, ErrorSeverity errorSeverity = ErrorSeverity.Normal)
        {
            if (string.IsNullOrEmpty(binaryPath))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(binaryPath));
            }
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(serviceName));
            }

            try
            {
                using (var mgr = ServiceControlManager.Connect(nativeInterop, machineName, databaseName, ServiceControlManagerAccessRights.All))
                {
                    DoCreateService(mgr, serviceName, displayName, description, binaryPath, credentials, autoStart, startImmediately, errorSeverity);
                }
            }
            catch (DllNotFoundException dllException)
            {
                throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.", dllException);
            }
        }

        private void DoCreateService(ServiceControlManager serviceControlManager, string serviceName, string displayName, string description, string binaryPath, Win32ServiceCredentials credentials, bool autoStart, bool startImmediately, ErrorSeverity errorSeverity)
        {
            using (var svc = serviceControlManager.CreateService(serviceName, displayName, binaryPath, ServiceType.Win32OwnProcess,
                    autoStart ? ServiceStartType.AutoStart : ServiceStartType.StartOnDemand, errorSeverity, credentials))
            {
                if (!string.IsNullOrEmpty(description))
                {
                    svc.SetDescription(description);
                }
                if (startImmediately)
                {
                    svc.Start();
                }
            }
        }

        public void CreateOrUpdateService(string serviceName, string displayName, string description, string binaryPath, Win32ServiceCredentials credentials, bool autoStart = false, bool startImmediately = false, ErrorSeverity errorSeverity = ErrorSeverity.Normal)
        {
            if (string.IsNullOrEmpty(binaryPath))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(binaryPath));
            }
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(serviceName));
            }

            try
            {
                using (var mgr = ServiceControlManager.Connect(nativeInterop, machineName, databaseName, ServiceControlManagerAccessRights.All))
                {
                    ServiceHandle existingService;
                    Win32Exception errorException;
                    if (mgr.TryOpenService(serviceName, ServiceControlAccessRights.All, out existingService, out errorException))
                    {
                        using (existingService)
                        {
                            DoUpdateService(displayName, description, binaryPath, credentials, autoStart, errorSeverity, existingService);
                        }
                    }
                    else
                    {
                        if (errorException.NativeErrorCode == KnownWin32ErrorCoes.ERROR_SERVICE_DOES_NOT_EXIST)
                        {
                            DoCreateService(mgr, serviceName, displayName, description, binaryPath, credentials, autoStart, startImmediately, errorSeverity);
                        }
                        else
                        {
                            throw errorException;
                        }
                    }
                }
            }
            catch (DllNotFoundException dllException)
            {
                throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.", dllException);
            }
        }

        private static void DoUpdateService(string displayName, string description, string binaryPath, Win32ServiceCredentials credentials, bool autoStart, ErrorSeverity errorSeverity, ServiceHandle existingService)
        {
            existingService.ChangeConfig(displayName, binaryPath, ServiceType.Win32OwnProcess,
                autoStart ? ServiceStartType.AutoStart : ServiceStartType.StartOnDemand, errorSeverity, credentials);
            existingService.SetDescription(description);
            existingService.Start(throwIfAlreadyRunning: false);
        }

        public void DeleteService(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(serviceName));
            }

            try
            {
                using (var mgr = ServiceControlManager.Connect(nativeInterop, machineName, databaseName, ServiceControlManagerAccessRights.All))
                {
                    using (var svc = mgr.OpenService(serviceName, ServiceControlAccessRights.All))
                    {
                        svc.Delete();
                    }
                }
            }
            catch (DllNotFoundException dllException)
            {
                throw new PlatformNotSupportedException(nameof(Win32ServiceHost) + " is only supported on Windows with service management API set.", dllException);
            }
        }
    }
}
