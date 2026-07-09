using BaGetter.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Core.Tests.Support;

public class TestDbContext : AbstractContext<TestDbContext>
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    { }

    public override bool IsUniqueConstraintViolationException(DbUpdateException exception) => false;

    public static TestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new TestDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }
}
