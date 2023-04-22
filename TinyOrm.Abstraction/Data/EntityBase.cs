using TinyOrm.Abstraction.Attributes;

namespace TinyOrm.Abstraction.Data
{
    public class EntityBase
    {
        [Column("id")] public long? Id { get; set; }
    }
}