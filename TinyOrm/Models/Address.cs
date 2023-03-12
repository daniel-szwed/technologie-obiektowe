using System.ComponentModel.DataAnnotations.Schema;

namespace TinyOrm.Models
{
    [Table("addresses")]
    public class Address : EntityBase
    {
        [Column("street")]
        public string? Street { get; set; }

        [Column("number")]
        public string? Number { get; set; }

        [Column("zipCode")]
        public string? ZipCode { get; set; }
    }
}