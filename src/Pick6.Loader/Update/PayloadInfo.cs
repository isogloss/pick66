namespace Pick6.Loader.Update;

/// <summary>
/// Record representing payload information from the update manifest
/// </summary>
/// <param name="PayloadVersion">Version string of the payload</param>
/// <param name="PayloadUrl">Download URL for the payload ZIP file</param>
/// <param name="Sha256">SHA256 hash for integrity verification</param>
/// <param name="EntryAssembly">Name of the main assembly to load</param>
/// <param name="EntryType">Fully qualified type name containing entry method</param>
/// <param name="EntryMethod">Static method name to invoke</param>
public record PayloadInfo(
    string PayloadVersion,
    string PayloadUrl,
    string Sha256,
    string EntryAssembly,
    string EntryType,
    string EntryMethod
);