using System.ComponentModel.DataAnnotations.Schema;

namespace TinyOrm.Models
{
    [Table("hobbies")]
    public class Hobby : EntityBase
    { 
        [Column("name")]
        public string? Name { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        //[ForeignKey("student_id")]
        //public Student student { get; set; }
    }
}

