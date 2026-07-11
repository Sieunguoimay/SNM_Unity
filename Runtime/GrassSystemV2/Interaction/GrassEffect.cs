namespace Snm.GrassSystemV2
{
    /// <summary>
    /// Area effects stampable onto grass via <see cref="GrassWorld.ApplyEffect"/>.
    /// Each maps to one channel of the effects canvas.
    /// </summary>
    public enum GrassEffect
    {
        /// <summary>Chars and shrivels blades. Permanent while inside the canvas range.</summary>
        Burn = 0,

        /// <summary>Frosty tint, stops swaying. Thaws over <see cref="GrassWorldConfig.freezeThawTime"/>.</summary>
        Freeze = 1,

        /// <summary>Dyes blades toward <see cref="GrassWorldConfig.tintColor"/>. Fades slowly.</summary>
        Tint = 2,
    }
}
