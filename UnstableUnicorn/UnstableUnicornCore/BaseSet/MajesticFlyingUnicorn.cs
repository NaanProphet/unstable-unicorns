﻿using System.Collections.Generic;
using UnstableUnicornCore.BasicEffects;

namespace UnstableUnicornCore.BaseSet {
    public class MajesticFlyingUnicorn : CardTemplateSource {
        public override CardTemplate GetCardTemplate() {
            return Card
                .Name("Majestic Flying Unicorn")
                .CardType(ECardType.MagicUnicorn)
                .Text("When this card enters your Stable, you may choose a Unicorn card from the discard pile and add it to your hand. If this card would be sacrificed or destroyed, return it to your hand instead.")
                .Cast((Card owningCard) => new ActivatableEffect(owningCard,
                        new SearchDeckEffect(owningCard, 1, CardLocation.DiscardPile, (Card card) => ECardTypeUtils.UnicornTarget.Contains(card.CardType))
                    )
                )
                .TriggerEffect(
                    TriggerPredicates.IfThisCardWouldBeSacrificedOrDestroyed,
                    new List<ETriggerSource> { ETriggerSource.ChangeLocationOfCard },
                    (Card owningCard) => new ReturnThisCardToLocation(owningCard)
                );
        }
    }
}
