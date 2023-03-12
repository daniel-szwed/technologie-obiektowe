using System.ComponentModel.DataAnnotations.Schema;

namespace TinyOrm.Models
{
	[Table("studentClass")]
    public class StudentClass : EntityBase
	{

		[Column("student_id")]
		public long? StudentId { get; set; }

		[Column("class_id")]
		public long? ClassId { get; set; }
	}
}

