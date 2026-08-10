namespace FairyMU.Api.Models;

public sealed record CharacterRecord(
    Guid AccountId,
    string Name,
    string Class,
    int Level,
    int Resets,
    int Pk,
    string Guild,
    bool Online
);
