using System;

namespace Chromatics.Helpers
{
    // Runs code that references types from assemblies Windows App Control
    // can block (RGB.NET and the vendor SDKs), catching the JIT-time load
    // fault instead of letting it crash the caller.
    //
    // The one rule that makes this work: the risky type references MUST live
    // inside the Action lambda, never in the calling method's own body. A
    // lambda compiles to its own method that is only JIT-compiled when
    // invoked, so the load fault surfaces at action() inside the try below.
    // A reference in the caller's body faults while the caller itself is
    // being JIT-compiled - before any try/catch in it exists (the
    // CHROMATICS-17 and CHROMATICS-19 lesson).
    public static class AssemblyLoadGuard
    {
        public static bool TryRun(string label, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex) when (ex is System.IO.FileLoadException or System.IO.FileNotFoundException
                or BadImageFormatException or TypeInitializationException or TypeLoadException)
            {
                var inner = ex is TypeInitializationException { InnerException: not null } tie ? tie.InnerException : ex;
                bool appControl = inner.HResult == unchecked((int)0x800711C7)
                    || (inner.Message?.Contains("Application Control", StringComparison.OrdinalIgnoreCase) ?? false);

                // An App Control block is the machine's policy doing its job,
                // not a fault in Chromatics, so the user hears about it but
                // Sentry doesn't. A load failure with any other cause could
                // be a packaging mistake and still reports.
                Core.Logger.WriteConsole(Enums.LoggerTypes.Error, appControl
                    ? $"[{label}] Windows App Control blocked a library this feature needs: {inner.Message} To use it, allow Chromatics in Windows Security (App & browser control -> Smart App Control) and restart."
                    : $"[{label}] A library failed to load: {inner.Message}",
                    forwardToSentry: !appControl);
                return false;
            }
            catch (Exception ex)
            {
                Core.Logger.WriteConsole(Enums.LoggerTypes.Error, $"[{label}] Error: {ex.Message}");
                return false;
            }
        }
    }
}
