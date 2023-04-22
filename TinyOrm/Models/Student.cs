using TinyOrm.Abstraction.Attributes;
using TinyOrm.Abstraction.Data;

namespace TinyOrm.Models;

[GenerateLazyDerivedClass]
[Table("students")]
public class Student : EntityBase
{
    [Column("firstName")] 
    public string? FirstName { get; set; }

    [Column("lastName")] 
    public string? LastName { get; set; }

    [OneToOne("student_id")] 
    public virtual Address? Address { get; set; }

    [OneToMany("student_id")] 
    public virtual IEnumerable<Hobby>? Hobbies { get; set; }

    [ManyToMany("class_id")]
    [JoinTable("studentClass")]
    public virtual IEnumerable<Class>? Classes { get; set; }
}