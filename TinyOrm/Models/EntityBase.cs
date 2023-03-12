using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TinyOrm.Models
{
	public class EntityBase
	{
		[Column("id")]
		public long? Id { get; set; }
	}
}

