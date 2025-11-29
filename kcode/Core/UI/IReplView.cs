using Kcode.Core.Input;
using Spectre.Console.Rendering;

namespace Kcode.Core.UI;

public interface IReplView
{
    IReplViewSession BeginSession(ReplViewState initialState);
}
