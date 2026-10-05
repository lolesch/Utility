using NUnit.Framework;
using Submodules.Utility.Persistence;
using System;
using System.IO;

namespace Submodules.Utility.Tests.EditMode
{
    [TestFixture]
    public sealed class FileSaveStoreTests
    {
        private const string Key = "hero";

        private string directory;
        private FileSaveStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "Utility.Persistence.Tests", Guid.NewGuid().ToString("N"));
            store = new FileSaveStore(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        private SaveSlot<Note> Slot(int version = 1) => new(store, new JsonUtilitySerializer(), Key, version);

        private static Note NoteOf(string text) => new() { text = text };

        [Test]
        public void ASlotOverTheFileStore_LoadsWhatItSaved()
        {
            var slot = Slot();
            slot.Save(NoteOf("equipped"));

            var result = Slot().Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded));
            Assert.That(result.Payload.text, Is.EqualTo("equipped"));
        }

        [Test]
        public void ASlotOverTheFileStore_ReportsMissing_ForAFileNeverWritten()
        {
            Assert.That(Slot().Load().Status, Is.EqualTo(LoadStatus.Missing));
            Assert.That(Directory.Exists(directory), Is.False, "a read never creates the folder");
        }

        [Test]
        public void ASlotOverTheFileStore_RestoresTheBackup_WhenThePrimaryIsCorrupt()
        {
            var slot = Slot();
            slot.Save(NoteOf("first"));
            slot.Save(NoteOf("second"));
            File.WriteAllText(Path.Combine(directory, Key + ".sav"), "{ truncated");

            var result = slot.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.RestoredFromBackup));
            Assert.That(result.Payload.text, Is.EqualTo("first"));
        }

        [Test]
        public void Write_KeepsTheReplacedFileAsTheBackup()
        {
            store.Write(Key, "one");
            store.Write(Key, "two");
            store.Write(Key, "three");

            store.TryRead(Key, out var current);
            store.TryReadBackup(Key, out var backup);

            Assert.That(current, Is.EqualTo("three"));
            Assert.That(backup, Is.EqualTo("two"));
        }

        [Test]
        public void Write_ThatFailsPartWay_LeavesThePreviousSaveIntactAndReadable()
        {
            var slot = Slot();
            slot.Save(NoteOf("safe"));
            Directory.CreateDirectory(Path.Combine(directory, Key + ".sav.tmp"));

            Assert.Catch(() => slot.Save(NoteOf("never lands")));

            var result = Slot().Load();
            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded));
            Assert.That(result.Payload.text, Is.EqualTo("safe"));
        }

        [Test]
        public void ALeftoverTempFile_IsNeverReadAsASave_AndIsReplacedByTheNextWrite()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, Key + ".sav.tmp"), "half a write");

            Assert.That(store.TryRead(Key, out _), Is.False);
            Assert.That(store.Keys(), Is.Empty);

            store.Write(Key, "whole");

            store.TryRead(Key, out var content);
            Assert.That(content, Is.EqualTo("whole"));
        }

        [Test]
        public void Delete_RemovesTheSaveItsBackupAndAnyTempFile()
        {
            store.Write(Key, "one");
            store.Write(Key, "two");

            Assert.That(store.Delete(Key), Is.True);

            Assert.That(Directory.GetFiles(directory), Is.Empty);
            Assert.That(store.Delete(Key), Is.False);
        }

        [Test]
        public void Keys_ListsTheSavesOnly_NotBackupsOrTempFiles()
        {
            store.Write("a", "1");
            store.Write("a", "2");
            store.Write("b", "3");
            File.WriteAllText(Path.Combine(directory, "c.sav.tmp"), "x");
            File.WriteAllText(Path.Combine(directory, "notes.txt"), "x");

            Assert.That(store.Keys(), Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void Keys_IsEmpty_BeforeTheFolderExists()
        {
            Assert.That(store.Keys(), Is.Empty);
        }

        [Test]
        public void Write_PreservesNonAsciiText()
        {
            store.Write(Key, "Zauberstab — üß");

            store.TryRead(Key, out var content);

            Assert.That(content, Is.EqualTo("Zauberstab — üß"));
        }

        [Test]
        public void AKeyThatCouldEscapeTheFolder_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => store.Write("../outside", "x"));
            Assert.Throws<ArgumentException>(() => store.TryRead("a/b", out _));
            Assert.Throws<ArgumentException>(() => store.Write("", "x"));
        }

        [Test]
        public void SetAside_RenamesTheSaveAndItsBackup_NeverDeletingThem()
        {
            store.Write(Key, "one");
            store.Write(Key, "two");

            Assert.That(store.SetAside(Key), Is.True);

            Assert.That(store.TryRead(Key, out _), Is.False);
            Assert.That(store.Keys(), Is.Empty);
            Assert.That(File.ReadAllText(Path.Combine(directory, Key + ".sav.corrupt")), Is.EqualTo("two"));
            Assert.That(File.ReadAllText(Path.Combine(directory, Key + ".sav.corrupt.bak")), Is.EqualTo("one"));
            Assert.That(store.SetAside(Key), Is.False, "nothing left to set aside");
        }

        [Test]
        public void SetAside_TwiceForOneKey_KeepsBothFiles()
        {
            store.Write(Key, "first");
            store.SetAside(Key);
            store.Write(Key, "second");

            store.SetAside(Key);

            Assert.That(File.ReadAllText(Path.Combine(directory, Key + ".sav.corrupt")), Is.EqualTo("first"));
            Assert.That(File.ReadAllText(Path.Combine(directory, Key + ".sav.corrupt2")), Is.EqualTo("second"));
        }

        [Test]
        public void AKeyWrittenAfterASetAside_IsAFreshSave()
        {
            store.Write(Key, "damaged");
            store.SetAside(Key);

            Assert.That(Slot().Load().Status, Is.EqualTo(LoadStatus.Missing));

            Slot().Save(NoteOf("fresh"));
            Assert.That(Slot().Load().Payload.text, Is.EqualTo("fresh"));
        }

        [Test]
        public void AppendSideFile_AppendsBesideTheSaves_AndIsNotListedAsAKey()
        {
            store.Write(Key, "one");

            store.AppendSideFile("hero.quarantine.json", "first\n");
            store.AppendSideFile("hero.quarantine.json", "second\n");

            Assert.That(File.ReadAllText(Path.Combine(directory, "hero.quarantine.json")), Is.EqualTo("first\nsecond\n"));
            Assert.That(store.Keys(), Is.EqualTo(new[] { Key }));
        }

        [Test]
        public void AppendSideFile_CreatesTheFolder_AndRejectsANameThatCouldEscapeOrIsASave()
        {
            store.AppendSideFile("a.txt", "x");
            Assert.That(Directory.Exists(directory), Is.True);

            Assert.Throws<ArgumentException>(() => store.AppendSideFile("../outside.txt", "x"));
            Assert.Throws<ArgumentException>(() => store.AppendSideFile("hero.sav", "x"));
            Assert.Throws<ArgumentException>(() => store.AppendSideFile("", "x"));
        }
    }
}
