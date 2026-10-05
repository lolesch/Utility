using NUnit.Framework;
using Submodules.Utility.Persistence;
using System;
using System.Collections.Generic;

namespace Submodules.Utility.Tests.EditMode
{
    [Serializable]
    internal sealed class Note
    {
        public string text;
    }

    /// <summary>A serializer that is not JSON: it hands out a token per object and resolves it back.
    /// Proves the slot depends on the seam and nothing of the adapter.</summary>
    internal sealed class TokenSerializer : ISaveSerializer
    {
        private readonly List<object> table = new();

        public string Serialize<T>(T value)
        {
            table.Add(value);
            return $"#{table.Count - 1}";
        }

        public T Deserialize<T>(string text) => (T)table[int.Parse(text.Substring(1))];
    }

    internal sealed class ImmutableNote
    {
        public string Text { get; }
        public ImmutableNote(string text) => Text = text;
    }

    internal sealed class ImmutableNoteMapper : IDtoMapper<ImmutableNote, Note>
    {
        public Note ToDto(ImmutableNote domain) => new() { text = domain.Text };
        public ImmutableNote ToDomain(Note dto) => new(dto.text);
    }

    [TestFixture]
    public sealed class SaveSlotTests
    {
        private const string Key = "hero";

        private static readonly DateTime SavedAt = new(2026, 10, 5, 12, 30, 15, DateTimeKind.Utc);

        private InMemorySaveStore store;
        private JsonUtilitySerializer json;

        [SetUp]
        public void SetUp()
        {
            store = new InMemorySaveStore();
            json = new JsonUtilitySerializer();
        }

        private SaveSlot<Note> Slot(int version = 1, MigrationChain migrations = null) =>
            new(store, json, Key, version, migrations, () => SavedAt);

        private static Note NoteOf(string text) => new() { text = text };

        [Test]
        public void Load_ReturnsMissing_WhenNothingWasSaved()
        {
            var result = Slot().Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Missing));
            Assert.That(result.HasPayload, Is.False);
        }

        [Test]
        public void Load_ReturnsTheSavedPayload_ThroughTheEnvelope()
        {
            var slot = Slot();
            slot.Save(NoteOf("equipped"));

            var result = slot.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded));
            Assert.That(result.Payload.text, Is.EqualTo("equipped"));
            Assert.That(result.SavedAtUtc, Is.EqualTo(SavedAt));
            Assert.That(result.SavedAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void Load_ReturnsTheBackup_WhenTheCurrentFileIsDamaged()
        {
            var slot = Slot();
            slot.Save(NoteOf("first"));
            slot.Save(NoteOf("second"));
            store.Write(Key, "{ not an envelope");

            var result = slot.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.RestoredFromBackup));
            Assert.That(result.Payload.text, Is.EqualTo("second"), "the damaged write pushed the last good save into the backup");
            Assert.That(result.Failure, Is.Not.Null);
        }

        [Test]
        public void Load_ReturnsCorrupt_WhenTheFileIsDamagedAndThereIsNoBackup()
        {
            store.Write(Key, "{ not an envelope");

            var result = Slot().Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Corrupt));
            Assert.That(result.HasPayload, Is.False);
            Assert.That(result.Failure, Is.Not.Null);
        }

        [Test]
        public void Load_ReturnsCorrupt_WhenTheBackupIsDamagedToo()
        {
            store.Write(Key, "garbage one");
            store.Write(Key, "garbage two");

            Assert.That(Slot().Load().Status, Is.EqualTo(LoadStatus.Corrupt));
        }

        [Test]
        public void Load_ReturnsCorrupt_WhenTheEnvelopeCarriesNoSchemaVersion()
        {
            store.Write(Key, "{}");

            Assert.That(Slot().Load().Status, Is.EqualTo(LoadStatus.Corrupt));
        }

        [Test]
        public void Load_ReturnsNewerVersion_AndLeavesTheStoreUntouched()
        {
            Slot(version: 2).Save(NoteOf("from the future"));
            Slot(version: 2).Save(NoteOf("from the future, again"));
            store.TryRead(Key, out var currentBefore);
            store.TryReadBackup(Key, out var backupBefore);

            var result = Slot(version: 1).Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.NewerVersion));
            Assert.That(result.HasPayload, Is.False);
            store.TryRead(Key, out var currentAfter);
            store.TryReadBackup(Key, out var backupAfter);
            Assert.That(currentAfter, Is.EqualTo(currentBefore));
            Assert.That(backupAfter, Is.EqualTo(backupBefore));
        }

        [Test]
        public void Load_RunsTheMigrationChain_FromTheSavedVersionToTheCurrent()
        {
            Slot(version: 1).Save(NoteOf("x"));
            var chain = new MigrationChain()
                .Add(1, text => json.Serialize(NoteOf(json.Deserialize<Note>(text).text + "+a")))
                .Add(2, text => json.Serialize(NoteOf(json.Deserialize<Note>(text).text + "+b")));

            var result = Slot(version: 3, chain).Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded));
            Assert.That(result.Payload.text, Is.EqualTo("x+a+b"));
        }

        [Test]
        public void Load_RunsOnlyTheStepsAfterTheSavedVersion()
        {
            Slot(version: 2).Save(NoteOf("x"));
            var chain = new MigrationChain()
                .Add(1, text => throw new InvalidOperationException("version 1 is already behind us"))
                .Add(2, text => json.Serialize(NoteOf(json.Deserialize<Note>(text).text + "+b")));

            var result = Slot(version: 3, chain).Load();

            Assert.That(result.Payload.text, Is.EqualTo("x+b"));
        }

        [Test]
        public void Load_ReturnsCorrupt_WhenAnOldVersionHasNoStepToTheCurrent()
        {
            Slot(version: 1).Save(NoteOf("x"));

            var result = Slot(version: 2).Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Corrupt));
            StringAssert.Contains("version 1", result.Failure);
        }

        [Test]
        public void Save_StampsTheCurrentVersion_SoAMigratedFileLoadsWithoutSteps()
        {
            Slot(version: 1).Save(NoteOf("x"));
            var chain = new MigrationChain().Add(1, text => json.Serialize(NoteOf("migrated")));
            var upgraded = Slot(version: 2, chain);
            upgraded.Save(upgraded.Load().Payload);

            var result = Slot(version: 2).Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded));
            Assert.That(result.Payload.text, Is.EqualTo("migrated"));
        }

        [Test]
        public void ASerializerThatIsNotJson_CanReplaceTheAdapter_WithoutTouchingTheSlot()
        {
            var slot = new SaveSlot<Note>(store, new TokenSerializer(), Key, 1, utcNow: () => SavedAt);
            slot.Save(NoteOf("equipped"));

            var result = slot.Load();

            Assert.That(result.Status, Is.EqualTo(LoadStatus.Loaded));
            Assert.That(result.Payload.text, Is.EqualTo("equipped"));
            store.TryRead(Key, out var raw);
            Assert.That(raw, Does.StartWith("#"), "the file is in the fake's format, not JSON");
        }

        [Test]
        public void AMapper_RoundTripsAnImmutableDomainObject_ThroughTheSlot()
        {
            var mapper = new ImmutableNoteMapper();
            var slot = Slot();
            slot.Save(mapper.ToDto(new ImmutableNote("immutable")));

            var restored = mapper.ToDomain(slot.Load().Payload);

            Assert.That(restored.Text, Is.EqualTo("immutable"));
        }

        [Test]
        public void Constructor_RejectsAVersionBelowOne()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Slot(version: 0));
        }
    }
}
