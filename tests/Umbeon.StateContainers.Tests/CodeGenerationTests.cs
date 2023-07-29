namespace Umbeon.StateContainers.Tests
{
    [TestClass]
    public class CodeGenerationTests
    {
        [TestMethod]
        public void Properties_AreGenerated()
        {
            var container = new TestStateContainer();
            
            var x = container.IntValue;

            var y = container.ObjectValue;

            var z = container.StringValue;
        }
    }
}