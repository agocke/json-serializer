using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace UnitTests
{
    public class Class1
    {
        [Fact]
        public void Test1()
        {
            var source = @"using JsonSerializerUtil;

namespace TestProj
{
    public class Poco : IJsonSerializable<Poco>
    {
        public int TestInt { get; set; }

        public string TestString { get; set; }
    }
}";
        }
    }
}
