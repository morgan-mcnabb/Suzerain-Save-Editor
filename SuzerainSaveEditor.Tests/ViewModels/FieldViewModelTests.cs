using SuzerainSaveEditor.App.ViewModels;
using SuzerainSaveEditor.Core.Schema;

namespace SuzerainSaveEditor.Tests.ViewModels;

public sealed class FieldViewModelTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var options = new List<string> { "A", "B", "C" };
        var vm = new FieldViewModel("test.id", "Test Label", "Test description",
            FieldType.Enum, "B", min: 0, max: 10, options: options);

        Assert.Equal("test.id", vm.FieldId);
        Assert.Equal("Test Label", vm.Label);
        Assert.Equal("Test description", vm.Description);
        Assert.Equal(FieldType.Enum, vm.FieldType);
        Assert.Equal("B", vm.Value);
        Assert.Equal("B", vm.OriginalValue);
        Assert.Equal(0, vm.Min);
        Assert.Equal(10, vm.Max);
        Assert.Equal(options, vm.Options);
        Assert.False(vm.IsDirty);
        Assert.Null(vm.ValidationError);
    }

    [Fact]
    public void Constructor_NullInitialValue_SetsEmptyString()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, null);

        Assert.Equal("", vm.Value);
        Assert.Null(vm.OriginalValue);
    }

    // type flag tests
    [Theory]
    [InlineData(FieldType.Bool, true, false, false, false)]
    [InlineData(FieldType.Int, false, true, false, true)]
    [InlineData(FieldType.String, false, false, true, true)]
    [InlineData(FieldType.Enum, false, false, false, false)]
    public void TypeFlags_ReturnCorrectValues(FieldType type, bool isBool, bool isInt, bool isString, bool isText)
    {
        var vm = new FieldViewModel("id", "Label", null, type, "");

        Assert.Equal(isBool, vm.IsBool);
        Assert.Equal(isInt, vm.IsInt);
        Assert.Equal(isString, vm.IsString);
        Assert.Equal(isText, vm.IsText);
        Assert.Equal(type == FieldType.Enum, vm.IsEnum);
    }

    [Fact]
    public void HasDescription_TrueWhenDescriptionProvided()
    {
        var vm = new FieldViewModel("id", "Label", "Some description", FieldType.String, "");
        Assert.True(vm.HasDescription);
    }

    [Fact]
    public void HasDescription_FalseWhenNull()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "");
        Assert.False(vm.HasDescription);
    }

    [Fact]
    public void HasDescription_FalseWhenEmpty()
    {
        var vm = new FieldViewModel("id", "Label", "", FieldType.String, "");
        Assert.False(vm.HasDescription);
    }

    // bool value binding tests
    [Theory]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void BoolValue_Getter_ConvertsFromString(string value, bool expected)
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.Bool, value);
        Assert.Equal(expected, vm.BoolValue);
    }

    [Fact]
    public void BoolValue_Setter_UpdatesValue()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.Bool, "False");

        vm.BoolValue = true;
        Assert.Equal("True", vm.Value);

        vm.BoolValue = false;
        Assert.Equal("False", vm.Value);
    }

    [Fact]
    public void BoolValue_Setter_SameValue_DoesNotRetrigger()
    {
        var callCount = 0;
        var vm = new FieldViewModel("id", "Label", null, FieldType.Bool, "True",
            onValueChanged: (_, _) => callCount++);

        // setting same bool value should not trigger the callback
        vm.BoolValue = true;
        Assert.Equal(0, callCount);
    }

    // callback tests
    [Fact]
    public void ValueChange_TriggersCallback()
    {
        string? capturedFieldId = null;
        string? capturedValue = null;
        var vm = new FieldViewModel("test.id", "Label", null, FieldType.String, "original",
            onValueChanged: (id, val) =>
            {
                capturedFieldId = id;
                capturedValue = val;
            });

        vm.Value = "new value";

        Assert.Equal("test.id", capturedFieldId);
        Assert.Equal("new value", capturedValue);
    }

    [Fact]
    public void ValueChange_NoCallback_DoesNotThrow()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "original");
        vm.Value = "new value"; // should not throw
        Assert.Equal("new value", vm.Value);
    }

    [Fact]
    public void ValueChange_SameValue_DoesNotTriggerCallback()
    {
        var callCount = 0;
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "same",
            onValueChanged: (_, _) => callCount++);

        vm.Value = "same";
        Assert.Equal(0, callCount);
    }

    // suppress changes tests
    [Fact]
    public void ResetToOriginal_DoesNotTriggerCallback()
    {
        var callCount = 0;
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "original",
            onValueChanged: (_, _) => callCount++);

        vm.Value = "changed";
        Assert.Equal(1, callCount);

        vm.ResetToOriginal();
        Assert.Equal(1, callCount); // no additional callback
        Assert.Equal("original", vm.Value);
        Assert.False(vm.IsDirty);
        Assert.Null(vm.ValidationError);
    }

    [Fact]
    public void UpdateFromSession_DoesNotTriggerCallback()
    {
        var callCount = 0;
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "original",
            onValueChanged: (_, _) => callCount++);

        vm.UpdateFromSession("new value", isDirty: true, validationError: "some error");
        Assert.Equal(0, callCount);
        Assert.Equal("new value", vm.Value);
        Assert.True(vm.IsDirty);
        Assert.Equal("some error", vm.ValidationError);
    }

    [Fact]
    public void UpdateFromSession_NullValue_SetsEmptyString()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "original");

        vm.UpdateFromSession(null, isDirty: false, validationError: null);
        Assert.Equal("", vm.Value);
    }

    // has validation error
    [Fact]
    public void HasValidationError_TrueWhenErrorSet()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "");
        vm.ValidationError = "some error";
        Assert.True(vm.HasValidationError);
    }

    [Fact]
    public void HasValidationError_FalseWhenNull()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "");
        vm.ValidationError = null;
        Assert.False(vm.HasValidationError);
    }

    [Fact]
    public void HasValidationError_FalseWhenEmpty()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "");
        vm.ValidationError = "";
        Assert.False(vm.HasValidationError);
    }

    // property changed notifications
    [Fact]
    public void Value_RaisesPropertyChanged()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "original");
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FieldViewModel.Value))
                raised = true;
        };

        vm.Value = "new";
        Assert.True(raised);
    }

    [Fact]
    public void BoolField_ValueChange_RaisesBoolValuePropertyChanged()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.Bool, "False");
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FieldViewModel.BoolValue))
                raised = true;
        };

        vm.Value = "True";
        Assert.True(raised);
    }

    [Fact]
    public void ResetToOriginal_Bool_RaisesBoolValuePropertyChanged()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.Bool, "True");
        vm.Value = "False";

        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FieldViewModel.BoolValue))
                raised = true;
        };

        vm.ResetToOriginal();
        Assert.True(raised);
    }

    [Fact]
    public void IsDirty_RaisesPropertyChanged()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "");
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FieldViewModel.IsDirty))
                raised = true;
        };

        vm.IsDirty = true;
        Assert.True(raised);
    }

    [Fact]
    public void ValidationError_RaisesHasValidationErrorPropertyChanged()
    {
        var vm = new FieldViewModel("id", "Label", null, FieldType.String, "");
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FieldViewModel.HasValidationError))
                raised = true;
        };

        vm.ValidationError = "error";
        Assert.True(raised);
    }
}
