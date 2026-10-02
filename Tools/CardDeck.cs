using System;
using System.Collections.Generic;
using Submodules.Utility.Extensions;

namespace Submodules.Utility.Tools
{
    public interface ICardDeck<T>
    {
        /// <summary> Cards the deck is made of, wherever they currently are. </summary>
        int Count { get; }

        /// <summary>
        ///     Cards left in the current draw pile. 0 does not mean the deck is exhausted — a default
        ///     deck reshuffles on the next draw. Use <see cref="CanDraw"/> for that.
        /// </summary>
        int Remaining { get; }

        /// <summary> Whether <see cref="Draw"/> would succeed, refilling the draw pile if needed. </summary>
        bool CanDraw { get; }

        /// <summary> Cards waiting in the discard pile. Always 0 unless the deck recycles discards. </summary>
        int DiscardCount { get; }

        T Draw();
        bool TryDraw(out T card);

        /// <summary> The card the next <see cref="Draw"/> returns, without drawing it. </summary>
        T Peek();

        /// <summary> Draws up to <paramref name="count"/> cards; fewer if the deck runs out. </summary>
        List<T> DrawMany(int count);

        /// <summary>
        ///     Puts a card that is currently in hand into the discard pile. Only valid when the deck
        ///     recycles discards, and throws if every copy of the card is already in a pile.
        /// </summary>
        void Discard(T card);

        void Add(T card);
        bool Remove(T card);

        /// <summary> Puts every card back and reshuffles. </summary>
        void Reset();
    }

    /// <summary>
    ///     A shuffle-bag: cards are drawn without replacement, and when the draw pile runs out it is
    ///     refilled and reshuffled.
    ///     <para>
    ///         By default the refill is the whole deck, so a drawn card is simply consumed. With
    ///         <c>recycleDiscards</c> the refill is only the discard pile — cards you are still holding,
    ///         or never discard, stay out of circulation until <see cref="Reset"/>.
    ///     </para>
    /// </summary>
    public sealed class CardDeck<T> : ICardDeck<T>
    {
        private readonly List<T> deck;
        private readonly List<T> drawPile = new List<T>();
        private readonly List<T> discardPile = new List<T>();
        private readonly Func<int, int> nextIndex;
        private readonly bool recycleDiscards;
        private readonly bool avoidImmediateRepeat;

        private bool hasLast;
        private T last;

        /// <param name="contents"> The cards. Copied; later changes to the source are not seen. </param>
        /// <param name="nextIndex">
        ///     Takes an exclusive upper bound n and returns an index in [0, n). Defaults to
        ///     <see cref="UnityEngine.Random"/>; pass <c>new System.Random(seed).Next</c> for a
        ///     reproducible order.
        /// </param>
        /// <param name="recycleDiscards"> Refill from the discard pile instead of the whole deck. </param>
        /// <param name="avoidImmediateRepeat">
        ///     After a refill, don't open with the card that was just drawn (when the deck holds
        ///     anything else).
        /// </param>
        public CardDeck(IEnumerable<T> contents, Func<int, int> nextIndex = null, bool recycleDiscards = false, bool avoidImmediateRepeat = false)
        {
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            deck = new List<T>(contents);
            this.nextIndex = nextIndex ?? EnumerableExtensions.UnityRandomIndex;
            this.recycleDiscards = recycleDiscards;
            this.avoidImmediateRepeat = avoidImmediateRepeat;

            Reset();
        }

        public int Count => deck.Count;
        public int Remaining => drawPile.Count;
        public int DiscardCount => discardPile.Count;
        public bool CanDraw => drawPile.Count > 0 || RefillSource.Count > 0;

        private List<T> RefillSource => recycleDiscards ? discardPile : deck;

        public void Reset()
        {
            drawPile.Clear();
            discardPile.Clear();
            hasLast = false;

            drawPile.AddRange(deck);
            drawPile.Shuffle(nextIndex);
        }

        public T Draw()
        {
            if (TryDraw(out var card))
                return card;

            throw NothingToDraw();
        }

        private InvalidOperationException NothingToDraw() =>
            new InvalidOperationException(deck.Count == 0 ? "Deck is empty" : "No cards left to draw");

        public bool TryDraw(out T card)
        {
            if (drawPile.Count == 0 && !Refill())
            {
                card = default;
                return false;
            }

            var top = drawPile.Count - 1;
            card = drawPile[top];
            drawPile.RemoveAt(top);

            last = card;
            hasLast = true;
            return true;
        }

        public T Peek()
        {
            if (drawPile.Count == 0 && !Refill())
                throw NothingToDraw();

            return drawPile[^1];
        }

        public List<T> DrawMany(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var cards = new List<T>(Math.Min(count, deck.Count));
            while (cards.Count < count && TryDraw(out var card))
                cards.Add(card);

            return cards;
        }

        public void Discard(T card)
        {
            if (!recycleDiscards)
                throw new InvalidOperationException("This deck does not recycle discards");

            if (CountOf(deck, card) <= CountOf(drawPile, card) + CountOf(discardPile, card))
                throw new InvalidOperationException("Card is not in hand");

            discardPile.Add(card);
        }

        private static int CountOf(List<T> cards, T card)
        {
            var comparer = EqualityComparer<T>.Default;
            var count = 0;
            foreach (var other in cards)
                if (comparer.Equals(other, card))
                    count++;

            return count;
        }

        /// <summary>
        ///     Adds a card to the deck and slips it into the draw pile at a random position — never on
        ///     top, so a prior <see cref="Peek"/> still holds and a refill's repeat check isn't undone.
        /// </summary>
        public void Add(T card)
        {
            deck.Add(card);
            drawPile.Insert(drawPile.Count == 0 ? 0 : nextIndex(drawPile.Count), card);
        }

        /// <summary>
        ///     Removes one card equal to <paramref name="card"/> from the deck and from whichever pile
        ///     holds it. A card currently in hand just won't come back. False if the deck has none.
        /// </summary>
        public bool Remove(T card)
        {
            if (!deck.Remove(card))
                return false;

            if (!drawPile.Remove(card))
                discardPile.Remove(card);

            return true;
        }

        private bool Refill()
        {
            var source = RefillSource;
            if (source.Count == 0)
                return false;

            drawPile.AddRange(source);
            if (recycleDiscards)
                discardPile.Clear();

            drawPile.Shuffle(nextIndex);
            SeparateFromLast();
            return true;
        }

        /// <summary> The pile is drawn from the end; swap in a different card if it would open with <see cref="last"/>. </summary>
        private void SeparateFromLast()
        {
            if (!avoidImmediateRepeat || !hasLast)
                return;

            var comparer = EqualityComparer<T>.Default;
            var top = drawPile.Count - 1;
            if (!comparer.Equals(drawPile[top], last))
                return;

            for (var i = 0; i < top; i++)
            {
                if (comparer.Equals(drawPile[i], last))
                    continue;

                (drawPile[i], drawPile[top]) = (drawPile[top], drawPile[i]);
                return;
            }
        }
    }
}
