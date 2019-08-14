using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TravelLog.Models
{
    public class TravelLogContext : DbContext
    {
        public TravelLogContext (DbContextOptions<TravelLogContext> options)
            : base(options)
        {
        }

        public DbSet<TravelLog.Models.TravelItems> TravelItems { get; set; }
    }
}
