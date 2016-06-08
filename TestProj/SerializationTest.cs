using System.IO;
using Xunit;

namespace TestProj
{
    public class SerializationTest
    {
        [Fact]
        public void PocoTest()
        {
            var p = new Poco { TestInt = -13, TestString = "test" };
            var writer = new StringWriter();
            p.Serialize(writer);
            Assert.Equal(@"{
    ""TestInt"": -13,
    ""TestString"": ""test"",
}", writer.ToString());
        }
    }
}
