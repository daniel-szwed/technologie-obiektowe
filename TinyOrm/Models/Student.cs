using System.ComponentModel.DataAnnotations.Schema;
using TinyOrm.Attributes;

namespace TinyOrm.Models
{
    [Table("students")]
	public class Student : EntityBase
	{ 
		[Column("firstName")]
		public string? FirstName { get; set; }

		[Column("lastName")]
		public string? LastName { get; set; }

		[OneToOne("student_id")]
		public Address? address { get; set; }

		[OneToMany("student_id")]
		public List<Hobby>? hobbies { get; set; }

		[ManyToMany("class_id")]
		[JoinTable("studentClass")]
        public IEnumerable<Class>? Classes { get; set; }
	}
}

