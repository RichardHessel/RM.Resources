﻿
namespace RM.Resources.WindowsService.Win32
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using Annotations;

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    [SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global", Justification = "Subclassed by test proxy")]
    internal class ServiceHandle : SafeHandle
    {
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Exposed for testing via InternalsVisibleTo.")]
        internal INativeInterop NativeInterop { get; set; } = Win32Interop.Wrapper;

        internal ServiceHandle() : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeInterop.CloseServiceHandle(handle);
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        public virtual void Start(bool throwIfAlreadyRunning = true)
        {
            if (!NativeInterop.StartServiceW(this, 0, IntPtr.Zero))
            {
                var win32Error = Marshal.GetLastWin32Error();
                if (win32Error != KnownWin32ErrorCoes.ERROR_SERVICE_ALREADY_RUNNING || throwIfAlreadyRunning)
                {
                    throw new Win32Exception(win32Error);
                }
            }
        }

        public virtual void Delete()
        {
            if (!NativeInterop.DeleteService(this))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public virtual void SetDescription(string description)
        {
            var descriptionInfo = new ServiceDescriptionInfo(description ?? string.Empty);
            var lpDescriptionInfo = Marshal.AllocHGlobal(Marshal.SizeOf<ServiceDescriptionInfo>());
            try
            {
                Marshal.StructureToPtr(descriptionInfo, lpDescriptionInfo, fDeleteOld: false);
                try
                {
                    if (!NativeInterop.ChangeServiceConfig2W(this, ServiceConfigInfoTypeLevel.ServiceDescription, lpDescriptionInfo))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                finally
                {
                    Marshal.DestroyStructure<ServiceDescriptionInfo>(lpDescriptionInfo);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(lpDescriptionInfo);
            }
        }

        public virtual void ChangeConfig(string displayName, string binaryPath, ServiceType serviceType, ServiceStartType startupType, ErrorSeverity errorSeverity, Win32ServiceCredentials credentials)
        {
            var success = NativeInterop.ChangeServiceConfigW(this, serviceType, startupType, errorSeverity, binaryPath, null, IntPtr.Zero, null, credentials.UserName, credentials.Password, displayName);
            if (!success)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}
