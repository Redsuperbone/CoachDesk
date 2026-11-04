using System.ComponentModel;

namespace CoachDesk.Desktop.ViewModels;

public sealed class AnthropometricMetricRowViewModel : ViewModelBase, IDataErrorInfo
{
    private readonly decimal?[] _months = new decimal?[10];

    public AnthropometricMetricRowViewModel(string key, string name, string unit, decimal minimum, decimal maximum)
    {
        Key = key;
        Name = name;
        Unit = unit;
        Minimum = minimum;
        Maximum = maximum;
    }

    public string Key { get; }
    public string Name { get; }
    public string Unit { get; }
    public decimal Minimum { get; }
    public decimal Maximum { get; }
    public string Error => string.Empty;
    public bool HasErrors => _months.Any(value => !IsValid(value));

    public decimal? Month1 { get => _months[0]; set => SetMonth(0, value); }
    public decimal? Month2 { get => _months[1]; set => SetMonth(1, value); }
    public decimal? Month3 { get => _months[2]; set => SetMonth(2, value); }
    public decimal? Month4 { get => _months[3]; set => SetMonth(3, value); }
    public decimal? Month5 { get => _months[4]; set => SetMonth(4, value); }
    public decimal? Month6 { get => _months[5]; set => SetMonth(5, value); }
    public decimal? Month7 { get => _months[6]; set => SetMonth(6, value); }
    public decimal? Month8 { get => _months[7]; set => SetMonth(7, value); }
    public decimal? Month9 { get => _months[8]; set => SetMonth(8, value); }
    public decimal? Month10 { get => _months[9]; set => SetMonth(9, value); }

    public decimal? GetValue(int monthNumber) => _months[monthNumber - 1];

    public void SetValue(int monthNumber, decimal? value) => SetMonth(monthNumber - 1, value);

    public string this[string columnName]
    {
        get
        {
            if (!columnName.StartsWith("Month", StringComparison.Ordinal) ||
                !int.TryParse(columnName[5..], out var monthNumber) ||
                monthNumber is < 1 or > 10)
            {
                return string.Empty;
            }

            return IsValid(_months[monthNumber - 1])
                ? string.Empty
                : $"Rango permitido: {Minimum:0.##} a {Maximum:0.##} {Unit}.";
        }
    }

    private void SetMonth(int index, decimal? value)
    {
        if (_months[index] == value)
        {
            return;
        }

        _months[index] = value;
        OnPropertyChanged($"Month{index + 1}");
        OnPropertyChanged(nameof(HasErrors));
    }

    private bool IsValid(decimal? value) => value is null || (value >= Minimum && value <= Maximum);
}
