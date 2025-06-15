using SCSA.Models;

namespace SCSA.Services;

public interface IAppSettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}