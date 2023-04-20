using TinyOrm.Abstraction.Attributes;

namespace TinyOrm.Abstraction.Data
{
    public class EntityBase
    {
        public EntityBase()
        {
            
        }
        public EntityBase(long? id)
        {
            Id = id;
        }
        [Column("id")] public long? Id { get; set; }
    }
}