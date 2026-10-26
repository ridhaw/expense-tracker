using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Endpoints;

/// <summary>Valide les DataAnnotations des corps de requête et renvoie un 400 détaillé.</summary>
public static class ValidationFilter
{
    public static RouteHandlerBuilder WithValidation(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            foreach (var argument in context.Arguments)
            {
                if (argument is null || argument.GetType().Namespace != typeof(ValidationFilter).Namespace) continue;

                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(argument, new ValidationContext(argument), results, validateAllProperties: true))
                {
                    var errors = results
                        .SelectMany(r => r.MemberNames.DefaultIfEmpty("").Select(m => (Member: m, r.ErrorMessage)))
                        .GroupBy(e => e.Member)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Valeur invalide.").ToArray());
                    return Results.ValidationProblem(errors);
                }
            }
            return await next(context);
        }).ProducesValidationProblem();
}
