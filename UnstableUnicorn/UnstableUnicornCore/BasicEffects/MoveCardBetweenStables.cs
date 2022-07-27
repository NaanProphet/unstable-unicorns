﻿using System;
using System.Collections.Generic;

namespace UnstableUnicornCore.BasicEffects {
    public class MoveCardBetweenStables : AEffect {
        Predicate<Card> whichCardMovePredicate;
        bool canOwningPlayerGetCard;
        public MoveCardBetweenStables(Card owningCard,
                                      Predicate<Card> whichCardMovePredicate,
                                      bool canOwningPlayerGetCard) : base(owningCard, 1)
        {
            this.whichCardMovePredicate = whichCardMovePredicate;
            this.canOwningPlayerGetCard = canOwningPlayerGetCard;

            TargetLocation = CardLocation.OnTable;
        }

        private List<Card> GetValidTargets(GameController gameController)
            => RemoveCardsWhichAreTargeted(gameController.GetCardsOnTable().FindAll(whichCardMovePredicate), gameController);
        public override void ChooseTargets(GameController gameController) {
            var cards = GetValidTargets(gameController);

            int numberCardsToSelect = Math.Min(_cardCount, cards.Count);
            CardTargets = OwningPlayer.WhichCardsToMove(numberCardsToSelect, this, cards);

            ValidatePlayerSelection(numberCardsToSelect, CardTargets, cards);

            List<APlayer> availablePlayers = new List<APlayer>(gameController.Players);
            if (!canOwningPlayerGetCard)
                availablePlayers.Remove(OwningPlayer);
            availablePlayers.Remove(CardTargets[0].Player);

            var players = OwningPlayer.ChoosePlayers(1, this, availablePlayers);

            ValidatePlayerSelection(1, players, availablePlayers);
            
            APlayer player = players[0];
            if (player == OwningPlayer && !canOwningPlayerGetCard)
                throw new InvalidOperationException("Owning player can't get card");

            if (player == CardTargets[0].Player)
                throw new InvalidOperationException("Player owner of this card and target owner is same player!");

            TargetOwner = player;
        }

        public override void InvokeEffect(GameController gameController) {
            foreach (var card in CardTargets)
                card.MoveCard(gameController, TargetOwner, TargetLocation);
        }

        public override bool MeetsRequirementsToPlayInner(GameController gameController)
            => GetValidTargets(gameController).Count >= _cardCount;
    }
}
