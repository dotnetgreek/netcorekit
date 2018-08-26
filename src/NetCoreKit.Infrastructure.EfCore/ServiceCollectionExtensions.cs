using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreKit.Domain;
using NetCoreKit.Infrastructure.EfCore.Db;
using NetCoreKit.Infrastructure.EfCore.Repository;

namespace NetCoreKit.Infrastructure.EfCore
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddEfCore(this IServiceCollection services)
    {
      var svcProvider = services.BuildServiceProvider();

      var entityTypes = svcProvider
        .GetRequiredService<IConfiguration>()
        .LoadFullAssemblies()
        .SelectMany(m => m.DefinedTypes)
        .Where(x => typeof(IEntity).IsAssignableFrom(x) && !x.GetTypeInfo().IsAbstract);

      foreach (var entity in entityTypes)
      {
        var repoType = typeof(IEfRepositoryAsync<>).MakeGenericType(entity);
        var implRepoType = typeof(EfRepositoryAsync<>).MakeGenericType(entity);
        services.AddScoped(repoType, implRepoType);

        var queryRepoType = typeof(IEfQueryRepository<>).MakeGenericType(entity);
        var implQueryRepoType = typeof(EfQueryRepository<>).MakeGenericType(entity);
        services.AddScoped(queryRepoType, implQueryRepoType);
      }

      services.AddScoped<IUnitOfWorkAsync, EfUnitOfWork>();
      services.AddScoped<IQueryRepositoryFactory, EfQueryRepositoryFactory>();

      return services;
    }

    public static IServiceCollection AddSqlLiteDb(this IServiceCollection services)
    {
      services.AddScoped<IDatabaseConnectionStringFactory, NoOpDatabaseConnectionStringFactory>();
      services.AddScoped<IExtendDbContextOptionsBuilder, InMemoryDbContextOptionsBuilderFactory>();
      return services;
    }
  }

  public class NoOpDatabaseConnectionStringFactory : IDatabaseConnectionStringFactory
  {
    public string Create()
    {
      return string.Empty;
    }
  }

  public class InMemoryDbContextOptionsBuilderFactory : IExtendDbContextOptionsBuilder
  {
    public DbContextOptionsBuilder Extend(
      DbContextOptionsBuilder optionsBuilder,
      IDatabaseConnectionStringFactory connectionStringFactory,
      string assemblyName)
    {
      return optionsBuilder.UseSqlite(
        "Data Source=App_Data\\localdb.db",
        sqlOptions => { sqlOptions.MigrationsAssembly(assemblyName); });
    }
  }
}
