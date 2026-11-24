using CyberList.Models;

namespace CyberList.Interfaces;

/// <summary>
/// User settings, kept outside the database (the DB path can't live in the DB it points to).
/// Loaded once when the service is created; a missing or corrupt file gives the defaults.
/// </summary>
public interface ISettingsService
{
    /// <summary>The settings in use now. Change them through <see cref="Update"/>, not directly.</summary>
    AppSettings Current { get; }

    /// <summary>Raised after every successful <see cref="Update"/>, with the new settings.</summary>
    event Action<AppSettings>? SettingsChanged;

    /// <summary>Apply a change, save it to disk, then raise <see cref="SettingsChanged"/>.</summary>
    void Update(Action<AppSettings> change);
}
