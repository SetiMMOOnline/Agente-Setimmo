using RagnaForge.Agent.Core.Parsing;

namespace RagnaForge.Agent.Core.Tests;

public sealed class EntityParsersTests
{
    [Fact]
    public void ItemDbParser_StripsInlineCommentBeforeParsingId()
    {
        var root = CreateTempDirectory();
        try
        {
            var file = Path.Combine(root, "item_db_usable.yml");
            File.WriteAllText(file, """
- Id: 102396 # note: item doesn't exist in itemInfo_true but exists in itemreformsystem
  AegisName: FA_Armor_Reform_1
  Name: FA Armor Reform 1
- Id: 102397 # note: item doesn't exist in itemInfo_true but exists in itemreformsystem
  AegisName: FA_Armor_Reform_2
  Name: FA Armor Reform 2
""");

            var items = ItemDbParser.Parse(file, root, "renewal");

            Assert.Collection(items,
                item => Assert.Equal(102396, item.Id),
                item => Assert.Equal(102397, item.Id));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void MobDbParser_StripsInlineCommentBeforeParsingId()
    {
        var root = CreateTempDirectory();
        try
        {
            var file = Path.Combine(root, "mob_db.yml");
            File.WriteAllText(file, """
- Id: 1002 # canonical monster
  AegisName: PORING
  Name: Poring
""");

            var monsters = MobDbParser.Parse(file, root, "renewal");

            Assert.Single(monsters);
            Assert.Equal(1002, monsters[0].Id);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
