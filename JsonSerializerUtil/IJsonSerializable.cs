using System.IO;

namespace JsonSerializerUtil
{
    public interface IJsonSerializable<T>
    {
        void Serialize(TextWriter writer);
    }
}
