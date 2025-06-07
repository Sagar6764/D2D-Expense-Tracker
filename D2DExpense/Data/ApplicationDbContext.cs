using Microsoft.EntityFrameworkCore;
using D2DExpense.Models;

namespace D2DExpense.Data
{
    public class ApplicationDbContext : DbContext
    {
        

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        { 
        }

    }
}
