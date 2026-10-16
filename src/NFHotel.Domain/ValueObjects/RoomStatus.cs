namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// Rummets bookbarhed — udelukkende det der forhindrer udlejning.
/// </summary>
/// <remarks>
/// Rengøringstilstand hører i <see cref="HousekeepingStatus"/> (B-04). Ordinalværdierne
/// 0-2 er bevaret fra det gamle system, så omlægningen koster nul datamigrering.
/// Den gamle XAML-dropdowns <c>Cleaning</c> er flyttet til <see cref="HousekeepingStatus"/>,
/// <c>Maintanance</c> var en stavefejl, og <c>Disabled</c> er semantisk identisk med
/// <see cref="OutOfService"/> (BR-126).
/// </remarks>
public enum RoomStatus
{
    /// <summary>Kan bookes. Siger intet om hvorvidt rummet er rent lige nu.</summary>
    Available = 0,

    /// <summary>Taget ud af drift på ubestemt tid (ombygning, omdannet til depot). Administrativ beslutning.</summary>
    OutOfService = 1,

    /// <summary>Midlertidigt spærret af en fejl der gør rummet ubeboeligt. Forventes at vende tilbage til <see cref="Available"/>.</summary>
    Maintenance = 2
}