using EduVerse.Core.Abstractions;
using EduVerse.Core.Institutions;

namespace EduVerse.Infrastructure.Institutions;

/// <summary>Message to a family written by the selected AI engine from facts computed in code; contact data is removed by the parser.</summary>
public sealed class AiFamilyWriter(IStructuredAi ai) : IFamilyWriter
{
    public async Task<FamilyMessage> WriteAsync(FamilyRequest request, CancellationToken cancellationToken = default) =>
        FamilyPrompt.Parse(await ai.GenerateAsync(FamilyPrompt.Build(request), cancellationToken).ConfigureAwait(false));
}
