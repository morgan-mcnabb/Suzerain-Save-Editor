using SuzerainSaveEditor.App.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class DragDropHelperTests
{
    [Fact]
    public void HasJsonFile_JsonFile_ReturnsTrue()
    {
        var files = new DroppedFile[] { new("save.json", null) };
        Assert.True(DragDropHelper.HasJsonFile(files));
    }

    [Fact]
    public void HasJsonFile_UppercaseExtension_ReturnsTrue()
    {
        var files = new DroppedFile[] { new("save.JSON", null) };
        Assert.True(DragDropHelper.HasJsonFile(files));
    }

    [Fact]
    public void HasJsonFile_MixedCaseExtension_ReturnsTrue()
    {
        var files = new DroppedFile[] { new("save.Json", null) };
        Assert.True(DragDropHelper.HasJsonFile(files));
    }

    [Fact]
    public void HasJsonFile_NonJsonFile_ReturnsFalse()
    {
        var files = new DroppedFile[] { new("readme.txt", null) };
        Assert.False(DragDropHelper.HasJsonFile(files));
    }

    [Fact]
    public void HasJsonFile_NullList_ReturnsFalse()
    {
        Assert.False(DragDropHelper.HasJsonFile(null));
    }

    [Fact]
    public void HasJsonFile_EmptyList_ReturnsFalse()
    {
        Assert.False(DragDropHelper.HasJsonFile(Array.Empty<DroppedFile>()));
    }

    [Fact]
    public void HasJsonFile_MixedFiles_OnlyOneJson_ReturnsTrue()
    {
        var files = new DroppedFile[]
        {
            new("image.png", null),
            new("save.json", null),
            new("notes.txt", null)
        };
        Assert.True(DragDropHelper.HasJsonFile(files));
    }

    [Fact]
    public void HasJsonFile_MultipleNonJson_ReturnsFalse()
    {
        var files = new DroppedFile[]
        {
            new("image.png", null),
            new("notes.txt", null)
        };
        Assert.False(DragDropHelper.HasJsonFile(files));
    }

    [Fact]
    public void GetFirstJsonFilePath_JsonFile_ReturnsPath()
    {
        var files = new DroppedFile[] { new("save.json", "C:\\saves\\save.json") };
        Assert.Equal("C:\\saves\\save.json", DragDropHelper.GetFirstJsonFilePath(files));
    }

    [Fact]
    public void GetFirstJsonFilePath_NullList_ReturnsNull()
    {
        Assert.Null(DragDropHelper.GetFirstJsonFilePath(null));
    }

    [Fact]
    public void GetFirstJsonFilePath_EmptyList_ReturnsNull()
    {
        Assert.Null(DragDropHelper.GetFirstJsonFilePath(Array.Empty<DroppedFile>()));
    }

    [Fact]
    public void GetFirstJsonFilePath_NoJsonFiles_ReturnsNull()
    {
        var files = new DroppedFile[] { new("readme.txt", "C:\\readme.txt") };
        Assert.Null(DragDropHelper.GetFirstJsonFilePath(files));
    }

    [Fact]
    public void GetFirstJsonFilePath_MultipleJsonFiles_PicksFirst()
    {
        var files = new DroppedFile[]
        {
            new("image.png", "C:\\image.png"),
            new("first.json", "C:\\saves\\first.json"),
            new("second.json", "C:\\saves\\second.json")
        };
        Assert.Equal("C:\\saves\\first.json", DragDropHelper.GetFirstJsonFilePath(files));
    }

    [Fact]
    public void GetFirstJsonFilePath_CaseInsensitive_ReturnsPath()
    {
        var files = new DroppedFile[] { new("save.JSON", "C:\\saves\\save.JSON") };
        Assert.Equal("C:\\saves\\save.JSON", DragDropHelper.GetFirstJsonFilePath(files));
    }

    [Fact]
    public void GetFirstJsonFilePath_NullLocalPath_ReturnsNull()
    {
        var files = new DroppedFile[] { new("save.json", null) };
        Assert.Null(DragDropHelper.GetFirstJsonFilePath(files));
    }
}
