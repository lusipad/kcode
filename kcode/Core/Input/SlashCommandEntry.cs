using Kcode.Core.Commands;

namespace Kcode.Core.Input;

public record SlashCommandEntry(string Name, string Description, CommandType Type);
