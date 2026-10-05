using NUnit.Framework;
using Submodules.Utility.Persistence;
using System;

namespace Submodules.Utility.Tests.EditMode
{
    [TestFixture]
    public sealed class InMemorySaveStoreTests
    {
        [Test]
        public void TryRead_ReturnsFalse_ForAKeyNeverWritten()
        {
            Assert.That(new InMemorySaveStore().TryRead("a", out _), Is.False);
        }

        [Test]
        public void Write_MakesTheContentTheCurrentValue_WithNoBackupYet()
        {
            var store = new InMemorySaveStore();

            store.Write("a", "one");

            Assert.That(store.TryRead("a", out var content), Is.True);
            Assert.That(content, Is.EqualTo("one"));
            Assert.That(store.TryReadBackup("a", out _), Is.False);
        }

        [Test]
        public void Write_KeepsTheReplacedValueAsTheOneBackup()
        {
            var store = new InMemorySaveStore();
            store.Write("a", "one");
            store.Write("a", "two");
            store.Write("a", "three");

            store.TryRead("a", out var current);
            store.TryReadBackup("a", out var backup);

            Assert.That(current, Is.EqualTo("three"));
            Assert.That(backup, Is.EqualTo("two"));
        }

        [Test]
        public void Delete_RemovesTheValueAndItsBackup()
        {
            var store = new InMemorySaveStore();
            store.Write("a", "one");
            store.Write("a", "two");

            Assert.That(store.Delete("a"), Is.True);

            Assert.That(store.TryRead("a", out _), Is.False);
            Assert.That(store.TryReadBackup("a", out _), Is.False);
            Assert.That(store.Delete("a"), Is.False);
        }

        [Test]
        public void Keys_ListsEveryKeyThatHasAValue()
        {
            var store = new InMemorySaveStore();
            store.Write("a", "1");
            store.Write("b", "2");
            store.Write("c", "3");
            store.Delete("b");

            Assert.That(store.Keys(), Is.EquivalentTo(new[] { "a", "c" }));
        }

        [Test]
        public void SetAside_TakesTheValueAndItsBackupOutOfTheWay_AndKeepsTheirText()
        {
            var store = new InMemorySaveStore();
            store.Write("a", "one");
            store.Write("a", "two");

            Assert.That(store.SetAside("a"), Is.True);

            Assert.That(store.TryRead("a", out _), Is.False);
            Assert.That(store.TryReadBackup("a", out _), Is.False);
            Assert.That(store.Keys(), Is.Empty);
            Assert.That(store.SetAsideValues, Is.EquivalentTo(new[] { "two", "one" }));
            Assert.That(store.SetAside("a"), Is.False, "nothing left to set aside");
        }

        [Test]
        public void SetAside_LeavesOtherKeysAlone()
        {
            var store = new InMemorySaveStore();
            store.Write("a", "1");
            store.Write("b", "2");

            _ = store.SetAside("a");

            Assert.That(store.Keys(), Is.EqualTo(new[] { "b" }));
        }

        [Test]
        public void AppendSideFile_AppendsInOrder_AndIsNotASave()
        {
            var store = new InMemorySaveStore();

            store.AppendSideFile("a.quarantine.json", "one\n");
            store.AppendSideFile("a.quarantine.json", "two\n");

            Assert.That(store.SideFiles["a.quarantine.json"], Is.EqualTo("one\ntwo\n"));
            Assert.That(store.Keys(), Is.Empty);
        }

        [Test]
        public void AppendSideFile_RejectsAnEmptyNameANameThatIsASaveAndNullText()
        {
            var store = new InMemorySaveStore();

            Assert.Throws<ArgumentException>(() => store.AppendSideFile("", "x"));
            Assert.Throws<ArgumentException>(() => store.AppendSideFile("a.sav", "x"));
            Assert.Throws<ArgumentNullException>(() => store.AppendSideFile("a.txt", null));
        }

        [Test]
        public void Write_RejectsAnEmptyKeyAndNullContent()
        {
            var store = new InMemorySaveStore();

            Assert.Throws<ArgumentException>(() => store.Write("", "x"));
            Assert.Throws<ArgumentNullException>(() => store.Write("a", null));
        }
    }
}
