using System;
using System.Linq;
using NUnit.Framework;
using Submodules.Utility.Extensions;
using Submodules.Utility.Tools;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="CardDeck{T}"/> and the <c>Shuffle</c>/<c>Randomize</c> extensions it is built on.
    /// Every deck gets a seeded <see cref="System.Random"/> so the orders are reproducible.
    /// </summary>
    [TestFixture]
    public sealed class CardDeckTests
    {
        private static Func<int, int> Seeded(int seed) => new System.Random(seed).Next;

        private static CardDeck<int> Deck(int cards, bool recycle = false, bool avoidRepeat = false, int seed = 1) =>
            new CardDeck<int>(Enumerable.Range(0, cards), Seeded(seed), recycle, avoidRepeat);

        [Test]
        public void Draw_OneFullCycle_YieldsEveryCardExactlyOnce()
        {
            var deck = Deck(6);

            var drawn = deck.DrawMany(6);

            Assert.That(drawn, Is.EquivalentTo(Enumerable.Range(0, 6)));
            Assert.That(deck.Remaining, Is.EqualTo(0));
        }

        [Test]
        public void Draw_PastTheEnd_ReshufflesTheWholeDeck()
        {
            var deck = Deck(4);

            var drawn = deck.DrawMany(8);

            Assert.That(drawn.GroupBy(c => c).Select(g => g.Count()), Is.All.EqualTo(2));
        }

        [Test]
        public void Draw_EmptyDeck_Throws_AndTryDrawReturnsFalse()
        {
            var deck = Deck(0);

            Assert.Throws<InvalidOperationException>(() => deck.Draw());
            Assert.That(deck.TryDraw(out _), Is.False);
        }

        [Test]
        public void SameSeed_SameOrder()
        {
            Assert.That(Deck(10, seed: 7).DrawMany(10), Is.EqualTo(Deck(10, seed: 7).DrawMany(10)));
        }

        [Test]
        public void Peek_DoesNotConsume_AndMatchesTheNextDraw()
        {
            var deck = Deck(5);

            var peeked = deck.Peek();

            Assert.That(deck.Remaining, Is.EqualTo(5));
            Assert.That(deck.Draw(), Is.EqualTo(peeked));
        }

        [Test]
        public void DrawMany_StopsEarly_WhenTheDeckRunsOut()
        {
            var deck = Deck(3, recycle: true);

            Assert.That(deck.DrawMany(5), Has.Count.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => deck.DrawMany(-1));
        }

        [Test]
        public void AvoidImmediateRepeat_NeverOpensACycleWithTheLastCard()
        {
            var deck = Deck(2, avoidRepeat: true);

            var drawn = deck.DrawMany(200);

            for (var i = 1; i < drawn.Count; i++)
                Assert.That(drawn[i], Is.Not.EqualTo(drawn[i - 1]), $"repeat at draw {i}");
        }

        [Test]
        public void AvoidImmediateRepeat_OnAnAllEqualDeck_StillDraws()
        {
            var deck = new CardDeck<int>(new[] { 3, 3, 3 }, Seeded(1), avoidImmediateRepeat: true);

            Assert.That(deck.DrawMany(7), Is.All.EqualTo(3));
        }

        [Test]
        public void RecycleDiscards_RefillsOnlyFromTheDiscardPile()
        {
            var deck = Deck(4, recycle: true);
            var hand = deck.DrawMany(4);
            deck.Discard(hand[0]);
            deck.Discard(hand[1]);

            var next = deck.DrawMany(10);

            Assert.That(next, Is.EquivalentTo(new[] { hand[0], hand[1] }));
        }

        [Test]
        public void RecycleDiscards_EverythingInHand_NothingToDraw()
        {
            var deck = Deck(3, recycle: true);
            deck.DrawMany(3);

            Assert.That(deck.TryDraw(out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => deck.Draw());
            Assert.Throws<InvalidOperationException>(() => deck.Peek());
        }

        [Test]
        public void Discard_OnADeckThatDoesNotRecycle_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Deck(3).Discard(0));
        }

        [Test]
        public void Discard_ACardNotInHand_Throws()
        {
            var deck = Deck(3, recycle: true);
            var hand = deck.DrawMany(1);
            deck.Discard(hand[0]);

            Assert.Throws<InvalidOperationException>(() => deck.Discard(hand[0]), "already discarded");
            Assert.Throws<InvalidOperationException>(() => deck.Discard(99), "never in the deck");

            var undrawn = deck.Peek();
            Assert.Throws<InvalidOperationException>(() => deck.Discard(undrawn), "still in the draw pile");
        }

        [Test]
        public void Discard_OneOfSeveralEqualCards_OnlyAsManyTimesAsAreInHand()
        {
            var deck = new CardDeck<int>(new[] { 7, 7 }, Seeded(1), recycleDiscards: true);
            deck.Draw();

            deck.Discard(7);

            Assert.Throws<InvalidOperationException>(() => deck.Discard(7));
            Assert.That(deck.DiscardCount, Is.EqualTo(1));
        }

        [Test]
        public void CanDraw_IsTrueOnADefaultDeckAtRemainingZero_AndFalseOnceRecycledCardsAreAllHeld()
        {
            var deck = Deck(2);
            deck.DrawMany(2);
            Assert.That(deck.Remaining, Is.EqualTo(0));
            Assert.That(deck.CanDraw, Is.True);

            var recycling = Deck(2, recycle: true);
            Assert.That(recycling.CanDraw, Is.True);
            recycling.DrawMany(2);
            Assert.That(recycling.CanDraw, Is.False);
            recycling.Discard(0);
            Assert.That(recycling.CanDraw, Is.True);

            Assert.That(Deck(0).CanDraw, Is.False);
        }

        [Test]
        public void DrawMany_HugeCount_DoesNotPreallocateIt_AndStopsWhenRecycledCardsRunOut()
        {
            var deck = Deck(3, recycle: true);

            Assert.That(deck.DrawMany(int.MaxValue), Has.Count.EqualTo(3));
        }

        [Test]
        public void Remove_ACardInTheDiscardPile_LeavesTheOtherPilesAlone()
        {
            var deck = Deck(4, recycle: true);
            var hand = deck.DrawMany(2);
            deck.Discard(hand[0]);

            Assert.That(deck.Remove(hand[0]), Is.True);

            Assert.That(deck.DiscardCount, Is.EqualTo(0));
            Assert.That(deck.Remaining, Is.EqualTo(2));
            Assert.That(deck.Count, Is.EqualTo(3));
        }

        [Test]
        public void Add_ToARecyclingDeck_IsDrawableEvenThoughItIsNotInTheDiscardPile()
        {
            var deck = Deck(2, recycle: true);
            deck.DrawMany(2);

            deck.Add(99);

            Assert.That(deck.Draw(), Is.EqualTo(99));
        }

        [Test]
        public void Peek_OnAnEmptyDrawPile_RefillsFromTheDiscardPile_AndMatchesTheNextDraw()
        {
            var deck = Deck(3, recycle: true);
            var hand = deck.DrawMany(3);
            deck.Discard(hand[1]);
            deck.Discard(hand[2]);

            var peeked = deck.Peek();

            Assert.That(deck.DiscardCount, Is.EqualTo(0));
            Assert.That(new[] { hand[1], hand[2] }, Does.Contain(peeked));
            Assert.That(deck.Draw(), Is.EqualTo(peeked));
        }

        [Test]
        public void Reset_PutsEveryCardBack_IncludingDiscardsAndHeldCards()
        {
            var deck = Deck(4, recycle: true);
            var hand = deck.DrawMany(3);
            deck.Discard(hand[0]);

            deck.Reset();

            Assert.That(deck.Remaining, Is.EqualTo(4));
            Assert.That(deck.DiscardCount, Is.EqualTo(0));
        }

        [Test]
        public void Add_MakesTheCardDrawableThisCycle()
        {
            var deck = Deck(3);

            deck.Add(99);

            Assert.That(deck.Count, Is.EqualTo(4));
            Assert.That(deck.DrawMany(4), Does.Contain(99));
        }

        [Test]
        public void Add_NeverChangesWhatPeekReturned()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var deck = Deck(4, seed: seed);
                var peeked = deck.Peek();

                deck.Add(99);

                Assert.That(deck.Draw(), Is.EqualTo(peeked), $"seed {seed}");
            }
        }

        [Test]
        public void Remove_TakesTheCardOutOfTheDeckAndItsPile()
        {
            var deck = Deck(4);

            Assert.That(deck.Remove(2), Is.True);
            Assert.That(deck.Remove(2), Is.False);
            Assert.That(deck.Count, Is.EqualTo(3));
            Assert.That(deck.DrawMany(3), Is.EquivalentTo(new[] { 0, 1, 3 }));
        }

        [Test]
        public void Constructor_NullContents_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CardDeck<int>(null));
        }

        [Test]
        public void Shuffle_KeepsTheSameElements_AndIsReproducible()
        {
            var a = Enumerable.Range(0, 20).ToList();
            var b = Enumerable.Range(0, 20).ToList();

            a.Shuffle(Seeded(5));
            b.Shuffle(Seeded(5));

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.EquivalentTo(Enumerable.Range(0, 20)));
            Assert.That(a, Is.Not.EqualTo(Enumerable.Range(0, 20)));
        }

        [Test]
        public void Randomize_ReturnsTheSameElements_InAStableOrderAcrossEnumerations()
        {
            var shuffled = Enumerable.Range(0, 15).Randomize();

            Assert.That(shuffled, Is.EquivalentTo(Enumerable.Range(0, 15)));
            Assert.That(shuffled.ToList(), Is.EqualTo(shuffled.ToList()));
        }

        [Test]
        public void Randomize_NullSource_ReturnsNull()
        {
            Assert.That(((int[])null).Randomize(), Is.Null);
        }
    }
}
