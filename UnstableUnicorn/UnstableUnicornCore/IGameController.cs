﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnstableUnicornCore {
    public interface IGameController {
        void SimulateGame();
    }

    public class GameController : IGameController, IPublisher {
        public Random Random { get; set; }
        public List<Card> Pile;
        public List<Card> DiscardPile = new();
        public List<Card> Nursery;
        public List<APlayer> Players;
        public List<AContinuousEffect> ContinuousEffects = new();
        private Dictionary<ETriggerSource, List<TriggerEffect>> EventsPool = new();
        private List<TriggerEffect> triggersToRemove = new();

        /// <summary>
        /// Actual chain link is link which is already in proccess of resolving
        /// 
        /// Next chain link is link which will follow in next iteration of resolving
        /// </summary>
        private List<AEffect> _actualChainLink = new();
        private List<AEffect> _nextChainLink = new();

        public List<AEffect> ActualChainLink => _actualChainLink;
        public List<AEffect> NextChainLink => _nextChainLink;

        public APlayer ActualPlayerOnTurn { get; set; }
        public int MaxCardsToPlayInOneTurn { get; set; } = 1;
        public int DrawExtraCards { get; set; } = 0;
        public bool SkipToEndTurnPhase { get; set; } = false;

        public HashSet<Card> CardsWhichAreTargeted { get; set; } = new();

        private bool _willTakeExtraTurn = false;

        public GameController(List<Card> pile, List<Card> nursery, List<APlayer> players, int seed = 42) {
            Random = new Random(seed);
            Pile = pile.Shuffle(Random);
            Nursery = new List<Card>( nursery );

            foreach(var babyUnicorn in Nursery) {
                babyUnicorn.Player = null;
                babyUnicorn.Location = CardLocation.Nursery;
            }

            foreach(APlayer p in players)
                p.GameController = this;
            Players = new List<APlayer>( players );

            // setting for unit tests
            ActualPlayerOnTurn = Players[0];
        }

        public void SimulateGame() {
            int index = 0;
            while (true) {
                APlayer player = Players[index];
                SimulateOneTurn(player);

                if (_willTakeExtraTurn)
                    _willTakeExtraTurn = false;
                else
                    index = (index + 1) % Players.Count;
            }
        }

        internal void SimulateOneTurn(APlayer playerOnTurn) {
            SkipToEndTurnPhase = false;
            MaxCardsToPlayInOneTurn = 1;
            DrawExtraCards = 0;
            ActualPlayerOnTurn = playerOnTurn;

            OnBeginTurn(playerOnTurn);

            for (int cardIdx = 0; cardIdx < MaxCardsToPlayInOneTurn; cardIdx++) {
                if (SkipToEndTurnPhase)
                    break;

                Card? card = playerOnTurn.WhichCardToPlay();
                if (card == null) {
                    if(cardIdx == 0)
                        PlayerDrawCard(playerOnTurn);
                    break;
                } else {
                    if (!playerOnTurn.Hand.Remove(card))
                        throw new InvalidOperationException($"Card {card.Name} not in player hand!");

                    // check instant
                    // interative resolving instant cards
                    // stack chain resolve

                    card.CardPlayed(this, playerOnTurn);
                }
            }

            OnEndTurn(playerOnTurn);
        }

        public void ThisPlayerTakeExtraTurn() => _willTakeExtraTurn = true;

        /// <summary>
        /// Warning: This method can be called during resolving chain link but
        /// do not called it during <see cref="AEffect.InvokeEffect(GameController)"/>
        /// 
        /// If you do then resolving will crash
        /// </summary>
        /// <param name="effect"></param>
        public void AddEffectToActualChainLink(AEffect effect) => _actualChainLink.Add(effect);
        public void AddNewEffectToChainLink(AEffect effect) => _nextChainLink.Add(effect);

        private void ResolveChainLink() {
            while (_nextChainLink.Count > 0) {
                CardsWhichAreTargeted = new HashSet<Card>();

                _actualChainLink = _nextChainLink;
                _nextChainLink = new List<AEffect>();

                // choosing targets of effects
                for (int i = 0; i < _actualChainLink.Count; i++) {
                    var effect = _actualChainLink[i];
                    effect.ChooseTargets(this);
                }

                // trigger all ChangeTargeting
                for (int i = 0; i < _actualChainLink.Count; i++) {
                    var effect = _actualChainLink[i];
                    PublishEvent(ETriggerSource.ChangeTargeting, effect.OwningCard, effect);
                }

                // trigger change card location
                // actual redirection work in this way
                // -> it is created a new affect which will be called after a card is moved
                //    for example: when card is destroyed and card have text:
                //    -  If this card would be sacrificed or destroyed, return it to your hand instead.
                //    then card will be moved on discard pile and then will be moved back to owner's hand
                //    in same chain link
                for (int i = 0; i < _actualChainLink.Count; i++) {
                    var effect = _actualChainLink[i];
                    PublishEvent(ETriggerSource.ChangeLocationOfCard, effect.OwningCard, effect);
                }

                // effect which trigger on leaving but they not change targeting or location
                // this event is primary used for cards which can't be triggered by CardLeftStable
                // because in this time are triggers unregistered
                for (int i = 0; i < _actualChainLink.Count; i++) {
                    var effect = _actualChainLink[i];
                    PublishEvent(ETriggerSource.PreCardLeftStable, effect.OwningCard, effect);
                }

                // unregister affects of targets cards then moves
                // the cards which published card leave and card enter events
                // for example barbed wire could cause trouble
                foreach (var effect in _actualChainLink)
                    foreach (var card in effect.CardTargets)
                        if (card.Location == CardLocation.OnTable)
                            card.UnregisterAllEffects();

                // move cards to new location, trigger leave card and
                foreach (var effect in _actualChainLink)
                    effect.InvokeEffect(this);

                // delay card enter to new stable to next chain link - a.k.a. add to chainLink
                // --> this i dont need to solve, nearly all trigger effects are by default delayed
                foreach (var effect in _actualChainLink) {
                    foreach (var card in effect.CardTargets)
                        if (card.Location == CardLocation.OnTable) {
                            // register all effects and trigger one time effects
                            // this will cast even cards which says when this card enter stable
                            // because this effects are not trigger effects but one time effects
                            // -> this will prevent interaction of cards in same chain link
                            card.RegisterAllEffects();
                        }
                }
            }
        }

        public void PlayerDrawCard(APlayer player) {
            Card topCard = Pile[^1];
            Pile.RemoveAt(Pile.Count - 1);
            topCard.MoveCard(this, player, CardLocation.InHand);
        }

        private void PlayerDrawCards(APlayer player, int numberCards) {
            for (int i = 0; i < numberCards; i++)
                PlayerDrawCard(player);
        }

        public void PlayerGetBabyUnicornOnTable(APlayer player) {
            // when no baby unicorn in Nursery -> player got nothing
            if (Nursery.Count == 0)
                return;

            Card babyUnicron = Nursery[^1];
            babyUnicron.MoveCard(this, player, CardLocation.OnTable);
            babyUnicron.RegisterAllEffects();
        }

        public void PlayerGetBabyUnicornsOnTable(APlayer player, int numberUnicorns) {
            for (int i = 0; i < numberUnicorns; i++)
                PlayerGetBabyUnicornOnTable(player);
        }

        private void OnBeginTurn(APlayer player) {
            // Trigger on begin turn effects
            PublishEvent(ETriggerSource.BeginningTurn);

            // resolve chain link
            ResolveChainLink();

            if(!SkipToEndTurnPhase)
                PlayerDrawCards(player, 1 + DrawExtraCards);
        }

        private void OnEndTurn(APlayer player) {
            // Trigger on end turn effects
            PublishEvent(ETriggerSource.EndTurn);

            // resolve chain link
            ResolveChainLink();

            if (player.Hand.Count > 7) {
                List<Card> cards = player.WhichCardsToDiscard(player.Hand.Count - 7, Enum.GetValues(typeof(ECardType)).Cast<ECardType>().ToList());
                foreach (var card in cards) {
                    if (!player.Hand.Remove(card))
                        throw new InvalidOperationException($"Card {card.Name} not in player hand!");
                    card.MoveCard(this, null, CardLocation.DiscardPile);
                }
            }
        }

        public void PublishEvent(ETriggerSource _event, Card? card = null, AEffect? effect = null) {
            if (!EventsPool.TryGetValue(_event, out List<TriggerEffect>? triggerList))
                return;
            foreach (var trigger in triggerList) {
                trigger.InvokeEffect(_event, card, effect, this);
            }

            foreach (var trigger in triggersToRemove)
                trigger.UnsubscribeToEvent(this);
            
            triggersToRemove.Clear();
        }

        public void AddContinuousEffect(AContinuousEffect effect) => ContinuousEffects.Add(effect);

        public void RemoveContinuousEffect(AContinuousEffect effect) => ContinuousEffects.Remove(effect);

        public void SubscribeEvent(ETriggerSource _event, TriggerEffect effect) {
            if (!EventsPool.TryGetValue(_event, out List<TriggerEffect>? triggerList)) {
                triggerList = new List<TriggerEffect> { };
                EventsPool.Add(_event, triggerList);
            }
            triggerList.Add(effect);
        }

        public void UnsubscribeEvent(ETriggerSource _event, TriggerEffect effect) {
            if (!EventsPool.TryGetValue(_event, out List<TriggerEffect>? triggerList))
                return; // Effect was not event registered, nothing to unregister -> OK state
            if (!triggerList.Remove(effect))
                return; // Effect was not found, nothing to unregister -> OK state
        }

        public void UnsubscribeEventAfterEndOfPublishing(TriggerEffect triggerEffect)
            => triggersToRemove.Add(triggerEffect);

        public List<Card> GetCardsOnTable() {
            List<Card> cards = new();
            foreach(var player in Players) {
                cards.AddRange(player.Stable);
                cards.AddRange(player.Upgrades);
                cards.AddRange(player.Downgrades);
            }
            return cards;
        }

        public void ShuffleDeck() => Pile = Pile.Shuffle(Random);

        /// <summary>
        /// Move all cards from discard pile to pile
        /// 
        /// Warning: Must be shuffled deck after this operation
        /// </summary>
        public void AddDiscardPileToPile() {
            var listCopy = new List<Card>(DiscardPile);
            foreach (var card in listCopy)
                card.MoveCard(this, null, CardLocation.Pile);
            
            Pile.AddRange(listCopy);
        }
    }
}
