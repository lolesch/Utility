using System.ComponentModel;

namespace Submodules.Utility.Tools.Statistics
{
    public enum ModifierType : short
    {
        // WORDING
        // - Overwrite   => absolute, explicit, fix
        // - FlatAdd     => additional, additive, bonus, 
        // - PercentAdd  => more/less
        // - PercentMult => multiplicative

        /// Values are the order the modifiers are applied
        [Description( "Sets the stat to a fixed value" )]
        Overwrite = 0,

        [Description( "Adds a flat value to the stat" )]
        FlatAdd = 100,

        [Description( "Adds a percentage to the stat" )]
        PercentAdd = 200,

        [Description( "Multiplies the total by a percentage" )]
        PercentMult = 300,
    }
}