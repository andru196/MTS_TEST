using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTS_TEST
{
	class ApplicationContext : DbContext
	{
		public ApplicationContext() : base("MainConnection")
		{
		}
		public DbSet<Weather> Weathers { get; set; }
	}
}
