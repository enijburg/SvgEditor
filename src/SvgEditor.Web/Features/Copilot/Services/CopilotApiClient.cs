using System.Net.Http.Json;
using SvgEditor.Web.Features.Copilot.Models;

namespace SvgEditor.Web.Features.Copilot.Services;

public sealed class CopilotApiClient(HttpClient httpClient)
{
    public async Task<CopilotPlanResponse?> PlanAsync(CopilotPlanRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/copilot/plan", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CopilotPlanResponse>(cancellationToken);
    }

    public async Task<CopilotApplyResponse?> ApplyAsync(CopilotApplyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/copilot/apply", request, cancellationToken);

        // Read the response even for non-success status codes (409, 400 contain ApplyResponse)
        return await response.Content.ReadFromJsonAsync<CopilotApplyResponse>(cancellationToken);
    }
}
