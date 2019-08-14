using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TravelLog.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new TravelLogContext(
                serviceProvider.GetRequiredService<DbContextOptions<TravelLogContext>>()))
            {
                // Look for any movies.
                if (context.TravelItems.Count() > 0)
                {
                    return;   // DB has been seeded
                }

                context.TravelItems.AddRange(
                    new TravelItems
                    {
                        Title = "Is Mayo an Instrument?",
                        Tags = "spongebob",
                        Uploaded = "07-10-18 4:20T18:25:43.511Z",
                        Width = "768",
                        Height = "432"
                    }


                );
                context.SaveChanges();
            }
        }
    }
}