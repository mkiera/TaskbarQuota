using TaskbarQuota.Taskbar;

namespace TaskbarQuota.Tests;

[Collection(WidgetRowSettingsCollection.Name)]
public class TaskbarSpaceTests
{
    [Fact]
    public void AvailableWidthIsTrackedIndependentlyForEachDisplay()
    {
        TaskbarSpace.ResetAvailableWidth();
        try
        {
            TaskbarSpace.ReportAvailableWidth("DISPLAY1", 700, isPrimary: true);
            TaskbarSpace.ReportAvailableWidth("DISPLAY2", 400);

            Assert.True(TaskbarSpace.TryGetAvailableWidth("DISPLAY1", out int primary));
            Assert.True(TaskbarSpace.TryGetAvailableWidth("DISPLAY2", out int secondary));
            Assert.Equal(700, primary);
            Assert.Equal(400, secondary);
            Assert.Equal(700, TaskbarSpace.AvailableLogicalWidth);
        }
        finally
        {
            TaskbarSpace.ResetAvailableWidth();
        }
    }

    [Fact]
    public void ResetAvailableWidthClearsPerDisplayMeasurements()
    {
        TaskbarSpace.ReportAvailableWidth("DISPLAY1", 700, isPrimary: true);
        TaskbarSpace.ResetAvailableWidth();

        Assert.False(TaskbarSpace.TryGetAvailableWidth("DISPLAY1", out _));
        Assert.Empty(TaskbarSpace.KnownDisplayKeys);
        Assert.Equal(TaskbarSpace.UnknownWidth, TaskbarSpace.AvailableLogicalWidth);
    }
}
