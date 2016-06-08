using JsonSerializerUtil;

namespace TestProj
{
    public partial class Poco : IJsonSerializable<Poco>
    {
        public int TestInt { get; set; }

        public string TestString { get; set; }
    }
}
