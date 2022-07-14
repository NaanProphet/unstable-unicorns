﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnstableUnicornCore;
using Xunit;

namespace UnstableUnicornCoreTest {
    public static class TestUtils {
        public static void CheckPlayerPileSizes(APlayer player, int handSize, int stableSize, int numUpgrades,
                                                int numDowngrades) {
            Assert.Equal(handSize, player.Hand.Count);
            Assert.Equal(stableSize, player.Stable.Count);
            Assert.Equal(numUpgrades, player.Upgrades.Count);
            Assert.Equal(numDowngrades, player.Downgrades.Count);
        }

        public static void CardCantBePlayed(Card card, APlayer targetOwner, GameController controller) {
            Assert.False(card.CanBePlayed(targetOwner));

            Action act = () => controller.PlayCardAndResolveChainLink(card, targetOwner);
            var exception = Assert.Throws<InvalidOperationException>(act);
            Assert.Equal(Card.CardCannotBePlayed, exception.Message);
        }
    }
}
