﻿using System.Collections.Generic;
using UnstableUnicornCore.BasicEffects;

namespace UnstableUnicornCore.BaseSet {
    /// <summary>
    /// Used 2nd Edition effect
    /// 
    /// With second second print text can occur edge case where is not clear effect resolution in the original game
    /// </summary>
    public class UnicornPhoenix : CardTemplateSource {
        public override CardTemplate GetCardTemplate() {
            return EmptyCard
                .Name("Unicorn Phoenix")
                .CardType(ECardType.MagicUnicorn)
                .Text("If this card would be sacrificed or destroyed, you may DISCARD a card instead.")
                .TriggerEffect(
                    (effect, causedCard, owningCard, controller) =>
                        TriggerPredicates.IfUnicornInYourStableWouldBeDestroyd(effect, causedCard, owningCard, controller) &&
                        effect!.CardTargets.Contains(owningCard),
                    new List<ETriggerSource> { ETriggerSource.ChangeTargeting },
                    (Card owningCard) => new ActivatableEffect(owningCard,
                        new DiscardCardToProtectCard(owningCard, 1, ECardTypeUtils.CardTarget)
                    )
                );
        }
    }
}
