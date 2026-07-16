using EphemeralFiles;

namespace Tests;

public class EphemeralFileTests
{
    private readonly List<string> cleanup = [];

    [SetUp]
    public void Setup()
    {
        cleanup.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var s in cleanup)
        {
            try
            {
                File.Delete(s);
            }
            catch
            {
                //NOOP
            }
        }
        cleanup.Clear();
    }

    [Test]
    public void TestInitialEphemeral()
    {
        using var ephemeral = new EphemeralFileStream(true);
        cleanup.Add(ephemeral.Name);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.Name, Is.Not.Empty);
            //File is initially ephemeral and should thus not exist
            Assert.That(ephemeral.IsEphemeral, Is.True);
            Assert.That(File.Exists(ephemeral.Name), Is.False);
        }
    }

    [Test]
    public void TestInitialNotEphemeral()
    {
        using var ephemeral = new EphemeralFileStream(false);
        cleanup.Add(ephemeral.Name);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.Name, Is.Not.Empty);
            //File is not initially ephemeral and should thus exist
            Assert.That(ephemeral.IsEphemeral, Is.False);
            Assert.That(File.Exists(ephemeral.Name), Is.True);
        }
    }

    [Test]
    public void TestUndelete()
    {
        using var ephemeral = new EphemeralFileStream(true);
        cleanup.Add(ephemeral.Name);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.IsEphemeral, Is.True);
            Assert.That(File.Exists(ephemeral.Name), Is.False);
        }
        ephemeral.Write([1, 2, 3, 4]);
        ephemeral.Flush();
        ephemeral.Restore();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.IsEphemeral, Is.False);
            Assert.That(File.Exists(ephemeral.Name), Is.True);

            //Ensure the 4 written bytes are still there.
            Assert.That(ephemeral.Length, Is.EqualTo(4));
        }
    }

    [Test]
    public void TestUndeleteWithPositionRestore()
    {
        byte[] data = [1, 2, 3, 4];
        using var ephemeral = new EphemeralFileStream(true);
        cleanup.Add(ephemeral.Name);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.IsEphemeral, Is.True);
            Assert.That(File.Exists(ephemeral.Name), Is.False);
        }
        ephemeral.Write(data);
        ephemeral.Flush();
        ephemeral.Position = 2;
        ephemeral.Restore();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.IsEphemeral, Is.False);
            Assert.That(File.Exists(ephemeral.Name), Is.True);

            //Ensure the 4 written bytes are still there.
            Assert.That(ephemeral.Length, Is.EqualTo(data.Length));

            //Ensure the position is correctly restored
            Assert.That(ephemeral.Position, Is.EqualTo(2));
            byte[] buffer = new byte[ephemeral.Length];
            Assert.DoesNotThrow(() => { ephemeral.ReadExactly(buffer, 0, 2); });
            Assert.That(buffer, Is.EquivalentTo(data.Skip(2).Concat(new byte[] { 0, 0 })));
        }
    }

    [Test]
    public void TestMakeEphemeral()
    {
        using var ephemeral = new EphemeralFileStream(false);
        cleanup.Add(ephemeral.Name);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.IsEphemeral, Is.False);
            Assert.That(File.Exists(ephemeral.Name), Is.True);
        }
        ephemeral.Write([1, 2, 3, 4]);
        ephemeral.Flush();
        ephemeral.MakeEphemeral();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.IsEphemeral, Is.True);
            Assert.That(File.Exists(ephemeral.Name), Is.False);

            //Ensure the 4 written bytes are still there.
            Assert.That(ephemeral.Length, Is.EqualTo(4));
        }
    }

    [Test]
    public void TestMakeEphemeralWithPositionRestore()
    {
        byte[] data = [1, 2, 3, 4];
        using var ephemeral = new EphemeralFileStream(false);
        cleanup.Add(ephemeral.Name);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.IsEphemeral, Is.False);
            Assert.That(File.Exists(ephemeral.Name), Is.True);
        }
        ephemeral.Write(data);
        ephemeral.Flush();
        ephemeral.Position = 2;
        ephemeral.MakeEphemeral();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ephemeral.IsEphemeral, Is.True);
            Assert.That(File.Exists(ephemeral.Name), Is.False);

            //Ensure the 4 written bytes are still there.
            Assert.That(ephemeral.Length, Is.EqualTo(data.Length));

            //Ensure the position is correctly restored
            Assert.That(ephemeral.Position, Is.EqualTo(2));
            byte[] buffer = new byte[ephemeral.Length];
            Assert.DoesNotThrow(() => { ephemeral.ReadExactly(buffer, 0, 2); });
            Assert.That(buffer, Is.EquivalentTo(data.Skip(2).Concat(new byte[] { 0, 0 })));
        }
    }
}
