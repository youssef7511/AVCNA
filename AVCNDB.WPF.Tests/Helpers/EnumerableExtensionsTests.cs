using AVCNDB.WPF.Helpers;
using FluentAssertions;

namespace AVCNDB.WPF.Tests.Helpers;

/// <summary>
/// Guards the duplicate-tolerant lookup used by the CNAM/Excel import so that data
/// containing a repeated key (e.g. two medics with the same PCT code) doesn't crash
/// the whole import with "An item with the same key has already been added".
/// </summary>
public class EnumerableExtensionsTests
{
    [Fact]
    public void ToFirstWinsDictionary_DuplicateKeys_KeepsFirst_DoesNotThrow()
    {
        var items = new[]
        {
            new { pct = "302257", id = 1 },
            new { pct = "302257", id = 2 }, // duplicate PCT — must not throw
            new { pct = "999",    id = 3 },
        };

        var dict = items.ToFirstWinsDictionary(x => x.pct);

        dict.Should().HaveCount(2);
        dict["302257"].id.Should().Be(1, "the first occurrence wins");
        dict["999"].id.Should().Be(3);
    }

    [Fact]
    public void ToFirstWinsDictionary_NoDuplicates_BehavesLikeToDictionary()
    {
        var items = new[] { new { k = "a", v = 1 }, new { k = "b", v = 2 } };

        var dict = items.ToFirstWinsDictionary(x => x.k);

        dict.Should().HaveCount(2);
        dict["a"].v.Should().Be(1);
        dict["b"].v.Should().Be(2);
    }
}
