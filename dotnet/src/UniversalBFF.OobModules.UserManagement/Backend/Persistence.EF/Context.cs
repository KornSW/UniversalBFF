using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace UniversalBFF.OobModules.UserManagement {

  partial class UserManagementDbContext {

    partial void OnModelCreatingCustom(ModelBuilder modelBuilder) {

      //var cfgSecurityEnvironment = modelBuilder.Entity<SecurityEnvironmentEntity>();
      //HACK: muss vom generator kommen!!!!!!!
      //cfgSecurityEnvironment.Property((e) => e.EnvironmentNumber).ValueGeneratedNever();

      //adjust things to become completely equivalent to the legacy model:
      //modelBuilder.Entity<WebUserEntity>().ToTable("WebUsers");
      //modelBuilder.Entity<WindowsUserEntity>().ToTable("WindowsUsers");

    }

    static UserManagementDbContext() {
      /*
      EntityAccessControl.RegisterPropertyAsAccessControlClassification(
        (SecurityEnvironmentEntity e) => e.EnvironmentNumber, "SecEnv"
      );

      EntityAccessControl.RegisterPropertyAsAccessControlClassification(
        (UserLogonEntity e) => e.LogonIdentifier, "User"
      );
      */
    }

  }

  public static class AccessControlContextExtensions {  
    /*
    public static bool ValidateEntityScope<TEntity>(this AccessControlContext context, TEntity entity) {
      var filterExpression = EntityAccessControl.BuildExpressionForLocalEntity<TEntity>(context);
      if (!filterExpression.Compile().Invoke(entity)) {
        return false;
      }
      return true;
    }
    */
  }

}
