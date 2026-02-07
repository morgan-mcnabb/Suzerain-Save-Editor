using CommunityToolkit.Mvvm.ComponentModel;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.App.ViewModels;

// represents a single editable field in the UI with type-specific bindings
public partial class FieldViewModel : ViewModelBase
{
    private readonly Action<string, string>? _onValueChanged;
    private bool _suppressChanges;

    public string FieldId { get; }
    public string Label { get; }
    public string? Description { get; }
    public FieldType FieldType { get; }
    public int? Min { get; }
    public int? Max { get; }
    public IReadOnlyList<string>? Options { get; }
    public string? OriginalValue { get; private set; }

    // computed type flags for visibility bindings
    public bool IsBool => FieldType == FieldType.Bool;
    public bool IsInt => FieldType == FieldType.Int;
    public bool IsString => FieldType == FieldType.String;
    public bool IsEnum => FieldType == FieldType.Enum;
    public bool IsDecimal => FieldType == FieldType.Decimal;
    public bool IsText => IsInt || IsString || IsDecimal;
    public bool HasDescription => !string.IsNullOrEmpty(Description);

    [ObservableProperty]
    private string _value = "";

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _isDirty;

    // bool binding for ToggleSwitch — converts to/from string Value
    public bool BoolValue
    {
        get => bool.TryParse(Value, out var v) && v;
        set
        {
            var str = value.ToString();
            if (Value != str)
                Value = str;
        }
    }

    public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);

    public FieldViewModel(
        string fieldId,
        string label,
        string? description,
        FieldType fieldType,
        string? initialValue,
        int? min = null,
        int? max = null,
        IReadOnlyList<string>? options = null,
        Action<string, string>? onValueChanged = null)
    {
        FieldId = fieldId;
        Label = label;
        Description = description;
        FieldType = fieldType;
        Min = min;
        Max = max;
        Options = options;
        _onValueChanged = onValueChanged;

        // set backing field directly to skip change notification during init
        _value = initialValue ?? "";
        OriginalValue = initialValue;
    }

    partial void OnValueChanged(string value)
    {
        if (_suppressChanges) return;
        _onValueChanged?.Invoke(FieldId, value);

        if (IsBool)
            OnPropertyChanged(nameof(BoolValue));
    }

    partial void OnValidationErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasValidationError));
    }

    // update field state from session without triggering the change callback
    public void UpdateFromSession(string? currentValue, bool isDirty, string? validationError)
    {
        _suppressChanges = true;
        Value = currentValue ?? "";
        IsDirty = isDirty;
        ValidationError = validationError;
        _suppressChanges = false;

        if (IsBool)
            OnPropertyChanged(nameof(BoolValue));
    }

    // reset to original value without triggering the change callback
    public void ResetToOriginal()
    {
        _suppressChanges = true;
        Value = OriginalValue ?? "";
        IsDirty = false;
        ValidationError = null;
        _suppressChanges = false;

        if (IsBool)
            OnPropertyChanged(nameof(BoolValue));
    }
}
