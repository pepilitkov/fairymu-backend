using FairyMU.Api.Models;

namespace FairyMU.Api.Services;

public sealed class DemoGameDataService
{
    private readonly List<CharacterRecord> _characters =
    [
        new(
            Guid.Empty,
            "DarkLord",
            "Blade Knight",
            400,
            85,
            34,
            "UNIVERSE",
            true
        ),

        new(
            Guid.Empty,
            "FairyQueen",
            "Muse Elf",
            400,
            82,
            12,
            "RESPECT",
            false
        ),

        new(
            Guid.Empty,
            "WizardBG",
            "Soul Master",
            400,
            79,
            48,
            "INSOMNIA",
            false
        ),

        new(
            Guid.Empty,
            "RageX",
            "Magic Gladiator",
            398,
            75,
            66,
            "DOMINION",
            true
        ),

        new(
            Guid.Empty,
            "BossHunter",
            "Dark Lord",
            395,
            72,
            22,
            "WARLORDS",
            false
        )
    ];

    public IReadOnlyList<CharacterRecord> Rankings()
    {
        return _characters
            .OrderByDescending(x => x.Resets)
            .ThenByDescending(x => x.Level)
            .ToList();
    }

    public IReadOnlyList<CharacterRecord> CharactersFor(
        Guid accountId)
    {
        var sample = _characters.Take(3);

        return sample
            .Select(x => x with
            {
                AccountId = accountId
            })
            .ToList();
    }

    public object Guilds()
    {
        return new[]
        {
            new
            {
                name = "UNIVERSE",
                master = "Ares",
                score = 12750,
                members = 42
            },

            new
            {
                name = "INSOMNIA",
                master = "Nyx",
                score = 9840,
                members = 36
            },

            new
            {
                name = "RESPECT",
                master = "FairyQueen",
                score = 8120,
                members = 31
            }
        };
    }

    public object Events()
    {
        return new[]
        {
            new
            {
                name = "Blood Castle",
                time = "12:30"
            },

            new
            {
                name = "Devil Square",
                time = "14:00"
            },

            new
            {
                name = "Chaos Castle",
                time = "17:00"
            },

            new
            {
                name = "Golden Invasion",
                time = "20:00"
            }
        };
    }
}
