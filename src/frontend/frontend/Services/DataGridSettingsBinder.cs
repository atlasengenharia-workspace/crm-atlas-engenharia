using Radzen;

namespace CrmAtlas.Web.Services;

public sealed class DataGridSettingsBinder
{
    private readonly IRadzenDataGridStateService _service;
    private readonly string _key;
    private DataGridSettings? _settings;

    public DataGridSettingsBinder(IRadzenDataGridStateService service, string key)
    {
        _service = service;
        _key = key;
    }

    public DataGridSettings? Value
    {
        get => _settings;
        set
        {
            if (ReferenceEquals(_settings, value) || _settings == value) return;
            _settings = value;
            if (value is not null)
                _ = _service.SaveAsync(_key, value);
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _settings = await _service.LoadAsync(_key, cancellationToken);
    }
}
