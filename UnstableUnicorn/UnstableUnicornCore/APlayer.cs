﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnstableUnicornCore {
    
    public abstract class APlayer {
        public List<Card> Hand       = new();
        public List<Card> Stable     = new();
        public List<Card> Upgrades   = new();
        public List<Card> Downgrades = new();
        public GameController GameController { get; set; }

        /// <summary>
        /// Which card from should be played
        /// 
        /// If function return null then no card will be played
        /// </summary>
        /// <returns>Card to play or null</returns>
        public abstract Card? WhichCardToPlay();

        /// <summary>
        /// When you decide to play a card, then this method will be called
        /// <br/>
        /// Cards can be played to any player's stable, but if card is spell,
        /// then "target" owner must be player who own this card (you)
        /// <br/>
        /// In <see cref="WhichCardToPlay"/> you could decide what and where to play
        /// and then here return cached data.
        /// To this method is passed card which you decided to play,
        /// but to method will be always passed last card which you returned from
        /// <see cref="WhichCardToPlay"/>
        /// </summary>
        /// <param name="card">Card which you decided to play</param>
        /// <returns>Target owner</returns>
        public abstract APlayer WhereShouldBeCardPlayed(Card card);
        public abstract List<Card> WhichCardsToSacrifice(int number, AEffect effect, List<Card> cardsWhichCanBeSelected);
        public abstract List<Card> WhichCardsToDestroy(int number, AEffect effect, List<Card> cardsWhichCanBeSelected);

        public abstract List<Card> WhichCardsToReturn(int number, AEffect effect, List<Card> cardsWhichCanBeSelected);
        public abstract List<Card> WhichCardsToSteal(int number, AEffect effect, List<Card> cardsWhichCanBeSelected);
        public abstract List<Card> WhichCardsToDiscard(int number, AEffect effect, List<Card> cardsWhichCanBeSelected);

        public abstract List<Card> WhichCardsToSave(int number, AEffect effect, List<Card> cardsWhichCanBeSelected);
        public abstract List<APlayer> ChoosePlayers(int number, bool canChooseMyself, AEffect effect);
        public abstract List<Card> WhichCardsToGet(int number, AEffect effect, List<Card> cardsWhichCanBeSelected);
        public abstract List<Card> WhichCardsToMove(int number, AEffect effect, List<Card> cardsWhichCanBeSelected);
        public abstract AEffect WhichEffectToSelect(List<AEffect> effectsVariants);
        public abstract bool ActivateEffect(AEffect effect);
    }
}
