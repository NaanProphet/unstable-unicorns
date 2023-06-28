/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */
﻿namespace UnstableUnicornCore.BasicContinuousEffects {
    public class PlayerCantPlayInstantCards : AContinuousEffect {
        public PlayerCantPlayInstantCards(Card owningCard) : base(owningCard) {}

        public override bool CanBePlayedInstantCards(APlayer player) => player != OwningPlayer;
    }
}
