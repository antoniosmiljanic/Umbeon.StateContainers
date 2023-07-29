namespace Umbeon.StateContainers.Tests;

public partial class TestStateContainer : StateContainerBase
{
    [StateContainerField]
    private string stringValue;

    [StateContainerField]
    private object objectValue;

    [StateContainerField]
    private int _intValue;
}
