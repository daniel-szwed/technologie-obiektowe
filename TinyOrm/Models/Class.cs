using TinyOrm.Abstraction.Attributes;
using TinyOrm.Abstraction.Data;

namespace TinyOrm.Models;

[Table("classes")]
public class Class : EntityBase
{
    [Column("name")] public string? Name { get; set; }

    [ManyToMany("student_id")]
    [JoinTable("studentClass")]
    public IEnumerable<Student>? Students { get; set; }
}