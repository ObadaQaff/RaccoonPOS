using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Base
{
	public class BaseEntity
	{
		public  int Id { get; set; }
        public virtual DateTime CreatedDate { get; set; } 
		public DateTime UpdatedDate { get; set; }
	}
}
