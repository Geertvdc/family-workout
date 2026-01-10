using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace FamilyFitness.IntegrationTests;

public class FakePolicyEvaluator : IPolicyEvaluator
{
    public Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
    {
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(context.User, "Test")));
    }

    public Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, 
        AuthenticateResult authenticationResult, HttpContext context, object? resource)
    {
        return Task.FromResult(PolicyAuthorizationResult.Success());
    }
}