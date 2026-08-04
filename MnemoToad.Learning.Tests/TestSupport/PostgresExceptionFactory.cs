using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MnemoToad.Learning.Tests.TestSupport;

internal static class PostgresExceptionFactory
{
    public static DbUpdateException UniqueViolation(string? tableName = null, string? constraintName = null) =>
        Wrap(new PostgresException("duplicate key value violates unique constraint", "ERROR", "ERROR",
            PostgresErrorCodes.UniqueViolation, tableName: tableName, constraintName: constraintName));

    public static DbUpdateException ForeignKeyViolation(string? tableName = null, string? constraintName = null) =>
        Wrap(new PostgresException("update or delete on table violates foreign key constraint", "ERROR", "ERROR",
            PostgresErrorCodes.ForeignKeyViolation, tableName: tableName, constraintName: constraintName));

    private static DbUpdateException Wrap(PostgresException exception) =>
        new("Simulated constraint violation.", exception);
}
