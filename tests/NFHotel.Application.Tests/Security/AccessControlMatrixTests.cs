using System.Text.RegularExpressions;

using NFHotel.Application.UseCases.Staff;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Security;

/// <summary>
/// The one permission table (<see cref="RolePermissions"/>) equals the Access Control Matrix in
/// <c>docs/refactor/Access-Control-Matrix.md</c> (MIL-018 criterion 6): every permission has one row there, every role,
/// the Guest role included, is stated allowed or refused for it, and the code says the same, cell by cell.
/// </summary>
public sealed partial class AccessControlMatrixTests
{
    /// <summary>The role columns of the matrix, in their order.</summary>
    private static readonly string[] RoleColumns = [Roles.Guest, Roles.Receptionist, Roles.Manager, Roles.Housekeeping];

    #region The document

    /// <summary>Every permission of the code has exactly one row in the matrix, and the matrix names no other.</summary>
    [Fact]
    public void Matrix_EveryPermission_HasExactlyOneRow()
    {
        // Arrange
        IReadOnlyList<MatrixRow> rows = ReadMatrix();

        // Act
        string[] documented = rows.Select(row => row.Permission).ToArray();
        string[] duplicates = documented.GroupBy(permission => permission).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();

        // Assert
        Assert.Empty(duplicates);
        Assert.Equal(Permissions.All.Order(StringComparer.Ordinal), documented.Order(StringComparer.Ordinal));
    }

    /// <summary>The header row names the role columns in the order in which the cells of a row are read.</summary>
    [Fact]
    public void Matrix_HeaderRow_NamesTheRolesInTheOrderOfTheCells()
    {
        // Arrange
        string header = File.ReadLines(MatrixPath()).Single(line => HeaderRow().IsMatch(line));

        // Act
        string[] roles = header.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[2..];

        // Assert
        Assert.Equal(RoleColumns, roles);
    }

    /// <summary>Every cell of the matrix says allowed (<c>o</c>) or refused (<c>x</c>); nothing is left to clarify.</summary>
    [Fact]
    public void Matrix_EveryCell_IsAllowedOrRefused()
    {
        // Arrange
        IReadOnlyList<MatrixRow> rows = ReadMatrix();

        // Act
        string[] unclear = rows
            .SelectMany(row => row.Cells.Select((cell, column) => (row.Permission, Role: RoleColumns[column], Cell: cell)))
            .Where(entry => entry.Cell is not ("o" or "x"))
            .Select(entry => $"{entry.Permission}/{entry.Role}: '{entry.Cell}'")
            .ToArray();

        // Assert
        Assert.True(unclear.Length == 0, $"Cells that are neither o nor x: {string.Join(", ", unclear)}");
    }

    #endregion

    #region The table in code

    /// <summary>
    /// Each role and each permission: the code allows exactly what the matrix allows and refuses what it refuses. One
    /// case per cell, so the run states every cell, the Guest role included.
    /// </summary>
    /// <param name="role">The role.</param>
    /// <param name="permission">The permission.</param>
    [Theory]
    [MemberData(nameof(EveryCell))]
    public void HasPermission_RoleAndPermission_EqualsTheMatrix(string role, string permission)
    {
        // Arrange
        MatrixRow? row = ReadMatrix().SingleOrDefault(candidate => candidate.Permission == permission);
        Assert.NotNull(row);
        string documented = row.Cells[Array.IndexOf(RoleColumns, role)];

        // Act
        bool allowed = RolePermissions.HasPermission(role, permission);

        // Assert
        Assert.Equal(documented == "o", allowed);
    }

    /// <summary>The cases above cover every role with every permission: four roles times the whole permission list.</summary>
    [Fact]
    public void EveryCell_RolesTimesPermissions_CoversTheWholeTable()
    {
        // Act
        int cases = EveryCell().Count;

        // Assert
        Assert.Equal(RoleColumns.Length * Permissions.All.Count, cases);
    }

    #endregion

    #region Helpers

    /// <summary>Every role with every permission of the code.</summary>
    /// <returns>The pairs of role and permission.</returns>
    public static TheoryData<string, string> EveryCell()
    {
        TheoryData<string, string> cells = [];
        foreach (string permission in Permissions.All)
        {
            foreach (string role in RoleColumns)
            {
                cells.Add(role, permission);
            }
        }

        return cells;
    }

    /// <summary>Reads the rows of the matrix whose second cell names a permission in backticks.</summary>
    private static List<MatrixRow> ReadMatrix()
    {
        List<MatrixRow> rows = [];
        foreach (string line in File.ReadLines(MatrixPath()))
        {
            Match match = PermissionRow().Match(line);
            if (match.Success)
            {
                string[] cells = match.Groups["cells"].Value.Split('|').Select(cell => cell.Trim()).ToArray();
                Assert.True(cells.Length == RoleColumns.Length, $"The row of {match.Groups["permission"].Value} has {cells.Length} role cells.");
                rows.Add(new MatrixRow(match.Groups["permission"].Value, cells));
            }
        }

        Assert.NotEmpty(rows);
        return rows;
    }

    /// <summary>The path of the matrix in the repository.</summary>
    private static string MatrixPath() => Path.Join(RepositoryRoot.Find(), "docs", "refactor", "Access-Control-Matrix.md");

    /// <summary>The header row of the table: the function, the word Permission, then the roles.</summary>
    [GeneratedRegex(@"^\|[^|]*\|\s*Permission\s*\|.*\|\s*$")]
    private static partial Regex HeaderRow();

    /// <summary>A row of the table: the function, the permission in backticks, then one cell per role.</summary>
    [GeneratedRegex(@"^\|[^|]*\|\s*`(?<permission>[A-Za-z.]+)`\s*\|(?<cells>.*)\|\s*$")]
    private static partial Regex PermissionRow();

    /// <summary>One row of the matrix.</summary>
    /// <param name="Permission">The permission, as the code names it.</param>
    /// <param name="Cells">The cells of the four roles, in the order of <see cref="RoleColumns"/>.</param>
    private sealed record MatrixRow(string Permission, string[] Cells);

    #endregion
}
