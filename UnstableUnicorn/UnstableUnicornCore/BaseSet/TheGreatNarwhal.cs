/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */
﻿using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnstableUnicornCore.BasicEffects;

[assembly: InternalsVisibleTo("UnstableUnicornCoreTest")]
namespace UnstableUnicornCore.BaseSet {
    public class TheGreatNarwhal : CardTemplateSource {
        internal static Regex narwhalRegex = new Regex(@"\bnarwhal\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public override CardTemplate GetCardTemplate() {
            return EmptyCard
                .Name("The Great Narwhal")
                .CardType(ECardType.MagicUnicorn)
                .Text("When this card enters your Stable, you may search the deck for a card with \"Narwhal\" in its name and add it to your hand. Shuffle the deck.")
                .Cast((Card owningCard) => new ActivatableEffect(owningCard,
                        new SearchDeckEffect(owningCard, 1, CardLocation.Pile, card => narwhalRegex.IsMatch(card.Name))
                    )
                );
        }
    }
}
