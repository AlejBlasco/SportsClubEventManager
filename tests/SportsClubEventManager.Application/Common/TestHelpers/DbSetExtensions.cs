using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace SportsClubEventManager.Application.Common.TestHelpers;

/// <summary>
/// Extension methods for creating mock DbSet instances in tests.
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    /// Builds a mock DbSet from an IQueryable collection for testing.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="source">The queryable source data.</param>
    /// <returns>A mocked DbSet that can be used with NSubstitute.</returns>
    public static DbSet<TEntity> BuildMockDbSet<TEntity>(this IQueryable<TEntity> source)
        where TEntity : class
    {
        var mockDbSet = Substitute.For<DbSet<TEntity>, IAsyncEnumerable<TEntity>, IQueryable<TEntity>>();

        ((IQueryable<TEntity>)mockDbSet).Provider.Returns(source.Provider);
        ((IQueryable<TEntity>)mockDbSet).Expression.Returns(source.Expression);
        ((IQueryable<TEntity>)mockDbSet).ElementType.Returns(source.ElementType);
        ((IQueryable<TEntity>)mockDbSet).GetEnumerator().Returns(source.GetEnumerator());

        return mockDbSet;
    }
}
